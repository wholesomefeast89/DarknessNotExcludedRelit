using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessNotIncluded
{
  public static class VisibilityUtils
  {
    // Bresenham line generator (cells inclusive)
    public static IEnumerable<int> CellsOnLine(int x0, int y0, int x1, int y1)
    {
      int dx = Math.Abs(x1 - x0);
      int dy = -Math.Abs(y1 - y0);
      int sx = x0 < x1 ? 1 : -1;
      int sy = y0 < y1 ? 1 : -1;
      int err = dx + dy;
      int x = x0;
      int y = y0;

      while (true)
      {
        yield return Grid.XYToCell(x, y);
        if (x == x1 && y == y1) yield break;
        int e2 = 2 * err;
        if (e2 >= dy)
        {
          err += dy;
          x += sx;
        }
        if (e2 <= dx)
        {
          err += dx;
          y += sy;
        }
      }
    }

    public static bool IsBlockingCell(int cell)
    {
      if (!Grid.IsValidCell(cell)) return false;

      try
      {
        var cfg = Config.instance;
        if (cfg != null && !cfg.occludeVisibilityByWalls)
          return false;
      }
      catch { }

      if (Grid.Solid[cell]) return true;

      var obj = Grid.Objects[cell, (int)ObjectLayer.Building];
      if (obj != null) return true;

      return false;
    }

    public static void RevealWithLineOfSight(int origin, int radius)
    {
      try
      {
        if (!Grid.IsValidCell(origin)) return;

        var originXY = Grid.CellToXY(origin);
        int ox = originXY.x;
        int oy = originXY.y;

        int r = Math.Max(1, radius);
        int minx = Math.Max(0, ox - r);
        int maxx = Math.Min(Grid.WidthInCells - 1, ox + r);
        int miny = Math.Max(0, oy - r);
        int maxy = Math.Min(Grid.HeightInCells - 1, oy + r);

        Grid.Reveal(origin);
        RevealIfValid(Grid.CellAbove(origin));
        RevealIfValid(Grid.CellRight(origin));
        RevealIfValid(Grid.CellBelow(origin));
        RevealIfValid(Grid.CellLeft(origin));

        for (int y = miny; y <= maxy; y++)
        {
          for (int x = minx; x <= maxx; x++)
          {
            int cell = Grid.XYToCell(x, y);
            if (!Grid.IsValidCell(cell)) continue;
            if (cell == origin) continue;
            if (Math.Abs(x - ox) + Math.Abs(y - oy) == 1) continue;
            if ((x - ox) * (x - ox) + (y - oy) * (y - oy) > r * r) continue;

            bool blocked = false;
            foreach (int stepCell in CellsOnLine(ox, oy, x, y))
            {
              if (stepCell == origin) continue;
              if (stepCell == cell) break;
              if (IsBlockingCell(stepCell))
              {
                blocked = true;
                break;
              }
            }

            try
            {
              if (blocked)
              {
                if (Grid.LightIntensity[cell] > 0 || Grid.Visible[cell] > 0 || Grid.ExposedToSunlight[cell] > 0)
                  Grid.Reveal(cell);
              }
              else
              {
                Grid.Reveal(cell);
              }
            }
            catch (Exception ex)
            {
              Debug.LogWarning($"[DarknessNotIncluded] RevealWithLineOfSight Grid.Reveal failed for cell {cell}: {ex}");
            }
          }
        }

        void RevealIfValid(int c)
        {
          try
          {
            if (Grid.IsValidCell(c)) Grid.Reveal(c);
          }
          catch { }
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[DarknessNotIncluded] RevealWithLineOfSight threw: {ex}");
      }
    }

    public static void RevealArea(int originCell, int radius, float innerRadius)
    {
      try
      {
        if (!Grid.IsValidCell(originCell)) return;

        bool occlusionEnabled = false;
        try
        {
          occlusionEnabled = Config.instance != null && Config.instance.occludeVisibilityByWalls;
        }
        catch { }

        if (occlusionEnabled)
        {
          RevealWithLineOfSight(originCell, radius);
        }
        else
        {
          var xy = Grid.CellToXY(originCell);
          try
          {
            GridVisibility.Reveal(xy.x, xy.y, radius, innerRadius);
          }
          catch (Exception ex)
          {
            Debug.LogWarning($"[DarknessNotIncluded] GridVisibility.Reveal threw for originCell {originCell}: {ex}");
          }
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[DarknessNotIncluded] RevealArea threw for originCell {originCell}: {ex}");
      }
    }
  }
}