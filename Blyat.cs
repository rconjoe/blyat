using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

using Microsoft.Extensions.Logging;

namespace Blyat;

[MinimumApiVersion(80)]
public class BlyatPlugin : BasePlugin
{
    public override string ModuleName => "Blyat";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "rconjoe";
    public override string ModuleDescription => "A playground where I break things";

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("Loading blyat...");
    }

    [ConsoleCommand("css_echo", "Echo the message sent by the caller")] // register with "css_" prefix makes it a chat command
    [CommandHelper(minArgs: 1, usage: "[message]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void EchoCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        commandInfo.GetArg(0); // command name, "css_echo"

        var message = commandInfo.GetArg(1);

        commandInfo.ReplyToCommand($"{message}, what about it?");
    }

    public override void Unload(bool hotReload)
    {
        Logger.LogInformation("Unloading blyat...");
    }
}
