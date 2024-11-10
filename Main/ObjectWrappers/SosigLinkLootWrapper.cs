using BepInEx;
using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework
{
    public class SosigLinkLootWrapper : MonoBehaviour
    {
        public TNH_Manager M;
        public TakeAndHoldCharacter character;
        public EquipmentGroup group;
        public bool dontDrop = false;
        public bool shouldDropOnCleanup;

        void OnDestroy()
        {
            if (dontDrop)
                return;
            
            TNHFrameworkLogger.Log("Lootable link was destroyed!", TNHFrameworkLogger.LogType.TNH);

            List<EquipmentGroup> selectedGroups = group.GetSpawnedEquipmentGroups();
            string selectedItem = null;
            int spawnedItems = 0;

            foreach (EquipmentGroup selectedGroup in selectedGroups)
            {
                for (int itemIndex = 0; itemIndex < selectedGroup.ItemsToSpawn; itemIndex++)
                {
                    if (selectedGroup.IsCompatibleMagazine)
                    {
                        FVRObject mag = FirearmUtils.GetAmmoContainerForEquipped(selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity, character.GlobalObjectBlacklist, character.GetMagazineBlacklist());
                        if (mag != null)
                        {
                            selectedItem = mag.ItemID;
                        }
                        else
                        {
                            TNHFrameworkLogger.Log("Spawning nothing since group was compatible magazines, and could not find a compatible magazine for player", TNHFrameworkLogger.LogType.TNH);
                            //return;
                        }
                    }
                    else
                    {
                        var list = selectedGroup.GetObjects();

                        if (list.Count == 0)
                        {
                            TNHFrameworkLogger.Log("Spawning nothing since group was empty", TNHFrameworkLogger.LogType.TNH);
                        }
                        else
                        {
                            selectedItem = list.GetRandom();
                        }
                    }

                    // If list is empty, then there's nothing to spawn
                    if (selectedItem.IsNullOrWhiteSpace())
                        continue;

                    if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(selectedItem))
                    {
                        TNHFrameworkLogger.Log($"Spawning vault file {selectedItem}", TNHFrameworkLogger.LogType.TNH);

                        Transform newTransform = transform;
                        newTransform.position = transform.position + (Vector3.up * 0.1f * spawnedItems);
                        VaultSystem.SpawnVaultFile(LoadedTemplateManager.LoadedVaultFiles[selectedItem], newTransform, true, false, false, out _, Vector3.zero);
                    }
                    else if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(selectedItem))
                    {
                        TNHFrameworkLogger.Log($"Spawning legacy vault file {selectedItem}", TNHFrameworkLogger.LogType.TNH);
                        AnvilManager.Run(TNHFrameworkUtils.SpawnFirearm(LoadedTemplateManager.LoadedLegacyVaultFiles[selectedItem],
                            transform.position + (Vector3.up * 0.1f * spawnedItems), transform.rotation, M));
                    }
                    else
                    {
                        TNHFrameworkLogger.Log($"Spawning item {selectedItem}", TNHFrameworkLogger.LogType.TNH);
                        GameObject gameObject = Instantiate(IM.OD[selectedItem].GetGameObject(), transform.position + (Vector3.up * 0.1f * spawnedItems), transform.rotation);
                        M.AddObjectToTrackedList(gameObject);
                    }

                    spawnedItems += 1;
                }
            }
        }
    }
}
