using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using SlayTheSpire2.LAN.Multiplayer.Services;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace SlayTheSpire2.LAN.Multiplayer.Patchs
{
    /// <summary>
    /// LocManager.SetLanguageInternal replaces the entire table set on every language
    /// (re)load, including game startup and locale switches in settings. Re-merge our
    /// loose JSON localization into the freshly loaded tables every time this happens.
    /// Note: this runs from inside the LocManager constructor on first call, so the
    /// static LocManager.Instance is not assigned yet - use __instance instead.
    /// See LocalizationFixService for why this is needed instead of a .pck.
    /// </summary>
    [HarmonyPatch(typeof(LocManager), "SetLanguageInternal")]
    internal class LocManagerSetLanguageInternalPatch
    {
        private static void Postfix(LocManager __instance, string language)
        {
            LocalizationFixService.MergeAll(__instance, language);
        }
    }
}
