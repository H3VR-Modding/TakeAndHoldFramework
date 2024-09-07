﻿using FistVR;
using HarmonyLib;
using MagazinePatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            bool isMagPatcherLoaded;
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

            if (!isMagPatcherLoaded) // Honey, we have Magazine Patcher at home.
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
                        {
                            TNHFramework.CartridgeDictionary.Add(item.Value.RoundType, []);
                        }
                        TNHFramework.CartridgeDictionary[item.Value.RoundType].Add(item.Value);

                    }
                    else if (item.Value.Category == FVRObject.ObjectCategory.Magazine)
                    {
                        if (!TNHFramework.MagazineDictionary.ContainsKey(item.Value.MagazineType))
                        {
                            TNHFramework.MagazineDictionary.Add(item.Value.MagazineType, []);
                        }
                        TNHFramework.MagazineDictionary[item.Value.MagazineType].Add(item.Value);
                    }
                    else if (item.Value.Category == FVRObject.ObjectCategory.Clip)
                    {
                        if (!TNHFramework.StripperDictionary.ContainsKey(item.Value.ClipType))
                        {
                            TNHFramework.StripperDictionary.Add(item.Value.ClipType, []);
                        }
                        TNHFramework.StripperDictionary[item.Value.ClipType].Add(item.Value);
                    }
                    else if (item.Value.Category == FVRObject.ObjectCategory.SpeedLoader)
                    {
                        if (!TNHFramework.SpeedloaderDictionary.ContainsKey(item.Value.RoundType))
                        {
                            TNHFramework.SpeedloaderDictionary.Add(item.Value.RoundType, []);
                        }
                        TNHFramework.SpeedloaderDictionary[item.Value.RoundType].Add(item.Value);
                    }
                }

                foreach (FVRObject firearm in gunsToIterate)
                {
                    if (ValidFireArm(firearm) && (firearm.CompatibleSingleRounds == null || firearm.CompatibleSingleRounds.Count == 0) &&
                        TNHFramework.CartridgeDictionary.ContainsKey(firearm.RoundType))
                    {
                        TNHFrameworkLogger.Log("Given firearm " + firearm.DisplayName + " new rounds of type " + firearm.RoundType, TNHFrameworkLogger.LogType.General);
                        firearm.CompatibleSingleRounds = TNHFramework.CartridgeDictionary[firearm.RoundType];
                    }

                    if ((firearm.CompatibleMagazines == null || firearm.CompatibleMagazines.Count == 0) &&
                        TNHFramework.MagazineDictionary.ContainsKey(firearm.MagazineType) &&
                        firearm.MagazineType != FireArmMagazineType.mNone)
                    {
                        TNHFrameworkLogger.Log("Given firearm " + firearm.DisplayName + " new magazines of type " + firearm.MagazineType, TNHFrameworkLogger.LogType.General);
                        firearm.CompatibleMagazines = TNHFramework.MagazineDictionary[firearm.MagazineType];
                    }

                    if ((firearm.CompatibleClips == null || firearm.CompatibleClips.Count == 0) &&
                        TNHFramework.StripperDictionary.ContainsKey(firearm.ClipType) &&
                        firearm.ClipType != FireArmClipType.None)
                    {
                        TNHFrameworkLogger.Log("Given firearm " + firearm.DisplayName + " new clips of type " + firearm.ClipType, TNHFrameworkLogger.LogType.General);
                        firearm.CompatibleClips = TNHFramework.StripperDictionary[firearm.ClipType];
                    }

                    if (ValidFireArm(firearm) && (firearm.CompatibleSpeedLoaders == null || firearm.CompatibleSpeedLoaders.Count == 0) &&
                        TNHFramework.SpeedloaderDictionary.ContainsKey(firearm.RoundType) &&
                        firearm.TagFirearmAction == FVRObject.OTagFirearmAction.Revolver)
                    {
                        foreach (FVRObject speedloader in TNHFramework.SpeedloaderDictionary[firearm.RoundType])
                        {
                            if (speedloader.MagazineCapacity == firearm.MagazineCapacity)
                            {
                                TNHFrameworkLogger.Log("Given firearm " + firearm.DisplayName + " new speedloader of type " + firearm.RoundType, TNHFrameworkLogger.LogType.General);
                                firearm.CompatibleSpeedLoaders.Add(speedloader);
                            }
                        }
                    }
                }
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

            // Now we wait for magazine caching to be done
            if (isMagPatcherLoaded)
            {
                float cachingProgress;
                do
                {
                    yield return null;

                    cachingProgress = PokeMagPatcher();// PatcherStatus.PatcherProgress;
                    itemsText.text = GetMagPatcherCacheLog();// PatcherStatus.CacheLog;
                    progressText.text = $"CACHING ITEMS : {cachingProgress * 100:0.0}%";

                    /*
                    if (PatcherStatus.CachingFailed)
                    {
                        MagazineCacheFailed = true;
                        progressText.text = "CACHING FAILED! SEE ABOVE";
                        throw new Exception("Magazine Caching Failed!");
                    }
                    */
                }
                while (cachingProgress < 1);
            }

            // Now perform final steps of loading characters
            LoadTNHTemplates(CharDatabase);
            SavedCharacters = CharDatabase.Characters;

            if (outputFiles)
            {
                CreateTNHFiles(path);
            }

            RefreshTNHUI(instance, Categories, CharDatabase);

            itemsText.text = "";
            progressText.text = "";
            hotdog.gameObject.SetActive(true);
            TNHInitialized = true;
        }




        public static bool ValidFireArm(FVRObject firearm)
        {
            // If all of these values are zero, then it's probably not valid data
            return firearm.RoundType != FireArmRoundType.a22_LR || firearm.MagazineType != FireArmMagazineType.mNone || firearm.MagazineCapacity != 0 || firearm.ClipType != FireArmClipType.None;
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

            // Load all of the default templates into our dictionaries
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
            // Create files relevant for character creation
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



        // Loads all default sosigs into the template manager
        private static void LoadDefaultSosigs()
        {
            foreach (SosigEnemyTemplate sosig in ManagerSingleton<IM>.Instance.odicSosigObjsByID.Values)
            {
                LoadedTemplateManager.AddSosigTemplate(sosig);
            }
        }

        // Loads all default characters into the template manager
        private static void LoadDefaultCharacters(List<TNH_CharacterDef> characters)
        {
            foreach (TNH_CharacterDef character in characters)
            {
                LoadedTemplateManager.AddCharacterTemplate(character);
            }
        }

        // Performs a delayed init on the sent list of custom characters, and removes any characters that failed to init
        private static void InitCharacters(List<CustomCharacter> characters, bool isCustom)
        {
            for (int i = 0; i < characters.Count; i++)
            {
                CustomCharacter character = characters[i];

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

        // Performs a delayed init on the sent list of sosigs. If a sosig fails to init, any character using that sosig will be removed
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

                    // Find any characters that use this sosig, and remove them
                    for (int j = 0; j < LoadedTemplateManager.LoadedCharactersDict.Values.Count; j++)
                    {
                        // This is probably monsterously inefficient, but if you're at this point you're already fucked :)
                        KeyValuePair<TNH_CharacterDef, CustomCharacter> value_pair = LoadedTemplateManager.LoadedCharactersDict.ToList()[j];

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


        public static void RefreshTNHUI(TNH_UIManager instance, List<TNH_UIManager.CharacterCategory> Categories, TNH_CharacterDatabase CharDatabase)
        {
            TNHFrameworkLogger.Log("Refreshing TNH UI", TNHFrameworkLogger.LogType.General);

            // Load all characters into the UI
            foreach (TNH_CharacterDef character in LoadedTemplateManager.LoadedCharactersDict.Keys)
            {
                bool flag = false;
                foreach (TNH_UIManager.CharacterCategory category in Categories)
                {
                    if (category.CategoryName == LoadedTemplateManager.LoadedCharactersDict[character].CategoryData.Name)
                    {
                        flag = true; 
                        break;
                    }
                }

                if (!flag)
                {
                    Categories.Insert(LoadedTemplateManager.LoadedCharactersDict[character].CategoryData.Priority, new TNH_UIManager.CharacterCategory()
                    {
                        CategoryName = LoadedTemplateManager.LoadedCharactersDict[character].CategoryData.Name,
                        Characters = []
                    });
                }

                if (!Categories[(int)character.Group].Characters.Contains(character.CharacterID))
                {
                    Categories[(int)character.Group].Characters.Add(character.CharacterID);
                    CharDatabase.Characters.Add(character);
                }
            }

            // Update the UI
            Traverse instanceTraverse = Traverse.Create(instance);
            int selectedCategory = (int)instanceTraverse.Field("m_selectedCategory").GetValue();
            int selectedCharacter = (int)instanceTraverse.Field("m_selectedCharacter").GetValue();

            instanceTraverse.Method("SetSelectedCategory", selectedCategory).GetValue();
            instance.OBS_CharCategory.SetSelectedButton(selectedCharacter);
        }
    }
}
