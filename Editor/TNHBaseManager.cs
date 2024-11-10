using System;
using System.Collections.Generic;
using FistVR;
using UnityEngine;
using UnityEngine.Events;

namespace TNHFramework.Editor
{
    public abstract class TNHBaseManager
    {
        public static Framework_UIManager cUIManager = null;
        /*
        - These determine the options and characters your mode will use.
        - Classic is the default, it means it will use the same type of characters and options as classic Take & Hold. These should probably always be changed in your manager's Init() method.
        */
        public string OptionType = "Classic";
        public List<string> IncludeOptionsFor = [];
        public string CharacterType = "Classic";

        public static void BaseInit(Framework_UIManager uiManager)
        {
            cUIManager = uiManager;

            cUIManager.OptionsData["Mandatory"].Add("CurrentManager", new OptionsData("Game Length:", cUIManager.OptionsListPrefab, []));
        }

        public abstract void Init();

        public void BaseOnMenuStart()
        {
        }

        public abstract void OnMenuStart();

        public void BaseOnOptionsChange(string option, int value)
        {
            OnOptionsChange(option, value);
        }

        public abstract void OnOptionsChange(string option, int value);

        public class OptionsData(string label, GameObject prefab, Dictionary<string, Action> options, int defaultOption = 0)
        {
            public string Label = label;
            public int DefaultOption = defaultOption;
            public GameObject Prefab = prefab;
            public Dictionary<string, Action> Options = options;
        }
    }

    public class TNHManagerClassic : TNHBaseManager
    {
        public override void Init()
        {
            cUIManager.OptionsData["Mandatory"]["CurrentManager"].Options.Add("Classic", delegate { Framework_UIManager.CurrentManager = this; });
            cUIManager.OptionsData.Add(OptionType, []);

            cUIManager.OptionsData[OptionType].Add("TNHGameLength", new OptionsData("Game Length:", cUIManager.OptionsListPrefab, new Dictionary<string, Action>
            {
                { "5 Hold Standard", delegate { OnOptionsChange("TNHGameLength", 0); } },
                { "3 Hold Short", delegate { OnOptionsChange("TNHGameLength", 1); } },
                { "Endless", delegate { OnOptionsChange("TNHGameLength", 2); } }
            }));
            cUIManager.OptionsData[OptionType].Add("TNHSeed", new OptionsData("Hold Sequence Seed:", cUIManager.OptionsListPrefab, new Dictionary<string, Action>
            {
                { "I can't be bothered to add this right now. Get randomised, nerd. :>", delegate { OnOptionsChange("TNHSeed", 0); } }
            }));
            cUIManager.OnButtonChange += OnLevelButtonChange;
            cUIManager.OptionsData[OptionType].Add("TNHSpawnlock", new OptionsData("Equipment Mode:", cUIManager.OptionsListPrefab, new Dictionary<string, Action>
            {
                { "Spawnlock Enabled", delegate { OnOptionsChange("TNHSpawnlock", 0); } },
                { "Limited Ammo", delegate { OnOptionsChange("TNHSpawnlock", 1); } }
            }));
            cUIManager.OptionsData[OptionType].Add("TNHHealthMode", new OptionsData("Game Length:", cUIManager.OptionsListPrefab, new Dictionary<string, Action>
            {
                { "Standard", delegate { OnOptionsChange("TNHHealthMode", 0); } },
                { "Hardcore [1 hit]", delegate { OnOptionsChange("TNHHealthMode", 1); } },
                { "Custom (Score Disabled)", delegate { OnOptionsChange("TNHHealthMode", 2); } }
            }));
            cUIManager.OptionsData[OptionType].Add("TNHGameLength", new OptionsData("Game Length:", cUIManager.OptionsListPrefab, new Dictionary<string, Action>
            {
                { "5 Hold Standard", delegate { OnOptionsChange("TNHGameLength", 0); } },
                { "3 Hold Short", delegate { OnOptionsChange("TNHGameLength", 1); } },
                { "Endless", delegate { OnOptionsChange("TNHGameLength", 2); } }
            }));
            /*
            Options.Add("TNHHealthMode", new OptionsData("Health Mode:", ManagerType, new string[] { "Standard", "Hardcore [1 hit]", "Custom (Score Disabled)" }));
            Options.Add("TNHHealthMult", new OptionsData("Custom Health Mult:", ManagerType, new string[] { "Human", "Armoured", "Meaty", "Beefy", "Juggernaut" }));
            Options.Add("TNHAIDifficulty", new OptionsData("AI Difficulty", ManagerType, new string[] { "Arcade [1x]", "Hardcore [+3x]" }));
            Options.Add("TNHRadar", new OptionsData("Radar Detect Mode:", ManagerType, new string[] { "Always On [1x]", "Standard [+2x]", "Off [+3x]" }));
            Options.Add("TNHTargetMode", new OptionsData("Target Mode:", ManagerType, new string[] { "All Types [+3x]", "Simple [+2x]", "No Targets [1x]" }));
            Options.Add("TNHSpawnerMode", new OptionsData("Item Spawner:", ManagerType, new string[] { "Disabled", "Enabled  (Score Disabled)" }));
            Options.Add("TNHSosiggunReloading", new OptionsData("Sosiggun Reloading:", ManagerType, new string[] { "Disabled", "Enabled  (Score Disabled)" }));
            Options.Add("TNHMusic", new OptionsData("BG Audio:", ManagerType, new string[] { "Music", "Ambient Only" }));
            Options.Add("TNHAIVoice", new OptionsData("AI Narration:", ManagerType, new string[] { "Default", "Disabled" }));
            Options.Add("TNHRadarHand", new OptionsData("Radar Hand:", ManagerType, new string[] { "Left", "Right" }));
            */
        }

        public void OnLevelButtonChange()
        {
            if (cUIManager != null)
            {
            }
        }

        public override void OnMenuStart()
        {
        }

        public override void OnOptionsChange(string option, int value)
        {
            switch (option)
            {
                case "TNHGameLength":
                    GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)value;
                    break;
                case "TNHSeed":
                    GM.TNHOptions.TNHSeed = value;
                    break;
                case "TNHSpawnlock":
                    GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)value;
                    break;
                case "TNHHealthMode":
                    GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)value;
                    break;
                case "TNHHealthMult":
                    GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)value;
                    break;
                case "TNHAIDifficulty":
                    GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)value;
                    break;
                case "TNHRadar":
                    GM.TNHOptions.HealthMult = (TNH_HealthMult)value;
                    break;
                case "TNHSpawnerMode":
                    GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)value;
                    break;
                case "TNHSosiggunReloading":
                    GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)value;
                    break;
                case "TNHMusic":
                    GM.TNHOptions.BGAudioMode = (TNH_BGAudioMode)value;
                    break;
                case "TNHAIVoice":
                    GM.TNHOptions.AIVoiceMode = (TNH_AIVoiceMode)value;
                    break;
                case "TNHRadarHand":
                    GM.TNHOptions.RadarHand = (TNH_RadarHand)value;
                    break;
                default:
                    break;
            }
        }
    }
}
