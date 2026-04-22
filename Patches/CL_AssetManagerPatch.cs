using HarmonyLib;
using HisTools.Prefabs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HisTools.Patches;

public static class CL_AssetManagerPatch
{
    [HarmonyPatch(typeof(CL_AssetManager), "InitializeAssetManager")]
    public static class CL_AssetManager_InitializeAssetManager_Patch
    {
        private static void Postfix()
        {
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "Feature_DebugInfo");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "SphereMarker");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "InfoLabel");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "UI_Speedrun");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "UI_BuffsDisplay");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "SettingIcon");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "UI_RouteRecorder");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "UI_Popup_Label");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "UI_Popup_Input");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "UI_Popup_SaveRecord");
            PrefabDatabase.Instance.LoadAsset<GameObject>("histools", "UI_RouteRecorderHistory");
        }
    }
}