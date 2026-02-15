using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace SortingChest;

[HarmonyPatch(typeof(GameManager))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class GameManager_Patch
{
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

        try
        {
            TileEntity chest = null;
            var lockerId = -1;

            if (_lootEntityId == -1)
            {
                // just... why!?
                chest = GameManager.Instance.World.GetTileEntity(_blockPos);
                foreach (var entry in GameManager.Instance.lockedTileEntities)
                    if (entry.Key is TileEntity te && entry.Key.ToWorldPos().Equals(_blockPos))
                    {
                        chest = te;
                        lockerId = entry.Value;
                    }
            }
            else
            {
                foreach (var entry in GameManager.Instance.lockedTileEntities)
                    if (entry.Key.EntityId == _lootEntityId && entry.Key is TileEntity te)
                    {
                        chest = te;
                        lockerId = entry.Value;
                    }
            }

            if (lockerId == -1) return;

            if (!GameManager.Instance.World.Players.dict.TryGetValue(lockerId, out var player)) return;

            SortingChestMod.DoSortingOut(chest, _blockPos, player);
        }
        catch (Exception e)
        {
            Log.Error($"[{Config.AssemblyName}] {e}");
            Console.WriteLine(e);
        }
    }
}