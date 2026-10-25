using System;
using UnityEngine;

namespace GTFOPause;

/// <summary>Per-frame driver. Update still runs at timeScale 0, so the hotkey and watchers keep working while paused.</summary>
internal class PauseController : MonoBehaviour
{
    public PauseController(IntPtr ptr) : base(ptr) { }

    private void Update()
    {
        try
        {
            PauseLogic.Tick();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }

    private void LateUpdate()
    {
        try
        {
            // After the game's own Update, so the banner wins over interaction prompts.
            Notify.UpdateBanner();
        }
        catch (Exception e)
        {
            Logger.Error(e);
        }
    }
}
