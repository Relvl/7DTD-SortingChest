using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using UniLinq;

namespace SortingChest;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class SortingChestMod : IModApi
{
    public void InitMod(Mod modInstance)
    {
        Config.Load();

        var harmony = new Harmony(Config.HarmonyId);
        harmony.PatchAll();
        Log.Out($"[{Config.AssemblyName}] initialized");
    }

    public static void DoSortingOut(TileEntity chest, Vector3i blockPos, EntityPlayer player)
    {
        if (chest == null || player == null) return;
        if (chest is not TileEntityComposite composite) return;

        var signable = composite.GetFeature<TEFeatureSignable>();
        var storage = composite.GetFeature<TEFeatureStorage>();
        if (storage == null) return;

        // check for correct named storage
        if (signable?.signText?.Text?.EqualsCaseInsensitive(Config.SortingChestTag) != true) return;

        // check there is any items
        if (!storage.items.Any(i => !i.IsEmpty())) return;
        var stacksBeforeSorting = storage.items.Count(i => !i.IsEmpty());

        var sortedTargets = GetPossibleTargets(blockPos);

        var someMoved = false;
        foreach (var target in sortedTargets)
        {
            if (!target.TryGetSelfOrFeature<ITileEntityLootable>(out var lootable)) continue;
            if (lootable.IsUserAccessing()) continue;

            var modified = false;
            try
            {
                lootable.SetUserAccessing(true);

                for (var i = 0; i < storage.items.Length; i++)
                {
                    var stack = storage.items[i];

                    if (stack.IsEmpty()) continue;
                    if (!lootable.HasItem(stack.itemValue)) continue;

                    if (lootable.TryStackItem(0, stack).allMoved)
                    {
                        modified = true;
                        storage.UpdateSlot(i, ItemStack.Empty);
                    }
                    else if (lootable.AddItem(stack))
                    {
                        modified = true;
                        storage.UpdateSlot(i, ItemStack.Empty);
                    }
                }
            }
            finally
            {
                if (modified)
                {
                    lootable.SetModified();
                    someMoved = true;
                }

                lootable.SetUserAccessing(false);
            }
        }

        if (someMoved)
            storage.SetModified();

        var restStacks = storage.items.Count(i => !i.IsEmpty());
        if (restStacks == stacksBeforeSorting)
        {
            var message = Localization.Get("sortingChestNothingSorted");
            GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, message, [player.entityId], EMessageSender.Server);
        }
        else if (restStacks > 0)
        {
            var sortedOut = stacksBeforeSorting - restStacks;
            var message = Localization.Get("sortingChestPartialSorting");
            message = string.Format(message, sortedOut, restStacks);
            GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, message, [player.entityId], EMessageSender.Server);
        }
        else
        {
            var message = Localization.Get("sortingChestCompleteSorting");
            message = string.Format(message, stacksBeforeSorting);
            GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, message, [player.entityId], EMessageSender.Server);
        }
    }

    public static List<TileEntity> GetPossibleTargets(Vector3i blockPos)
    {
        var possibleTargets = new Dictionary<Vector3i, TileEntity>();
        var chunkX = World.toChunkXZ(blockPos.x);
        var chunkZ = World.toChunkXZ(blockPos.z);

        var distance = Config.SortingDistance < 0 ? 0 : Math.Pow(Config.SortingDistance, 2);

        for (var offX = -1; offX < 2; offX++)
        for (var offZ = -1; offZ < 2; offZ++)
        {
            if (GameManager.Instance.World.GetChunkSync(chunkX + offX, chunkZ + offZ) is not Chunk chunk) continue;
            if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] process chunk: {chunk}");
            foreach (var entry in chunk.GetTileEntities().dict)
            {
                var targetPos = chunk.ToWorldPos(entry.Key);
                if (CheckTileEntityTarget(entry, targetPos, blockPos, distance))
                    possibleTargets[targetPos] = entry.Value;
            }
        }

        var sortedTargets = possibleTargets
            .OrderBy(kv => (blockPos.ToVector3() - kv.Key).sqrMagnitude)
            .Select(kv => kv.Value)
            .ToList();
        if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] #sortedTargets: {sortedTargets.Count}");

        return sortedTargets;
    }

    public static bool CheckTileEntityTarget(KeyValuePair<Vector3i, TileEntity> entry, Vector3i targetPos, Vector3i blockPos, double distance)
    {
        // skip self
        if (targetPos.Equals(blockPos))
        {
            if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] skip target at [{targetPos.x}, {targetPos.x}] - same as starter");
            return false;
        }

        // skip non-chest-like
        if (!Config.AvailableTargetTypes.Contains(entry.Value.GetTileEntityType()))
        {
            if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] skip target at [{targetPos.x}, {targetPos.x}] - non available type ({entry.Value.GetTileEntityType()})");
            return false;
        }

        // skip another sorting chests
        if (entry.Value is TileEntityComposite targetComposite)
        {
            var targetSignable = targetComposite.GetFeature<TEFeatureSignable>();
            if (targetSignable?.signText?.Text?.EqualsCaseInsensitive(Config.SortingChestTag) == true)
            {
                if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] skip target at [{targetPos.x}, {targetPos.x}] - another sorting chest");
                return false;
            }
        }

        // don't push to untouched loot containers (in case of another mods that allows to claim loot containers)
        if (entry.Value is TileEntityLootContainer { bTouched: false })
        {
            if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] skip target at [{targetPos.x}, {targetPos.x}] - another loot container");
            return false;
        }

        if (entry.Value is TileEntitySecureLootContainer { bTouched: false })
        {
            if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] skip target at [{targetPos.x}, {targetPos.x}] - another secu loot container");
            return false;
        }

        // skip too far away
        if (distance > 0)
        {
            var distanceSq = (blockPos.ToVector3() - targetPos).sqrMagnitude;
            if (distanceSq > distance)
            {
                if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] skip target at [{targetPos.x}, {targetPos.x}] - too far away ({distanceSq})");
                return false;
            }
        }

        // skip opened by players
        var anotherLockerId = GameManager.Instance.GetEntityIDForLockedTileEntity(entry.Value);
        if (anotherLockerId != -1)
        {
            if (Config.VerboseLogging) Log.Out($"[{Config.AssemblyName}] skip target at [{targetPos.x}, {targetPos.x}] - locked by {anotherLockerId}");
            return false;
        }

        return true;
    }
}