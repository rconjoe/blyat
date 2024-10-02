using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
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

    public override void Unload(bool hotReload)
    {
        Logger.LogInformation("Unloading blyat...");
    }
}
