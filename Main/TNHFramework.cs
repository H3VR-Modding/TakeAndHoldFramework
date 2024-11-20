using BepInEx;
using BepInEx.Configuration;
using Deli.Setup;
using FistVR;
using HarmonyLib;
using Stratum;
using Stratum.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TNHFramework.ObjectTemplates;
using TNHFramework.Patches;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Anvil;
using UnityEngine.AI;
using TNHFramework.Editor;

namespace TNHFramework
{
    [BepInPlugin("h3vr.tnhframework", "TNH Framework", "0.2.0")]
    [BepInDependency(StratumRoot.GUID, StratumRoot.Version)]
    public class TNHFramework : StratumPlugin
    {
        public static ConfigEntry<string> manager;
        public static ConfigEntry<int> pathfindingIterations;
        private static ConfigEntry<bool> printCharacters;
        private static ConfigEntry<bool> logTNH;
        private static ConfigEntry<bool> logFileReads;
        private static ConfigEntry<bool> allowLog;
        public static ConfigEntry<bool> InternalMagPatcher;
        public static ConfigEntry<bool> BuildCharacterFiles;
        public static ConfigEntry<bool> ConvertFilesToYAML;
        public static ConfigEntry<bool> AlwaysMagUpgrader;
        public static ConfigEntry<bool> UnlimitedTokens;
        public static ConfigEntry<bool> EnableDebugText;
        public static ConfigEntry<bool> EnableScoring;

        public static string InfoPath;
        public static string OutputFilePath;

        public static Dictionary<string, Type> Serializables = [];

        // Bodged Magazine Patcher replacement stuff.
        public static Dictionary<FireArmRoundType, List<FVRObject>> CartridgeDictionary = [];
        public static Dictionary<FireArmMagazineType, List<FVRObject>> MagazineDictionary = [];
        public static Dictionary<FireArmClipType, List<FVRObject>> StripperDictionary = [];
        public static Dictionary<FireArmRoundType, List<FVRObject>> SpeedloaderDictionary = [];

        // Handle loading custom scripts
        public static List<TNHBaseManager> availableManagers = [];

        // Options for TNH Tweaker's in-built 

        // Variables used by various patches
        public static bool PreventOutfitFunctionality = false;
        public static List<int> SpawnedBossIndexes = [];
        public static List<int> PatrolIndexPool = [];
        public static List<int> SupplyPointIFFList = [];

        public static List<GameObject> SpawnedConstructors = [];
        public static List<GameObject> SpawnedPanels = [];
        public static List<EquipmentPoolDef.PoolEntry> SpawnedPools = [];

        public static List<List<string>> HoldActions = [];
        public static List<HoldStats> HoldStats = [];

        public static int GunsRecycled;
        public static int ShotsFired;

        /// <summary>
        /// First method that gets called
        /// </summary>
        private void Awake()
        {
            InfoPath = Path.GetDirectoryName(Info.Location);

            if (TNHFrameworkLogger.BepLog == null)
            {
                TNHFrameworkLogger.Init();
            }

            TNHFrameworkLogger.Log("Hello World (from TNH Tweaker)", TNHFrameworkLogger.LogType.General);

            SetupOutputDirectory();

            LoadConfigFile();
            LoadPanelSprites();

            Serializables.Add("TNHCharacter", typeof(TakeAndHoldCharacter));
            Serializables.Add("WarmupPhase", typeof(WarmupPhase));
            Serializables.Add("ScanPhase", typeof(ScanPhase));
            Serializables.Add("EncryptionPhase", typeof(EncryptionPhase));

            Harmony.CreateAndPatchAll(typeof(TNHFramework));
            Harmony.CreateAndPatchAll(typeof(TNHPatches));
            Harmony.CreateAndPatchAll(typeof(PatrolPatches));
            Harmony.CreateAndPatchAll(typeof(HoldPatches));
            Harmony.CreateAndPatchAll(typeof(HighScorePatches));

            if (EnableDebugText.Value)
                Harmony.CreateAndPatchAll(typeof(DebugPatches));
        }

        public override void OnSetup(IStageContext<Empty> ctx)
        {
            FrameworkLoaders Loader = new();

            ctx.Loaders.Add("tnhchar", Loader.TNHLoadChar);
            ctx.Loaders.Add("framechar", Loader.LoadChar);
            ctx.Loaders.Add("tnhsosig", Loader.LoadSosig);
            ctx.Loaders.Add("tnhvaultgun", Loader.LoadVaultFile);
        }

        public override IEnumerator OnRuntime(IStageContext<IEnumerator> ctx)
        {
            yield break;
        }



        /// <summary>
        /// Loads the sprites used in secondary panels in TNH
        /// </summary>
        private void LoadPanelSprites()
        {
            DirectoryInfo pluginDirectory = new(Path.GetDirectoryName(Info.Location));

            FileInfo file = ExtDirectoryInfo.GetFile(pluginDirectory, "mag_dupe_background.png");
            Sprite result = TNHFrameworkUtils.LoadSprite(file);
            MagazinePanel.background = result;

            file = ExtDirectoryInfo.GetFile(pluginDirectory, "ammo_purchase_background.png");
            result = TNHFrameworkUtils.LoadSprite(file);
            AmmoPurchasePanel.background = result;

            file = ExtDirectoryInfo.GetFile(pluginDirectory, "full_auto_background.png");
            result = TNHFrameworkUtils.LoadSprite(file);
            FullAutoPanel.background = result;

            file = ExtDirectoryInfo.GetFile(pluginDirectory, "fire_rate_background.png");
            result = TNHFrameworkUtils.LoadSprite(file);
            FireRatePanel.background = result;

            file = ExtDirectoryInfo.GetFile(pluginDirectory, "minus_icon.png");
            result = TNHFrameworkUtils.LoadSprite(file);
            FireRatePanel.minusSprite = result;

            file = ExtDirectoryInfo.GetFile(pluginDirectory, "plus_icon.png");
            result = TNHFrameworkUtils.LoadSprite(file);
            FireRatePanel.plusSprite = result;
        }




        /// <summary>
        /// Loads the BepInEx config file, and applies those settings
        /// </summary>
        private void LoadConfigFile()
        {
            TNHFrameworkLogger.Log("Getting config file", TNHFrameworkLogger.LogType.File);

            InternalMagPatcher = Config.Bind("General",
                                    "InternalMagPatcher",
                                    true,
                                    "If true and MagazinePatcher plugin is NOT used, run internal version. There may be a short delay in the TNH lobby");

            manager = Config.Bind("Manager",
                                    "ManagerType",
                                    "ClassicManager",
                                    "Set to true to enable logging");

            pathfindingIterations = Config.Bind("General",
                                    "PathfindingIterations",
                                    50,
                                    "performance shitn't");

            BuildCharacterFiles = Config.Bind("General",
                                    "BuildCharacterFiles",
                                    false,
                                    "If true, files useful for character creation will be generated in TNHTweaker folder");

            AlwaysMagUpgrader = Config.Bind("General",
                                    "AlwaysMagUpgrade",
                                    true,
                                    "If true, all Mag Duplicators become Mag Upgraders. This is default legacy TNHTweaker behavior.\n" +
                                    "If false, Mag Duplicators and Mag Upgraders are different. Mag Upgraders allow you to buy a new mag for your current gun, while Mag Duplicators don't.");

            ConvertFilesToYAML = Config.Bind("General",
                                    "ConvertFilesToYAML",
                                    false,
                                    "If true, any Stratum-based custom characters will have their JSON files converted to YAML");

            EnableScoring = Config.Bind("General",
                                    "EnableScoring",
                                    true,
                                    "Custom scoreboard is permanently offline, so this does nothing");

            allowLog = Config.Bind("Debug",
                                    "EnableLogging",
                                    true,
                                    "Set to true to enable logging");

            printCharacters = Config.Bind("Debug",
                                    "LogCharacterInfo",
                                    false,
                                    "Decide if should print all character info");

            logTNH = Config.Bind("Debug",
                                    "LogTNH",
                                    false,
                                    "If true, general TNH information will be logged");

            logFileReads = Config.Bind("Debug",
                                    "LogFileReads",
                                    false,
                                    "If true, reading from a file will log the reading process");

            UnlimitedTokens = Config.Bind("Debug",
                                    "EnableUnlimitedTokens",
                                    false,
                                    "If true, you will spawn with 999999 tokens for any character in TNH (useful for testing loot pools)");

            EnableDebugText = Config.Bind("Debug",
                                    "EnableDebugText",
                                    false,
                                    "If true, some text will appear in TNH maps showing additional info");

            

            TNHFrameworkLogger.AllowLogging = allowLog.Value;
            TNHFrameworkLogger.LogCharacter = printCharacters.Value;
            TNHFrameworkLogger.LogTNH = logTNH.Value;
            TNHFrameworkLogger.LogFile = logFileReads.Value;
        }


        /// <summary>
        /// Creates the main TNH Tweaker file folder
        /// </summary>
        private void SetupOutputDirectory()
        {
            OutputFilePath = Path.Combine(Path.GetDirectoryName(Info.Location), "CharFiles");

            if (!Directory.Exists(OutputFilePath))
            {
                Directory.CreateDirectory(OutputFilePath);
            }
        }



        [HarmonyPatch(typeof(TNH_ScoreDisplay), "SubmitScoreAndGoToBoard")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool PreventScoring(TNH_ScoreDisplay __instance, int score)
        {
            TNHFrameworkLogger.Log("Preventing vanilla score submission", TNHFrameworkLogger.LogType.TNH);

            GM.Omni.OmniFlags.AddScore(__instance.m_curSequenceID, score);

            __instance.m_hasCurrentScore = true;
            __instance.m_currentScore = score;

            //Draw local scores
            __instance.RedrawHighScoreDisplay(__instance.m_curSequenceID);

            GM.Omni.SaveToFile();

            return false;
        }

        [HarmonyPatch(typeof(AIManager), "Start")]
        [HarmonyPostfix]
        public static void AIPerfFix(AIManager __instance)
        {
            NavMesh.pathfindingIterationsPerFrame = pathfindingIterations.Value;
        }

        [HarmonyPatch(typeof(Sosig), "Start")]
        [HarmonyPostfix]
        public static void AIPerfFix2(Sosig __instance)
        {
            __instance.Agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        [HarmonyPatch(typeof(Sosig), "TryToGetTo")]
        [HarmonyPrefix]
        public static bool AIPerfFix3(Sosig __instance, Vector3 v)
        {
            if (!__instance.Agent.enabled)
            {
                return false;
            }
            __instance.m_navToPoint = v;
            float num = Vector3.Distance(__instance.m_navToPoint, __instance.lastDest);
            __instance.debug_haspath = __instance.Agent.hasPath;
            float num2 = Vector3.Distance(__instance.m_navToPoint, __instance.Agent.transform.position);
            __instance.debug_pathpending = __instance.Agent.pathPending;
            if (!__instance.m_isOnOffMeshLink && !__instance.debug_pathpending && (num > 0.2f || (!__instance.debug_haspath && num2 > 1f)))
            {
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(v, out navMeshHit, 1f, -1))
                {
                    v = navMeshHit.position;
                }
                if (__instance.Agent.isOnNavMesh)
                {
                    __instance.Agent.SetDestination(v);
                }
                __instance.lastDest = v;
            }
            Vector3 velocity = __instance.Agent.velocity;
            Vector3 vector = __instance.transform.InverseTransformDirection(velocity);
            Vector3 vector2 = new Vector3(Mathf.Clamp(-vector.z, -3f, 3f), 0f, Mathf.Clamp(vector.x, -3f, 3f)) * __instance.MovementRotMagnitude;
            __instance.curEuluer = Vector3.Lerp(__instance.curEuluer, vector2, Time.deltaTime * 1f);
            __instance.Pose_Standing.localEulerAngles = __instance.m_poseLocalEulers_Standing + __instance.curEuluer;
            __instance.Pose_Crouching.localEulerAngles = __instance.m_poseLocalEulers_Crouching + __instance.curEuluer;
            __instance.Pose_Prone.localEulerAngles = __instance.m_poseLocalEulers_Prone + __instance.curEuluer;

            return false;
        }
    }

    public class TNHTweakerDeli : DeliBehaviour
    {
        public void Awake()
        {
            if (TNHFrameworkLogger.BepLog == null)
            {
                TNHFrameworkLogger.Init();
            }

            Stages.Setup += DeliOnSetup;
        }

        /// <summary>
        /// Performs initial setup for TNH Tweaker
        /// </summary>
        /// <param name="stage"></param>
        private void DeliOnSetup(SetupStage stage)
        {
            stage.SetupAssetLoaders[Source, "sosig"] = new SosigLoaderDeli().LoadAsset;
            stage.SetupAssetLoaders[Source, "vault_file"] = new VaultFileLoaderDeli().LoadAsset;
            stage.SetupAssetLoaders[Source, "character"] = new CharacterLoaderDeli().LoadAsset;
        }
    }
}
