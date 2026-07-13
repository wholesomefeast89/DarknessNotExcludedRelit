using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace DarknessNotIncluded
{
  public enum MinionLightType
  {
    None,
    Intrinsic,
    Mining1,
    Mining2,
    Mining3,
    Mining4,
    Science,
    Rocketry,
    AtmoSuit,
    JetSuit,
    LeadSuit,
    Rover,
    Bionic,
    Building1,
    Building2,
    Building3,
    Technicals1,
    Technicals2,
    Engineering1,
    Basekeeping2,
    Farming1,
    Farming2,
    Farming3,
    Ranching1,
    Ranching2,
    Cooking1,
    Cooking2,
    Arting1,
    Arting2,
    Arting3,
    Hauling1,
    Hauling2,
    Basekeeping1,
    Medicine1,
    Medicine2,
    Medicine3,
    Suits1
  }

  public class MinionLightingConfig : Dictionary<MinionLightType, LightConfig>
  {
    public MinionLightingConfig()
    {
      Add(MinionLightType.Intrinsic, new LightConfig(true, 200, 2, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Mining1, new LightConfig(true, 800, 3, 6, LightShape.DirectedCone, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.Mining2, new LightConfig(true, 1000, 4, 7, LightShape.DirectedCone, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.Mining3, new LightConfig(true, 1200, 5, 8, LightShape.DirectedCone, Color.white));
      Add(MinionLightType.Mining4, new LightConfig(true, 1400, 6, 9, LightShape.DirectedCone, Color.white));
      Add(MinionLightType.Science, new LightConfig(true, 800, 3, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Rocketry, new LightConfig(true, 800, 4, 0, LightShape.DirectedCone, Color.white));
      Add(MinionLightType.AtmoSuit, new LightConfig(true, 600, 3, 0, LightShape.Pill, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.JetSuit, new LightConfig(true, 800, 5, 0, LightShape.Pill, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.LeadSuit, new LightConfig(true, 400, 3, 0, LightShape.Pill, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.Rover, new LightConfig(true, 1400, 6, 9, LightShape.DirectedCone, Color.white));
      Add(MinionLightType.Bionic, new LightConfig(true, 400, 4, 0, LightShape.Pill, Color.cyan));
      Add(MinionLightType.Building1, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Building2, new LightConfig(true, 900, 4, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Building3, new LightConfig(true, 1100, 4, 0, LightShape.Pill, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.Technicals1, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.cyan));
      Add(MinionLightType.Technicals2, new LightConfig(true, 900, 4, 0, LightShape.Pill, Color.cyan));
      Add(MinionLightType.Engineering1, new LightConfig(true, 1000, 4, 0, LightShape.Pill, Color.cyan));
      Add(MinionLightType.Basekeeping2, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Farming1, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.green));
      Add(MinionLightType.Farming2, new LightConfig(true, 900, 4, 0, LightShape.Pill, Color.green));
      Add(MinionLightType.Farming3, new LightConfig(true, 1100, 4, 0, LightShape.Pill, Color.green));
      Add(MinionLightType.Ranching1, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Ranching2, new LightConfig(true, 900, 4, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Cooking1, new LightConfig(true, 700, 3, 0, LightShape.Pill, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.Cooking2, new LightConfig(true, 900, 4, 0, LightShape.Pill, LIGHT2D.LIGHT_YELLOW));
      Add(MinionLightType.Arting1, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.magenta));
      Add(MinionLightType.Arting2, new LightConfig(true, 900, 4, 0, LightShape.Pill, Color.magenta));
      Add(MinionLightType.Arting3, new LightConfig(true, 1100, 4, 0, LightShape.Pill, Color.magenta));
      Add(MinionLightType.Hauling1, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Hauling2, new LightConfig(true, 900, 4, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Basekeeping1, new LightConfig(true, 700, 3, 0, LightShape.Pill, Color.white));
      Add(MinionLightType.Medicine1, new LightConfig(true, 700, 3, 0, LightShape.Pill, new Color(1f, 0.7f, 0.7f)));
      Add(MinionLightType.Medicine2, new LightConfig(true, 900, 4, 0, LightShape.Pill, new Color(1f, 0.7f, 0.7f)));
      Add(MinionLightType.Medicine3, new LightConfig(true, 1100, 4, 0, LightShape.Pill, new Color(1f, 0.7f, 0.7f)));
      Add(MinionLightType.Suits1, new LightConfig(true, 600, 3, 0, LightShape.Pill, Color.cyan));
    }

    public MinionLightingConfig DeepClone()
    {
      var newConfig = new MinionLightingConfig();
      foreach (var pair in this)
      {
        newConfig[pair.Key] = pair.Value.DeepClone();
      }
      return newConfig;
    }

    public LightConfig Get(MinionLightType lightType)
    {
      return this.ContainsKey(lightType) ? this[lightType] : LightConfig.None;
    }
  }
}
