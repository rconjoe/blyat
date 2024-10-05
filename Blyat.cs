using System;
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
        [JsonPropertyName("IsPluginEnabled")] public bool IsPluginEnabled { get; set; } = true;

        [JsonPropertyName("LogPrefix")] public string LogPrefix { get; set; } = "BlyatPlugin";
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

            Logger.LogInformation($"{ModuleName} loaded, hot reload flag is {hotReload}, path is {ModulePath}");

            VirtualFunctions.SwitchTeamFunc.Hook(hook =>
            {
                Logger.LogInformation("Switch team func called");
                return HookResult.Continue;
            }, HookMode.Pre);

            SetupConvars();

            // ValveInterface provides pointers to loaded modules via Interface Name exposed from the engine.
            var server = ValveInterface.Server;
            Logger.LogInformation("Server pointer found @ {Pointer:X}", server.Pointer);

            // Use `ModuleDirectory` to get the directory of the plugin for things like config files
            File.WriteAllText(Path.Join(ModuleDirectory, "example.txt"),
                    $"Test file created by BlyatPlugin at ${DateTime.Now}");

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
                Logger.LogInformation("mp_warmuptime = {Value}", numericCvar?.GetPrimitiveValue<float>());

                var stringCvar = ConVar.Find("sv_skyname");
                Logger.LogInformation("sv_skyname = {Value}", stringCvar?.StringValue);

                var fogCvar = ConVar.Find("fog_color");
                Logger.LogInformation("fog_color = {Value}", fogCvar?.GetNativeValue<Vector>());
            });
        }


        private void SetupGameEvents()
        {
            // Register Game Event Handlers
        }


        public override void Unload(bool hotReload)
        {
            Logger.LogInformation("Unloading blyat...");
            Server.PrintToChatAll("Unloading blyat...");
        }
    }
}
