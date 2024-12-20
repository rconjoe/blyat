﻿using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace Blyat
{
    public class SampleConfig : BasePluginConfig
    {
        [JsonPropertyName("IsPluginEnabled")]
        public bool IsPluginEnabled { get; set; } = true;

        [JsonPropertyName("LogPrefix")]
        public string LogPrefix { get; set; } = "BlyatPlugin";
    }

    [MinimumApiVersion(80)]
    public class BlyatPlugin : BasePlugin, IPluginConfig<SampleConfig>
    {
        public override string ModuleName => "Blyat";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "rconjoe";
        public override string ModuleDescription => "A playground where I break things";

        public SampleConfig Config { get; set; } = null!;

        // As far as I can tell, these hook functions are all arranged in a sort of sequential order:
        public void OnConfigParsed(SampleConfig config)
        {
            Config = config;
        }

        // private TestInjectedClass _testInjectedClass;
        //
        // public BlyatPlugin(TestInjectedClass testInjectedClass)
        // {
        //     _testInjectedClass = testInjectedClass;
        // }


        public override void Load(bool hotReload)
        {
            if (!Config.IsPluginEnabled)
            {
                Logger.LogWarning($"{Config.LogPrefix} {ModuleName} is disabled!!");
                return;
            }

            Logger.LogInformation(
                $"{ModuleName} loaded, hot reload flag is {hotReload}, path is {ModulePath}"
            );

            VirtualFunctions.SwitchTeamFunc.Hook(
                hook =>
                {
                    Logger.LogInformation("Switch team func called");
                    return HookResult.Continue;
                },
                HookMode.Pre
            );

            SetupConvars();

            // ValveInterface provides pointers to loaded modules via Interface Name exposed from the engine.
            var server = ValveInterface.Server;
            Logger.LogInformation("Server pointer found @ {Pointer:X}", server.Pointer);

            // Use `ModuleDirectory` to get the directory of the plugin for things like config files
            File.WriteAllText(
                Path.Join(ModuleDirectory, "example.txt"),
                $"Test file created by BlyatPlugin at ${DateTime.Now}"
            );

            // Execute a server command as if typed into the server console.
            Server.ExecuteCommand("meta list");
            Server.ExecuteCommand("css_plugins list");

            // Example vfunc call that usually gets the game event manager pointer
            // by calling the func at offset 91 then subtracting 8 from the result pointer.
            // This value is asserted against the native code that points to the same function.
            var virtualFunc = VirtualFunction.Create<IntPtr>(server.Pointer, 91);
            var result = virtualFunc() - 8;
            Logger.LogInformation("Result of virtual func call is {Pointer:X}", result);

            // _testInjectedClass.Hello();
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            Logger.LogInformation("All plugins loaded!!");
        }

        private void SetupConvars()
        {
            RegisterListener<Listeners.OnMapStart>(name =>
            {
                ConVar.Find("sv_cheats")?.SetValue(true);

                var numericCvar = ConVar.Find("mp_warmuptime");
                Logger.LogInformation(
                    "mp_warmuptime = {Value}",
                    numericCvar?.GetPrimitiveValue<float>()
                );

                var stringCvar = ConVar.Find("sv_skyname");
                Logger.LogInformation("sv_skyname = {Value}", stringCvar?.StringValue);

                var fogCvar = ConVar.Find("fog_color");
                Logger.LogInformation("fog_color = {Value}", fogCvar?.GetNativeValue<Vector>());
            });
        }

        private void SetupGameEvents()
        {
            // Register Game Event Handlers
            RegisterEventHandler<EventPlayerConnect>(GenericEventHandler, HookMode.Pre);
            RegisterEventHandler<EventPlayerBlind>(GenericEventHandler);

            // Mirrors a chat message back to the player
            RegisterEventHandler<EventPlayerChat>(
                (
                    (@event, _) =>
                    {
                        var player = Utilities.GetPlayerFromIndex(@event.Userid);
                        if (player == null)
                            return HookResult.Continue;

                        player.PrintToChat($"You said {@event.Text}");
                        return HookResult.Continue;
                    }
                )
            );

            RegisterEventHandler<EventPlayerDeath>(
                (@event, info) =>
                {
                    // you can use info.DontBroadcast to set the don't broadcast flag on the event.
                    if (new Random().NextSingle() > 0.5f)
                    {
                        @event.Attacker?.PrintToChat(
                            $"Skipping player_death broadcast at {Server.CurrentTime}"
                        );
                        info.DontBroadcast = true;
                    }

                    if (@event.Attacker != null)
                    {
                        var message = UserMessage.FromPartialName("Shake");
                        Logger.LogInformation(
                            "Created user message CCSUsrMsg_Shake {Message:x}",
                            message.Handle
                        );

                        message.SetFloat("duration", 2);
                        message.SetFloat("amplitude", 5);
                        message.SetFloat("frequency", 10f);
                        message.SetInt("command", 0);

                        message.Send(@event.Attacker);
                    }

                    return HookResult.Continue;
                },
                HookMode.Pre
            );

            RegisterEventHandler<EventGrenadeBounce>(
                (@event, info) =>
                {
                    Logger.LogInformation(
                        "Player {Player} grenade bounce",
                        @event.Userid!.PlayerName
                    );

                    return HookResult.Continue;
                },
                HookMode.Pre
            );

            RegisterEventHandler<EventPlayerSpawn>(
                (@event, info) =>
                {
                    var player = @event.Userid;
                    var playerPawn = player?.PlayerPawn.Get();
                    if (player == null || playerPawn == null)
                        return HookResult.Continue;

                    Logger.LogInformation(
                        "Player spawned with entity index: {EntityIndex} & User ID: {UserId}",
                        playerPawn.Index,
                        player.UserId
                    );

                    return HookResult.Continue;
                }
            );

            RegisterEventHandler<EventBulletImpact>(
                (@event, info) =>
                {
                    var player = @event.Userid;
                    var pawn = player?.PlayerPawn.Get();
                    var activeWeapon = pawn?.WeaponServices?.ActiveWeapon.Get();

                    if (pawn == null)
                        return HookResult.Continue;

                    Server.NextFrame(() =>
                    {
                        player?.PrintToChat(activeWeapon?.DesignerName ?? "no active weapon");
                    });

                    // set player to random color
                    pawn.Render = Color.FromArgb(
                        Random.Shared.Next(0, 255),
                        Random.Shared.Next(0, 255),
                        Random.Shared.Next(0, 255)
                    );
                    Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

                    // give player 5 health and set their reserve ammo to 250
                    if (activeWeapon != null)
                    {
                        activeWeapon.ReserveAmmo[0] = 250;
                        activeWeapon.Clip1 = 250;
                    }

                    pawn.Health += 5;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                    return HookResult.Continue;
                }
            );
        }

        private void SetupCommands()
        {
            // adds new server console command
            AddCommand(
                "blyat_info",
                "a test command",
                (player, info) =>
                {
                    if (player == null)
                        return;
                    Logger.LogInformation(
                        "Blyat - a test command was called by {SteamID2} with {Arguments}",
                        ((SteamID)player.SteamID).SteamId2,
                        info.ArgString
                    );
                }
            );

            AddCommand(
                "css_changeteam",
                "change team",
                (player, _) =>
                {
                    if (player == null)
                        return;

                    player.ChangeTeam(
                        (CsTeam)player.TeamNum == CsTeam.Terrorist
                            ? CsTeam.CounterTerrorist
                            : CsTeam.Terrorist
                    );
                }
            );

            // listens for any client use of the command "jointeam"
            AddCommandListener(
                "jointeam",
                (player, info) =>
                {
                    Logger.LogInformation(
                        "{PlayerName} just did a jointeam (pre) [{ArgString}]",
                        player?.PlayerName,
                        info.ArgString
                    );

                    return HookResult.Continue;
                }
            );
        }

        private void SetupEntityOutputHooks()
        {
            HookEntityOutput(
                "weapon_knife",
                "OnPlayerPickup",
                (output, _, activator, caller, _, delay) =>
                {
                    Logger.LogInformation(
                        "weapon_knife called OnPlayerPickup ({name}, {activator}, {caller}, {delay})",
                        output.Description.Name,
                        activator.DesignerName,
                        caller.DesignerName,
                        delay
                    );

                    return HookResult.Continue;
                }
            );

            HookEntityOutput(
                "*",
                "*",
                (output, _, activator, caller, _, delay) =>
                {
                    Logger.LogInformation(
                        "All EntityOutput ({name}, {activator}, {caller}, {delay})",
                        output.Description.Name,
                        activator.DesignerName,
                        caller.DesignerName,
                        delay
                    );

                    return HookResult.Continue;
                }
            );

            HookEntityOutput(
                "*",
                "OnStartTouch",
                (_, name, activator, caller, _, delay) =>
                {
                    Logger.LogInformation(
                        "OnStartTouch: ({name}, {activator}, {caller}, {delay})",
                        name,
                        activator.DesignerName,
                        caller.DesignerName,
                        delay
                    );

                    return HookResult.Continue;
                }
            );
        }

        private HookResult GenericEventHandler<T>(T @event, GameEventInfo info)
            where T : GameEvent
        {
            Logger.LogInformation(
                "Event found {Pointer:X}, event name: {EventName}, dont broadcast: {DontBroadcast}",
                @event.Handle,
                @event.EventName,
                info.DontBroadcast
            );

            return HookResult.Continue;
        }

        public override void Unload(bool hotReload)
        {
            Logger.LogInformation("Unloading blyat...");
            Server.PrintToChatAll("Unloading blyat...");
        }
    }
}
