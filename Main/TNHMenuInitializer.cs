using FistVR;
using HarmonyLib;
using MagazinePatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TNHFramework
{
    public static class TNHMenuInitializer
    {

        public static bool TNHInitialized = false;
        public static bool MagazineCacheFailed = false;
        public static List<TNH_CharacterDef> SavedCharacters;

        public static IEnumerator InitializeTNHMenuAsync(string path, Text progressText, Text itemsText, SceneLoader hotdog, List<TNH_UIManager.CharacterCategory> Categories, TNH_CharacterDatabase CharDatabase, TNH_UIManager instance, bool outputFiles)
        {
            hotdog.gameObject.SetActive(false);

            bool isOtherLoaderLoaded;
            bool isMagPatcherLoaded = false;
            try
            {
                PokeOtherloader();
                isOtherLoaderLoaded = true;
            }
            catch
            {
                isOtherLoaderLoaded = false;
                TNHFrameworkLogger.LogWarning("OtherLoader not found. If you are using OtherLoader, please ensure you have version 0.1.6 or later!");
            }

            // First thing we want to do is wait for all asset bundles to be loaded in
            float itemLoadProgress = 0;
            do
            {
                yield return null;
                itemLoadProgress = AsyncLoadMonitor.GetProgress();

                if (isOtherLoaderLoaded)
                {
                    itemLoadProgress = Mathf.Min(itemLoadProgress, GetOtherLoaderProgress());
                    itemsText.text = GetLoadingItems();
                }

                progressText.text = $"LOADING ITEMS : {itemLoadProgress * 100:0.0}%";
            }
            while (itemLoadProgress < 1);

            try
            {
                PokeMagPatcher();
                isMagPatcherLoaded = true;
                TNHFrameworkLogger.LogWarning("MagazinePatcher is detected.");
            }
            catch
            {
                isMagPatcherLoaded = false;
                TNHFrameworkLogger.LogWarning("MagazinePatcher not found.");
            }

            // Now we wait for magazine caching to be done
            if (isMagPatcherLoaded)
            {
                float cachingProgress;
                do
                {
                    yield return null;

                    cachingProgress = PokeMagPatcher();
                    itemsText.text = GetMagPatcherCacheLog();
                    progressText.text = $"CACHING ITEMS : {cachingProgress * 100:0.0}%";
                }
                while (cachingProgress < 1);
            }
            else if (TNHFramework.InternalMagPatcher.Value) // Honey, we have Magazine Patcher at home.
            {
                TNHFrameworkLogger.Log($"[{DateTime.Now:HH:mm:ss}] Internal Mag Patcher started!", TNHFrameworkLogger.LogType.General);
                InternalMagPatcher();
                TNHFrameworkLogger.Log($"[{DateTime.Now:HH:mm:ss}] Internal Mag Patcher finished!", TNHFrameworkLogger.LogType.General);
            }

            //Now perform final steps of loading characters
            LoadTNHTemplates(CharDatabase);
            SavedCharacters = CharDatabase.Characters;

            if (outputFiles)
            {
                CreateTNHFiles(path);
            }

            // RefreshTNHUI(instance, Categories, CharDatabase);

            itemsText.text = "";
            progressText.text = "";
            hotdog.gameObject.SetActive(true);
            TNHInitialized = true;
        }




        public static void InternalMagPatcher()
        {
            List<FVRObject> gunsToIterate = [];

            // *violent screaming*
            foreach (KeyValuePair<string, FVRObject> item in IM.OD)
            {
                if (item.Value.Category == FVRObject.ObjectCategory.Firearm)
                {
                    gunsToIterate.Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.Cartridge)
                {
                    if (!TNHFramework.CartridgeDictionary.ContainsKey(item.Value.RoundType))
                        TNHFramework.CartridgeDictionary.Add(item.Value.RoundType, []);

                    TNHFramework.CartridgeDictionary[item.Value.RoundType].Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.Magazine)
                {
                    if (!TNHFramework.MagazineDictionary.ContainsKey(item.Value.MagazineType))
                        TNHFramework.MagazineDictionary.Add(item.Value.MagazineType, []);

                    TNHFramework.MagazineDictionary[item.Value.MagazineType].Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.Clip)
                {
                    if (!TNHFramework.StripperDictionary.ContainsKey(item.Value.ClipType))
                        TNHFramework.StripperDictionary.Add(item.Value.ClipType, []);

                    TNHFramework.StripperDictionary[item.Value.ClipType].Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.SpeedLoader)
                {
                    if (!TNHFramework.SpeedloaderDictionary.ContainsKey(item.Value.RoundType))
                        TNHFramework.SpeedloaderDictionary.Add(item.Value.RoundType, []);

                    // Metadata fix
                    if (item.Value.ItemID == "Speedloader12gauge_5Shot")
                        item.Value.MagazineCapacity = 5;

                    TNHFramework.SpeedloaderDictionary[item.Value.RoundType].Add(item.Value);
                }
            }

            foreach (FVRObject firearm in gunsToIterate)
            {
                FVRFireArm firearmComp = null;
                FireArmClipType clipType = firearm.ClipType;
                FireArmRoundType roundType = firearm.RoundType;
                FireArmMagazineType magazineType = firearm.MagazineType;
                int magazineCapacity = firearm.MagazineCapacity;

                // If the data is mostly zeroes, get the FVRFireArm component
                if (!ValidFireArm(roundType, clipType, magazineType, 0))
                {
                    // Some muzzle loaded vanilla guns should be skipped
                    if (!firearm.IsModContent && firearm.TagFirearmAction == FVRObject.OTagFirearmAction.OpenBreach && firearm.TagFirearmFeedOption.Contains(FVRObject.OTagFirearmFeedOption.BreachLoad))
                        continue;

                    TNHFrameworkLogger.Log($"Loading firearm {firearm.DisplayName} [Mod = {firearm.IsModContent}]", TNHFrameworkLogger.LogType.General);

                    GameObject gameObject = firearm.GetGameObject();
                    if (gameObject != null)
                        firearmComp = gameObject.GetComponent<FVRFireArm>();

                    if (firearmComp != null)
                    {
                        roundType = firearmComp.RoundType;
                        magazineType = firearmComp.MagazineType;
                        clipType = firearmComp.ClipType;
                    }
                }

                // If it's still mostly zeroes, skip it, otherwise stuff like the Graviton Beamer gets .22LR ammo
                if (!ValidFireArm(roundType, clipType, magazineType, magazineCapacity))
                {
                    TNHFrameworkLogger.Log($"Firearm {firearm.DisplayName} skipped!", TNHFrameworkLogger.LogType.General);
                    continue;
                }

                if ((firearm.CompatibleSingleRounds == null || firearm.CompatibleSingleRounds.Count == 0) &&
                    TNHFramework.CartridgeDictionary.ContainsKey(roundType))
                {
                    TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new rounds of type {roundType}", TNHFrameworkLogger.LogType.General);
                    firearm.CompatibleSingleRounds = TNHFramework.CartridgeDictionary[roundType];
                }

                if ((firearm.CompatibleMagazines == null || firearm.CompatibleMagazines.Count == 0) &&
                    TNHFramework.MagazineDictionary.ContainsKey(magazineType) &&
                    magazineType != FireArmMagazineType.mNone)
                {
                    TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new magazines of type {magazineType}", TNHFrameworkLogger.LogType.General);
                    firearm.CompatibleMagazines = TNHFramework.MagazineDictionary[magazineType];
                }

                if ((firearm.CompatibleClips == null || firearm.CompatibleClips.Count == 0) &&
                    TNHFramework.StripperDictionary.ContainsKey(clipType) &&
                    clipType != FireArmClipType.None)
                {
                    TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new clips of type {clipType}", TNHFrameworkLogger.LogType.General);
                    firearm.CompatibleClips = TNHFramework.StripperDictionary[clipType];
                }

                if ((firearm.CompatibleSpeedLoaders == null || firearm.CompatibleSpeedLoaders.Count == 0) &&
                    firearm.TagFirearmAction == FVRObject.OTagFirearmAction.Revolver)
                {
                    if (firearmComp == null)
                    {
                        GameObject gameObject = firearm.GetGameObject();

                        if (gameObject != null)
                            firearmComp = gameObject.GetComponent<FVRFireArm>();
                    }

                    if (firearmComp != null)
                    {
                        roundType = firearmComp.RoundType;

                        // The Revolver component has the real chamber capacity
                        Revolver revolverComp = firearmComp.gameObject.GetComponent<Revolver>();
                        if (revolverComp != null)
                            magazineCapacity = revolverComp.Chambers.Length;
                    }

                    if (TNHFramework.SpeedloaderDictionary.ContainsKey(roundType))
                    {
                        foreach (FVRObject speedloader in TNHFramework.SpeedloaderDictionary[roundType])
                        {
                            if (speedloader.MagazineCapacity == magazineCapacity)
                            {
                                TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new speedloader of type {roundType}", TNHFrameworkLogger.LogType.General);
                                firearm.CompatibleSpeedLoaders.Add(speedloader);
                            }
                        }
                    }
                }
            }
        }

        public static bool ValidFireArm(FireArmRoundType roundType, FireArmClipType clipType, FireArmMagazineType magazineType, int magazineCapacity)
        {
            return roundType != FireArmRoundType.a22_LR || magazineType != FireArmMagazineType.mNone || magazineCapacity != 0 || clipType != FireArmClipType.None;
        }

        public static void PokeOtherloader()
        {
            OtherLoader.LoaderStatus.GetLoaderProgress();
            List<string> items = OtherLoader.LoaderStatus.LoadingItems;
        }

        public static float PokeMagPatcher()
        {
            return PatcherStatus.PatcherProgress;
        }

        public static string GetMagPatcherCacheLog()
        {
            return PatcherStatus.CacheLog;
        }

        public static float GetOtherLoaderProgress()
        {
            return OtherLoader.LoaderStatus.GetLoaderProgress();
        }

        public static string GetLoadingItems()
        {
            List<string> loading = OtherLoader.LoaderStatus.LoadingItems;

            for(int i = 0; i < loading.Count; i++)
            {
                string colorHex = ColorUtility.ToHtmlStringRGBA(new Color(0.5f, 0.5f, 0.5f, Mathf.Clamp(((float)loading.Count - i) / loading.Count, 0, 1)));
                loading[i] = "<color=#" + colorHex + ">Loading Assets (" + loading[i] + ")</color>";
            }

            loading.Reverse();

            return string.Join("\n", loading.ToArray());
        }


        public static void LoadTNHTemplates(TNH_CharacterDatabase CharDatabase)
        {
            TNHFrameworkLogger.Log("Performing TNH Initialization", TNHFrameworkLogger.LogType.General);

            //Load all of the default templates into our dictionaries
            TNHFrameworkLogger.Log("Adding default sosigs to template dictionary", TNHFrameworkLogger.LogType.General);
            LoadDefaultSosigs();
            TNHFrameworkLogger.Log("Adding default characters to template dictionary", TNHFrameworkLogger.LogType.General);
            LoadDefaultCharacters(CharDatabase.Characters);

            LoadedTemplateManager.DefaultIconSprites = TNHFrameworkUtils.GetAllIcons(LoadedTemplateManager.DefaultCharacters);

            TNHFrameworkLogger.Log("Delayed Init of default characters", TNHFrameworkLogger.LogType.General);
            InitCharacters(LoadedTemplateManager.DefaultCharacters, false);

            TNHFrameworkLogger.Log("Delayed Init of custom characters", TNHFrameworkLogger.LogType.General);
            InitCharacters(LoadedTemplateManager.CustomCharacters, true);

            TNHFrameworkLogger.Log("Delayed Init of custom sosigs", TNHFrameworkLogger.LogType.General);
            InitSosigs(LoadedTemplateManager.CustomSosigs);
        }



        public static void CreateTNHFiles(string path)
        {
            //Create files relevant for character creation
            TNHFrameworkLogger.Log("Creating character creation files", TNHFrameworkLogger.LogType.General);
            TNHFrameworkUtils.CreateDefaultSosigTemplateFiles(LoadedTemplateManager.DefaultSosigs, path);
            TNHFrameworkUtils.CreateDefaultCharacterFiles(LoadedTemplateManager.DefaultCharacters, path);
            TNHFrameworkUtils.CreateIconIDFile(path, LoadedTemplateManager.DefaultIconSprites.Keys.ToList());
            TNHFrameworkUtils.CreateObjectIDFile(path);
            TNHFrameworkUtils.CreateSosigIDFile(path);
            TNHFrameworkUtils.CreateJsonVaultFiles(path);
            TNHFrameworkUtils.CreateGeneratedTables(path);
            TNHFrameworkUtils.CreatePopulatedCharacterTemplate(path);
        }



        /// <summary>
        /// Loads all default sosigs into the template manager
        /// </summary>
        private static void LoadDefaultSosigs()
        {
            foreach (SosigEnemyTemplate sosig in ManagerSingleton<IM>.Instance.odicSosigObjsByID.Values)
            {
                LoadedTemplateManager.AddSosigTemplate(sosig);
            }
        }

        /// <summary>
        /// Loads all default characters into the template manager
        /// </summary>
        /// <param name="characters">A list of TNH characters</param>
        private static void LoadDefaultCharacters(List<TNH_CharacterDef> characters)
        {
            foreach (TNH_CharacterDef character in characters)
            {
                LoadedTemplateManager.AddCharacterTemplate(character);
            }
        }

        /// <summary>
        /// Performs a delayed init on the sent list of custom characters, and removes any characters that failed to init
        /// </summary>
        /// <param name="characters"></param>
        /// <param name="isCustom"></param>
        private static void InitCharacters(List<TakeAndHoldCharacter> characters, bool isCustom)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                TakeAndHoldCharacter character = characters[i];

                try
                {
                    character.DelayedInit(isCustom);
                }
                catch (Exception e)
                {
                    TNHFrameworkLogger.LogError("Failed to load character: " + character.DisplayName + ". Error Output:\n" + e.ToString());
                    characters.RemoveAt(i);
                    LoadedTemplateManager.LoadedCharactersDict.Remove(character.GetCharacter());
                    i -= 1;
                }
            }
        }

        /// <summary>
        /// Performs a delayed init on the sent list of sosigs. If a sosig fails to init, any character using that sosig will be removed
        /// </summary>
        /// <param name="sosigs"></param>
        private static void InitSosigs(List<SosigTemplate> sosigs)
        {
            for (int i = 0; i < sosigs.Count; i++)
            {
                SosigTemplate sosig = sosigs[i];

                try
                {
                    sosig.DelayedInit();
                }
                catch (Exception e)
                {
                    TNHFrameworkLogger.LogError("Failed to load sosig: " + sosig.DisplayName + ". Error Output:\n" + e.ToString());

                    //Find any characters that use this sosig, and remove them
                    for (int j = 0; j < LoadedTemplateManager.LoadedCharactersDict.Values.Count; j++)
                    {
                        //This is probably monsterously inefficient, but if you're at this point you're already fucked :)
                        KeyValuePair<TNH_CharacterDef, TakeAndHoldCharacter> value_pair = LoadedTemplateManager.LoadedCharactersDict.ToList()[j];

                        if (value_pair.Value.CharacterUsesSosig(sosig.SosigEnemyID))
                        {
                            TNHFrameworkLogger.LogError("Removing character that used removed sosig: " + value_pair.Value.DisplayName);
                            LoadedTemplateManager.LoadedCharactersDict.Remove(value_pair.Key);
                            j -= 1;
                        }
                    }
                }
            }
        }

        /*
        public static void RefreshTNHUI(TNH_UIManager instance, List<TNH_UIManager.CharacterCategory> Categories, TNH_CharacterDatabase CharDatabase)
        {
            TNHFrameworkLogger.Log("Refreshing TNH UI", TNHFrameworkLogger.LogType.General);

            //Load all characters into the UI
            foreach (TNH_CharacterDef character in LoadedTemplateManager.LoadedCharactersDict.Keys)
            {
                bool flag = false;
                foreach (TNH_UIManager.CharacterCategory category in Categories)
                {
                    if (category.CategoryName == LoadedTemplateManager.LoadedCharactersDict[character].DisplayName)
                    {
                        flag = true; 
                        break;
                    }
                }

                if (!flag)
                {
                    Categories.Insert(LoadedTemplateManager.LoadedCharactersDict[character].CategoryData.Priority, new TNH_UIManager.CharacterCategory()
                    {
                        CategoryName = LoadedTemplateManager.LoadedCharactersDict[character].DisplayName,
                        Characters = []
                    });
                }

                if (!Categories[(int)character.Group].Characters.Contains(character.CharacterID))
                {
                    Categories[(int)character.Group].Characters.Add(character.CharacterID);
                    CharDatabase.Characters.Add(character);
                }
            }

            //Update the UI
            Traverse instanceTraverse = Traverse.Create(instance);
            int selectedCategory = (int)instanceTraverse.Field("m_selectedCategory").GetValue();
            int selectedCharacter = (int)instanceTraverse.Field("m_selectedCharacter").GetValue();

            instanceTraverse.Method("SetSelectedCategory", selectedCategory).GetValue();
            instance.OBS_CharCategory.SetSelectedButton(selectedCharacter);
        }
        */
    }
}
