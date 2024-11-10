using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using FistVR;
using UnityEngine;
using TNHFramework.ObjectTemplates;

namespace TNHFramework.Patches
{
    [HarmonyPatch(typeof(TNH_HoldPoint))]
    public class HoldPatches
    {
        [HarmonyPatch("CompletePhase")]
        [HarmonyPrefix]
        public static bool NextPhasePatch(TNH_HoldPoint __instance)
        {
            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];
            if (character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[__instance.m_phaseIndex].DespawnBetweenWaves)
            {
                __instance.DeletionBurst();
                __instance.M.ClearMiscEnemies();
                UnityEngine.Object.Instantiate(__instance.VFX_HoldWave, __instance.m_systemNode.NodeCenter.position, __instance.m_systemNode.NodeCenter.rotation);
            }
            if (character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[__instance.m_phaseIndex].UsesVFX)
            {
                SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, __instance.AUDEvent_HoldWave, __instance.transform.position);
                __instance.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
            }
            __instance.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldDecisecondsRemaining, (int)(__instance.m_tickDownToFailure * 10f));
            __instance.m_phaseIndex++;
            if (character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases.Count > __instance.m_phaseIndex &&
                character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[__instance.m_phaseIndex].ScanTime == 0 && 
                character.GetCurrentLevel(__instance.M.m_curLevel).HoldPhases[__instance.m_phaseIndex].WarmupTime == 0)
            {
                __instance.SpawnPoints_Targets.Shuffle();
                __instance.m_validSpawnPoints.Shuffle();
                __instance.IdentifyEncryption();
            }
            else
            {
                __instance.m_state = TNH_HoldPoint.HoldState.Transition;
                __instance.m_tickDownTransition = 5f;
                __instance.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);
            }
            __instance.LowerAllBarriers();
            if (!__instance.m_hasBeenDamagedThisPhase)
            {
                __instance.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldWaveCompleteNoDamage, 1);
            }

            return false;
        }
    }
}
