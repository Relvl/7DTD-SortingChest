using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace SortingChest;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ModApi : IModApi
{
    public void InitMod(Mod modInstance)
    {
        var harmony = new Harmony("sorting.chest.harmony");
        harmony.PatchAll();
        Log.Out($"[SortingChest] ModAPI initialized");
    }
}