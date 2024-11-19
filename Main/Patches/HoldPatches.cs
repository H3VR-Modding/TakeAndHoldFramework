using System.Collections.Generic;
using FistVR;
using HarmonyLib;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    [HarmonyPatch(typeof(TNH_HoldPoint))]
    public class HoldPatches
    {
        [HarmonyPatch("BeginHoldChallenge")]
        [HarmonyPrefix]
        public static bool BeginHoldPatch(TNH_HoldPoint __instance)
        {
            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];

            List<Phase> currentPhases = character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases;
            List<Phase> validPhases = [];

            foreach (Phase phase in currentPhases)
            {
                if (phase.Keys.Contains("Start"))
                {
                    validPhases.Add(phase);
                }
            }

            Phase chosenPhase = validPhases.GetRandom();

            __instance.m_phaseIndex = currentPhases.IndexOf(chosenPhase);
            character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[__instance.m_phaseIndex].BeginHold(__instance, character);

            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool UpdatePatch(TNH_HoldPoint __instance)
        {
            if (!__instance.m_isInHold)
            {
                return false;
            }
            __instance.CyclePointAttack();

            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];

            character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[__instance.m_phaseIndex].HoldUpdate(__instance, character);

            return false;
        }


        [HarmonyPatch("TargetDestroyed")]
        [HarmonyPrefix]
        public static bool TargetDestroyedPatch(TNH_HoldPoint __instance, TNH_EncryptionTarget t)
        {
            __instance.m_activeTargets.Remove(t);
            if (__instance.m_activeTargets.Count <= 0)
            {
                TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];
                character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[__instance.m_phaseIndex].EndPhase(__instance, character);
            }

            return false;
        }

        [HarmonyPatch("GetMaxTargsInHold")]
        [HarmonyPrefix]
        public static bool GetMaxTargsPatch(TNH_HoldPoint __instance, ref int __result)
        {
            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];
            
            int num = 0;
            for (int i = 0; i < character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases.Count; i++)
            {
                num = Mathf.Max(num, (character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[i] as EncryptionPhase).MaxTargets);
            }

            __result = num;
            return false;
        }
    }
}
