using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using GTFOPause.Net;
using HarmonyLib;

namespace GTFOPause;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency("dev.gtfomodding.gtfo-api")]
public class Plugin : BasePlugin
{
    public const string Guid = "mrwave.GTFOPause";
    public const string Name = "GTFOPause";
    public const string Version = "0.1.1";

    public override void Load()
    {
        Logger.Setup(Log);
        PauseConfig.Bind(Config);

        new Harmony(Guid).PatchAll();
        AddComponent<PauseController>();
        PauseNet.Register();

        EventAPI.OnFocusStateChanged += PauseLogic.OnFocusStateChanged;
        EventAPI.OnGameStateChanged += PauseLogic.OnGameStateChanged;
        EventAPI.OnCheckpointReloaded += PauseLogic.OnCheckpointReloaded;

        Logger.Info($"{Name} {Version} loaded");
    }
}
