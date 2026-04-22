using System;
using HarmonyLib;
using UnityEngine;

namespace HisTools.Patches;

public static class Projectile_ReturnRebarPatch
{
    public static event Action<Projectile_ReturnRebar> OnSpawned;
    public static event Action<Projectile_ReturnRebar> OnDespawned;

    [HarmonyPatch(typeof(Projectile_ReturnRebar), "Awake")]
    public static class Projectile_ReturnRebar_Awake_Patch
    {
        public static void Postfix(Projectile_ReturnRebar __instance)
        {
            try
            {
                OnSpawned?.Invoke(__instance);
            }
            catch (Exception e)
            {
                Utils.Logger.Error($"Error in Projectile_ReturnRebar.Awake Postfix: {e}");
            }
        }
    }

    // Note: OnDestroy patch removed — game updated and Projectile_ReturnRebar no longer has OnDestroy.
    // OnDespawned event is kept for API compatibility but will not fire.
    // Cleanup of rebar displays relies on Unity's null-to-destroyed-object detection in Update polling.
}
