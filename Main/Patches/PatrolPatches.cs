using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    public static class PatrolPatches
    {
        /////////////////////////////
        //PATCHES FOR PATROL SPAWNING
        /////////////////////////////


        /// <summary>
        /// Finds an index in the patrols list which can spawn, preventing bosses that have already spawned from spawning again
        /// </summary>
        /// <param name="patrols">List of patrols that can spawn</param>
        /// <returns>Returns -1 if no valid index is found, otherwise returns a random index for a patrol </returns>
        private static int GetValidPatrolIndex(List<Patrol> patrols)
        {
            // Create a pool of valid patrols
            // This allows us to generate one patrol of each type before generating more
            if (TNHFramework.PatrolIndexPool == null || TNHFramework.PatrolIndexPool.Count == 0)
            {
                for (int i = 0; i < patrols.Count; i++)
                {
                    if (TNHFramework.SpawnedBossIndexes == null || !TNHFramework.SpawnedBossIndexes.Contains(i))
                        TNHFramework.PatrolIndexPool.Add(i);
                }
            }

            // No valid patrols left
            if (TNHFramework.PatrolIndexPool.Count == 0)
            {
                TNHFrameworkLogger.Log("No valid patrols can spawn", TNHFrameworkLogger.LogType.TNH);
                return -1;
            }

            // Choose from the valid patrol pool
            int index = UnityEngine.Random.Range(0, TNHFramework.PatrolIndexPool.Count);
            int patrolIndex = TNHFramework.PatrolIndexPool[index];
            TNHFramework.PatrolIndexPool.RemoveAt(index);

            TNHFrameworkLogger.Log($"Valid patrol found: {patrolIndex}", TNHFrameworkLogger.LogType.TNH);
            return patrolIndex;
        }


        private static void SetTimeTilPatrolCanSpawn(TNH_Manager instance, Patrol patrol)
        {
            if (instance.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking)
            {
                instance.m_timeTilPatrolCanSpawn = patrol.PatrolCadence;
            }
            else
            {
                instance.m_timeTilPatrolCanSpawn = patrol.PatrolCadenceLimited;
            }
        }


        [HarmonyPatch(typeof(TNH_Manager), "GetRandomSafeSupplyIndexFromHoldPoint")]
        [HarmonyPrefix]
        private static bool GetRandomSafeSupplyIndexFromHoldPoint_BugFix(TNH_Manager __instance, out int __result, int index)
        {
            // Anton pls fix - Wrong list used (DicSafeHoldIndiciesForHoldPoint)
            __result = __instance.DicSafeSupplyIndiciesForHoldPoint[index][UnityEngine.Random.Range(0, __instance.DicSafeSupplyIndiciesForHoldPoint[index].Count)];
            return false;
        }


        [HarmonyPatch(typeof(TNH_Manager), "UpdatePatrols")]
        [HarmonyPrefix]
        public static bool UpdatePatrolsReplacement(TNH_Manager __instance)
        {
            // Update global patrol spawn timer
            if (__instance.m_timeTilPatrolCanSpawn > 0f)
            {
                __instance.m_timeTilPatrolCanSpawn -= Time.deltaTime;
            }

            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.C];
            Level currLevel = character.GetCurrentLevel(__instance.m_curLevel);

            int maxPatrols = (__instance.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking) ?
                currLevel.Patrols[0].MaxPatrols : currLevel.Patrols[0].MaxPatrolsLimited;

            bool isCustomCharacter = !TNHPatches.BaseCharStrings.Contains(__instance.C.TableID);

            // Adjust max patrols for new patrol behavior
            if (!__instance.UsesClassicPatrolBehavior && !isCustomCharacter)
            {
                maxPatrols += (__instance.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo) ? 1 : 2;
            }

            // Time to generate a new patrol
            if (__instance.m_timeTilPatrolCanSpawn <= 0f && __instance.m_patrolSquads.Count < maxPatrols)
            {
                Vector3 playerPos = GM.CurrentPlayerBody.Head.position;

                // Check if player is in a supply point
                int supplyIndex = -1;
                for (int i = 0; i < __instance.SupplyPoints.Count; i++)
                {
                    if (__instance.SupplyPoints[i].IsPointInBounds(playerPos))
                    {
                        supplyIndex = i;
                        break;
                    }
                }

                // Check if player is in a hold point
                int holdIndex = -1;
                for (int i = 0; i < __instance.HoldPoints.Count; i++)
                {
                    if (i != __instance.m_lastHoldIndex && i != __instance.m_curHoldIndex && __instance.HoldPoints[i].IsPointInBounds(playerPos))
                    {
                        holdIndex = i;
                        break;
                    }
                }

                // Player is in a supply point
                if (supplyIndex > -1)
                {
                    if (__instance.UsesClassicPatrolBehavior)
                    {
                        GenerateValidPatrolReplacement(__instance, supplyIndex, __instance.m_curHoldIndex, true);
                    }
                    else if (currLevel.Patrols.Count > 0)
                    {
                        TNHFrameworkLogger.Log($"Player is in Supply {supplyIndex} (S {__instance.SupplyPoints.Count}, H {__instance.HoldPoints.Count})", TNHFrameworkLogger.LogType.TNH);

                        int firstPoint;
                        TNH_Manager.SentryPatrolPointType firstSpawnPointType;
                        TNH_Manager.SentryPatrolPointType firstPatrolPointType;

                        // Choose supply or hold to generate sentry patrol from
                        if (UnityEngine.Random.value >= 0.5f)
                        {
                            firstPoint = __instance.GetRandomSafeHoldIndexFromSupplyPoint(supplyIndex);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Hold;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSHold;
                        }
                        else
                        {
                            firstPoint = __instance.GetRandomSafeSupplyIndexFromSupplyPoint(supplyIndex);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Supply;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSSupply;
                        }

                        int patrolIndex = GetValidPatrolIndex(currLevel.Patrols);

                        if (patrolIndex > -1)
                        {
                            Patrol patrol = currLevel.Patrols[patrolIndex];

                            // Anton pls fix - GetSpawnPoints() sometimes uses wrong type
                            //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrolChallenge.Patrols[index], __instance.GetSpawnPoints(nextPoint, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, nextPoint, supplyIndex, supplyIndex, __instance.m_curHoldIndex));
                            TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, __instance.GetSpawnPoints(firstPoint, firstSpawnPointType), __instance.GetForwardVectors(), __instance.GetPatrolPoints(firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, firstPoint, supplyIndex, supplyIndex, __instance.m_curHoldIndex), patrolIndex);
                            __instance.m_patrolSquads.Add(squad);

                            SetTimeTilPatrolCanSpawn(__instance, patrol);
                        }
                    }
                }
                // Player is in a hold point
                else if (holdIndex > -1)
                {
                    if (!__instance.UsesClassicPatrolBehavior && currLevel.Patrols.Count > 0)
                    {
                        TNHFrameworkLogger.Log($"Player is in Hold {holdIndex} (S {__instance.SupplyPoints.Count}, H {__instance.HoldPoints.Count})", TNHFrameworkLogger.LogType.TNH);

                        int firstPoint;
                        TNH_Manager.SentryPatrolPointType firstSpawnPointType;
                        TNH_Manager.SentryPatrolPointType firstPatrolPointType;

                        // Choose supply or hold to generate sentry patrol from
                        if (UnityEngine.Random.value >= 0.5f)
                        {
                            firstPoint = __instance.GetRandomSafeHoldIndexFromHoldPoint(holdIndex);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Hold;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSHold;
                        }
                        else
                        {
                            firstPoint = __instance.GetRandomSafeSupplyIndexFromHoldPoint(holdIndex);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Supply;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSSupply;
                        }

                        int patrolIndex = GetValidPatrolIndex(currLevel.Patrols);

                        if (patrolIndex > -1)
                        {
                            Patrol patrol = currLevel.Patrols[patrolIndex];

                            // Anton pls fix - GetSpawnPoints() sometimes uses wrong type
                            //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrolChallenge.Patrols[index], __instance.GetSpawnPoints(nextPoint, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, nextPoint, holdIndex, holdIndex, __instance.m_curHoldIndex));
                            TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, __instance.GetSpawnPoints(firstPoint, firstSpawnPointType), __instance.GetForwardVectors(), __instance.GetPatrolPoints(firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, firstPoint, holdIndex, holdIndex, __instance.m_curHoldIndex), patrolIndex);
                            __instance.m_patrolSquads.Add(squad);

                            SetTimeTilPatrolCanSpawn(__instance, patrol);
                        }
                    }
                }
                else
                {
                    // Try again later
                    __instance.m_timeTilPatrolCanSpawn = 6f;
                }
            }

            for (int squadIndex = 0; squadIndex < __instance.m_patrolSquads.Count; squadIndex++)
            {
                TNH_Manager.SosigPatrolSquad patrolSquad = __instance.m_patrolSquads[squadIndex];

                // Remove dead sosigs from squad
                for (int i = patrolSquad.Squad.Count - 1; i >= 0; i--)
                {
                    if (patrolSquad.Squad[i] == null)
                        patrolSquad.Squad.RemoveAt(i);
                }

                if (patrolSquad.Squad.Count > 0)
                {
                    if (__instance.UsesAlertPatrolSystem && __instance.m_AlertTickDownTime > 0f)
                    {
                        // Patrols are on alert
                        for (int i = 0; i < patrolSquad.Squad.Count; i++)
                        {
                            patrolSquad.Squad[i].UpdateAssaultPoint(__instance.m_lastAlertSpottedPoint);
                        }
                    }
                    else
                    {
                        // Determine whether all sosigs in squad are near the patrol point target
                        bool hasReachedPatrolPoint = false;
                        int reached = 0;
                        int total = patrolSquad.Squad.Count;

                        for (int i = 0; i < total; i++)
                        {
                            if ((float)Vector3.Distance(patrolSquad.Squad[i].transform.position, patrolSquad.PatrolPoints[patrolSquad.CurPatrolPointIndex]) <= 4f)
                                reached++;
                        }

                        if (total > 0 && reached == total)
                            hasReachedPatrolPoint = true;

                        if (hasReachedPatrolPoint)
                        {
                            SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[patrolSquad.ID_Leader];
                            SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[template];

                            // Last patrol point
                            if (patrolSquad.CurPatrolPointIndex + 1 >= patrolSquad.PatrolPoints.Count && patrolSquad.IsPatrollingUp)
                            {
                                TNHFrameworkLogger.Log($"Patrol {squadIndex + 1} [{customTemplate.SosigEnemyID}] reached last patrol point ({patrolSquad.CurPatrolPointIndex})! Going back down", TNHFrameworkLogger.LogType.TNH);
                                patrolSquad.IsPatrollingUp = false;
                            }
                            // First patrol point
                            else if (patrolSquad.CurPatrolPointIndex == 0 && !patrolSquad.IsPatrollingUp)
                            {
                                TNHFrameworkLogger.Log($"Patrol {squadIndex + 1} [{customTemplate.SosigEnemyID}] reached first patrol point ({patrolSquad.CurPatrolPointIndex})! Going back up", TNHFrameworkLogger.LogType.TNH);
                                patrolSquad.IsPatrollingUp = true;
                            }
                            else
                            {
                                TNHFrameworkLogger.Log($"Patrol {squadIndex + 1} [{customTemplate.SosigEnemyID}] reached patrol point ({patrolSquad.CurPatrolPointIndex})! Continuing patrol", TNHFrameworkLogger.LogType.TNH);
                            }

                            // Go back and forth between patrol points
                            if (patrolSquad.IsPatrollingUp)
                                patrolSquad.CurPatrolPointIndex++;
                            else
                                patrolSquad.CurPatrolPointIndex--;

                            // Set the patrol point as target
                            foreach (var sosig in patrolSquad.Squad)
                            {
                                sosig.CommandAssaultPoint(patrolSquad.PatrolPoints[patrolSquad.CurPatrolPointIndex]);
                            }
                        }
                    }

                    // Adjust squad orders
                    for (int i = 0; i < patrolSquad.Squad.Count; i++)
                    {
                        if (patrolSquad.Squad[i] != null)
                        {
                            if (patrolSquad.Squad[i].CurrentOrder == Sosig.SosigOrder.Wander)
                            {
                                patrolSquad.Squad[i].CurrentOrder = Sosig.SosigOrder.Assault;
                            }

                            patrolSquad.Squad[i].FallbackOrder = Sosig.SosigOrder.Assault;
                        }
                    }
                }
            }

            for (int squadIndex = 0; squadIndex < __instance.m_patrolSquads.Count; squadIndex++)
            {
                TNH_Manager.SosigPatrolSquad patrolSquad = __instance.m_patrolSquads[squadIndex];

                // If this squad still needs to be spawned
                if (patrolSquad.NumLeftToSpawn > 0)
                {
                    if (patrolSquad.TimeTilNextSpawn > 0f)
                    {
                        // Update timer for this patrol squad only
                        patrolSquad.TimeTilNextSpawn -= Time.deltaTime;
                    }
                    else
                    {
                        int patrolIndex = patrolSquad.HoldPointStart;
                        Patrol patrol = currLevel.Patrols[patrolIndex];

                        // Choose leader or regular sosig type
                        SosigEnemyTemplate template;
                        bool allowAllWeapons;

                        if (patrolSquad.IndexOfNextSpawn == 0)
                        {
                            SosigEnemyID sosigID = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.LeaderType];
                            template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[sosigID];
                            string sosigName = LoadedTemplateManager.LoadedSosigsDict[template].SosigEnemyID;
                            allowAllWeapons = true;

                            TNHFrameworkLogger.Log($"[{DateTime.Now:HH:mm:ss}] Spawning {patrolSquad.NumLeftToSpawn} sosigs for Patrol {squadIndex + 1} [{sosigName}]", TNHFrameworkLogger.LogType.TNH);
                        }
                        else
                        {
                            SosigEnemyID sosigID = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.EnemyType.GetRandom()];
                            template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[sosigID];
                            allowAllWeapons = false;
                        }

                        Vector3 spawnPos = patrolSquad.SpawnPoints[patrolSquad.IndexOfNextSpawn];
                        Quaternion lookRot = Quaternion.LookRotation(patrolSquad.ForwardVectors[patrolSquad.IndexOfNextSpawn], Vector3.up);
                        Vector3 pointOfInterest = patrolSquad.PatrolPoints[0];

                        // Spawn the sosig
                        //Sosig sosig = __instance.SpawnEnemy(template, spawnPos, lookRot, patrolSquad.IFF, true, pointOfInterest, allowAllWeapons);
                        Sosig sosig = PatrolPatches.SpawnEnemy(template, character, spawnPos, lookRot, __instance, patrolSquad.IFF, true, pointOfInterest, allowAllWeapons);

                        // Add random health pickup to leader
                        if (patrolSquad.IndexOfNextSpawn == 0 && (float)UnityEngine.Random.Range(0f, 1f) > 0.65f)
                        {
                            sosig.Links[1].RegisterSpawnOnDestroy(__instance.Prefab_HealthPickupMinor);
                        }

                        // Add the sosig to the squad
                        sosig.SetAssaultSpeed(patrol.AssualtSpeed);
                        patrolSquad.Squad.Add(sosig);
                        patrolSquad.NumLeftToSpawn--;
                        patrolSquad.IndexOfNextSpawn++;
                    }
                }
            }

            // Clean up squad if all sosigs in it are dead
            for (int squadIndex = __instance.m_patrolSquads.Count - 1; squadIndex >= 0; squadIndex--)
            {
                TNH_Manager.SosigPatrolSquad patrolSquad = __instance.m_patrolSquads[squadIndex];

                if (patrolSquad.Squad.Count < 1 && patrolSquad.NumLeftToSpawn <= 0)
                {
                    __instance.m_patrolSquads[squadIndex].PatrolPoints.Clear();
                    __instance.m_patrolSquads.RemoveAt(squadIndex);
                }
            }

            return false;
        }


        [HarmonyPatch(typeof(TNH_Manager), "GenerateInitialTakeSentryPatrols")]
        [HarmonyPrefix]
        private static bool GenerateInitialTakeSentryPatrolsReplacement(TNH_Manager __instance, TNH_PatrolChallenge P, int curSupplyPoint, int lastHoldIndex, int curHoldIndex, bool isStart)
        {
            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.C];
            Level currLevel = character.GetCurrentLevel(__instance.m_curLevel);

            // Get a valid patrol index, and exit if there are no valid patrols
            int patrolIndex = GetValidPatrolIndex(currLevel.Patrols);

            if (patrolIndex == -1)
                return false;

            Patrol patrol = currLevel.Patrols[patrolIndex];

            int maxPatrols = (__instance.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking) ?
                currLevel.Patrols[0].MaxPatrols : currLevel.Patrols[0].MaxPatrolsLimited;

            if (isStart)
            {
                //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, curHoldIndex, curSupplyPoint, curHoldIndex, curHoldIndex));
                TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, __instance.GetSpawnPoints(curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, curHoldIndex, curSupplyPoint, curHoldIndex, curHoldIndex), patrolIndex);
                __instance.m_patrolSquads.Add(squad);

                for (int i = 1; i < Math.Min(__instance.m_activeSupplyPointIndicies.Count, maxPatrols); i++)
                {
                    patrolIndex = GetValidPatrolIndex(currLevel.Patrols);

                    if (patrolIndex == -1)
                        return false;

                    patrol = currLevel.Patrols[patrolIndex];

                    //TNH_Manager.SosigPatrolSquad squad2 = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(__instance.m_activeSupplyPointIndicies[i], TNH_Manager.SentryPatrolPointType.Supply), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, __instance.m_activeSupplyPointIndicies[i], __instance.m_activeSupplyPointIndicies[i], curSupplyPoint, curHoldIndex));
                    TNH_Manager.SosigPatrolSquad squad2 = GenerateSentryPatrol(__instance, patrol, __instance.GetSpawnPoints(__instance.m_activeSupplyPointIndicies[i], TNH_Manager.SentryPatrolPointType.Supply), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, __instance.m_activeSupplyPointIndicies[i], __instance.m_activeSupplyPointIndicies[i], curSupplyPoint, curHoldIndex), patrolIndex);
                    __instance.m_patrolSquads.Add(squad2);
                }
            }
            else
            {
                //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.Hold, curHoldIndex, lastHoldIndex, lastHoldIndex, lastHoldIndex));
                TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, __instance.GetSpawnPoints(curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.Hold, curHoldIndex, lastHoldIndex, lastHoldIndex, lastHoldIndex), patrolIndex);
                __instance.m_patrolSquads.Add(squad);

                for (int i = 1; i < Math.Min(__instance.m_activeSupplyPointIndicies.Count, maxPatrols); i++)
                {
                    patrolIndex = GetValidPatrolIndex(currLevel.Patrols);

                    if (patrolIndex == -1)
                        return false;

                    patrol = currLevel.Patrols[patrolIndex];

                    //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(__instance.m_activeSupplyPointIndicies[j], TNH_Manager.SentryPatrolPointType.Supply), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, __instance.m_activeSupplyPointIndicies[j], __instance.m_activeSupplyPointIndicies[j], __instance.m_activeSupplyPointIndicies[j], __instance.m_activeSupplyPointIndicies[j]));
                    TNH_Manager.SosigPatrolSquad squad2 = GenerateSentryPatrol(__instance, patrol, __instance.GetSpawnPoints(__instance.m_activeSupplyPointIndicies[i], TNH_Manager.SentryPatrolPointType.Supply), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, __instance.m_activeSupplyPointIndicies[i], __instance.m_activeSupplyPointIndicies[i], __instance.m_activeSupplyPointIndicies[i], __instance.m_activeSupplyPointIndicies[i]), patrolIndex);
                    __instance.m_patrolSquads.Add(squad2);
                }
            }

            SetTimeTilPatrolCanSpawn(__instance, patrol);
            return false;
        }


        public static TNH_Manager.SosigPatrolSquad GenerateSentryPatrol(TNH_Manager instance, Patrol patrol, List<Vector3> SpawnPoints, List<Vector3> ForwardVectors, List<Vector3> PatrolPoints, int patrolIndex)
        {
            TNHFrameworkLogger.Log($"Generating a sentry patrol -- There are currently {instance.m_patrolSquads.Count} patrols active", TNHFrameworkLogger.LogType.TNH);

            return GeneratePatrol(patrol, SpawnPoints, ForwardVectors, PatrolPoints, patrolIndex);
        }


        [HarmonyPatch(typeof(TNH_Manager), "GenerateSentryPatrol")]
        [HarmonyPrefix]
        public static bool GenerateSentryPatrolStub(TNH_PatrolChallenge.Patrol curPatrol, List<Vector3> SpawnPoints, List<Vector3> ForwardVectors, List<Vector3> PatrolPoints)
        {
            // We've replaced all calls to GenerateSentryPatrol() with our own, so stub this out
            TNHFrameworkLogger.LogWarning("GenerateSentryPatrolStub() called! This should have been overridden!");
            //throw new ArgumentException("GenerateSentryPatrolStub called");  // DEBUG
            return false;
        }


        /// <summary>
        /// Decides the spawning location and patrol pathing for sosig patrols, and then spawns the patrol
        /// </summary>
        [HarmonyPatch(typeof(TNH_Manager), "GenerateValidPatrol")]
        [HarmonyPrefix]
        public static bool GenerateValidPatrolReplacement(TNH_Manager __instance, int curStandardIndex, int excludeHoldIndex, bool isStart)
        {
            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.C];
            Level currLevel = character.GetCurrentLevel(__instance.m_curLevel);

            //Get a valid patrol index, and exit if there are no valid patrols
            int patrolIndex = GetValidPatrolIndex(currLevel.Patrols);
            if (patrolIndex == -1)
                return false;

            Patrol patrol = currLevel.Patrols[patrolIndex];
            List<int> validLocations = [];
            float minDist = __instance.TAHReticle.Range * 1.2f;

            //Get a safe starting point for the patrol to spawn
            TNH_SafePositionMatrix.PositionEntry startingEntry = (isStart) ?
                __instance.SafePosMatrix.Entries_SupplyPoints[curStandardIndex] : __instance.SafePosMatrix.Entries_HoldPoints[curStandardIndex];

            for (int i = 0; i < startingEntry.SafePositions_HoldPoints.Count; i++)
            {
                if (i != excludeHoldIndex && startingEntry.SafePositions_HoldPoints[i])
                {
                    float playerDist = Vector3.Distance(GM.CurrentPlayerBody.transform.position, __instance.HoldPoints[i].transform.position);
                    if (playerDist > minDist)
                    {
                        validLocations.Add(i);
                    }
                }
            }

            if (validLocations.Count < 1) return false;
            validLocations.Shuffle();

            TNH_Manager.SosigPatrolSquad squad = GeneratePatrol(__instance, validLocations[0], patrol, patrolIndex);
            __instance.m_patrolSquads.Add(squad);

            SetTimeTilPatrolCanSpawn(__instance, patrol);
            return false;
        }


        /// <summary>
        /// Spawns a patrol at the desired patrol point
        /// </summary>
        public static TNH_Manager.SosigPatrolSquad GeneratePatrol(TNH_Manager instance, int HoldPointStart, Patrol patrol, int patrolIndex)
        {
            TNHFrameworkLogger.Log($"Generating a patrol -- There are currently {instance.m_patrolSquads.Count} patrols active", TNHFrameworkLogger.LogType.TNH);

            List<int> list = [];

            // Add all hold points except starting point
            for (int i = 0; i < instance.HoldPoints.Count; i++)
            {
                if (i != HoldPointStart)
                    list.Add(i);
            }

            // Shuffle list, then insert starting point at beginning
            list.Shuffle();
            list.Insert(0, HoldPointStart);

            List<Vector3> PatrolPoints = [];

            // Create patrol path, limited to 6 points
            for (int i = 0; i < list.Count && i < 6; i++)
            {
                PatrolPoints.Add(instance.HoldPoints[list[i]].SpawnPoints_Sosigs_Defense.GetRandom<Transform>().position);
            }
            TNHFrameworkLogger.Log($"Patrol path is: {string.Join(", ", list.GetRange(0, Math.Min(list.Count, 6)).Select(x => x.ToString()).ToArray())}", TNHFrameworkLogger.LogType.TNH);

            List<Vector3> SpawnPoints = [];
            List<Vector3> ForwardVectors = [];

            foreach (Transform spawnPoint in instance.HoldPoints[HoldPointStart].SpawnPoints_Sosigs_Defense)
            {
                SpawnPoints.Add(spawnPoint.position);
                ForwardVectors.Add(spawnPoint.forward);
            }

            return GeneratePatrol(patrol, SpawnPoints, ForwardVectors, PatrolPoints, patrolIndex);
        }


        // TODO: To choose spawn based on IFF, we need to basically generate spawn points on our own in this method!
        public static TNH_Manager.SosigPatrolSquad GeneratePatrol(Patrol patrol, List<Vector3> SpawnPoints, List<Vector3> ForwardVectors, List<Vector3> PatrolPoints, int patrolIndex)
        {
            // If this is a boss, then we can only spawn it once, so add it to the list of spawned bosses
            if (patrol.IsBoss)
            {
                TNHFramework.SpawnedBossIndexes.Add(patrolIndex);
            }

            TNH_Manager.SosigPatrolSquad squad = new()
            {
                PatrolPoints = new List<Vector3>(PatrolPoints),
                IsPatrollingUp = true,
                ID_Leader = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.LeaderType],
                ID_Regular = (patrol.EnemyType.Count > 0) ? (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.EnemyType[0]] : SosigEnemyID.None,
                HoldPointStart = patrolIndex,  // Commandeering this to hold patrolIndex because it's not used anywhere
                IFF = patrol.IFFUsed,
                IndexOfNextSpawn = 0,
                NumLeftToSpawn = Mathf.Clamp(patrol.PatrolSize, 1, SpawnPoints.Count)
            };

            // If squad is set to swarm, the first point they path to should be the player's current position
            if (patrol.SwarmPlayer)
            {
                squad.PatrolPoints[0] = GM.CurrentPlayerBody.transform.position;
            }

            for (int i = 0; i < squad.NumLeftToSpawn; i++)
            {
                squad.SpawnPoints.Add(SpawnPoints[i]);
                squad.ForwardVectors.Add(ForwardVectors[i]);
            }

            return squad;
        }


        [HarmonyPatch(typeof(TNH_Manager), "GeneratePatrol")]
        [HarmonyPrefix]
        public static bool GeneratePatrolStub()
        {
            // We've replaced all calls to GeneratePatrol() with our own, so stub this out
            TNHFrameworkLogger.LogWarning("GeneratePatrolStub() called! This should have been overridden!");
            //throw new ArgumentException("GeneratePatrolStub called");  // DEBUG
            return false;
        }


        /////////////////////////////
        //PATCHES FOR SPAWNING SOSIGS
        /////////////////////////////

        [HarmonyPatch(typeof(Sosig), "ClearSosig")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static void ClearSosig(Sosig __instance)
        {
            SosigLinkLootWrapper lootWrapper = __instance.GetComponentInChildren<SosigLinkLootWrapper>();
            if (lootWrapper != null)
            {
                lootWrapper.dontDrop = !lootWrapper.shouldDropOnCleanup;
            }
        }

        public static Sosig SpawnEnemy(SosigEnemyTemplate template, TakeAndHoldCharacter character, Transform spawnLocation, TNH_Manager M, int IFF, bool isAssault, Vector3 pointOfInterest, bool allowAllWeapons)
        {
            return SpawnEnemy(template, character, spawnLocation.position, spawnLocation.rotation, M, IFF, isAssault, pointOfInterest, allowAllWeapons);
        }

        public static Sosig SpawnEnemy(SosigEnemyTemplate template, TakeAndHoldCharacter character, Vector3 spawnLocation, Quaternion spawnRotation, TNH_Manager M, int IFF, bool isAssault, Vector3 pointOfInterest, bool allowAllWeapons)
        {
            SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[template];

            TNHFrameworkLogger.Log("Spawning sosig: " + customTemplate.SosigEnemyID, TNHFrameworkLogger.LogType.TNH);

            //Fill out the sosig's config based on the difficulty
            SosigConfig config;

            if (M.AI_Difficulty == TNHModifier_AIDifficulty.Arcade && customTemplate.ConfigsEasy.Count > 0)
            {
                config = customTemplate.ConfigsEasy.GetRandom<SosigConfig>();
            }
            else if (customTemplate.Configs.Count > 0)
            {
                config = customTemplate.Configs.GetRandom<SosigConfig>();
            }
            else
            {
                TNHFrameworkLogger.LogError("Sosig did not have normal difficulty config when playing on normal difficulty! Not spawning this enemy!");
                return null;
            }

            // Create the sosig object
            GameObject sosigPrefab = UnityEngine.Object.Instantiate(IM.OD[customTemplate.SosigPrefabs.GetRandom<string>()].GetGameObject(), spawnLocation, spawnRotation);
            Sosig sosigComponent = sosigPrefab.GetComponentInChildren<Sosig>();

            sosigComponent.Configure(config.GetConfigTemplate());
            sosigComponent.SetIFF(IFF);

            //Set up the sosig's inventory
            sosigComponent.Inventory.Init();
            sosigComponent.Inventory.FillAllAmmo();
            sosigComponent.InitHands();

            //Equip the sosig's weapons
            if (customTemplate.WeaponOptions.Count > 0)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptions.GetRandom<string>()].GetGameObject();
                EquipSosigWeapon(sosigComponent, weaponPrefab, M.AI_Difficulty);
            }

            if (character.ForceAllAgentWeapons)
                allowAllWeapons = true;

            if (customTemplate.WeaponOptionsSecondary.Count > 0 && allowAllWeapons && customTemplate.SecondaryChance >= UnityEngine.Random.value)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptionsSecondary.GetRandom<string>()].GetGameObject();
                EquipSosigWeapon(sosigComponent, weaponPrefab, M.AI_Difficulty);
            }

            if (customTemplate.WeaponOptionsTertiary.Count > 0 && allowAllWeapons && customTemplate.TertiaryChance >= UnityEngine.Random.value)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptionsTertiary.GetRandom<string>()].GetGameObject();
                EquipSosigWeapon(sosigComponent, weaponPrefab, M.AI_Difficulty);
            }

            //Equip clothing to the sosig
            OutfitConfig outfitConfig = customTemplate.OutfitConfigs.GetRandom<OutfitConfig>();

            if (outfitConfig.Chance_Headwear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Headwear, sosigComponent.Links[0], outfitConfig.ForceWearAllHead);
            }

            if (outfitConfig.Chance_Facewear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Facewear, sosigComponent.Links[0], outfitConfig.ForceWearAllFace);
            }

            if (outfitConfig.Chance_Eyewear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Eyewear, sosigComponent.Links[0], outfitConfig.ForceWearAllEye);
            }

            if (outfitConfig.Chance_Torsowear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Torsowear, sosigComponent.Links[1], outfitConfig.ForceWearAllTorso);
            }

            if (outfitConfig.Chance_Pantswear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Pantswear, sosigComponent.Links[2], outfitConfig.ForceWearAllPants);
            }

            if (outfitConfig.Chance_Pantswear_Lower >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Pantswear_Lower, sosigComponent.Links[3], outfitConfig.ForceWearAllPantsLower);
            }

            if (outfitConfig.Chance_Backpacks >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Backpacks, sosigComponent.Links[1], outfitConfig.ForceWearAllBackpacks);
            }

            //Set up the sosig's orders
            if (isAssault)
            {
                sosigComponent.CurrentOrder = Sosig.SosigOrder.Assault;
                sosigComponent.FallbackOrder = Sosig.SosigOrder.Assault;
                sosigComponent.CommandAssaultPoint(pointOfInterest);
            }
            else
            {
                sosigComponent.CurrentOrder = Sosig.SosigOrder.Wander;
                sosigComponent.FallbackOrder = Sosig.SosigOrder.Wander;
                sosigComponent.CommandGuardPoint(pointOfInterest, true);
                sosigComponent.SetDominantGuardDirection(UnityEngine.Random.onUnitSphere);
            }
            sosigComponent.SetGuardInvestigateDistanceThreshold(25f);

            //Handle sosig dropping custom loot
            if (customTemplate.DroppedObjectPool != null && UnityEngine.Random.value < customTemplate.DroppedLootChance)
            {
                SosigLinkLootWrapper component = sosigComponent.Links[2].gameObject.AddComponent<SosigLinkLootWrapper>();
                component.M = M;
                component.character = character;
                component.shouldDropOnCleanup = !character.DisableCleanupSosigDrops;
                component.group = new(customTemplate.DroppedObjectPool);
                component.group.DelayedInit(character.GlobalObjectBlacklist);
            }

            return sosigComponent;
        }


        [HarmonyPatch(typeof(FVRPlayerBody), "SetOutfit")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool SetOutfitReplacement(FVRPlayerBody __instance, SosigEnemyTemplate tem)
        {
            if (__instance.m_sosigPlayerBody == null) return false;

            GM.Options.ControlOptions.MBClothing = tem.SosigEnemyID;
            if (tem.SosigEnemyID != SosigEnemyID.None)
            {
                if (tem.OutfitConfig.Count > 0 && LoadedTemplateManager.LoadedSosigsDict.ContainsKey(tem))
                {
                    OutfitConfig outfitConfig = LoadedTemplateManager.LoadedSosigsDict[tem].OutfitConfigs.GetRandom();

                    foreach (GameObject item in __instance.m_sosigPlayerBody.m_curClothes)
                    {
                        UnityEngine.Object.Destroy(item);
                    }
                    __instance.m_sosigPlayerBody.m_curClothes.Clear();

                    if (outfitConfig.Chance_Headwear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Headwear, __instance.m_sosigPlayerBody.m_curClothes, __instance.m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllHead);
                    }

                    if (outfitConfig.Chance_Facewear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Facewear, __instance.m_sosigPlayerBody.m_curClothes, __instance.m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllFace);
                    }

                    if (outfitConfig.Chance_Eyewear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Eyewear, __instance.m_sosigPlayerBody.m_curClothes, __instance.m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllEye);
                    }

                    if (outfitConfig.Chance_Torsowear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Torsowear, __instance.m_sosigPlayerBody.m_curClothes, __instance.m_sosigPlayerBody.Sosig_Torso, outfitConfig.ForceWearAllTorso);
                    }

                    if (outfitConfig.Chance_Pantswear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Pantswear, __instance.m_sosigPlayerBody.m_curClothes, __instance.m_sosigPlayerBody.Sosig_Abdomen, outfitConfig.ForceWearAllPants);
                    }

                    if (outfitConfig.Chance_Pantswear_Lower >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Pantswear_Lower, __instance.m_sosigPlayerBody.m_curClothes, __instance.m_sosigPlayerBody.Sosig_Legs, outfitConfig.ForceWearAllPantsLower);
                    }

                    if (outfitConfig.Chance_Backpacks >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Backpacks, __instance.m_sosigPlayerBody.m_curClothes, __instance.m_sosigPlayerBody.Sosig_Torso, outfitConfig.ForceWearAllBackpacks);
                    }
                }
            }

            return false;
        }


        public static void EquipSosigWeapon(Sosig sosig, GameObject weaponPrefab, TNHModifier_AIDifficulty difficulty)
        {
            SosigWeapon weapon = UnityEngine.Object.Instantiate(weaponPrefab, sosig.transform.position + Vector3.up * 0.1f, sosig.transform.rotation).GetComponent<SosigWeapon>();
            weapon.SetAutoDestroy(true);
            weapon.O.SpawnLockable = false;

            //TNHFrameworkLogger.Log("Equipping sosig weapon: " + weapon.gameObject.name, TNHFrameworkLogger.LogType.TNH);

            //Equip the sosig weapon to the sosig
            sosig.ForceEquip(weapon);
            weapon.SetAmmoClamping(true);
            if (difficulty == TNHModifier_AIDifficulty.Arcade) weapon.FlightVelocityMultiplier = 0.3f;
        }

        public static void EquipSosigClothing(List<string> options, SosigLink link, bool wearAll)
        {
            if (wearAll)
            {
                foreach (string clothing in options)
                {
                    GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[clothing].GetGameObject(), link.transform.position, link.transform.rotation);
                    clothingObject.transform.SetParent(link.transform);
                    clothingObject.GetComponent<SosigWearable>().RegisterWearable(link);
                }
            }

            else
            {
                GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[options.GetRandom<string>()].GetGameObject(), link.transform.position, link.transform.rotation);
                clothingObject.transform.SetParent(link.transform);
                clothingObject.GetComponent<SosigWearable>().RegisterWearable(link);
            }
        }


        public static void EquipSosigClothing(List<string> options, List<GameObject> playerClothing, Transform link, bool wearAll)
        {
            if (wearAll)
            {
                foreach (string clothing in options)
                {
                    GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[clothing].GetGameObject(), link.position, link.rotation);

                    Component[] children = clothingObject.GetComponentsInChildren<Component>(true);
                    foreach (Component child in children)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("ExternalCamOnly");

                        if (!(child is Transform) && !(child is MeshFilter) && !(child is MeshRenderer))
                        {
                            UnityEngine.Object.Destroy(child);
                        }
                    }

                    playerClothing.Add(clothingObject);
                    clothingObject.transform.SetParent(link);
                }
            }

            else
            {
                GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[options.GetRandom<string>()].GetGameObject(), link.position, link.rotation);

                Component[] children = clothingObject.GetComponentsInChildren<Component>(true);
                foreach (Component child in children)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("ExternalCamOnly");

                    if (!(child is Transform) && !(child is MeshFilter) && !(child is MeshRenderer))
                    {
                        UnityEngine.Object.Destroy(child);
                    }
                }

                playerClothing.Add(clothingObject);
                clothingObject.transform.SetParent(link);
            }
        }

        [HarmonyPatch(typeof(Sosig), "BuffHealing_Invis")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool OverrideCloaking()
        {
            return !TNHFramework.PreventOutfitFunctionality;
        }


    }
}
