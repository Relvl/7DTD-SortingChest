using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace SortingChest;

public static class Config
{
    public static string AssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
    public static string HarmonyId { get; private set; } = "sorting.chest.harmony";
    public static float SortingDistance { get; private set; } = 20f;
    public static string SortingChestTag { get; private set; } = "[sort]";
    public static bool VerboseLogging { get; private set; } = true;

    public static readonly List<TileEntityType> AvailableTargetTypes = [TileEntityType.Loot, TileEntityType.SecureLoot, TileEntityType.SecureLootSigned, TileEntityType.Composite];

    public static void Load()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Mods/{AssemblyName}/Config/{AssemblyName}.xml");
            var doc = new XmlDocument();
            doc.Load(path);
            var root = doc.DocumentElement;
            if (root == null || root.Name != $"{AssemblyName}.Config") throw new Exception($"Invalid config root (expected <{AssemblyName}.Config />)");

            var loadedHarmonyId = root["HarmonyId"]?.InnerText;
            if (!string.IsNullOrWhiteSpace(loadedHarmonyId)) HarmonyId = loadedHarmonyId;

            var loadedVerboseLogging = root["VerboseLogging"]?.InnerText;
            if (!string.IsNullOrWhiteSpace(loadedVerboseLogging)) VerboseLogging = bool.Parse(loadedVerboseLogging);

            var loadedMaxDistance = root["MaxDistance"]?.InnerText;
            if (!string.IsNullOrEmpty(loadedMaxDistance)) SortingDistance = float.Parse(loadedMaxDistance, CultureInfo.InvariantCulture);

            var loadedSortingChestTag = root["ContainerName"]?.InnerText;
            if (!string.IsNullOrEmpty(loadedSortingChestTag)) SortingChestTag = loadedSortingChestTag;

            if (VerboseLogging)
            {
                Log.Out($"[{AssemblyName}] Loaded {path}:");
                Log.Out($"[{AssemblyName}]\t\tSortingDistance: {SortingDistance}");
                Log.Out($"[{AssemblyName}]\t\tSortingChestTag: {SortingChestTag}");
                Log.Out($"[{AssemblyName}]\t\tVerboseLogging: {VerboseLogging}");
            }
        }
        catch (Exception e)
        {
            Log.Error($"[{AssemblyName}] {e.Message}");
        }
    }
}