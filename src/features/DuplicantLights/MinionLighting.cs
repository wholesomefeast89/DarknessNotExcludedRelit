using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace DarknessNotIncluded.DuplicantLights
{
  public static class MinionLighting
  {
    // Keep the original patch for base-game Minions (safe, idempotent with AddOrGet)
    [HarmonyPatch(typeof(MinionConfig)), HarmonyPatch("CreatePrefab")]
    static class Patched_MinionConfig_CreatePrefab
    {
      static void Postfix(GameObject __result)
      {
        __result.AddOrGet<MinionLights>();
      }
    }

    // Ensure ALL minions get MinionLights
    [HarmonyPatch(typeof(MinionIdentity), "OnPrefabInit")]
    static class Patched_MinionIdentity_OnPrefabInit
    {
      static void Postfix(MinionIdentity __instance)
      {
        __instance.gameObject.AddOrGet<MinionLights>();
      }
    }

    [HarmonyPatch(typeof(BionicMinionConfig), "CreatePrefab")]
    static class Patched_BionicMinionConfig_CreatePrefab
    {
      static void Postfix(GameObject __result)
      {
        __result.AddOrGet<MinionLighting.MinionLights>();
      }
    }

    public class MinionLights : Behavior.UnitLights
    {
      [MyCmpGet] private MinionIdentity minion;
      [MyCmpGet] private MinionResume resume;

      // Tag used on printing pod selection preview dupes
      private static readonly Tag MINION_SELECT_PREVIEW_TAG = new Tag("MinionSelectPreview");

      protected override MinionLightType GetActiveLightType(MinionLightingConfig minionLightingConfig)
      {
        minionLightingConfig = EnsureLightingConfig(minionLightingConfig);
        if (minionLightingConfig == null) return MinionLightType.None;

        var lightType = GetLightTypeForCurrentState(minionLightingConfig);
        if (lightType == MinionLightType.None) return lightType;

        var cfg = minionLightingConfig.Get(lightType);
        if (cfg == null || !cfg.enabled) lightType = MinionLightType.Intrinsic;
        cfg = minionLightingConfig.Get(lightType);
        if (cfg == null || !cfg.enabled) lightType = MinionLightType.None;

        return lightType;
      }

      private MinionLightingConfig EnsureLightingConfig(MinionLightingConfig cfg)
      {
        if (cfg != null) return cfg;
        var inst = Config.instance;
        if (inst != null && inst.minionLightingConfig != null) return inst.minionLightingConfig;
        return new MinionLightingConfig(); // fallback defaults so we never NRE
      }

      private Equipment SafeGetEquipment()
      {
        if (minion == null) return null;
        try
        {
          return minion.GetEquipment(); // Can throw early during printing pod preview
        }
        catch
        {
          return null;
        }
      }

      private MinionLightType GetLightTypeForCurrentState(MinionLightingConfig minionLightingConfig)
      {
        // Printing pod selection / preview dupes: do not inspect equipment; use intrinsic only
        if (gameObject != null && gameObject.HasTag(MINION_SELECT_PREVIEW_TAG))
          return MinionLightType.Intrinsic;

        if (minionLightingConfig == null) return MinionLightType.None;
        if (minion == null) return MinionLightType.None;
        if (!minion.isSpawned) return MinionLightType.None;
        if (minion.IsSleeping()) return MinionLightType.None;

        // Resume can be null in early lifecycle
        var hat = (resume != null) ? resume.CurrentHat : null;

        // Safe suit/equipment access
        var equipment = SafeGetEquipment();
        Equippable suit = null;
        if (equipment != null)
        {
          var suitSlot = equipment.GetSlot(Db.Get().AssignableSlots.Suit);
          suit = suitSlot != null ? suitSlot.assignable as Equippable : null;
        }
        var suitPrefab = suit != null ? suit.GetComponent<KPrefabID>() : null;

        var possibleLightTypes = new List<MinionLightType>();

        // Prefer suit lights if equipped
        if (suitPrefab != null && suit.isEquipped)
        {
          if (suitPrefab.HasTag(GameTags.AtmoSuit)) possibleLightTypes.Add(MinionLightType.AtmoSuit);
          if (suitPrefab.HasTag(GameTags.JetSuit)) possibleLightTypes.Add(MinionLightType.JetSuit);
          if (suitPrefab.HasTag(GameTags.LeadSuit)) possibleLightTypes.Add(MinionLightType.LeadSuit);
        }

        // Bionic dupes
        var kpid = minion.GetComponent<KPrefabID>();
        if (kpid != null &&
            (kpid.HasTag(new Tag("BionicDuplicant")) ||
             kpid.HasTag(new Tag("BionicMinion")) ||
             kpid.HasTag(new Tag("Bionic"))))
        {
          possibleLightTypes.Add(MinionLightType.Bionic);
        }

        // Hats
        if (!string.IsNullOrEmpty(hat))
        {
          if (hat.StartsWith("hat_role_mining"))
          {
            if (hat == "hat_role_mining1") possibleLightTypes.Add(MinionLightType.Mining1);
            else if (hat == "hat_role_mining2") possibleLightTypes.Add(MinionLightType.Mining2);
            else if (hat == "hat_role_mining3") possibleLightTypes.Add(MinionLightType.Mining3);
            else if (hat == "hat_role_mining4") possibleLightTypes.Add(MinionLightType.Mining4);
            else possibleLightTypes.Add(MinionLightType.Mining4);
          }
          else if (hat.StartsWith("hat_role_research"))
          {
            possibleLightTypes.Add(MinionLightType.Science);
          }
          else if (hat.StartsWith("hat_role_astronaut"))
          {
            possibleLightTypes.Add(MinionLightType.Rocketry);
          }
          else if (hat.StartsWith("hat_role_building"))
          {
            if (hat == "hat_role_building1") possibleLightTypes.Add(MinionLightType.Building1);
            else if (hat == "hat_role_building2") possibleLightTypes.Add(MinionLightType.Building2);
            else if (hat == "hat_role_building3") possibleLightTypes.Add(MinionLightType.Building3);
            else possibleLightTypes.Add(MinionLightType.Building3);
          }
          else if (hat.StartsWith("hat_role_technicals"))
          {
            if (hat == "hat_role_technicals1") possibleLightTypes.Add(MinionLightType.Technicals1);
            else if (hat == "hat_role_technicals2") possibleLightTypes.Add(MinionLightType.Technicals2);
            else possibleLightTypes.Add(MinionLightType.Technicals2);
          }
          else if (hat.StartsWith("hat_role_engineering"))
          {
            possibleLightTypes.Add(MinionLightType.Engineering1);
          }
          else if (hat == "hat_role_basekeeping2")
          {
            possibleLightTypes.Add(MinionLightType.Basekeeping2);
          }
          else if (hat == "hat_role_basekeeping1")
          {
            possibleLightTypes.Add(MinionLightType.Basekeeping1);
          }
          else if (hat.StartsWith("hat_role_farming"))
          {
            if (hat == "hat_role_farming1") possibleLightTypes.Add(MinionLightType.Farming1);
            else if (hat == "hat_role_farming2") possibleLightTypes.Add(MinionLightType.Farming2);
            else if (hat == "hat_role_farming3") possibleLightTypes.Add(MinionLightType.Farming3);
            else possibleLightTypes.Add(MinionLightType.Farming3);
          }
          else if (hat.StartsWith("hat_role_rancher"))
          {
            if (hat == "hat_role_rancher1") possibleLightTypes.Add(MinionLightType.Ranching1);
            else if (hat == "hat_role_rancher2") possibleLightTypes.Add(MinionLightType.Ranching2);
            else possibleLightTypes.Add(MinionLightType.Ranching2);
          }
          else if (hat.StartsWith("hat_role_cooking"))
          {
            if (hat == "hat_role_cooking1") possibleLightTypes.Add(MinionLightType.Cooking1);
            else if (hat == "hat_role_cooking2") possibleLightTypes.Add(MinionLightType.Cooking2);
            else possibleLightTypes.Add(MinionLightType.Cooking2);
          }
          else if (hat.StartsWith("hat_role_art"))
          {
            if (hat == "hat_role_art1") possibleLightTypes.Add(MinionLightType.Arting1);
            else if (hat == "hat_role_art2") possibleLightTypes.Add(MinionLightType.Arting2);
            else if (hat == "hat_role_art3") possibleLightTypes.Add(MinionLightType.Arting3);
            else possibleLightTypes.Add(MinionLightType.Arting3);
          }
          else if (hat.StartsWith("hat_role_hauling"))
          {
            if (hat == "hat_role_hauling1") possibleLightTypes.Add(MinionLightType.Hauling1);
            else if (hat == "hat_role_hauling2") possibleLightTypes.Add(MinionLightType.Hauling2);
            else possibleLightTypes.Add(MinionLightType.Hauling2);
          }
          else if (hat.StartsWith("hat_role_medicalaid"))
          {
            if (hat == "hat_role_medicalaid1") possibleLightTypes.Add(MinionLightType.Medicine1);
            else if (hat == "hat_role_medicalaid2") possibleLightTypes.Add(MinionLightType.Medicine2);
            else if (hat == "hat_role_medicalaid3") possibleLightTypes.Add(MinionLightType.Medicine3);
            else possibleLightTypes.Add(MinionLightType.Medicine3);
          }
          else if (hat == "hat_role_suits1")
          {
            possibleLightTypes.Add(MinionLightType.Suits1);
          }
        }

        // Only consider enabled & defined types; pick brightest
        possibleLightTypes = possibleLightTypes.FindAll(type =>
        {
          var cfg = minionLightingConfig.Get(type);
          return cfg != null && cfg.enabled;
        });

        possibleLightTypes.Sort((a, b) =>
        {
          var ca = minionLightingConfig.Get(a);
          var cb = minionLightingConfig.Get(b);
          int la = ca != null ? ca.lux : 0;
          int lb = cb != null ? cb.lux : 0;
          return lb.CompareTo(la);
        });

        // Fall back to intrinsic glow if enabled
        var intrinsicCfg = minionLightingConfig.Get(MinionLightType.Intrinsic);
        if (possibleLightTypes.Count > 0) return possibleLightTypes[0];
        if (intrinsicCfg != null && intrinsicCfg.enabled) return MinionLightType.Intrinsic;
        return MinionLightType.None;
      }
    }
  }
}
