﻿using FistVR;
using System.Collections.Generic;
using System.Linq;
using TNHFramework.ObjectTemplates;
using UnityEngine;

namespace TNHFramework.Utilities
{
    static class FirearmUtils
    {

		// Returns a list of magazines, clips, or speedloaders compatible with the firearm, and also within any of the optional criteria
		public static List<FVRObject> GetCompatibleAmmoContainers(FVRObject firearm, int minCapacity = 0, int maxCapacity = 9999, bool smallestIfEmpty = true, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
		{
			// Refresh the FVRObject to have data directly from object dictionary
			firearm = IM.OD[firearm.ItemID];

			// If the max capacity is zero or negative, we iterpret that as no limit on max capacity
			if (maxCapacity <= 0) maxCapacity = 9999;

			// Create a list containing all compatible ammo containers
			List<FVRObject> compatibleContainers = [];
			if (firearm.CompatibleSpeedLoaders != null)
				compatibleContainers.AddRange(firearm.CompatibleSpeedLoaders);

			// Go through each magazine and add compatible ones
			foreach (FVRObject magazine in firearm.CompatibleMagazines)
			{
				if (blacklist != null && !blacklist.IsMagazineAllowed(magazine.ItemID))
				{
					continue;
				}
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(magazine.ItemID))
                {
                    continue;
                }
                else if (magazine.MagazineCapacity < minCapacity || magazine.MagazineCapacity > maxCapacity)
				{
					continue;
				}

				compatibleContainers.Add(magazine);
			}

			// Go through each magazine and add compatible ones
			foreach (FVRObject clip in firearm.CompatibleClips)
			{
				if (blacklist != null && !blacklist.IsClipAllowed(clip.ItemID))
				{
					continue;
				}
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(clip.ItemID))
                {
                    continue;
                }
                else if (clip.MagazineCapacity < minCapacity || clip.MagazineCapacity > maxCapacity)
				{
					continue;
				}

				compatibleContainers.Add(clip);
			}

			// If the resulting list is empty, and smallestIfEmpty is true, add the smallest capacity magazine to the list
			if (compatibleContainers.Count == 0 && smallestIfEmpty && firearm.CompatibleMagazines != null)
			{
				FVRObject magazine = GetSmallestCapacityMagazine(firearm.CompatibleMagazines, globalObjectBlacklist);
				if (magazine != null)
					compatibleContainers.Add(magazine);
			}

			return compatibleContainers;
		}




		public static List<FVRObject> GetCompatibleMagazines(FVRObject firearm, int minCapacity = 0, int maxCapacity = 9999, bool smallestIfEmpty = true, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
		{
			// Refresh the FVRObject to have data directly from object dictionary
			firearm = IM.OD[firearm.ItemID];

			// If the max capacity is zero or negative, we iterpret that as no limit on max capacity
			if (maxCapacity <= 0)
				maxCapacity = 9999;

			// Create a list containing all compatible ammo containers
			List<FVRObject> compatibleMagazines = [];
			if (firearm.CompatibleMagazines != null)
				compatibleMagazines.AddRange(firearm.CompatibleMagazines);

			// Go through these containers and remove any that don't fit given criteria
			for (int i = compatibleMagazines.Count - 1; i >= 0; i--)
			{
				if (blacklist != null && !blacklist.IsMagazineAllowed(compatibleMagazines[i].ItemID))
				{
					compatibleMagazines.RemoveAt(i);
				}
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(compatibleMagazines[i].ItemID))
                {
                    compatibleMagazines.RemoveAt(i);
                }
                else if (compatibleMagazines[i].MagazineCapacity < minCapacity || compatibleMagazines[i].MagazineCapacity > maxCapacity)
				{
					compatibleMagazines.RemoveAt(i);
				}
			}

			// If the resulting list is empty, and smallestIfEmpty is true, add the smallest capacity magazine to the list
			if (compatibleMagazines.Count == 0 && smallestIfEmpty && firearm.CompatibleMagazines is not null)
			{
				FVRObject magazine = GetSmallestCapacityMagazine(firearm.CompatibleMagazines, globalObjectBlacklist, blacklist);
				if (magazine != null)
					compatibleMagazines.Add(magazine);
			}

			return compatibleMagazines;
		}


		public static List<FVRObject> GetCompatibleClips(FVRObject firearm, int minCapacity = 0, int maxCapacity = 9999, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
		{
			// Refresh the FVRObject to have data directly from object dictionary
			firearm = IM.OD[firearm.ItemID];

			// If the max capacity is zero or negative, we iterpret that as no limit on max capacity
			if (maxCapacity <= 0) maxCapacity = 9999;

			// Create a list containing all compatible ammo containers
			List<FVRObject> compatibleClips = [];
			if (firearm.CompatibleClips != null)
				compatibleClips.AddRange(firearm.CompatibleClips);

			// Go through these containers and remove any that don't fit given criteria
			for (int i = compatibleClips.Count - 1; i >= 0; i--)
			{
				if (blacklist != null && !blacklist.IsClipAllowed(compatibleClips[i].ItemID))
				{
					compatibleClips.RemoveAt(i);
				}
				else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(compatibleClips[i].ItemID))
				{
                    compatibleClips.RemoveAt(i);
                }
                else if (compatibleClips[i].MagazineCapacity < minCapacity || compatibleClips[i].MagazineCapacity > maxCapacity)
				{
					compatibleClips.RemoveAt(i);
				}
			}

			return compatibleClips;
		}


		public static List<FVRObject> GetCompatibleRounds(FVRObject firearm, List<TagEra> eras, List<TagSet> sets, List<string> globalBulletBlacklist = null, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
		{
			return GetCompatibleRounds(firearm, eras.Select(o => (FVRObject.OTagEra)o).ToList(), sets.Select(o => (FVRObject.OTagSet)o).ToList(), globalBulletBlacklist, globalObjectBlacklist, blacklist);
		}


		public static List<FVRObject> GetCompatibleRounds(FVRObject firearm, List<FVRObject.OTagEra> eras, List<FVRObject.OTagSet> sets, List<string> globalBulletBlacklist = null, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
		{
			// Refresh the FVRObject to have data directly from object dictionary
			firearm = IM.OD[firearm.ItemID];

			// Create a list containing all compatible ammo containers
			List<FVRObject> compatibleRounds = [];
			if (firearm.CompatibleSingleRounds != null)
				compatibleRounds.AddRange(firearm.CompatibleSingleRounds);

			// Go through these containers and remove any that don't fit given criteria
			for (int i = compatibleRounds.Count - 1; i >= 0; i--)
			{
				if (blacklist != null && !blacklist.IsRoundAllowed(compatibleRounds[i].ItemID))
				{
					compatibleRounds.RemoveAt(i);
				}
				else if (globalBulletBlacklist != null && globalBulletBlacklist.Contains(compatibleRounds[i].ItemID))
                {
					compatibleRounds.RemoveAt(i);
                }
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(compatibleRounds[i].ItemID))
                {
                    compatibleRounds.RemoveAt(i);
                }
                else if (!eras.Contains(compatibleRounds[i].TagEra) || !sets.Contains(compatibleRounds[i].TagSet))
                {
					compatibleRounds.RemoveAt(i);
                }
			}

			foreach (KeyValuePair<FireArmRoundClass, FVRFireArmRoundDisplayData.DisplayDataClass> dogshit in AM.STypeDic[firearm.RoundType])
			{
				if (compatibleRounds.Contains(dogshit.Value.ObjectID) && dogshit.Value.Cost > 0)
				{
					compatibleRounds.Remove(dogshit.Value.ObjectID);
				}
			}

			return compatibleRounds;
		}


		// Returns the smallest capacity magazine from the given list of magazine FVRObjects
		public static FVRObject GetSmallestCapacityMagazine(List<FVRObject> magazines, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
		{
			if (magazines == null || magazines.Count == 0)
				return null;

			// This was done with a list because whenever there are multiple smallest magazines of the same size, we want to return a random one from those options
			List<FVRObject> smallestMagazines = [];

			foreach (FVRObject magazine in magazines)
			{
				if (blacklist != null && !blacklist.IsMagazineAllowed(magazine.ItemID))
				{
					continue;
				}
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(magazine.ItemID))
                {
                    continue;
                }
                else if (smallestMagazines.Count == 0)
				{
					smallestMagazines.Add(magazine);
				}
				// If we find a new smallest mag, clear the list and add the new smallest
				else if (magazine.MagazineCapacity < smallestMagazines[0].MagazineCapacity)
				{
					smallestMagazines.Clear();
					smallestMagazines.Add(magazine);
				}
				// If the magazine is the same capacity as current smallest, add it to the list
				else if (magazine.MagazineCapacity == smallestMagazines[0].MagazineCapacity)
				{
					smallestMagazines.Add(magazine);
				}
			}

			if (smallestMagazines.Count == 0)
				return null;

			// Return a random magazine from the smallest
			return smallestMagazines.GetRandom();
		}



		// Returns the smallest capacity magazine that is compatible with the given firearm
		public static FVRObject GetSmallestCapacityMagazine(FVRObject firearm, List<string> globalObjectBlacklist = null, MagazineBlacklistEntry blacklist = null)
		{
			// Refresh the FVRObject to have data directly from object dictionary
			firearm = IM.OD[firearm.ItemID];

			return GetSmallestCapacityMagazine(firearm.CompatibleMagazines, globalObjectBlacklist, blacklist);
		}



		// Returns true if the given FVRObject has any compatible rounds, clips, magazines, or speedloaders
		public static bool FVRObjectHasAmmoObject(FVRObject item)
		{
			if (item == null) return false;

			// Refresh the FVRObject to have data directly from object dictionary
			item = IM.OD[item.ItemID];

			return (item.CompatibleSingleRounds != null && item.CompatibleSingleRounds.Count != 0) || (item.CompatibleClips != null && item.CompatibleClips.Count > 0) || (item.CompatibleMagazines != null && item.CompatibleMagazines.Count > 0) || (item.CompatibleSpeedLoaders != null && item.CompatibleSpeedLoaders.Count != 0);
		}


		// Returns true if the given FVRObject has any compatible clips, magazines, or speedloaders
		public static bool FVRObjectHasAmmoContainer(FVRObject item)
		{
			if (item == null) return false;

			// Refresh the FVRObject to have data directly from object dictionary
			item = IM.OD[item.ItemID];

			return (item.CompatibleClips != null && item.CompatibleClips.Count > 0) || (item.CompatibleMagazines != null && item.CompatibleMagazines.Count > 0) || (item.CompatibleSpeedLoaders != null && item.CompatibleSpeedLoaders.Count != 0);
		}




		// Returns the next largest magazine when compared to the current magazine. Only magazines from the possibleMagazines list are considered as next largest magazine candidates
		public static FVRObject GetNextHighestCapacityMagazine(FVRObject currentMagazine, List<string> globalObjectBlacklist = null, List<string> blacklistedMagazines = null)
		{
			currentMagazine = IM.OD[currentMagazine.ItemID];

			if (!IM.CompatMags.ContainsKey(currentMagazine.MagazineType))
			{
				TNHTweakerLogger.LogError($"TNHTWEAKER -- magazine type for ({currentMagazine.ItemID}) is not in compatible magazines dictionary! Will return null");
				return null;
			}

			// We make this a list so that when several next largest mags have the same capacity, we can return a random magazine from that selection
			List<FVRObject> nextLargestMagazines = [];
			
            foreach (FVRObject magazine in IM.CompatMags[currentMagazine.MagazineType])
            {
				if (blacklistedMagazines != null && blacklistedMagazines.Contains(magazine.ItemID))
				{
					continue;
				}
                else if (globalObjectBlacklist != null && globalObjectBlacklist.Contains(magazine.ItemID))
                {
                    continue;
                }
                else if (nextLargestMagazines.Count == 0 && magazine.MagazineCapacity > currentMagazine.MagazineCapacity)
				{
					nextLargestMagazines.Add(magazine);
					continue;
				}
				else if (nextLargestMagazines.Count > 0 && magazine.MagazineCapacity > currentMagazine.MagazineCapacity && magazine.MagazineCapacity < nextLargestMagazines[0].MagazineCapacity)
				{
					nextLargestMagazines.Clear();
					nextLargestMagazines.Add(magazine);
					continue;
				}
				else if (nextLargestMagazines.Count > 0 && magazine.MagazineCapacity == nextLargestMagazines[0].MagazineCapacity)
				{
					nextLargestMagazines.Add(magazine);
				}
            }

			if (nextLargestMagazines.Count > 0)
				return nextLargestMagazines.GetRandom();

			return null;
		}




		// Returns a list of FVRPhysicalObjects for items that are either in the players hand, or in one of the players quickbelt slots. This also includes any items in a players backpack if they are wearing one
		public static List<FVRPhysicalObject> GetEquippedItems()
		{
			List<FVRPhysicalObject> heldItems = [];

			FVRInteractiveObject rightHandObject = GM.CurrentMovementManager.Hands[0].CurrentInteractable;
			FVRInteractiveObject leftHandObject = GM.CurrentMovementManager.Hands[1].CurrentInteractable;

			// Get any items in the players hands
			if (rightHandObject is FVRPhysicalObject)
			{
				heldItems.Add((FVRPhysicalObject)rightHandObject);
			}

			if (leftHandObject is FVRPhysicalObject)
			{
				heldItems.Add((FVRPhysicalObject)leftHandObject);
			}

			// Get any items on the players body
			foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QuickbeltSlots)
			{
				if (slot.CurObject is not null && slot.CurObject.ObjectWrapper is not null)
				{
					heldItems.Add(slot.CurObject);
				}

				// If the player has a backpack on, we should search through that as well
				if (slot.CurObject is PlayerBackPack && ((PlayerBackPack)slot.CurObject).ObjectWrapper is not null)
				{
					foreach (FVRQuickBeltSlot backpackSlot in GM.CurrentPlayerBody.QuickbeltSlots)
					{
						if (backpackSlot.CurObject is not null)
						{
							heldItems.Add(backpackSlot.CurObject);
						}
					}
				}
			}

			return heldItems;
		}



		// Returns a list of FVRObjects for all of the items that are equipped on the player. Items without a valid FVRObject are excluded. There may also be duplicate entries if the player has identical items equipped
		public static List<FVRObject> GetEquippedFVRObjects()
		{
			List<FVRObject> equippedFVRObjects = [];

			foreach (FVRPhysicalObject item in GetEquippedItems())
			{
				if (item.ObjectWrapper is null) continue;

				equippedFVRObjects.Add(item.ObjectWrapper);
			}

			return equippedFVRObjects;
		}





		// Returns a random magazine, clip, or speedloader that is compatible with one of the player's equipped items
		public static FVRObject GetAmmoContainerForEquipped(int minCapacity = 0, int maxCapacity = 9999, List<string> globalObjectBlacklist = null, Dictionary<string, MagazineBlacklistEntry> blacklist = null)
		{
			List<FVRObject> heldItems = GetEquippedFVRObjects();

			// Interpret -1 as having no max capacity
			if (maxCapacity == -1) maxCapacity = 9999;

			// Go through and remove any items that have no ammo containers
			for (int i = heldItems.Count - 1; i >= 0; i--)
			{
				if (!FVRObjectHasAmmoContainer(heldItems[i]))
				{
					heldItems.RemoveAt(i);
				}
			}

			// Now go through all items that do have ammo containers, and try to get an ammo container for one of them
			heldItems.Shuffle();
			foreach (FVRObject item in heldItems)
			{
				MagazineBlacklistEntry blacklistEntry = null;
				if (blacklist != null && blacklist.ContainsKey(item.ItemID))
					blacklistEntry = blacklist[item.ItemID];

				List<FVRObject> containers = GetCompatibleAmmoContainers(item, minCapacity, maxCapacity, false, globalObjectBlacklist, blacklistEntry);
				if (containers.Count > 0)
					return containers.GetRandom();
			}

			return null;
		}




		// Returns a list of all attached objects on the given firearm. This includes attached magazines
		public static List<FVRPhysicalObject> GetAllAttachedObjects(FVRFireArm fireArm, bool includeSelf = false)
		{
			List<FVRPhysicalObject> detectedObjects = [];

			if (includeSelf)
			{
				detectedObjects.Add(fireArm);
			}

			if (fireArm.Magazine is not null && !fireArm.Magazine.IsIntegrated && fireArm.Magazine.ObjectWrapper is not null)
			{
				detectedObjects.Add(fireArm.Magazine);
			}

			foreach (FVRFireArmAttachment attachment in fireArm.Attachments)
			{
				if (attachment.ObjectWrapper is not null) detectedObjects.Add(attachment);
			}

			return detectedObjects;
		}

		/*
		public static List<FVRObject> GetLoadedFVRObjectsFromTemplateList(List<AmmoObjectDataTemplate> items)
        {
			List<FVRObject> loadedItems = new List<FVRObject>();

			foreach(AmmoObjectDataTemplate item in items)
            {
				if (IM.OD.ContainsKey(item.ObjectID)) loadedItems.Add(IM.OD[item.ObjectID]);
            }

			return loadedItems;
        }
		*/

		public static FVRFireArmMagazine SpawnDuplicateMagazine(FVRFireArmMagazine magazine, Vector3 position, Quaternion rotation)
        {
            FVRObject objectWrapper = magazine.ObjectWrapper;
			GameObject gameObject = UnityEngine.Object.Instantiate(objectWrapper.GetGameObject(), position, rotation);
			FVRFireArmMagazine component = gameObject.GetComponent<FVRFireArmMagazine>();
			for (int i = 0; i < Mathf.Min(magazine.LoadedRounds.Length, component.LoadedRounds.Length); i++)
			{
				if (magazine.LoadedRounds[i] != null && magazine.LoadedRounds[i].LR_Mesh != null)
				{
					component.LoadedRounds[i].LR_Class = magazine.LoadedRounds[i].LR_Class;
					component.LoadedRounds[i].LR_Mesh = magazine.LoadedRounds[i].LR_Mesh;
					component.LoadedRounds[i].LR_Material = magazine.LoadedRounds[i].LR_Material;
					component.LoadedRounds[i].LR_ObjectWrapper = magazine.LoadedRounds[i].LR_ObjectWrapper;
				}
			}
			component.m_numRounds = magazine.m_numRounds;
			component.UpdateBulletDisplay();

			return component;
		}


		public static Speedloader SpawnDuplicateSpeedloader(Speedloader speedloader, Vector3 position, Quaternion rotation)
		{
			FVRObject objectWrapper = speedloader.ObjectWrapper;
			GameObject gameObject = UnityEngine.Object.Instantiate(objectWrapper.GetGameObject(), position, rotation);
			Speedloader component = gameObject.GetComponent<Speedloader>();
			for (int i = 0; i < speedloader.Chambers.Count; i++)
			{
				if (speedloader.Chambers[i].IsLoaded)
				{
					component.Chambers[i].Load(speedloader.Chambers[i].LoadedClass, false);
				}
				else
				{
					component.Chambers[i].Unload();
				}
			}

			return component;
		}
	}
}
