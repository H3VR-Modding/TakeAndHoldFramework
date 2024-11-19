using ADepIn;
using BepInEx.Logging;
using FistVR;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TNHFramework.Main.ObjectWrappers;
using TNHFramework.ObjectTemplates.V1;
using TNHFramework.Patches;
using TNHFramework.Utilities;
using UnityEngine;
using Valve.Newtonsoft.Json;
using YamlDotNet.Serialization;
using static FistVR.TNH_HoldChallenge;

namespace TNHFramework.ObjectTemplates
{
    public abstract class BaseCustomCharacter
    {
        // Generic data for interacting with TNH Framework.
        public string ManagerType;
        public string DisplayName;
        public string Description;
        public CategoryInfo CategoryData;
        public string TableID;
    }

    public class TakeAndHoldCharacter : BaseCustomCharacter
    {
        public int StartingTokens;
        public bool ForceAllAgentWeapons;
        public bool ForceDisableOutfitFunctionality;
        public bool UsesPurchasePriceIncrement;
        public bool DisableCleanupSosigDrops;
        public List<TagEra> ValidAmmoEras = [];
        public List<TagSet> ValidAmmoSets = [];
        public List<string> GlobalObjectBlacklist = [];
        public List<string> GlobalAmmoBlacklist = [];
        public List<MagazineBlacklistEntry> MagazineBlacklist = [];

        public EquipmentGroup RequireSightTable;
        public StartingPoint StartRoom;
        public LoadoutEntry PrimaryWeapon;
        public LoadoutEntry SecondaryWeapon;
        public LoadoutEntry TertiaryWeapon;
        public LoadoutEntry PrimaryItem;
        public LoadoutEntry SecondaryItem;
        public LoadoutEntry TertiaryItem;
        public LoadoutEntry Shield;
        public List<EquipmentPool> EquipmentPools = [];
        public List<Level> Levels = [];
        public List<Level> LevelsEndless = [];

        [JsonIgnore]
        private TNH_CharacterDef character;

        [JsonIgnore]
        private Dictionary<string, MagazineBlacklistEntry> magazineBlacklistDict;

        public TakeAndHoldCharacter()
        {
            ValidAmmoEras = [];
            ValidAmmoSets = [];
            GlobalObjectBlacklist = [];
            GlobalAmmoBlacklist = [];
            MagazineBlacklist = [];
            RequireSightTable = new EquipmentGroup();
            PrimaryWeapon = new LoadoutEntry();
            SecondaryWeapon = new LoadoutEntry();
            TertiaryWeapon = new LoadoutEntry();
            PrimaryItem = new LoadoutEntry();
            SecondaryItem = new LoadoutEntry();
            TertiaryItem = new LoadoutEntry();
            Shield = new LoadoutEntry();
            EquipmentPools = [];
            Levels = [];
            LevelsEndless = [];
        }

        public TakeAndHoldCharacter(TNH_CharacterDef character)
        {
            ManagerType = "Classic";
            DisplayName = character.DisplayName;

            // CharacterGroup = (int)character.Group;
            TableID = character.TableID;
            StartingTokens = character.StartingTokens;
            ForceAllAgentWeapons = character.ForceAllAgentWeapons;
            Description = character.Description;
            UsesPurchasePriceIncrement = character.UsesPurchasePriceIncrement;
            ValidAmmoEras = character.ValidAmmoEras.Select(o => (TagEra)o).ToList();
            ValidAmmoSets = character.ValidAmmoSets.Select(o => (TagSet)o).ToList();
            GlobalObjectBlacklist = [];
            GlobalAmmoBlacklist = [];
            MagazineBlacklist = [];
            PrimaryWeapon = new LoadoutEntry(character.Weapon_Primary);
            SecondaryWeapon = new LoadoutEntry(character.Weapon_Secondary);
            TertiaryWeapon = new LoadoutEntry(character.Weapon_Tertiary);
            PrimaryItem = new LoadoutEntry(character.Item_Primary);
            SecondaryItem = new LoadoutEntry(character.Item_Secondary);
            TertiaryItem = new LoadoutEntry(character.Item_Tertiary);
            Shield = new LoadoutEntry(character.Item_Shield);

            RequireSightTable = new EquipmentGroup(character.RequireSightTable);

            EquipmentPools = character.EquipmentPool.Entries.Select(o => new EquipmentPool(o)).ToList();
            Levels = character.Progressions[0].Levels.Select(o => new Level(o)).ToList();
            LevelsEndless = character.Progressions_Endless[0].Levels.Select(o => new Level(o)).ToList();

            ForceDisableOutfitFunctionality = false;

            this.character = character;
        }

        public TakeAndHoldCharacter(V1.CustomCharacter character)
        {
            DisplayName = character.DisplayName;
            Description = character.Description;
            CategoryData = new CategoryInfo();
            switch (character.CharacterGroup)
            {
                case 0:
                    CategoryData.Name = "Daring Defaults";
                    break;

                case 1:
                    CategoryData.Name = "Wieners Through Time";
                    break;

                case 2:
                    CategoryData.Name = "Memetastic Meats";
                    break;

                case 3:
                    CategoryData.Name = "Competitive Casings";
                    break;
            }
            CategoryData.Priority = (int)character.CharacterGroup;
            TableID = character.TableID;
            StartingTokens = character.StartingTokens;
            ForceAllAgentWeapons = character.ForceAllAgentWeapons;
            ForceDisableOutfitFunctionality = character.ForceDisableOutfitFunctionality;
            UsesPurchasePriceIncrement = character.UsesPurchasePriceIncrement;
            DisableCleanupSosigDrops = character.DisableCleanupSosigDrops;
            ValidAmmoEras = character.ValidAmmoEras ?? [];
            ValidAmmoSets = character.ValidAmmoSets ?? [];
            GlobalObjectBlacklist = character.GlobalObjectBlacklist ?? [];
            GlobalAmmoBlacklist = character.GlobalAmmoBlacklist ?? [];
            MagazineBlacklist = character.MagazineBlacklist ?? [];

            RequireSightTable = new EquipmentGroup(character.RequireSightTable);
            PrimaryWeapon = new LoadoutEntry(character.PrimaryWeapon);
            SecondaryWeapon = new LoadoutEntry(character.SecondaryWeapon);
            TertiaryWeapon = new LoadoutEntry(character.TertiaryWeapon);
            PrimaryItem = new LoadoutEntry(character.PrimaryItem);
            SecondaryItem = new LoadoutEntry(character.SecondaryItem);
            TertiaryItem = new LoadoutEntry(character.TertiaryItem);
            Shield = new LoadoutEntry(character.Shield);

            EquipmentPools = [];
            foreach (V1.EquipmentPool oldPool in character.EquipmentPools)
            {
                EquipmentPools.Add(new EquipmentPool(oldPool));
            }

            Levels = [];
            LevelsEndless = [];

            foreach (V1.Level oldLevel in character.Levels)
            {
                Levels.Add(new Level(oldLevel));
            }

            foreach (V1.Level oldLevel in character.LevelsEndless)
            {
                LevelsEndless.Add(new Level(oldLevel));
            }
        }

        public void Validate()
        {
            // Fix any null values that came from the JSON file
            ValidAmmoEras ??= [];
            ValidAmmoSets ??= [];
            GlobalObjectBlacklist ??= [];
            GlobalAmmoBlacklist ??= [];

            MagazineBlacklist ??= [];
            foreach (MagazineBlacklistEntry entry in MagazineBlacklist)
            {
                entry.Validate();
            }

            RequireSightTable ??= new();
            RequireSightTable.Validate();

            PrimaryWeapon ??= new();
            PrimaryWeapon.Validate();

            SecondaryWeapon ??= new();
            SecondaryWeapon.Validate();

            TertiaryWeapon ??= new();
            TertiaryWeapon.Validate();

            PrimaryItem ??= new();
            PrimaryItem.Validate();

            SecondaryItem ??= new();
            SecondaryItem.Validate();

            TertiaryItem ??= new();
            TertiaryItem.Validate();

            Shield ??= new();
            Shield.Validate();

            EquipmentPools ??= [];
            foreach (EquipmentPool pool in EquipmentPools)
            {
                pool.Validate();
            }

            Levels ??= [];
            foreach (Level level in Levels)
            {
                level.Validate();
            }

            LevelsEndless ??= [];
            foreach (Level level in LevelsEndless)
            {
                level.Validate();
            }
        }

        public TNH_CharacterDef GetCharacter(int ID, Sprite thumbnail)
        {
            if (character == null)
            {
                ValidAmmoSets ??= [];
                ValidAmmoEras ??= [];

                character = (TNH_CharacterDef)ScriptableObject.CreateInstance(typeof(TNH_CharacterDef));
                character.DisplayName = DisplayName;
                character.CharacterID = (TNH_Char)ID;
                character.Group = (TNH_CharacterDef.CharacterGroup)CategoryData.Priority;
                character.TableID = TableID;
                character.StartingTokens = StartingTokens;
                character.ForceAllAgentWeapons = ForceAllAgentWeapons;
                character.Description = Description;
                character.UsesPurchasePriceIncrement = UsesPurchasePriceIncrement;
                character.ValidAmmoEras = ValidAmmoEras.Select(o => (FVRObject.OTagEra)o).ToList();
                character.ValidAmmoSets = ValidAmmoSets.Select(o => (FVRObject.OTagSet)o).ToList();
                character.Picture = thumbnail;
                character.Weapon_Primary = PrimaryWeapon.GetLoadoutEntry();
                character.Weapon_Secondary = SecondaryWeapon.GetLoadoutEntry();
                character.Weapon_Tertiary = TertiaryWeapon.GetLoadoutEntry();
                character.Item_Primary = PrimaryItem.GetLoadoutEntry();
                character.Item_Secondary = SecondaryItem.GetLoadoutEntry();
                character.Item_Tertiary = TertiaryItem.GetLoadoutEntry();
                character.Item_Shield = Shield.GetLoadoutEntry();

                character.Has_Weapon_Primary = PrimaryWeapon.PrimaryGroup != null || PrimaryWeapon.BackupGroup != null;
                character.Has_Weapon_Secondary = SecondaryWeapon.PrimaryGroup != null || SecondaryWeapon.BackupGroup != null;
                character.Has_Weapon_Tertiary = TertiaryWeapon.PrimaryGroup != null || TertiaryWeapon.BackupGroup != null;
                character.Has_Item_Primary = PrimaryItem.PrimaryGroup != null || PrimaryItem.BackupGroup != null;
                character.Has_Item_Secondary = SecondaryItem.PrimaryGroup != null || SecondaryItem.BackupGroup != null;
                character.Has_Item_Tertiary = TertiaryItem.PrimaryGroup != null || TertiaryItem.BackupGroup != null;
                character.Has_Item_Shield = Shield.PrimaryGroup != null || Shield.BackupGroup != null;

                character.RequireSightTable = RequireSightTable.GetObjectTableDef();
                character.EquipmentPool = (EquipmentPoolDef)ScriptableObject.CreateInstance(typeof(EquipmentPoolDef));
                character.EquipmentPool.Entries = EquipmentPools.Select(o => o.GetPoolEntry()).ToList();

                character.Progressions = [(TNH_Progression)ScriptableObject.CreateInstance(typeof(TNH_Progression))];
                character.Progressions[0].Levels = [];
                foreach (Level level in Levels)
                {
                    character.Progressions[0].Levels.Add(level.GetLevel());
                }


                character.Progressions_Endless = [(TNH_Progression)ScriptableObject.CreateInstance(typeof(TNH_Progression))];
                character.Progressions_Endless[0].Levels = [];
                foreach (Level level in LevelsEndless)
                {
                    character.Progressions_Endless[0].Levels.Add(level.GetLevel());
                }
            }

            return character;
        }


        public TNH_CharacterDef GetCharacter()
        {
            if (character == null)
            {
                TNHFrameworkLogger.LogError("Tried to get character, but it hasn't been initialized yet! Returning null! Character Name : " + DisplayName);
                return null;
            }

            return character;
        }


        public Dictionary<string, MagazineBlacklistEntry> GetMagazineBlacklist()
        {
            return magazineBlacklistDict;
        }


        public Level GetCurrentLevel(TNH_Progression.Level currLevel)
        {
            foreach (Level level in Levels)
            {
                if (level.GetLevel().Equals(currLevel))
                {
                    return level;
                }
            }

            foreach (Level level in LevelsEndless)
            {
                if (level.GetLevel().Equals(currLevel))
                {
                    return level;
                }
            }

            return null;
        }

        public bool CharacterUsesSosig(string id)
        {
            foreach (Level level in Levels)
            {
                if (level.LevelUsesSosig(id)) return true;
            }

            foreach (Level level in LevelsEndless)
            {
                if (level.LevelUsesSosig(id)) return true;
            }

            return false;
        }

        public void DelayedInit(bool isCustom)
        {
            TNHFrameworkLogger.Log("Delayed init of character: " + DisplayName, TNHFrameworkLogger.LogType.Character);

            TNHFrameworkLogger.Log("Init of Primary Weapon", TNHFrameworkLogger.LogType.Character);
            if (PrimaryWeapon != null && !PrimaryWeapon.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Primary starting weapon had no pools to spawn from, and will not spawn equipment!");
                character.Has_Weapon_Primary = false;
            }

            TNHFrameworkLogger.Log("Init of Secondary Weapon", TNHFrameworkLogger.LogType.Character);
            if (SecondaryWeapon != null && !SecondaryWeapon.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Secondary starting weapon had no pools to spawn from, and will not spawn equipment!");
                character.Has_Weapon_Secondary = false;
            }

            TNHFrameworkLogger.Log("Init of Tertiary Weapon", TNHFrameworkLogger.LogType.Character);
            if (TertiaryWeapon != null && !TertiaryWeapon.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Tertiary starting weapon had no pools to spawn from, and will not spawn equipment!");
                character.Has_Weapon_Tertiary = false;
            }

            TNHFrameworkLogger.Log("Init of Primary Item", TNHFrameworkLogger.LogType.Character);
            if (PrimaryItem != null && !PrimaryItem.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Primary starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Primary = false;
            }

            TNHFrameworkLogger.Log("Init of Secondary Item", TNHFrameworkLogger.LogType.Character);
            if (SecondaryItem != null && !SecondaryItem.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Secondary starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Secondary = false;
            }

            TNHFrameworkLogger.Log("Init of Tertiary Item", TNHFrameworkLogger.LogType.Character);
            if (TertiaryItem != null && !TertiaryItem.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Tertiary starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Tertiary = false;
            }

            TNHFrameworkLogger.Log("Init of Shield", TNHFrameworkLogger.LogType.Character);
            if (Shield != null && !Shield.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Shield starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Shield = false;
            }

            TNHFrameworkLogger.Log("Init of required sights table", TNHFrameworkLogger.LogType.Character);
            if (RequireSightTable != null && !RequireSightTable.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Required sight table was empty, guns will not spawn with required sights");
                RequireSightTable = null;
            }

            TNHFrameworkLogger.Log("Init of equipment pools", TNHFrameworkLogger.LogType.Character);
            magazineBlacklistDict = [];

            if (MagazineBlacklist != null)
            {
                foreach (MagazineBlacklistEntry entry in MagazineBlacklist)
                {
                    magazineBlacklistDict.Add(entry.FirearmID, entry);
                }
            }

            for (int i = 0; i < EquipmentPools.Count; i++)
            {
                EquipmentPool pool = EquipmentPools[i];
                if (!pool.DelayedInit(GlobalObjectBlacklist))
                {
                    TNHFrameworkLogger.LogWarning("Equipment pool had an empty table! Removing it so that it can't spawn!");
                    EquipmentPools.RemoveAt(i);
                    character.EquipmentPool.Entries.RemoveAt(i);
                    i -= 1;
                }
            }

            TNHFrameworkLogger.Log("Init of levels", TNHFrameworkLogger.LogType.Character);
            for (int i = 0; i < Levels.Count; i++)
            {
                Levels[i].DelayedInit(isCustom, i);
            }

            TNHFrameworkLogger.Log("Init of endless levels", TNHFrameworkLogger.LogType.Character);
            for (int i = 0; i < LevelsEndless.Count; i++)
            {
                LevelsEndless[i].DelayedInit(isCustom, i);
            }
        }

    }


    public class CategoryInfo
    {
        public string Name;
        public int Priority;
    }


    public class MagazineBlacklistEntry
    {
        public string FirearmID;

        public List<string> MagazineBlacklist = [];

        public List<string> MagazineWhitelist = [];

        public List<string> ClipBlacklist = [];

        public List<string> ClipWhitelist = [];

        public List<string> SpeedLoaderBlacklist = [];

        public List<string> SpeedLoaderWhitelist = [];

        public List<string> RoundBlacklist = [];

        public List<string> RoundWhitelist = [];

        public void Validate()
        {
            MagazineBlacklist ??= [];
            MagazineWhitelist ??= [];
            ClipBlacklist ??= [];
            ClipWhitelist ??= [];
            SpeedLoaderBlacklist ??= [];
            SpeedLoaderWhitelist ??= [];
            RoundBlacklist ??= [];
            RoundWhitelist ??= [];
        }

        public bool IsItemBlacklisted(string itemID)
        {
            return MagazineBlacklist.Contains(itemID) || ClipBlacklist.Contains(itemID) || RoundBlacklist.Contains(itemID) || SpeedLoaderBlacklist.Contains(itemID);
        }

        public bool IsMagazineAllowed(string itemID)
        {
            if (MagazineWhitelist.Count > 0 && !MagazineWhitelist.Contains(itemID))
            {
                return false;
            }

            if (MagazineBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }

        public bool IsClipAllowed(string itemID)
        {
            if (ClipWhitelist.Count > 0 && !ClipWhitelist.Contains(itemID))
            {
                return false;
            }

            if (ClipBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }

        public bool IsSpeedloaderAllowed(string itemID)
        {
            if (SpeedLoaderWhitelist.Count > 0 && !SpeedLoaderWhitelist.Contains(itemID))
            {
                return false;
            }

            if (SpeedLoaderBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }

        public bool IsRoundAllowed(string itemID)
        {
            if (RoundWhitelist.Count > 0 && !RoundWhitelist.Contains(itemID))
            {
                return false;
            }

            if (RoundBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// An equipment pool is an entry that can spawn at a constructor panel
    /// </summary>
    public class EquipmentPool
    {
        public EquipmentPoolDef.PoolEntry.PoolEntryType Type;
        public string IconName;
        public int TokenCost;
        public int TokenCostLimited;
        public int MinLevelAppears;
        public int MaxLevelAppears;
        public bool SpawnsInSmallCase;
        public bool SpawnsInLargeCase;
        public EquipmentGroup PrimaryGroup;
        public EquipmentGroup BackupGroup;

        [JsonIgnore]
        private EquipmentPoolDef.PoolEntry pool;

        public EquipmentPool()
        {
            PrimaryGroup = new EquipmentGroup();
            BackupGroup = new EquipmentGroup();
        }

        public EquipmentPool(V1.EquipmentPool oldPool) : this()
        {
            if (oldPool == null)
                return;

            Type = oldPool.Type;
            IconName = oldPool.IconName;
            TokenCost = oldPool.TokenCost;
            TokenCostLimited = oldPool.TokenCostLimited;
            MinLevelAppears = oldPool.MinLevelAppears;
            MaxLevelAppears = oldPool.MaxLevelAppears;
            SpawnsInSmallCase = oldPool.SpawnsInSmallCase;
            SpawnsInLargeCase = oldPool.SpawnsInLargeCase;
            PrimaryGroup = new EquipmentGroup(oldPool.PrimaryGroup);
            BackupGroup = new EquipmentGroup(oldPool.BackupGroup);
            pool = oldPool.GetPoolEntry();
        }

        public EquipmentPool(EquipmentPoolDef.PoolEntry pool)
        {
            Type = pool.Type;
            IconName = pool.TableDef.Icon.name;
            TokenCost = pool.TokenCost;
            TokenCostLimited = pool.TokenCost_Limited;
            MinLevelAppears = pool.MinLevelAppears;
            MaxLevelAppears = pool.MaxLevelAppears;
            PrimaryGroup = new EquipmentGroup(pool.TableDef);
            PrimaryGroup.Rarity = pool.Rarity;
            SpawnsInLargeCase = pool.TableDef.SpawnsInLargeCase;
            SpawnsInSmallCase = pool.TableDef.SpawnsInSmallCase;
            BackupGroup = new EquipmentGroup();

            this.pool = pool;
        }

        public void Validate()
        {
            PrimaryGroup ??= new();
            PrimaryGroup.Validate();

            BackupGroup ??= new();
            BackupGroup.Validate();
        }

        public EquipmentPoolDef.PoolEntry GetPoolEntry()
        {
            if (pool == null)
            {
                pool = new EquipmentPoolDef.PoolEntry();
                pool.Type = Type;
                pool.TokenCost = TokenCost;
                pool.TokenCost_Limited = TokenCostLimited;
                pool.MinLevelAppears = MinLevelAppears;
                pool.MaxLevelAppears = MaxLevelAppears;

                if (PrimaryGroup != null)
                {
                    pool.Rarity = PrimaryGroup.Rarity;
                }
                else
                {
                    pool.Rarity = 1;
                }

                pool.TableDef = PrimaryGroup.GetObjectTableDef();
            }

            return pool;
        }


        public bool DelayedInit(List<string> globalObjectBlacklist)
        {
            if (pool != null)
            {
                if (LoadedTemplateManager.DefaultIconSprites.ContainsKey(IconName))
                {
                    if (pool.TableDef == null)
                    {
                        pool.TableDef = (PrimaryGroup as EquipmentGroup).GetObjectTableDef();
                    }
                    pool.TableDef.Icon = LoadedTemplateManager.DefaultIconSprites[IconName];
                }

                if (PrimaryGroup != null)
                {
                    if (!PrimaryGroup.DelayedInit(globalObjectBlacklist))
                    {
                        TNHFrameworkLogger.Log("Primary group for equipment pool entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);
                        PrimaryGroup = null;
                    }
                }

                if (BackupGroup != null)
                {
                    if (!BackupGroup.DelayedInit(globalObjectBlacklist))
                    {
                        if (PrimaryGroup == null) TNHFrameworkLogger.Log("Backup group for equipment pool entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);
                        BackupGroup = null;
                    }
                }

                return PrimaryGroup != null || BackupGroup != null;
            }

            return false;
        }


        public List<EquipmentGroup> GetSpawnedEquipmentGroups()
        {
            if (PrimaryGroup != null)
            {
                return PrimaryGroup.GetSpawnedEquipmentGroups();
            }

            else if (BackupGroup != null)
            {
                return BackupGroup.GetSpawnedEquipmentGroups();
            }

            TNHFrameworkLogger.LogWarning("EquipmentPool had both PrimaryGroup and BackupGroup set to null! Returning an empty list for spawned equipment");
            return [];
        }


        public override string ToString()
        {
            string output = "Equipment Pool : IconName=" + IconName + " : CostLimited=" + TokenCostLimited + " : CostSpawnlock=" + TokenCost;

            if (PrimaryGroup != null)
            {
                output += "\nPrimary Group";
                output += PrimaryGroup.ToString(0);
            }

            if (BackupGroup != null)
            {
                output += "\nBackup Group";
                output += BackupGroup.ToString(0);
            }

            return output;
        }

    }

    public class EquipmentGroup
    {
        public ObjectCategory Category;
        public float Rarity;
        public int ItemsToSpawn;
        public int MinAmmoCapacity;
        public int MaxAmmoCapacity;
        public int NumMagsSpawned;
        public int NumClipsSpawned;
        public int NumRoundsSpawned;
        public bool SpawnMagAndClip;
        public float BespokeAttachmentChance;
        public bool IsCompatibleMagazine;
        public bool AutoPopulateGroup;
        public bool ForceSpawnAllSubPools;
        public List<string> IDOverride = [];
        public FVRTags Tags;
        public List<EquipmentGroup> SubGroups = [];

        [JsonIgnore]
        private ObjectTableDef objectTableDef;

        [JsonIgnore]
        private List<string> objects = [];

        public EquipmentGroup()
        {
            IDOverride = [];
            Tags = new();
            SubGroups = [];
        }

        public EquipmentGroup(V1.EquipmentGroup thing) : this()
        {
            if (thing == null)
                return;

            Category = thing.Category;
            Rarity = thing.Rarity;
            ItemsToSpawn = thing.ItemsToSpawn;
            MinAmmoCapacity = thing.MinAmmoCapacity;
            MaxAmmoCapacity = thing.MaxAmmoCapacity;
            NumMagsSpawned = thing.NumMagsSpawned;
            NumClipsSpawned = thing.NumClipsSpawned;
            NumRoundsSpawned = thing.NumRoundsSpawned;
            SpawnMagAndClip = thing.SpawnMagAndClip;
            BespokeAttachmentChance = thing.BespokeAttachmentChance;
            IsCompatibleMagazine = thing.IsCompatibleMagazine;
            AutoPopulateGroup = thing.AutoPopulateGroup;
            ForceSpawnAllSubPools = thing.ForceSpawnAllSubPools;
            IDOverride = thing.IDOverride ?? [];
            Tags = new()
            {
                Eras = thing.Eras ?? [],
                Sets = thing.Sets ?? [],
                Sizes = thing.Sizes ?? [],
                Actions = thing.Actions ?? [],
                Modes = thing.Modes ?? [],
                ExcludedModes = thing.ExcludedModes ?? [],
                FeedOptions = thing.FeedOptions ?? [],
                MountsAvailable = thing.MountsAvailable ?? [],
                RoundPowers = thing.RoundPowers ?? [],
                Features = thing.Features ?? [],
                MeleeStyles = thing.MeleeStyles ?? [],
                MeleeHandedness = thing.MeleeHandedness ?? [],
                MountTypes = thing.MountTypes ?? [],
                ThrownTypes = thing.ThrownTypes ?? [],
                ThrownDamageTypes = thing.ThrownDamageTypes ?? []
            };
            SubGroups = [];
            foreach (V1.EquipmentGroup subGroup in thing.SubGroups)
            {
                SubGroups.Add(new EquipmentGroup(subGroup));
            }
        }

        public void Validate()
        {
            IDOverride ??= [];
            SubGroups ??= [];
            foreach (EquipmentGroup subGroup in SubGroups)
            {
                subGroup.Validate();
            }
        }

        public EquipmentGroup(ObjectTableDef objectTableDef)
        {
            Category = (ObjectCategory)objectTableDef.Category;
            ItemsToSpawn = 1;
            MinAmmoCapacity = objectTableDef.MinAmmoCapacity;
            MaxAmmoCapacity = objectTableDef.MaxAmmoCapacity;
            NumMagsSpawned = 3;
            NumClipsSpawned = 3;
            NumRoundsSpawned = 8;
            BespokeAttachmentChance = 0.5f;
            IsCompatibleMagazine = false;
            AutoPopulateGroup = !objectTableDef.UseIDListOverride;
            IDOverride = new List<string>(objectTableDef.IDOverride);
            objectTableDef.IDOverride.Clear();

            Tags = new()
            {
                Eras = objectTableDef.Eras.Select(o => (TagEra)o).ToList(),
                Sets = objectTableDef.Sets.Select(o => (TagSet)o).ToList(),
                Sizes = objectTableDef.Sizes.Select(o => (TagFirearmSize)o).ToList(),
                Actions = objectTableDef.Actions.Select(o => (TagFirearmAction)o).ToList(),
                Modes = objectTableDef.Modes.Select(o => (TagFirearmFiringMode)o).ToList(),
                ExcludedModes = objectTableDef.ExcludeModes.Select(o => (TagFirearmFiringMode)o).ToList(),
                FeedOptions = objectTableDef.Feedoptions.Select(o => (TagFirearmFeedOption)o).ToList(),
                MountsAvailable = objectTableDef.MountsAvailable.Select(o => (TagFirearmMount)o).ToList(),
                RoundPowers = objectTableDef.RoundPowers.Select(o => (TagFirearmRoundPower)o).ToList(),
                Features = objectTableDef.Features.Select(o => (TagAttachmentFeature)o).ToList(),
                MeleeHandedness = objectTableDef.MeleeHandedness.Select(o => (TagMeleeHandedness)o).ToList(),
                MeleeStyles = objectTableDef.MeleeStyles.Select(o => (TagMeleeStyle)o).ToList(),
                MountTypes = objectTableDef.MountTypes.Select(o => (TagFirearmMount)o).ToList(),
                PowerupTypes = objectTableDef.PowerupTypes.Select(o => (TagPowerupType)o).ToList(),
                ThrownTypes = objectTableDef.ThrownTypes.Select(o => (TagThrownType)o).ToList(),
                ThrownDamageTypes = objectTableDef.ThrownDamageTypes.Select(o => (TagThrownDamageType)o).ToList()
            };

            this.objectTableDef = objectTableDef;
        }

        public ObjectTableDef GetObjectTableDef()
        {
            if (objectTableDef == null)
            {
                if (Tags == null)
                {
                    Tags = new();
                }
                else
                {
                    Tags.Eras ??= [];
                    Tags.Sets ??= [];
                    Tags.Sizes ??= [];
                    Tags.Actions ??= [];
                    Tags.Modes ??= [];
                    Tags.ExcludedModes ??= [];
                    Tags.FeedOptions ??= [];
                    Tags.MountsAvailable ??= [];
                    Tags.RoundPowers ??= [];
                    Tags.Features ??= [];
                    Tags.MeleeHandedness ??= [];
                    Tags.MeleeStyles ??= [];
                    Tags.PowerupTypes ??= [];
                    Tags.ThrownTypes ??= [];
                    Tags.ThrownDamageTypes ??= [];
                }

                objectTableDef = (ObjectTableDef)ScriptableObject.CreateInstance(typeof(ObjectTableDef));
                objectTableDef.Category = (FVRObject.ObjectCategory)Category;
                objectTableDef.MinAmmoCapacity = MinAmmoCapacity;
                objectTableDef.MaxAmmoCapacity = MaxAmmoCapacity;
                objectTableDef.RequiredExactCapacity = -1;
                objectTableDef.IsBlanked = false;
                objectTableDef.SpawnsInSmallCase = false;
                objectTableDef.SpawnsInLargeCase = false;
                objectTableDef.UseIDListOverride = !AutoPopulateGroup;
                objectTableDef.IDOverride = [];
                objectTableDef.Eras = Tags.Eras.Select(o => (FVRObject.OTagEra)o).ToList();
                objectTableDef.Sets = Tags.Sets.Select(o => (FVRObject.OTagSet)o).ToList();
                objectTableDef.Sizes = Tags.Sizes.Select(o => (FVRObject.OTagFirearmSize)o).ToList();
                objectTableDef.Actions = Tags.Actions.Select(o => (FVRObject.OTagFirearmAction)o).ToList();
                objectTableDef.Modes = Tags.Modes.Select(o => (FVRObject.OTagFirearmFiringMode)o).ToList();
                objectTableDef.ExcludeModes = Tags.ExcludedModes.Select(o => (FVRObject.OTagFirearmFiringMode)o).ToList();
                objectTableDef.Feedoptions = Tags.FeedOptions.Select(o => (FVRObject.OTagFirearmFeedOption)o).ToList();
                objectTableDef.MountsAvailable = Tags.MountsAvailable.Select(o => (FVRObject.OTagFirearmMount)o).ToList();
                objectTableDef.RoundPowers = Tags.RoundPowers.Select(o => (FVRObject.OTagFirearmRoundPower)o).ToList();
                objectTableDef.Features = Tags.Features.Select(o => (FVRObject.OTagAttachmentFeature)o).ToList();
                objectTableDef.MeleeHandedness = Tags.MeleeHandedness.Select(o => (FVRObject.OTagMeleeHandedness)o).ToList();
                objectTableDef.MeleeStyles = Tags.MeleeStyles.Select(o => (FVRObject.OTagMeleeStyle)o).ToList();
                objectTableDef.MountTypes = Tags.MountTypes.Select(o => (FVRObject.OTagFirearmMount)o).ToList();
                objectTableDef.PowerupTypes = Tags.PowerupTypes.Select(o => (FVRObject.OTagPowerupType)o).ToList();
                objectTableDef.ThrownTypes = Tags.ThrownTypes.Select(o => (FVRObject.OTagThrownType)o).ToList();
                objectTableDef.ThrownDamageTypes = Tags.ThrownDamageTypes.Select(o => (FVRObject.OTagThrownDamageType)o).ToList();
            }
            return objectTableDef;
        }

        public List<string> GetObjects()
        {
            return objects;
        }


        public List<EquipmentGroup> GetSpawnedEquipmentGroups()
        {
            List<EquipmentGroup> result;

            if (IsCompatibleMagazine || SubGroups == null || SubGroups.Count == 0)
            {
                result = [this];
                return result;
            }
            else if (ForceSpawnAllSubPools)
            {
                result = (objects.Count == 0) ? [] : [this];

                foreach (EquipmentGroup group in SubGroups)
                {
                    result.AddRange(group.GetSpawnedEquipmentGroups());
                }

                return result;
            }
            else
            {
                float thisRarity = (objects.Count == 0) ? 0f : (float)Rarity;
                float combinedRarity = thisRarity;
                foreach (EquipmentGroup group in SubGroups)
                {
                    combinedRarity += group.Rarity;
                }

                float randomSelection = UnityEngine.Random.Range(0, combinedRarity);

                if (randomSelection < thisRarity)
                {
                    result = [this];
                    return result;
                }
                else
                {
                    float progress = thisRarity;
                    for (int i = 0; i < SubGroups.Count; i++)
                    {
                        progress += SubGroups[i].Rarity;
                        if (randomSelection < progress)
                        {
                            return SubGroups[i].GetSpawnedEquipmentGroups();
                        }
                    }
                }
            }

            return [];
        }



        /// <summary>
        /// Fills out the object table and removes any unloaded items
        /// </summary>
        /// <returns> Returns true if valid, and false if empty </returns>
        public bool DelayedInit(List<string> globalObjectBlacklist)
        {
            //Before we add anything from the IDOverride list, remove anything that isn't loaded
            TNHFrameworkUtils.RemoveUnloadedObjectIDs(this);


            //Every item in IDOverride gets added to the list of spawnable objects
            if (IDOverride != null)
            {
                foreach (var objectID in IDOverride)
                {
                    if (!globalObjectBlacklist.Contains(objectID))
                        objects.Add(objectID);
                }
            }


            //If this pool isn't a compatible magazine or manually set, then we need to populate it based on its parameters
            if (!IsCompatibleMagazine && AutoPopulateGroup)
            {
                Initialise(globalObjectBlacklist);
            }


            //Perform delayed init on all subgroups. If they are empty, we remove them
            if (SubGroups != null)
            {
                for (int i = 0; i < SubGroups.Count; i++)
                {
                    if (!SubGroups[i].DelayedInit(globalObjectBlacklist))
                    {
                        //TNHFrameworkLogger.Log("Subgroup was empty, removing it!", TNHFrameworkLogger.LogType.Character);
                        SubGroups.RemoveAt(i);
                        i -= 1;
                    }
                }
            }

            if (Rarity <= 0)
            {
                //TNHFrameworkLogger.Log("Equipment group had a rarity of 0 or less! Setting rarity to 1", TNHFrameworkLogger.LogType.Character);
                Rarity = 1;
            }

            //The table is valid if it has items in it, or is a compatible magazine
            return objects.Count != 0 || IsCompatibleMagazine || (SubGroups != null && SubGroups.Count != 0);
        }

        public void Initialise(List<string> globalObjectBlacklist)
        {
            List<FVRObject> Objs = new(ManagerSingleton<IM>.Instance.odicTagCategory[(FVRObject.ObjectCategory)Category]);
            for (int j = Objs.Count - 1; j >= 0; j--)
            {
                FVRObject fvrobject = Objs[j];
                if (globalObjectBlacklist.Contains(fvrobject.ItemID))
                {
                    continue;
                }
                else if (!fvrobject.OSple)
                {
                    continue;
                }
                else if (MinAmmoCapacity > -1 && fvrobject.MaxCapacityRelated < MinAmmoCapacity)
                {
                    if (Category != ObjectCategory.MeleeWeapon)
                        continue;
                }
                else if (MaxAmmoCapacity > -1 && fvrobject.MinCapacityRelated > MaxAmmoCapacity)
                {
                    if (Category != ObjectCategory.MeleeWeapon)  // Fix for Meat Fortress melee weapons
                        continue;
                }
                // ????
                // anton, why?
                /*
                else if (requiredExactCapacity > -1 && !this.DoesGunMatchExactCapacity(fvrobject))
                {
                    continue;
                }
                */
                else if (Tags.MinYear != -1 && Tags.MinYear > fvrobject.TagFirearmFirstYear)
                {
                    continue;
                }
                else if (Tags.MaxYear != -1 && Tags.MaxYear < fvrobject.TagFirearmFirstYear)
                {
                    continue;
                }
                else if (Tags.Eras != null && Tags.Eras.Count > 0 && !Tags.Eras.Contains((TagEra)fvrobject.TagEra))
                {
                    continue;
                }
                else if (Tags.Sets != null && Tags.Sets.Count > 0 && !Tags.Sets.Contains((TagSet)fvrobject.TagSet))
                {
                    continue;
                }
                else if (Tags.Sizes != null && Tags.Sizes.Count > 0 && !Tags.Sizes.Contains((TagFirearmSize)fvrobject.TagFirearmSize))
                {
                    continue;
                }
                else if (Tags.Actions != null && Tags.Actions.Count > 0 && !Tags.Actions.Contains((TagFirearmAction)fvrobject.TagFirearmAction))
                {
                    continue;
                }
                else if (Tags.RoundPowers != null && Tags.RoundPowers.Count > 0 && !Tags.RoundPowers.Contains((TagFirearmRoundPower)fvrobject.TagFirearmRoundPower))
                {
                    continue;
                }
                else
                {
                    if (Tags.Modes != null && Tags.Modes.Count > 0)
                    {
                        bool flag = false;
                        for (int k = 0; k < Tags.Modes.Count; k++)
                        {
                            if (!fvrobject.TagFirearmFiringModes.Contains((FVRObject.OTagFirearmFiringMode)Tags.Modes[k]))
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            continue;
                        }
                    }
                    if (Tags.ExcludedModes != null)
                    {
                        bool flag2 = false;
                        for (int l = 0; l < Tags.ExcludedModes.Count; l++)
                        {
                            if (fvrobject.TagFirearmFiringModes.Contains((FVRObject.OTagFirearmFiringMode)Tags.ExcludedModes[l]))
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            continue;
                        }
                    }
                    if (Tags.FeedOptions != null && Tags.FeedOptions.Count > 0)
                    {
                        bool flag3 = true;
                        for (int m = 0; m < Tags.FeedOptions.Count; m++)
                        {
                            if (fvrobject.TagFirearmFeedOption.Contains((FVRObject.OTagFirearmFeedOption)Tags.FeedOptions[m]))
                            {
                                flag3 = false;
                                break;
                            }
                        }
                        if (flag3)
                        {
                            continue;
                        }
                    }
                    if (Tags.MountsAvailable != null)
                    {
                        bool flag4 = false;
                        for (int n = 0; n < Tags.MountsAvailable.Count; n++)
                        {
                            if (!fvrobject.TagFirearmMounts.Contains((FVRObject.OTagFirearmMount)Tags.MountsAvailable[n]))
                            {
                                flag4 = true;
                                break;
                            }
                        }
                        if (flag4)
                        {
                            continue;
                        }
                    }
                    if (Tags.PowerupTypes != null && Tags.PowerupTypes.Count > 0 && !Tags.PowerupTypes.Contains((TagPowerupType)fvrobject.TagPowerupType))
                    {
                        continue;
                    }
                    else if (Tags.ThrownTypes != null && Tags.ThrownTypes.Count > 0 && !Tags.ThrownTypes.Contains((TagThrownType)fvrobject.TagThrownType))
                    {
                        continue;
                    }
                    else if (Tags.ThrownDamageTypes != null && Tags.ThrownDamageTypes.Count > 0 && !Tags.ThrownDamageTypes.Contains((TagThrownDamageType)fvrobject.TagThrownDamageType))
                    {
                        continue;
                    }
                    else if (Tags.MeleeStyles != null && Tags.MeleeStyles.Count > 0 && !Tags.MeleeStyles.Contains((TagMeleeStyle)fvrobject.TagMeleeStyle))
                    {
                        continue;
                    }
                    else if (Tags.MeleeHandedness != null && Tags.MeleeHandedness.Count > 0 && !Tags.MeleeHandedness.Contains((TagMeleeHandedness)fvrobject.TagMeleeHandedness))
                    {
                        continue;
                    }
                    else if (Tags.MountTypes != null && Tags.MountTypes.Count > 0 && !Tags.MountTypes.Contains((TagFirearmMount)fvrobject.TagAttachmentMount))
                    {
                        continue;
                    }
                    else if (Tags.Features != null && Tags.Features.Count > 0 && !Tags.Features.Contains((TagAttachmentFeature)fvrobject.TagAttachmentFeature))
                    {
                        continue;
                    }
                    objects.Add(fvrobject.ItemID);
                }
            }
        }


        public string ToString(int level)
        {
            string prefix = "\n-";
            for (int i = 0; i < level; i++) prefix += "-";

            string output = prefix + "Group : Rarity=" + Rarity;

            if (IsCompatibleMagazine)
            {
                output += prefix + "Compatible Magazine";
            }

            else
            {
                foreach (string item in objects)
                {
                    output += prefix + item;
                }

                if (SubGroups != null)
                {
                    foreach (EquipmentGroup group in SubGroups)
                    {
                        output += group.ToString(level + 1);
                    }
                }
            }

            return output;
        }

    }

    public class FVRTags
    {
        public int MinYear = -1;
        public int MaxYear = -1;
        public List<TagEra> Eras = [];
        public List<TagSet> Sets = [];
        public List<TagFirearmSize> Sizes = [];
        public List<TagFirearmAction> Actions = [];
        public List<TagFirearmFiringMode> Modes = [];
        public List<TagFirearmFiringMode> ExcludedModes = [];
        public List<TagFirearmFeedOption> FeedOptions = [];
        public List<TagFirearmMount> MountsAvailable = [];
        public List<TagFirearmRoundPower> RoundPowers = [];
        public List<TagAttachmentFeature> Features = [];
        public List<TagMeleeStyle> MeleeStyles = [];
        public List<TagMeleeHandedness> MeleeHandedness = [];
        public List<TagFirearmMount> MountTypes = [];
        public List<TagPowerupType> PowerupTypes = [];
        public List<TagThrownType> ThrownTypes = [];
        public List<TagThrownDamageType> ThrownDamageTypes = [];
    }

    public class LoadoutEntry
    {
        public EquipmentGroup PrimaryGroup;
        public EquipmentGroup BackupGroup;

        [JsonIgnore]
        private TNH_CharacterDef.LoadoutEntry loadout;

        public LoadoutEntry()
        {
            PrimaryGroup = new EquipmentGroup();
            BackupGroup = new EquipmentGroup();
        }

        public LoadoutEntry(V1.LoadoutEntry oldEntry) : this()
        {
            if (oldEntry == null)
                return;

            PrimaryGroup = new EquipmentGroup(oldEntry.PrimaryGroup);
            BackupGroup = new EquipmentGroup(oldEntry.BackupGroup);
        }

        public LoadoutEntry(TNH_CharacterDef.LoadoutEntry loadout)
        {
            if (loadout == null)
            {
                loadout = new TNH_CharacterDef.LoadoutEntry();
                loadout.TableDefs = [];
                loadout.ListOverride = [];
            }

            else if (loadout.ListOverride != null && loadout.ListOverride.Count > 0)
            {
                PrimaryGroup = new EquipmentGroup
                {
                    Rarity = 1,
                    IDOverride = loadout.ListOverride.Select(o => o.ItemID).ToList(),
                    ItemsToSpawn = 1,
                    MinAmmoCapacity = -1,
                    MaxAmmoCapacity = 9999,
                    NumMagsSpawned = loadout.Num_Mags_SL_Clips,
                    NumClipsSpawned = loadout.Num_Mags_SL_Clips,
                    NumRoundsSpawned = loadout.Num_Rounds
                };
            }

            else if (loadout.TableDefs != null && loadout.TableDefs.Count > 0)
            {
                //If we have just one pool, then the primary pool becomes that pool
                if (loadout.TableDefs.Count == 1)
                {
                    PrimaryGroup = new EquipmentGroup(loadout.TableDefs[0])
                    {
                        Rarity = 1,
                        NumMagsSpawned = loadout.Num_Mags_SL_Clips,
                        NumClipsSpawned = loadout.Num_Mags_SL_Clips,
                        NumRoundsSpawned = loadout.Num_Rounds
                    };
                }

                else
                {
                    PrimaryGroup = new EquipmentGroup
                    {
                        Rarity = 1,
                        SubGroups = []
                    };
                    foreach (ObjectTableDef table in loadout.TableDefs)
                    {
                        EquipmentGroup group = new(table);
                        group.Rarity = 1;
                        group.NumMagsSpawned = loadout.Num_Mags_SL_Clips;
                        group.NumClipsSpawned = loadout.Num_Mags_SL_Clips;
                        group.NumRoundsSpawned = loadout.Num_Rounds;
                        PrimaryGroup.SubGroups.Add(group);
                    }
                }
            }

            this.loadout = loadout;
        }

        public void Validate()
        {
            PrimaryGroup ??= new();
            PrimaryGroup.Validate();

            BackupGroup ??= new();
            BackupGroup.Validate();
        }

        public TNH_CharacterDef.LoadoutEntry GetLoadoutEntry()
        {
            if (loadout == null)
            {
                loadout = new TNH_CharacterDef.LoadoutEntry();
                loadout.Num_Mags_SL_Clips = 3;
                loadout.Num_Rounds = 9;

                if (PrimaryGroup != null)
                {
                    loadout.TableDefs = [PrimaryGroup.GetObjectTableDef()];
                }
            }

            return loadout;
        }



        public bool DelayedInit(List<string> globalObjectBlacklist)
        {
            if (loadout != null)
            {
                if (PrimaryGroup != null)
                {
                    if (!PrimaryGroup.DelayedInit(globalObjectBlacklist))
                    {
                        TNHFrameworkLogger.Log("Primary group for loadout entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);
                        PrimaryGroup = null;
                    }
                }

                if (BackupGroup != null)
                {
                    if (!BackupGroup.DelayedInit(globalObjectBlacklist))
                    {
                        if (PrimaryGroup == null) TNHFrameworkLogger.Log("Backup group for loadout entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);

                        BackupGroup = null;
                    }
                }

                return PrimaryGroup != null || BackupGroup != null;
            }

            return false;
        }


        public override string ToString()
        {
            string output = "Loadout Entry";

            if (PrimaryGroup != null)
            {
                output += "\nPrimary Group";
                output += PrimaryGroup.ToString(0);
            }

            if (BackupGroup != null)
            {
                output += "\nBackup Group";
                output += BackupGroup.ToString(0);
            }

            return output;
        }
    }

    public class Level
    {
        public int NumOverrideTokensForHold;
        public int MinSupplyPoints;
        public int MaxSupplyPoints;
        public int MinConstructors;
        public int MaxConstructors;
        public int MinPanels;
        public int MaxPanels;
        public int MinBoxesSpawned;
        public int MaxBoxesSpawned;
        public int MinTokensPerSupply;
        public int MaxTokensPerSupply;
        public float BoxTokenChance;
        public float BoxHealthChance;
        public List<PanelType> PossiblePanelTypes;
        public TakeChallenge TakeChallenge;
        public List<Phase> HoldPhases;
        public TakeChallenge SupplyChallenge;
        public List<Patrol> Patrols;

        [JsonIgnore]
        private TNH_Progression.Level level;

        public Level()
        {
            PossiblePanelTypes = [];
            TakeChallenge = new TakeChallenge();
            HoldPhases = [];
            SupplyChallenge = new TakeChallenge();
            Patrols = [];
        }

        public Level(V1.Level oldLevel) : this()
        {
            if (oldLevel == null)
                return;

            NumOverrideTokensForHold = oldLevel.NumOverrideTokensForHold;
            MinSupplyPoints = oldLevel.MinSupplyPoints;
            MaxSupplyPoints = oldLevel.MaxSupplyPoints;
            MinConstructors = oldLevel.MinConstructors;
            MaxConstructors = oldLevel.MaxConstructors;
            MinPanels = oldLevel.MinPanels;
            MaxPanels = oldLevel.MaxPanels;
            MinBoxesSpawned = oldLevel.MinBoxesSpawned;
            MaxBoxesSpawned = oldLevel.MaxBoxesSpawned;
            MinTokensPerSupply = oldLevel.MinTokensPerSupply;
            MaxTokensPerSupply = oldLevel.MaxTokensPerSupply;
            BoxTokenChance = oldLevel.BoxTokenChance;
            BoxHealthChance = oldLevel.BoxHealthChance;
            PossiblePanelTypes = oldLevel.PossiblePanelTypes ?? [];
            TakeChallenge = new(oldLevel.TakeChallenge);
            HoldPhases = [];

            int HoldMyBeer = 0;
            foreach (V1.Phase oldPhase in oldLevel.HoldPhases)
            {
                foreach (Phase newPhase in Phase.GetNewPhases(oldPhase))
                {
                    if (HoldMyBeer == 0)
                    {
                        newPhase.Keys = ["Start"];
                    }
                    else
                    {
                        newPhase.Keys = ["Phase" + HoldMyBeer];
                    }
                    
                    HoldMyBeer++;
                    newPhase.Paths = ["Phase" + HoldMyBeer];
                    // fix this if you want. i'm too tired rn
                    if (HoldPhases.Count == (oldLevel.HoldPhases.Count * 3) - 1)
                    {
                        newPhase.Paths = ["End"];
                    }
                    HoldPhases.Add(newPhase);
                }
            }

            SupplyChallenge = new(oldLevel.SupplyChallenge);
            Patrols = [];

            foreach (V1.Patrol oldPatrol in oldLevel.Patrols)
            {
                Patrols.Add(new(oldPatrol));
            }
        }

        public Level(TNH_Progression.Level level)
        {
            NumOverrideTokensForHold = level.NumOverrideTokensForHold;
            TakeChallenge = new TakeChallenge(level.TakeChallenge);
            SupplyChallenge = new TakeChallenge(level.TakeChallenge);

            HoldPhases = [];
            int HoldMyBeer = 0;
            foreach (TNH_HoldChallenge.Phase oldPhase in level.HoldChallenge.Phases)
            {
                foreach (Phase newPhase in Phase.GetNewPhases(oldPhase))
                {
                    newPhase.Keys = ["Phase" + HoldMyBeer];
                    if (HoldMyBeer == 0)
                    {
                        newPhase.Keys = ["Start"];
                    }
                    HoldMyBeer++;
                    newPhase.Paths = ["Phase" + HoldMyBeer];

                    HoldPhases.Add(newPhase);
                }
            }
            HoldPhases.Add(new WarmupPhase
            {
                Keys = [("Phase" + HoldMyBeer)],
                Paths = ["End"],
                PhaseLength = 5f,
                DespawnSosigsBeforePhase = true,
                DestroyCover = true,
                AddCover = false,
                IsEnd = true
            });

            Patrols = level.PatrolChallenge.Patrols.Select(o => new Patrol(o)).ToList();
            PossiblePanelTypes =
            [
                PanelType.AmmoReloader,
                PanelType.MagDuplicator,
                PanelType.Recycler,
            ];
            MinConstructors = 1;
            MaxConstructors = 1;
            MinPanels = 1;
            MaxPanels = 1;
            MinSupplyPoints = 2;
            MaxSupplyPoints = 3;
            MinBoxesSpawned = 2;
            MaxBoxesSpawned = 4;
            MinTokensPerSupply = 1;
            MaxTokensPerSupply = 1;
            BoxTokenChance = 0;
            BoxHealthChance = 0.5f;

            this.level = level;
        }

        public void Validate()
        {
            PossiblePanelTypes ??= [];
            TakeChallenge ??= new();

            HoldPhases ??= [];

            SupplyChallenge ??= new();

            Patrols ??= [];
            foreach (Patrol patrol in Patrols)
            {
                patrol.Validate();
            }
        }

        public TNH_Progression.Level GetLevel()
        {
            if (level == null)
            {
                level = new();
                level.NumOverrideTokensForHold = NumOverrideTokensForHold;
                level.TakeChallenge = TakeChallenge.GetTakeChallenge();

                level.HoldChallenge = (TNH_HoldChallenge)ScriptableObject.CreateInstance(typeof(TNH_HoldChallenge));
                level.HoldChallenge.Phases = null;
                /*
                foreach (Phase phase in HoldPhases)
                {
                    level.HoldChallenge.Phases.Add(phase.GetPhase());
                }
                */

                level.SupplyChallenge = SupplyChallenge.GetTakeChallenge();
                level.PatrolChallenge = (TNH_PatrolChallenge)ScriptableObject.CreateInstance(typeof(TNH_PatrolChallenge));
                level.PatrolChallenge.Patrols = Patrols.Select(o => o.GetPatrol()).ToList();
                level.TrapsChallenge = (TNH_TrapsChallenge)ScriptableObject.CreateInstance(typeof(TNH_TrapsChallenge));
            }

            return level;
        }

        public Patrol GetPatrol(TNH_PatrolChallenge.Patrol patrol)
        {
            if (Patrols.Select(o => o.GetPatrol()).Contains(patrol))
            {
                return Patrols.Find(o => o.GetPatrol().Equals(patrol));
            }

            return null;
        }

        public void DelayedInit(bool isCustom, int levelIndex)
        {
            //If this is a level for a default character, we should try to replicate the vanilla layout
            if (!isCustom)
            {
                MaxSupplyPoints = Mathf.Clamp(levelIndex + 1, 1, 3);
                MinSupplyPoints = Mathf.Clamp(levelIndex + 1, 1, 3);
            }
        }

        public bool LevelUsesSosig(string id)
        {
            if (TakeChallenge.EnemyType == id)
            {
                return true;
            }

            else if (SupplyChallenge.EnemyType == id)
            {
                return true;
            }

            foreach (Patrol patrol in Patrols)
            {
                if (patrol.LeaderType == id)
                {
                    return true;
                }

                foreach (string sosigID in patrol.EnemyType)
                {
                    if (sosigID == id)
                    {
                        return true;
                    }
                }
            }

            foreach (Phase phase in HoldPhases)
            {
                if (phase.DoesUseSosig(id))
                {
                    return true;
                }
            }

            return false;
        }
    }


    public class TakeChallenge
    {
        public TNH_TurretType TurretType;
        public string EnemyType;

        public int NumTurrets;
        public int NumGuards;
        public int IFFUsed;


        [JsonIgnore]
        private TNH_TakeChallenge takeChallenge;

        public TakeChallenge() { }

        public TakeChallenge(V1.TakeChallenge oldTake)
        {
            TurretType = oldTake.TurretType;
            EnemyType = oldTake.EnemyType;

            NumTurrets = oldTake.NumTurrets;
            NumGuards = oldTake.NumGuards;
            IFFUsed = oldTake.IFFUsed;
        }

        public TakeChallenge(TNH_TakeChallenge takeChallenge)
        {
            TurretType = takeChallenge.TurretType;
            EnemyType = takeChallenge.GID.ToString();
            NumGuards = takeChallenge.NumGuards;
            NumTurrets = takeChallenge.NumTurrets;
            IFFUsed = takeChallenge.IFFUsed;

            this.takeChallenge = takeChallenge;
        }

        public TNH_TakeChallenge GetTakeChallenge()
        {
            if (takeChallenge == null)
            {
                takeChallenge = (TNH_TakeChallenge)ScriptableObject.CreateInstance(typeof(TNH_TakeChallenge));
                takeChallenge.TurretType = TurretType;

                //Try to get the necessary SosigEnemyIDs
                if (LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType))
                {
                    takeChallenge.GID = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[EnemyType];
                }
                else
                {
                    takeChallenge.GID = (SosigEnemyID)Enum.Parse(typeof(SosigEnemyID), EnemyType);
                }

                takeChallenge.NumTurrets = NumTurrets;
                takeChallenge.NumGuards = NumGuards;
                takeChallenge.IFFUsed = IFFUsed;
            }

            return takeChallenge;
        }
    }

    public abstract class Phase
    {
        public List<string> Keys = [];
        public List<string> Paths = [];

        public float PhaseLength = 5f;
        public bool DespawnSosigsBeforePhase = false;
        public bool DestroyCover = true;
        public bool AddCover = true;

        public static float TimeSinceLastWarmup = 0f;
        [YamlIgnore]
        public float TimeLeft = 900f;

        public virtual void BeginHold(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            if (!manager.M.HasGuardBeenKilledThatWasAltered())
            {
                manager.M.IncrementScoringStat(TNH_Manager.ScoringEvent.TakeHoldPointTakenClean, 1);
            }
            if (!manager.M.HasPlayerAlertedSecurityThisPhase())
            {
                manager.M.IncrementScoringStat(TNH_Manager.ScoringEvent.TakeCompleteNoAlert, 1);
            }
            if (!manager.M.HasPlayerTakenDamageThisPhase())
            {
                manager.M.IncrementScoringStat(TNH_Manager.ScoringEvent.TakeCompleteNoDamage, 1);
            }
            manager.M.ResetAlertedThisPhase();
            manager.M.ResetPlayerTookDamageThisPhase();
            manager.M.ResetHasGuardBeenKilledThatWasAltered();
            manager.NavBlockers.SetActive(true);
            manager.m_maxPhases = 99;
            manager.m_hasBeenDamagedThisHold = false;
            manager.M.EnqueueLine(TNH_VoiceLineID.BASE_IntrusionDetectedInitiatingLockdown);
            manager.M.EnqueueLine(TNH_VoiceLineID.AI_InterfacingWithSystemNode);
            manager.M.EnqueueLine(TNH_VoiceLineID.BASE_ResponseTeamEnRoute);
            manager.m_isInHold = true;
            manager.m_numWarnings = 0;
            manager.M.HoldPointStarted(manager);

            BeginPhase(manager, character);
        }

        public void BaseBeginPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            TNHFrameworkLogger.Log($"Start of phase {manager.m_phaseIndex}/{Keys[0]}.", TNHFrameworkLogger.LogType.General);
            TimeLeft = PhaseLength;

            if (Paths.Contains("End") && !Paths.Contains("Start"))
            {
                manager.M.SetHoldWaveIntensity(2);
            }
            else
            {
                manager.M.SetHoldWaveIntensity(1);
            }

            manager.m_hasPlayedTimeWarning1 = false;
            manager.m_hasPlayedTimeWarning2 = false;
            manager.m_isFirstWave = true;

            if (DespawnSosigsBeforePhase)
            {
                manager.DeletionBurst();
                manager.M.ClearMiscEnemies();

                UnityEngine.Object.Instantiate(manager.VFX_HoldWave, manager.m_systemNode.NodeCenter.position, manager.m_systemNode.NodeCenter.rotation);
            }
            if (DestroyCover)
            {
                manager.LowerAllBarriers();
            }
            if (AddCover)
            {
                manager.RefreshCoverInHold();
            }
        }

        public virtual void BeginPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character) { }

        public void BaseHoldUpdate(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f)
            {
                EndPhase(manager, character);
            }
            TimeSinceLastWarmup += Time.deltaTime;
        }

        public virtual void HoldUpdate(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseHoldUpdate(manager, character);
        }

        public string BaseEndPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            string path = Paths.GetRandom();
            if (path == "End")
            {
                manager.m_phaseIndex = 0;
                EndHold(manager, character);
                return path;
            }
            else
            {
                List<Phase> currentPhases = character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases;
                List<Phase> validPhases = [];

                foreach (Phase phase in currentPhases)
                {
                    if (phase.Keys.Contains(path))
                    {
                        validPhases.Add(phase);
                    }
                }

                Phase chosenPhase = validPhases.GetRandom();

                manager.m_phaseIndex = currentPhases.IndexOf(chosenPhase);
                chosenPhase.BeginPhase(manager, character);

                return path;
            }
        }

        public virtual void EndPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            TNHFrameworkLogger.Log($"End of phase {manager.m_phaseIndex}/{Keys[0]}.", TNHFrameworkLogger.LogType.General);

            BaseEndPhase(manager, character);
        }

        public virtual void EndHold(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            manager.m_isInHold = false;
            if (!manager.m_hasBeenDamagedThisHold)
            {
                manager.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldPhaseCompleteNoDamage, 1);
            }
            manager.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldPhaseComplete, 1);
            SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, manager.AUDEvent_Success, manager.transform.position);
            manager.M.EnqueueLine(TNH_VoiceLineID.AI_HoldSuccessfulDataExtracted);
            int num = manager.m_tokenReward;
            if (manager.m_numWarnings > 6)
            {
                num--;
            }
            else if (manager.m_numWarnings > 3)
            {
                num--;
            }
            if (num > 0)
            {
                manager.M.EnqueueTokenLine(num);
                manager.M.AddTokens(num, true);
            }
            manager.m_tokenReward = 0;
            manager.M.EnqueueLine(TNH_VoiceLineID.AI_AdvanceToNextSystemNodeAndTakeIt);
            manager.M.HoldPointCompleted(manager, true);
            manager.ShutDownHoldPoint();
        }

        public virtual void SpawnWarpInMarkers(TNH_HoldPoint manager, TakeAndHoldCharacter character) { }

        public static List<Phase> GetNewPhases(TNH_HoldChallenge.Phase phase)
        {
            return
            [
                new WarmupPhase
                {
                    PhaseLength = phase.WarmUp,
                    DespawnSosigsBeforePhase = true,
                    DestroyCover = true,
                    AddCover = false
                },
                new ScanPhase
                {
                    PhaseLength = phase.ScanTime,
                    DespawnSosigsBeforePhase = false,
                    DestroyCover = false,
                    AddCover = true,
                    Waves = [
                        new ScanPhase.EnemySpawn
                        {
                            EnemyType = [phase.EType.ToString()],
                            LeaderType = phase.LType.ToString(),
                            MinEnemies = phase.MinEnemies,
                            MaxEnemies = phase.MaxEnemies,
                            MaxEnemiesAlive = phase.MaxEnemiesAlive,
                            MaxDirections = phase.MaxDirections,
                            SpawnCadence = phase.SpawnCadence,
                            IFFUsed = phase.IFFUsed,
                            GrenadeChance = 0,
                            GrenadeType = "Sosiggrenade_Flash",
                            SwarmPlayer = false
                        }
                    ]
                },
                new EncryptionPhase
                {
                    PhaseLength = 120f,
                    DespawnSosigsBeforePhase = false,
                    DestroyCover = false,
                    AddCover = false,
                    Encryptions = [phase.Encryption],
                    MinTargets = phase.MinTargets,
                    MaxTargets = phase.MaxTargets,
                    MinTargetsLimited = aaAAAA(phase.MinTargets, phase.Encryption),
                    MaxTargetsLimited = aaAAAA(phase.MaxTargets, phase.Encryption),
                    FirstWarningTime = 60,
                    SecondWarningTime = 30,
                    Waves = [
                        new ScanPhase.EnemySpawn
                        {
                            EnemyType = [phase.EType.ToString()],
                            LeaderType = phase.LType.ToString(),
                            MinEnemies = phase.MinEnemies,
                            MaxEnemies = phase.MaxEnemies,
                            MaxEnemiesAlive = phase.MaxEnemiesAlive,
                            MaxDirections = phase.MaxDirections,
                            SpawnCadence = phase.SpawnCadence,
                            IFFUsed = phase.IFFUsed,
                            GrenadeChance = 0,
                            GrenadeType = "Sosiggrenade_Flash",
                            SwarmPlayer = false
                        }
                    ]
                }
            ];

            static int aaAAAA(int targets, TNH_EncryptionType type)
            {
                if (type != TNH_EncryptionType.Static)
                {
                    return 1;
                }
                return Mathf.Clamp(targets, 1, 3);
            }
        }

        public static List<Phase> GetNewPhases(V1.Phase oldPhase)
        {
            return
            [
                new WarmupPhase
                {
                    PhaseLength = oldPhase.WarmupTime,
                    DespawnSosigsBeforePhase = true,
                    DestroyCover = true,
                    AddCover = false
                },
                new ScanPhase
                {
                    PhaseLength = oldPhase.ScanTime,
                    DespawnSosigsBeforePhase = false,
                    DestroyCover = false,
                    AddCover = true,
                    Waves = [
                        new ScanPhase.EnemySpawn
                        {
                            EnemyType = oldPhase.EnemyType ?? [],
                            LeaderType = oldPhase.LeaderType,
                            MinEnemies = oldPhase.MinEnemies,
                            MaxEnemies = oldPhase.MaxEnemies,
                            MaxEnemiesAlive = oldPhase.MaxEnemiesAlive,
                            MaxDirections = oldPhase.MaxDirections,
                            SpawnCadence = oldPhase.SpawnCadence,
                            IFFUsed = oldPhase.IFFUsed,
                            GrenadeChance = oldPhase.GrenadeChance,
                            GrenadeType = oldPhase.GrenadeType,
                            SwarmPlayer = oldPhase.SwarmPlayer
                        }
                    ]
                },
                new EncryptionPhase
                {
                    PhaseLength = 120f,
                    DespawnSosigsBeforePhase = false,
                    DestroyCover = false,
                    AddCover = false,
                    Encryptions = oldPhase.Encryptions ?? [],
                    MinTargets = oldPhase.MinTargets,
                    MaxTargets = oldPhase.MaxTargets,
                    MinTargetsLimited = oldPhase.MinTargetsLimited,
                    MaxTargetsLimited = oldPhase.MaxTargetsLimited,
                    FirstWarningTime = 60,
                    SecondWarningTime = 30,
                    Waves = [
                        new ScanPhase.EnemySpawn
                        {
                            EnemyType = oldPhase.EnemyType ?? [],
                            LeaderType = oldPhase.LeaderType,
                            MinEnemies = oldPhase.MinEnemies,
                            MaxEnemies = oldPhase.MaxEnemies,
                            MaxEnemiesAlive = oldPhase.MaxEnemiesAlive,
                            MaxDirections = oldPhase.MaxDirections,
                            SpawnCadence = oldPhase.SpawnCadence,
                            IFFUsed = oldPhase.IFFUsed,
                            GrenadeChance = oldPhase.GrenadeChance,
                            GrenadeType = oldPhase.GrenadeType,
                            SwarmPlayer = oldPhase.SwarmPlayer
                        }
                    ]
                }
            ];
        }

        public virtual bool DoesUseSosig(string sosigString)
        {
            return false;
        }
    }

    public class WarmupPhase : Phase
    {
        public bool IsEnd = false;

        public override void BeginPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseBeginPhase(manager, character);

            manager.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);

            manager.m_tickDownToNextGroupSpawn = 0f;
            manager.m_hasBeenDamagedThisPhase = false;
        }

        [YamlIgnore]
        float TextTime = 0f;

        public override void HoldUpdate(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseHoldUpdate(manager, character);
            TextTime += Time.deltaTime;

            switch (TextTime)
            {
                case > 1f:
                    manager.m_systemNode.SetDisplayString("SCANNING SYSTEM");
                    TextTime = 0f;
                    break;
                case > 0.45f:
                    manager.m_systemNode.SetDisplayString("SCANNING SYSTEM...");
                    break;
                case > 0.3f:
                    manager.m_systemNode.SetDisplayString("SCANNING SYSTEM..");
                    break;
                case > 0.15f:
                    manager.m_systemNode.SetDisplayString("SCANNING SYSTEM.");
                    break;
                default:
                    manager.m_systemNode.SetDisplayString("SCANNING SYSTEM");
                    break;
            }
        }

        public override void EndPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseEndPhase(manager, character);
            TimeSinceLastWarmup = 0f;
        }
    }

    public class ScanPhase : Phase
    {
        public List<EnemySpawn> Waves = [];

        [YamlIgnore]
        public static Dictionary<string, float> TagTimers = null;
        [YamlIgnore]
        public Phase LastPhase = null;
        [YamlIgnore]
        public Phase NextPhase = null;

        public override void BeginPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseBeginPhase(manager, character);

            manager.M.EnqueueLine(TNH_VoiceLineID.AI_AnalyzingSystem);
            manager.m_state = TNH_HoldPoint.HoldState.Analyzing;

            manager.m_tickDownToIdentification = PhaseLength * 0.8f;

            if (manager.M.TargetMode == TNHSetting_TargetMode.NoTargets)
            {
                manager.m_tickDownToIdentification = PhaseLength * 0.9f + 60f;
            }
            else if (manager.M.IsBigLevel)
            {
                manager.m_tickDownToIdentification += 15f;
            }
            manager.SpawnPoints_Targets.Shuffle();
            manager.m_validSpawnPoints.Shuffle();
            manager.m_tickDownToNextGroupSpawn = 0f;

            string path = Paths.GetRandom();
            if (path != "End")
            {
                List<Phase> currentPhases = character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases;
                List<Phase> validPhases = [];

                foreach (Phase phase in currentPhases)
                {
                    if (phase.Keys.Contains(path))
                    {
                        validPhases.Add(phase);
                    }
                }

                NextPhase = validPhases.GetRandom();
                if (NextPhase is ScanPhase)
                {
                    (NextPhase as ScanPhase).LastPhase = this;
                }
                
                NextPhase.SpawnWarpInMarkers(manager, character);
            }

            manager.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Analyzing);
        }

        [YamlIgnore]
        float TextTime = 0f;

        public override void HoldUpdate(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseHoldUpdate(manager, character);

            foreach (EnemySpawn spawnWave in Waves)
            {
                spawnWave.SpawningRoutine(manager, this, character);
            }
            manager.m_isFirstWave = false;

            if (manager.M.TargetMode == TNHSetting_TargetMode.NoTargets)
            {
                manager.m_systemNode.SetDisplayString("ANALYZING, COMPLETE IN " + manager.FloatToTime(TimeLeft, "0:00.00"));
            }
            else
            {
                TextTime += Time.deltaTime;

                switch (TextTime)
                {
                    case > 1f:
                        manager.m_systemNode.SetDisplayString("ANALYZING");
                        TextTime = 0f;
                        break;
                    case > 0.45f:
                        manager.m_systemNode.SetDisplayString("ANALYZING...");
                        break;
                    case > 0.3f:
                        manager.m_systemNode.SetDisplayString("ANALYZING..");
                        break;
                    case > 0.15f:
                        manager.m_systemNode.SetDisplayString("ANALYZING.");
                        break;
                    default:
                        manager.m_systemNode.SetDisplayString("ANALYZING");
                        break;
                }
            }
        }

        public override void EndPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            if (NextPhase == null)
            {
                EndHold(manager, character);
            }
            else
            {
                List<Phase> currentPhases = character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases;

                manager.m_phaseIndex = currentPhases.IndexOf(NextPhase);
                NextPhase.BeginPhase(manager, character);
            }
        }

        public override bool DoesUseSosig(string sosigString)
        {
            bool boolean = false;
            foreach (EnemySpawn wave in Waves)
            {
                if ((wave.LeaderType == sosigString) || wave.EnemyType.Contains(sosigString))
                {
                    boolean = true;
                    break; 
                }
            }
            return boolean;
        }

        public class EnemySpawn
        {
            public string Tag;
            public string LeaderType;
            public List<string> EnemyType;
            public int MinEnemies;
            public int MaxEnemies;
            public int MaxEnemiesAlive;
            public int MaxDirections;
            public float SpawnCadence;
            public float Delay = 0;
            public int SpawnLimit = -1;
            public int IFFUsed;
            public float GrenadeChance;
            public string GrenadeType;
            public bool SwarmPlayer;

            [YamlIgnore]
            public float SpawnTimer = 0f;
            [YamlIgnore]
            public int SpawnedSoFar = 0;

            public virtual List<Sosig> SpawningRoutine(TNH_HoldPoint manager, ScanPhase phase, TakeAndHoldCharacter character)
            {
                SpawnTimer -= Time.deltaTime;

                if (manager.m_isFirstWave)
                {
                    // Check if a spawn wave is identical 
                    if (phase.LastPhase != null && phase.LastPhase is ScanPhase)
                    {
                        ScanPhase last = phase.LastPhase as ScanPhase;
                        bool didWeJustDoThat = false;
                        foreach (EnemySpawn spawn in last.Waves)
                        {
                            if (spawn.Tag == Tag && last.Waves.IndexOf(spawn) == phase.Waves.IndexOf(this))
                            {
                                SpawnTimer = spawn.SpawnTimer;
                                SpawnedSoFar = spawn.SpawnedSoFar;
                                didWeJustDoThat = true;
                                break;
                            }
                        }

                        foreach (EnemySpawn spawn in phase.Waves)
                        {
                            if (spawn == this || didWeJustDoThat)
                            {
                                break;
                            }
                            else if (spawn.Tag == Tag)
                            {
                                SpawnTimer += spawn.SpawnCadence;
                            }
                        }
                    }
                    SpawnTimer += Delay;
                }

                if (!manager.m_hasThrownNadesInWave && SpawnTimer <= 5f && !manager.m_isFirstWave)
                {
                    // Check if grenade vectors exist before throwing grenades
                    if (manager.AttackVectors[0].GrenadeVector != null)
                        SpawnGrenades(manager.AttackVectors, manager.M, manager.m_phaseIndex);

                    manager.m_hasThrownNadesInWave = true;
                }

                // Handle spawning of a wave if it is time
                if (SpawnTimer <= 0 && manager.m_activeSosigs.Count + MaxEnemies <= MaxEnemiesAlive && (SpawnLimit < 0 || SpawnLimit > SpawnedSoFar))
                {
                    manager.AttackVectors.Shuffle();

                    List<Sosig> spawns = SpawnHoldEnemyGroup(manager.m_phaseIndex, manager.AttackVectors, manager.SpawnPoints_Turrets, manager.m_activeSosigs, phase, manager.M);
                    manager.m_hasThrownNadesInWave = false;

                    // Adjust spawn cadence depending on ammo mode
                    float ammoMult = (manager.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo ? 1.35f : 1f);
                    float randomMult = (GM.TNHOptions.TNHSeed >= 0) ? 0.9f : UnityEngine.Random.Range(0.9f, 1.1f);
                    SpawnTimer += SpawnCadence * randomMult * ammoMult;

                    foreach (EnemySpawn wave in phase.Waves)
                    {
                        if (wave != this && wave.Tag == Tag)
                        {
                            SpawnTimer += wave.SpawnCadence;
                        }
                    }

                    return spawns;
                }
                return [];
            }

            public void SpawnGrenades(List<TNH_HoldPoint.AttackVector> AttackVectors, TNH_Manager M, int phaseIndex)
            {
                TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[M.C];

                float grenadeChance = GrenadeChance;
                string grenadeType = GrenadeType;

                if (grenadeChance >= UnityEngine.Random.Range(0f, 1f))
                {
                    TNHFrameworkLogger.Log($"Throwing grenade [{grenadeType}]", TNHFrameworkLogger.LogType.TNH);

                    //Get a random grenade vector to spawn a grenade at
                    AttackVectors.Shuffle();
                    TNH_HoldPoint.AttackVector randAttackVector = AttackVectors[UnityEngine.Random.Range(0, AttackVectors.Count)];

                    //Instantiate the grenade object
                    if (IM.OD.ContainsKey(grenadeType))
                    {
                        GameObject grenadeObject = UnityEngine.Object.Instantiate(IM.OD[grenadeType].GetGameObject(), randAttackVector.GrenadeVector.position, randAttackVector.GrenadeVector.rotation);

                        //Give the grenade an initial velocity based on the grenade vector
                        grenadeObject.GetComponent<Rigidbody>().velocity = 15 * randAttackVector.GrenadeVector.forward;
                        grenadeObject.GetComponent<SosigWeapon>().FuseGrenade();
                    }
                }
            }

            public List<Sosig> SpawnHoldEnemyGroup(int phaseIndex, List<TNH_HoldPoint.AttackVector> AttackVectors, List<Transform> SpawnPoints_Turrets, List<Sosig> ActiveSosigs, ScanPhase phase, TNH_Manager M)
            {
                TNHFrameworkLogger.Log("Spawning enemy wave", TNHFrameworkLogger.LogType.TNH);

                //TODO add custom property form MinDirections
                int numAttackVectors = UnityEngine.Random.Range(1, MaxDirections + 1);
                numAttackVectors = Mathf.Clamp(numAttackVectors, 1, AttackVectors.Count);

                //Get the custom character data
                TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[M.C];

                //Set first enemy to be spawned as leader
                string selectedID = LeaderType;
                SosigEnemyTemplate enemyTemplate = ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[selectedID]];
                int enemiesToSpawn = UnityEngine.Random.Range(MinEnemies, MaxEnemies + 1);

                TNHFrameworkLogger.Log($"Spawning {enemiesToSpawn} hold guards (Phase {phaseIndex})", TNHFrameworkLogger.LogType.TNH);

                int sosigsSpawned = 0;
                int vectorSpawnPoint = 0;
                Vector3 targetVector;
                int vectorIndex = 0;
                List<Sosig> newSosigs = [];
                while (sosigsSpawned < enemiesToSpawn)
                {
                    TNHFrameworkLogger.Log("Spawning at attack vector: " + vectorIndex, TNHFrameworkLogger.LogType.TNH);

                    if (AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack.Count <= vectorSpawnPoint) break;

                    //Set the sosig's target position
                    if (SwarmPlayer)
                    {
                        targetVector = GM.CurrentPlayerBody.TorsoTransform.position;
                    }
                    else
                    {
                        targetVector = SpawnPoints_Turrets[UnityEngine.Random.Range(0, SpawnPoints_Turrets.Count)].position;
                    }

                    Sosig enemy = PatrolPatches.SpawnEnemy(enemyTemplate, character, AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack[vectorSpawnPoint], M, IFFUsed, true, targetVector, true);

                    ActiveSosigs.Add(enemy);
                    newSosigs.Add(enemy);
                    SosigKillTester tester = enemy.gameObject.AddComponent<SosigKillTester>();
                    tester.SosigID = selectedID;

                    //At this point, the leader has been spawned, so always set enemy to be regulars
                    selectedID = EnemyType.GetRandom();
                    enemyTemplate = ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[selectedID]];
                    sosigsSpawned += 1;

                    vectorIndex += 1;
                    if (vectorIndex >= numAttackVectors)
                    {
                        vectorIndex = 0;
                        vectorSpawnPoint += 1;
                    }
                }
                return newSosigs;
            }
        }
    }

    public class EncryptionPhase : ScanPhase
    {
        public List<TNH_EncryptionType> Encryptions;
        public int MinTargets;
        public int MaxTargets;
        public int MinTargetsLimited;
        public int MaxTargetsLimited;
        public float FirstWarningTime;
        public float SecondWarningTime;

        public override void BeginPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseBeginPhase(manager, character);

            //If we shouldn't spawn any targets, we exit out early
            if ((MaxTargets < 1 && manager.M.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking) ||
                (MaxTargetsLimited < 1 && manager.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo) ||
                (manager.M.TargetMode == TNHSetting_TargetMode.NoTargets))
            {
                EndPhase(manager, character);
                return;
            }

            manager.m_state = TNH_HoldPoint.HoldState.Hacking;
            manager.m_tickDownToFailure = PhaseLength;

            // Make sure we've actually generated the bastards
            if (manager.m_warpInTargets == new List<GameObject>())
            {
                SpawnWarpInMarkers(manager, character);
            } 

            if (manager.M.TargetMode == TNHSetting_TargetMode.Simple)
            {
                manager.M.EnqueueEncryptionLine(TNH_EncryptionType.Static);
                manager.DeleteAllActiveWarpIns();
                SpawnEncryptions(manager, true);
            }
            else
            {
                manager.M.EnqueueEncryptionLine(Encryptions[0]);
                manager.DeleteAllActiveWarpIns();
                SpawnEncryptions(manager, false);
            }

            manager.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Indentified);
        }

        public void SpawnEncryptions(TNH_HoldPoint holdPoint, bool isSimple)
        {
            int numTargets;
            if (holdPoint.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo)
            {
                numTargets = UnityEngine.Random.Range(MinTargetsLimited, MaxTargetsLimited + 1);
            }
            else
            {
                numTargets = UnityEngine.Random.Range(MinTargets, MaxTargets + 1);
            }

            List<FVRObject> encryptions;
            if (isSimple)
            {
                encryptions = [holdPoint.M.GetEncryptionPrefab(TNH_EncryptionType.Static)];
            }
            else
            {
                encryptions = Encryptions.Select(o => holdPoint.M.GetEncryptionPrefab(o)).ToList();
            }


            for (int i = 0; i < numTargets && i < holdPoint.m_validSpawnPoints.Count; i++)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(encryptions[i % encryptions.Count].GetGameObject(), holdPoint.m_validSpawnPoints[i].position, holdPoint.m_validSpawnPoints[i].rotation);
                TNH_EncryptionTarget encryption = gameObject.GetComponent<TNH_EncryptionTarget>();
                encryption.SetHoldPoint(holdPoint);
                holdPoint.RegisterNewTarget(encryption);
            }
        }

        public override void HoldUpdate(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseHoldUpdate(manager, character);

            foreach (EnemySpawn spawnWave in Waves)
            {
                spawnWave.SpawningRoutine(manager, this, character);
            }
            manager.m_isFirstWave = false;

            if (!manager.m_hasPlayedTimeWarning1 && TimeLeft < FirstWarningTime)
            {
                manager.m_hasPlayedTimeWarning1 = true;
                manager.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Reminder1);
            }
            if (!manager.m_hasPlayedTimeWarning2 && TimeLeft < SecondWarningTime)
            {
                manager.m_hasPlayedTimeWarning2 = true;
                manager.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Reminder2);
                manager.m_numWarnings++;
            }

            if (PhaseLength > 0f)
            {
                manager.m_systemNode.SetDisplayString("FAILURE IN: " + manager.FloatToTime(TimeLeft, "0:00.00"));
                if (TimeLeft <= 0f)
                {
                    manager.FailOut();
                }
            }
            else
            {
                manager.m_systemNode.SetDisplayString("REMAINING ENCRYPTIONS: " + manager.m_activeTargets.Count);
            }
        }

        public override void EndPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            string next = BaseEndPhase(manager, character);

            SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, manager.AUDEvent_HoldWave, manager.transform.position);

            // Check if our upcoming Phase isn't an encryption phase. Sounds cleaner to not use this voice line in this case.
            if (next != "End" && character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases[manager.m_phaseIndex] is not EncryptionPhase)
            {
                manager.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
            }
            else
            {
                manager.SpawnPoints_Targets.Shuffle();
                // manager.m_validSpawnPoints.Shuffle();
            }
            manager.m_validSpawnPoints.Clear();
            if (character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases[manager.m_phaseIndex] is not WarmupPhase || !(character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases[manager.m_phaseIndex] as WarmupPhase).IsEnd)
            {
                manager.M.EnqueueLine(TNH_VoiceLineID.AI_AdvancingToNextSystemLayer);
            }
            if (!manager.m_hasBeenDamagedThisPhase)
            {
                manager.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldWaveCompleteNoDamage, 1);
            }
        }

        public override void SpawnWarpInMarkers(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            manager.m_validSpawnPoints.Clear();
            for (int i = 0; i < manager.SpawnPoints_Targets.Count; i++)
            {
                if (manager.SpawnPoints_Targets[i] != null)
                {
                    TNH_EncryptionSpawnPoint component = manager.SpawnPoints_Targets[i].gameObject.GetComponent<TNH_EncryptionSpawnPoint>();
                    if (component == null)
                    {
                        manager.m_validSpawnPoints.Add(manager.SpawnPoints_Targets[i]);
                    }
                    else if (component.AllowedSpawns[(int)Encryptions[manager.m_validSpawnPoints.Count % Encryptions.Count]])
                    {
                        manager.m_validSpawnPoints.Add(manager.SpawnPoints_Targets[i]);
                    }
                }
            }
            if (manager.m_validSpawnPoints.Count <= 0)
            {
                manager.m_validSpawnPoints.Add(manager.SpawnPoints_Targets[0]);
            }
            manager.m_numTargsToSpawn = UnityEngine.Random.Range(MinTargets, MaxTargets + 1);
            manager.m_numTargsToSpawn = Mathf.Min(manager.m_numTargsToSpawn, manager.m_validSpawnPoints.Count);
            if (manager.M.TargetMode == TNHSetting_TargetMode.Simple)
            {
                manager.m_numTargsToSpawn = manager.GetMaxTargsInHold();
                if (manager.m_phaseIndex == 0)
                {
                    manager.m_numTargsToSpawn -= 2;
                }
                if (manager.m_phaseIndex == 1)
                {
                    manager.m_numTargsToSpawn--;
                }
                if (manager.m_numTargsToSpawn < 3)
                {
                    manager.m_numTargsToSpawn = 3;
                }
            }
            manager.m_validSpawnPoints.Shuffle();
            for (int j = 0; j < manager.m_numTargsToSpawn; j++)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(manager.M.Prefab_TargetWarpingIn, manager.m_validSpawnPoints[j].position, manager.m_validSpawnPoints[j].rotation);
                manager.m_warpInTargets.Add(gameObject);
            }
        }

    }

    public class HeadhuntPhase : ScanPhase
    {
        public int MinTargets;
        public int MaxTargets;
        public int MinTargetsLimited;
        public int MaxTargetsLimited;
        public List<string> ValidTargets = null;

        public int TargetsLeft = 0;

        public override void BeginPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseBeginPhase(manager, character);

            if (manager.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo)
            {
                TargetsLeft = UnityEngine.Random.Range(MinTargetsLimited, MaxTargetsLimited + 1);
            }
            else
            {
                TargetsLeft = UnityEngine.Random.Range(MinTargets, MaxTargets + 1);
            }

            manager.m_state = TNH_HoldPoint.HoldState.Hacking;
            manager.m_tickDownToFailure = PhaseLength;

            manager.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Indentified);
        }

        public override void HoldUpdate(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            BaseHoldUpdate(manager, character);

            foreach (EnemySpawn spawnWave in Waves)
            {
                List<Sosig> spawnedSosigs = spawnWave.SpawningRoutine(manager, this, character);

                foreach (Sosig sosig in spawnedSosigs)
                {
                    SosigKillTester killTester = sosig.GetComponent<SosigKillTester>();
                    if (ValidTargets == null || ValidTargets.Contains(killTester.SosigID))
                    {
                        killTester.InvokeOnKill += delegate { TargetsLeft -= 1; };
                    }
                }
            }
            manager.m_isFirstWave = false;

            if (PhaseLength > 0f)
            {
                manager.m_systemNode.SetDisplayString("FAILURE IN: " + manager.FloatToTime(TimeLeft, "0:00.00"));
                if (TimeLeft <= 0f)
                {
                    manager.FailOut();
                }
            }
            else
            {
                manager.m_systemNode.SetDisplayString("REMAINING ENEMIES: " + TargetsLeft);
            }
        }

        public override void EndPhase(TNH_HoldPoint manager, TakeAndHoldCharacter character)
        {
            string next = BaseEndPhase(manager, character);

            SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, manager.AUDEvent_HoldWave, manager.transform.position);

            // Check if our upcoming Phase isn't an encryption phase. Sounds cleaner to not use this voice line in this case.
            if (next != "End" && character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases[manager.m_phaseIndex] is not EncryptionPhase)
            {
                manager.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
            }
            else
            {
                manager.SpawnPoints_Targets.Shuffle();
                // manager.m_validSpawnPoints.Shuffle();
            }
            manager.m_validSpawnPoints.Clear();
            if (character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases[manager.m_phaseIndex] is not WarmupPhase || !(character.GetCurrentLevel(manager.M.m_curLevel).HoldPhases[manager.m_phaseIndex] as WarmupPhase).IsEnd)
            {
                manager.M.EnqueueLine(TNH_VoiceLineID.AI_AdvancingToNextSystemLayer);
            }
            if (!manager.m_hasBeenDamagedThisPhase)
            {
                manager.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldWaveCompleteNoDamage, 1);
            }
        }
    }

    public class Patrol
    {
        public List<string> EnemyType;
        public string LeaderType;
        public int PatrolSize;
        public int MaxPatrols;
        public int MaxPatrolsLimited;
        public float PatrolCadence;
        public float PatrolCadenceLimited;
        public int IFFUsed;
        public bool SwarmPlayer;
        public Sosig.SosigMoveSpeed AssualtSpeed;
        public bool IsBoss;
        public float DropChance;
        public bool DropsHealth;

        [JsonIgnore]
        private TNH_PatrolChallenge.Patrol patrol;

        public Patrol()
        {
            EnemyType = [];
        }

        public Patrol(V1.Patrol oldPatrol)
        {
            EnemyType = oldPatrol.EnemyType ?? [];
            LeaderType = oldPatrol.LeaderType;
            PatrolSize = oldPatrol.PatrolSize;
            MaxPatrols = oldPatrol.MaxPatrols;
            MaxPatrolsLimited = oldPatrol.MaxPatrolsLimited;
            PatrolCadence = oldPatrol.PatrolCadence;
            PatrolCadenceLimited = oldPatrol.PatrolCadenceLimited;
            IFFUsed = oldPatrol.IFFUsed;
            SwarmPlayer = oldPatrol.SwarmPlayer;
            AssualtSpeed = oldPatrol.AssualtSpeed;
            IsBoss = oldPatrol.IsBoss;
            DropChance = oldPatrol.DropChance;
            DropsHealth = oldPatrol.DropsHealth;
        }

        public Patrol(TNH_PatrolChallenge.Patrol patrol)
        {
            EnemyType = [patrol.EType.ToString()];
            LeaderType = patrol.LType.ToString();
            PatrolSize = patrol.PatrolSize;
            MaxPatrols = patrol.MaxPatrols;
            MaxPatrolsLimited = patrol.MaxPatrols_LimitedAmmo;
            PatrolCadence = patrol.TimeTilRegen;
            PatrolCadenceLimited = patrol.TimeTilRegen_LimitedAmmo;
            IFFUsed = patrol.IFFUsed;
            SwarmPlayer = false;
            AssualtSpeed = Sosig.SosigMoveSpeed.Walking;
            DropChance = 0.65f;
            DropsHealth = true;
            IsBoss = false;

            this.patrol = patrol;
        }

        public void Validate()
        {
            EnemyType ??= [];
        }

        public TNH_PatrolChallenge.Patrol GetPatrol()
        {
            if (patrol == null)
            {
                patrol = new TNH_PatrolChallenge.Patrol();

                //Try to get the necessary SosigEnemyIDs
                if (LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType[0]))
                {
                    patrol.EType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[EnemyType[0]];
                }
                else
                {
                    patrol.EType = (SosigEnemyID)Enum.Parse(typeof(SosigEnemyID), EnemyType[0]);
                }

                if (LoadedTemplateManager.SosigIDDict.ContainsKey(LeaderType))
                {
                    patrol.LType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[LeaderType];
                }
                else
                {
                    patrol.LType = (SosigEnemyID)Enum.Parse(typeof(SosigEnemyID), LeaderType);
                }

                patrol.PatrolSize = PatrolSize;
                patrol.MaxPatrols = MaxPatrols;
                patrol.MaxPatrols_LimitedAmmo = MaxPatrolsLimited;
                patrol.TimeTilRegen = PatrolCadence;
                patrol.TimeTilRegen_LimitedAmmo = PatrolCadenceLimited;
                patrol.IFFUsed = IFFUsed;
            }

            return patrol;
        }

    }



}
