using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessNotIncluded.Exploration
{
  static class LightsRevealFogOfWar
  {
    [HarmonyPatch(typeof(LightGridManager.LightGridEmitter)), HarmonyPatch("AddToGrid")]
    static class Patched_LightGridManager_LightGridEmitter_AddToGrid
    {
      static void Postfix(List<int> ___litCells)
      {
        try
        {
          // When LOS occlusion is enabled, do not permanently reveal via lights.
          if (Config.instance != null && Config.instance.occludeVisibilityByWalls)
            return;

          if (___litCells == null || ___litCells.Count == 0) return;

          var shouldExpandFogOfWar = false;
          foreach (var cell in ___litCells)
          {
            if (!Grid.IsValidCell(cell)) continue;
            if (Grid.Visible[cell] > 0)
            {
              shouldExpandFogOfWar = true;
              break;
            }
          }

          if (!shouldExpandFogOfWar) return;

          var expandedCells = ExpandRegion(___litCells);
          foreach (var cell in expandedCells)
          {
            if (!Grid.IsValidCell(cell)) continue;

            try
            {
              Grid.Reveal(cell);
            }
            catch (Exception ex)
            {
              Debug.LogWarning($"[DarknessNotIncluded] LightsRevealFogOfWar Grid.Reveal failed for cell {cell}: {ex}");
            }
          }
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[DarknessNotIncluded] LightsRevealFogOfWar AddToGrid postfix threw: {ex}");
        }
      }
    }

    static HashSet<int> ExpandRegion(List<int> litCells)
    {
      var newRegion = new HashSet<int>(litCells);
      foreach (var cell in litCells)
      {
        newRegion.Add(Grid.CellAbove(cell));
        newRegion.Add(Grid.CellUpRight(cell));
        newRegion.Add(Grid.CellRight(cell));
        newRegion.Add(Grid.CellDownRight(cell));
        newRegion.Add(Grid.CellBelow(cell));
        newRegion.Add(Grid.CellDownLeft(cell));
        newRegion.Add(Grid.CellLeft(cell));
        newRegion.Add(Grid.CellUpLeft(cell));

        newRegion.Add(Grid.CellUpLeft(Grid.CellAbove(cell)));
        newRegion.Add(Grid.CellAbove(Grid.CellAbove(cell)));
        newRegion.Add(Grid.CellUpRight(Grid.CellAbove(cell)));

        newRegion.Add(Grid.CellUpRight(Grid.CellRight(cell)));
        newRegion.Add(Grid.CellRight(Grid.CellRight(cell)));
        newRegion.Add(Grid.CellDownRight(Grid.CellRight(cell)));

        newRegion.Add(Grid.CellDownRight(Grid.CellBelow(cell)));
        newRegion.Add(Grid.CellBelow(Grid.CellBelow(cell)));
        newRegion.Add(Grid.CellDownLeft(Grid.CellBelow(cell)));

        newRegion.Add(Grid.CellDownLeft(Grid.CellLeft(cell)));
        newRegion.Add(Grid.CellLeft(Grid.CellLeft(cell)));
        newRegion.Add(Grid.CellUpLeft(Grid.CellLeft(cell)));
      }

      return newRegion;
    }
  }
}
