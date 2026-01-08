using HarmonyLib;
using System;
using System.Collections.Generic;
using ProcGen;
using UnityEngine;

namespace DarknessNotIncluded.Exploration
{
  public static class RevealAllOfSpace
  {
    // Cells we've already processed (prevents loops and duplicate work)
    private static readonly HashSet<int> SEEN = new HashSet<int>();

    // Frontier to BFS through sunlit space
    private static readonly Queue<int> frontier = new Queue<int>();

    private static bool isHooked;
    private static bool isProcessing;

    // Tune: work-budget per scheduler tick
    private const int REVEAL_BATCH_SIZE = 250;

    [HarmonyPatch(typeof(World)), HarmonyPatch("OnSpawn")]
    static class Patched_World_OnSpawn
    {
      static void Postfix()
      {
        if (isHooked) return;
        isHooked = true;

        Grid.OnReveal += OnReveal;
      }

      private static void OnReveal(int targetCell)
      {
        try
        {
          if (!Grid.IsValidCell(targetCell)) return;
          if (Game.Instance == null || Game.Instance.world == null) return;
          if (ClusterManager.Instance == null) return;

          // Only react if this is a valid "sunlit space" frontier cell
          if (!IsSpaceBiomeAndLitBySunlight(targetCell)) return;

          // Prime BFS with the trigger cell (skip if already processed)
          if (SEEN.Contains(targetCell)) return;
          SEEN.Add(targetCell);
          frontier.Enqueue(targetCell);

          EnsureProcessing();
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[DarknessNotIncluded] RevealAllOfSpace OnReveal handler threw: {ex}");
        }
      }

      private static void EnsureProcessing()
      {
        if (isProcessing) return;
        isProcessing = true;

        try
        {
          // Schedule on the main thread; process a bounded batch each frame
          GameScheduler.Instance.Schedule("DNI_RevealAllOfSpace", 0f, ProcessBatch);
        }
        catch (Exception ex)
        {
          isProcessing = false;
          Debug.LogWarning($"[DarknessNotIncluded] Failed to schedule RevealAllOfSpace batch processor: {ex}");
        }
      }

      private static void ProcessBatch(object _)
      {
        try
        {
          // Hard guards: if game state is gone, stop and clear work
          if (Game.Instance == null || Game.Instance.world == null || ClusterManager.Instance == null)
          {
            frontier.Clear();
            isProcessing = false;
            return;
          }

          int processed = 0;
          while (processed < REVEAL_BATCH_SIZE && frontier.Count > 0)
          {
            var cell = frontier.Dequeue();
            if (!Grid.IsValidCell(cell)) continue;

            // Validate again at processing time (state may have changed)
            if (!IsSpaceBiomeAndLitBySunlight(cell)) continue;

            // Reveal the current cell
            try
            {
              Grid.Reveal(cell);
            }
            catch (Exception ex)
            {
              Debug.LogWarning($"[DarknessNotIncluded] Grid.Reveal failed for cell {cell}: {ex}");
            }

            // Expand to neighbours within the same world
            int worldIdx = -1;
            try
            {
              worldIdx = Grid.WorldIdx[cell];
            }
            catch
            {
              worldIdx = -1;
            }

            if (worldIdx >= 0)
            {
              EnqueueIfEligible(worldIdx, Grid.CellAbove(cell));
              EnqueueIfEligible(worldIdx, Grid.CellRight(cell));
              EnqueueIfEligible(worldIdx, Grid.CellBelow(cell));
              EnqueueIfEligible(worldIdx, Grid.CellLeft(cell));

              // Diagonals too: space connections are commonly open
              EnqueueIfEligible(worldIdx, Grid.CellUpRight(cell));
              EnqueueIfEligible(worldIdx, Grid.CellDownRight(cell));
              EnqueueIfEligible(worldIdx, Grid.CellDownLeft(cell));
              EnqueueIfEligible(worldIdx, Grid.CellUpLeft(cell));
            }

            processed++;
          }

          if (frontier.Count > 0)
          {
            // More to do: schedule next frame
            GameScheduler.Instance.Schedule("DNI_RevealAllOfSpace", 0f, ProcessBatch);
          }
          else
          {
            isProcessing = false;
          }
        }
        catch (Exception ex)
        {
          isProcessing = false;
          Debug.LogWarning($"[DarknessNotIncluded] RevealAllOfSpace batch processor threw: {ex}");
        }
      }

      private static void EnqueueIfEligible(int worldIdx, int neighbour)
      {
        if (!Grid.IsValidCell(neighbour)) return;

        // Keep BFS inside the same world
        try
        {
          if (Grid.WorldIdx[neighbour] != worldIdx) return;
        }
        catch
        {
          return;
        }

        if (!SEEN.Add(neighbour)) return;

        // Only expand BFS if neighbour is still "sunlit space"
        if (IsSpaceBiomeAndLitBySunlight(neighbour))
        {
          frontier.Enqueue(neighbour);
        }
      }

      static bool IsSpaceBiomeAndLitBySunlight(int cell)
      {
        if (!Grid.IsValidCell(cell)) return false;
        if (Game.Instance == null || Game.Instance.world == null) return false;

        var zoneRender = Game.Instance.world.zoneRenderData;
        if (zoneRender == null) return false;

        SubWorld.ZoneType zoneType;
        try
        {
          zoneType = zoneRender.GetSubWorldZoneType(cell);
        }
        catch
        {
          return false;
        }

        var isSpaceBiome = zoneType == SubWorld.ZoneType.Space;

        bool isSpaceCell;
        try
        {
          // treat "no object in foundation layer" as open space
          isSpaceCell = Grid.Objects[cell, 2] == null;
        }
        catch
        {
          return false;
        }

        bool isLitBySunlight;
        try
        {
          isLitBySunlight = Grid.ExposedToSunlight[cell] > 0;
        }
        catch
        {
          return false;
        }

        return isSpaceBiome && isSpaceCell && isLitBySunlight;
      }
    }
  }
}
