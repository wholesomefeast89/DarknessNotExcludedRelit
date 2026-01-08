using DarknessNotIncluded.Exploration;
using System;
using DarknessNotIncluded;
using UnityEngine;

namespace DarknessNotIncluded.DuplicantLights
{
  public static class Behavior
  {
    public abstract class UnitLights : KMonoBehaviour, ISim33ms
    {
      private static bool disableLightsInBedrooms;
      private static bool disableLightsInLitAreas;
      private static int litWorkspaceLux;
      private static MinionLightingConfig minionLightingConfig;
      private static bool occludeVisibilityByWalls;

      private static Config.Observer configObserver = new Config.Observer((config) =>
      {
        disableLightsInBedrooms = config.disableDupeLightsInBedrooms;
        disableLightsInLitAreas = config.disableDupeLightsInLitAreas;
        litWorkspaceLux = config.litWorkspaceLux;
        minionLightingConfig = config.minionLightingConfig;
        occludeVisibilityByWalls = config.occludeVisibilityByWalls;
      });

      [MyCmpGet]
      private GridVisibility gridVisibility;

      public Light2D Light { get; set; }

      private MinionLightType currentLightType = MinionLightType.None;

      protected override void OnPrefabInit()
      {
        base.OnPrefabInit();

        Light = gameObject.AddComponent<Light2D>();

        // Ensure GridVisibility exists on all units (including preview/minion select)
        gridVisibility = gameObject.AddOrGet<GridVisibility>();

        Config.ObserveFor(this, (config) =>
        {
          UpdateLights(true);
        });
      }

      protected override void OnSpawn()
      {
        base.OnSpawn();
        UpdateLights();
      }

      public void Sim33ms(float dt)
      {
        UpdateLights();
      }

      private void UpdateLights(bool force = false)
      {
        if (gameObject == null) return;

        MinionLightType lightType;
        try
        {
          lightType = GetActiveLightType(minionLightingConfig);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[DarknessNotIncluded] UnitLights.GetActiveLightType threw: {ex}");
          return;
        }

        var cfg = minionLightingConfig;
        if (cfg == null) return;

        var lightConfig = cfg.Get(lightType);
        if (lightConfig == null) return;

        // Reveal: wrap to prevent fatal errors during sim tick transitions (world/cluster changes, invalid cells).
        if (gridVisibility != null)
        {
          try
          {
            gridVisibility.SetRadius(lightConfig.reveal);

            // NOTE: Grid.PosToCell can be problematic if transform/world not ready; keep inside try.
            var originCell = Grid.PosToCell(gameObject);
            VisibilityUtils.RevealArea(originCell, gridVisibility.radius, gridVisibility.innerRadius);
          }
          catch (Exception ex)
          {
            Debug.LogWarning($"[DarknessNotIncluded] UnitLights reveal threw for '{gameObject.name}': {ex}");
          }
        }

        if (disableLightsInBedrooms && lightType != MinionLightType.None)
        {
          try
          {
            if (MinionRoomState.SleepersInSameRoom(gameObject))
            {
              lightType = MinionLightType.None;
            }
          }
          catch (Exception ex)
          {
            Debug.LogWarning($"[DarknessNotIncluded] UnitLights bedroom check threw: {ex}");
          }
        }

        if (disableLightsInLitAreas && lightType != MinionLightType.None)
        {
          try
          {
            var cell = Grid.PosToCell(gameObject);
            var cellLux = Grid.IsValidCell(cell) ? Grid.LightIntensity[cell] : 0;

            var dupeLux = Light != null && Light.enabled ? Light.Lux : 0;
            var baseCellLux = Math.Max(0, cellLux - dupeLux);
            var targetLux = litWorkspaceLux;

            if (baseCellLux >= targetLux)
            {
              lightType = MinionLightType.None;
            }
          }
          catch (Exception ex)
          {
            Debug.LogWarning($"[DarknessNotIncluded] UnitLights lit-area check threw: {ex}");
          }
        }

        SetLightType(lightType, force);
      }

      private void SetLightType(MinionLightType lightType, bool force)
      {
        if (lightType == currentLightType && !force) return;

        var cfg = minionLightingConfig;
        if (cfg == null) return;

        var lightCfg = cfg.Get(lightType);
        if (lightCfg == null) return;

        currentLightType = lightType;

        try
        {
          lightCfg.ConfigureLight(Light);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[DarknessNotIncluded] ConfigureLight threw for '{gameObject.name}': {ex}");
        }
      }

      protected abstract MinionLightType GetActiveLightType(MinionLightingConfig minionLightingConfig);
    }
  }
}
