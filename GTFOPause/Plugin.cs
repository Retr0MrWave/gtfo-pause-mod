using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace GTFOPause;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency("dev.gtfomodding.gtfo-api")]
public class Plugin : BasePlugin
{
    public const string Guid = "retr0mrwave.GTFOPause";
    public const string Name = "GTFOPause";
    public const string Version = "0.1.0";

    public override void Load()
    {
        Logger.Setup(Log);
        new Harmony(Guid).PatchAll();
        Logger.Info($"{Name} {Version} loaded");
    }
}
