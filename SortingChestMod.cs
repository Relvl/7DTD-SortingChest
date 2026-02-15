using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace SortingChest;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class SortingChestMod : IModApi
{
    public void InitMod(Mod modInstance)
    {
        Config.Load();

        var harmony = new Harmony(Config.HarmonyId);
        harmony.PatchAll();
        Log.Out($"[{Config.AssemblyName}] initialized");
    }
}