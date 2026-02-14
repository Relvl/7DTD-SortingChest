using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using UniLinq;

namespace SortingChest;

[HarmonyPatch(typeof(GameManager))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class GameManager_Patch
{
    public static readonly List<TileEntityType> AvailableTargetTypes = [TileEntityType.Loot, TileEntityType.SecureLoot, TileEntityType.SecureLootSigned, TileEntityType.Composite];
    public static readonly int MaxDistanceSq = 20 * 20;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameManager.TEUnlockServer))]
    public static void TEUnlockServer_Prefix(
        int _clrIdx,
        Vector3i _blockPos,
        int _lootEntityId,
        bool _allowContainerDestroy
    )
    {
        // the code of 7D is... ugh... I guess is better to dupe some heavy logic instead of using 2 IL injections via transpiler... sorry.
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) return;

        TileEntity chest = null;
        var lockerId = -1;

        if (_lootEntityId == -1)
        {
            // just... why!?
            chest = GameManager.Instance.World.GetTileEntity(_blockPos);
            foreach (var entry in GameManager.Instance.lockedTileEntities)
            {
                if (entry.Key is TileEntity te && entry.Key.ToWorldPos().Equals(_blockPos))
                {
                    chest = te;
                    lockerId = entry.Value;
                }
            }
        }
        else
        {
            foreach (var entry in GameManager.Instance.lockedTileEntities)
            {
                if (entry.Key.EntityId == _lootEntityId && entry.Key is TileEntity te)
                {
                    chest = te;
                    lockerId = entry.Value;
                }
            }
        }

        if (lockerId == -1) return;
        if (chest is not TileEntityComposite composite) return;

        if (!GameManager.Instance.World.Players.dict.TryGetValue(lockerId, out var player)) return;
        var persPlayer = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(lockerId);
        if (persPlayer == null) return;

        var signable = composite.GetFeature<TEFeatureSignable>();
        var storage = composite.GetFeature<TEFeatureStorage>();
        if (storage == null) return;

        // check for correct named storage
        if (signable?.signText?.Text?.EqualsCaseInsensitive("[sort]") != true) return;

        // check there is any items
        if (!storage.items.Any(i => !i.IsEmpty())) return;
        var stacksBeforeSorting = storage.items.Count(i => !i.IsEmpty());

        var possibleTargets = new Dictionary<Vector3i, TileEntity>();
        var chunkX = World.toChunkXZ(_blockPos.x);
        var chunkZ = World.toChunkXZ(_blockPos.z);

        for (var offX = -1; offX < 2; offX++)
        for (var offZ = -1; offZ < 2; offZ++)
        {
            if (GameManager.Instance.World.GetChunkSync(chunkX + offX, chunkZ + offZ) is not Chunk chunk) continue;
            foreach (var entry in chunk.GetTileEntities().dict)
            {
                // skip self
                if (entry.Key.Equals(_blockPos)) continue;

                // skip non-chest-like
                if (!AvailableTargetTypes.Contains(entry.Value.GetTileEntityType())) continue;

                // skip another sorting chests
                if (entry.Value is TileEntityComposite targetComposite)
                {
                    var targetSignable = targetComposite.GetFeature<TEFeatureSignable>();
                    if (targetSignable?.signText?.Text?.EqualsCaseInsensitive("[sort]") == true) continue;
                }

                // don't push to untouched loot containers (in case of another mods that allows to claim loot containers)
                if (entry.Value is TileEntityLootContainer { bTouched: false }) continue;

                if (entry.Value is TileEntitySecureLootContainer { bTouched: false }) continue;

                // skip too far away
                if (player.GetDistanceSq(entry.Key) > MaxDistanceSq) continue;

                // skip opened by players
                var anotherLockerId = GameManager.Instance.GetEntityIDForLockedTileEntity(entry.Value);
                if (anotherLockerId != -1) continue;

                possibleTargets[entry.Key] = entry.Value;
            }
        }

        var sortedTargets = possibleTargets
            .OrderBy(kv => (_blockPos.ToVector3() - kv.Key).sqrMagnitude)
            .Select(kv => kv.Value)
            .ToList();

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
            GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, "[00ff00]Nothing sorted out", [lockerId], EMessageSender.Server);
        }
        else if (restStacks > 0)
        {
            var sortedOut = stacksBeforeSorting - restStacks;
            GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, $"[00ff00]Sorted out {sortedOut} stack(s) ({restStacks} left)", [lockerId], EMessageSender.Server);
        }
        else
        {
            GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, $"[00ff00]All {stacksBeforeSorting} stack(s) is sorted out", [lockerId], EMessageSender.Server);
        }
    }
}