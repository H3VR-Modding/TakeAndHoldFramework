using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FistVR;
using HarmonyLib;
using System.Linq;

namespace TNHFramework.Editor
{
    public class Framework_UIManager : MonoBehaviour
    {
        public Dictionary<string, Dictionary<string, TNHBaseManager.OptionsData>> OptionsData = [];

        public static Framework_UIManager CurrentUIManager;
        public static TNHBaseManager CurrentManager;
        public static List<TNHBaseManager> AllManagers;

        [Serializable]
        public class LevelData
        {
            public string LevelID;

            public string LevelDisplayName;

            public string LevelSceneName;

            public string LevelAuthor;

            public bool IsModLevel;

            public int TotalSeeds = 10;

            [Multiline(8)]
            public string LevelDescription;

            public Sprite LevelImage;
        }

        [Serializable]
        public class ManagerMenuData
        {
            public string ManagerType;
            public string ManagerCharacterType;

            public List<CategoryMenuData>[] Categories;
        }

        [Serializable]
        public class CategoryMenuData
        {
            public string CategoryName;

            public List<CharacterMenuData>[] Characters;
        }

        [Serializable]
        public class CharacterMenuData
        {
            public string CharacterID;
            public string CharacterName;
            public string CharacterDescription;
            public Sprite CharacterImage;
        }

        [Header("Level Options")]
        [HideInInspector]
        public string CurLevelID = "Classic";

        private int m_currentLevelIndex;

        public SceneLoader SceneLoader;

        [HideInInspector]
        public List<LevelData> Levels;

        [Header("Category/Character Section")]
        public Image SelectedCharacter_Image;

        public Text SelectedCharacter_Title;

        public Text SelectedCharacter_Description;

        [Header("Level Select Options")]
        public Text LBL_LevelName;

        public Text LBL_LevelAuthor;

        public Text LBL_LevelDescription;

        public Image IM_LevelImage;

        [HideInInspector]
        public List<AudioEvent> ButtonSoundEvents;

        [HideInInspector]
        // [Header("Scoring")]
        public TNH_ScoreDisplay ScoreDisplay;

        [Header("UI Prefabs")]
        public GameObject OptionDisplay;
        public GameObject OptionReserveHolder;
        public Dictionary<string, List<OptionsComponent>> OptionReserve = [];

        public GameObject ManagerPrefab;
        public GameObject OptionsListPrefab;
        public GameObject OptionPrefab;
        public GameObject RowPrefab;
        public GameObject PaddingPrefab;

        public GameObject CategoryList;
        public GameObject CharacterList;

        public event Action OnButtonChange;
        public event Action OnCategoryChange;
        public event Action OnCharacterChange;

        [Header("Runtime Data")]
        public List<TNH_CharacterDef> CharacterDefs = [];

        public Dictionary<string, ManagerMenuData> MenuLayout = [];

        public Text OptionTitle;
        [HideInInspector]
        public int OptionsPage = 0;
        [HideInInspector]
        public int SelectedManager = 0;

        [HideInInspector]
        public int CategoryPage = 0;
        [HideInInspector]
        public int SelectedCategory = 0;
        [HideInInspector]
        public int CharacterPage = 0;
        [HideInInspector]
        public int SelectedCharacter = 0;

        public static string SavedCategory;
        public static string SavedCharacter;
        //public Dictionary<string, int> SavedOptions = new Dictionary<string, int>();

        public void Start()
        {
            CurrentUIManager = this;
            InitLevelData();

            TNHBaseManager.BaseInit(this);
            foreach (TNHBaseManager gamemodeManager in AllManagers)
            {
                gamemodeManager.Init();
            }

            foreach (KeyValuePair<string, Dictionary<string, TNHBaseManager.OptionsData>> optionsCategory in OptionsData)
            {
                foreach (KeyValuePair<string, TNHBaseManager.OptionsData> optionsData in optionsCategory.Value)
                {
                    AddOptionsButton(optionsCategory.Key, optionsData.Key, optionsData.Value);
                }
            }

            RefreshOptions();
            /*
            ConfigureButtonStateFromOptions();
            for (int i = 0; i < LBL_CategoryName.Count; i++)
            {
                if (i < Categories.Count)
                {
                    LBL_CategoryName[i].gameObject.SetActive(true);
                    LBL_CategoryName[i].text = i + 1 + ". " + Categories[i].CategoryName;
                }
                else
                {
                    LBL_CategoryName[i].gameObject.SetActive(false);
                }
            }
            OBS_CharCategory.SetSelectedButton(0);
            SetSelectedCategory(0);
            UpdateOptionVis();
            */
        }

        public void SetCategoryPage(int pageModifier)
        {
            int target = CategoryPage + pageModifier;
            if ((target >= 0) && (MenuLayout[CurrentManager.CharacterType].Categories.Length > target))
            {
                CategoryPage += pageModifier;
                SelectedCategory = 0;
                CharacterPage = Math.Min(CharacterPage, MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][SelectedCategory].Characters[CharacterPage].Count);

                RefreshText();
            }
        }

        public void SetCharacterPage(int pageModifier)
        {
            int target = CharacterPage + pageModifier;
            if ((target >= 0) && (MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][SelectedCategory].Characters.Length > target))
            {
                CharacterPage += pageModifier;
                SelectedCharacter = 0;

                RefreshText();
            }
        }

        public void SetOptionsPage(int pageModifier)
        {
            int target = OptionsPage + pageModifier;
            if ((target >= 0) && (PageMax > target))
            {
                OptionsPage += pageModifier;

                RefreshOptions();
            }
        }

        public void RefreshText()
        {
            OptionsPanel_ButtonSet categoryButtonSet = CategoryList.GetComponent<OptionsPanel_ButtonSet>();
            for (int i = 0; i < categoryButtonSet.ButtonsInSet.Length; i++)
            {
                if (MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage].Count >= i)
                {
                    CategoryList.GetComponent<OptionsPanel_ButtonSet>().ButtonsInSet[i].gameObject.GetComponent<Text>().text = i + 1 + ". " + MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][i].CategoryName;
                }
                else
                {
                    CategoryList.GetComponent<OptionsPanel_ButtonSet>().ButtonsInSet[i].gameObject.GetComponent<Text>().text = "Temp missing";
                }
            }

            OptionsPanel_ButtonSet characterButtonSet = CharacterList.GetComponent<OptionsPanel_ButtonSet>();
            for (int i = 0; i < characterButtonSet.ButtonsInSet.Length; i++)
            {
                if (MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][SelectedCategory].Characters[CharacterPage].Count >= i)
                {
                    CategoryList.GetComponent<OptionsPanel_ButtonSet>().ButtonsInSet[i].gameObject.GetComponent<Text>().text = i + 1 + ". " + MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][SelectedCategory].Characters[CharacterPage][i].CharacterName;
                }
                else
                {
                    CategoryList.GetComponent<OptionsPanel_ButtonSet>().ButtonsInSet[i].gameObject.GetComponent<Text>().text = "Temp missing";
                }
            }
            CharacterMenuData pain = MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][SelectedCategory].Characters[CharacterPage][SelectedCharacter];
            SelectedCharacter_Title.text = pain.CharacterName;
            SelectedCharacter_Description.text = pain.CharacterDescription;
            SelectedCharacter_Image.sprite = pain.CharacterImage;
        }

        public void SetCategory(int buttonPosition)
        {
            SelectedCategory = buttonPosition;
            CharacterPage = Math.Min(CharacterPage, MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][SelectedCategory].Characters[CharacterPage].Count);

            RefreshText();
        }

        public void SetCharacter(int buttonPosition)
        {
            SelectedCharacter = buttonPosition;

            RefreshText();
        }

        public void SetManager(int buttonPosition)
        {
            SelectedManager = buttonPosition;
            CharacterPage = Math.Min(CharacterPage, MenuLayout[CurrentManager.CharacterType].Categories[CategoryPage][SelectedCategory].Characters[CharacterPage].Count);

            RefreshText();
        }

        private void InitLevelData()
        {
            CurLevelID = GM.TNHOptions.SavedLevelID;
            m_currentLevelIndex = GetCurrentLevelIndexFromID(CurLevelID);
            UpdateLevelSelectDisplayAndLoader();
        }

        private LevelData GetLevelData(string ID)
        {
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i].LevelID == ID)
                {
                    return Levels[i];
                }
            }
            return Levels[0];
        }

        private string GetDisplayNameFromID(string ID)
        {
            return GetLevelData(ID).LevelDisplayName;
        }

        private string GetSceneNameFromID(string ID)
        {
            return GetLevelData(ID).LevelSceneName;
        }

        private string GetAuthorFromID(string ID)
        {
            return GetLevelData(ID).LevelAuthor;
        }

        private string GetDescriptionFromID(string ID)
        {
            return GetLevelData(ID).LevelDescription;
        }

        private Sprite GetImageFromID(string ID)
        {
            return GetLevelData(ID).LevelImage;
        }

        private int GetNumSeedsFromID(string ID)
        {
            return GetLevelData(ID).TotalSeeds;
        }

        private int GetCurrentLevelIndexFromID(string ID)
        {
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i].LevelID == ID)
                {
                    return i;
                }
            }
            return 0;
        }

        private void UpdateLevelSelectDisplayAndLoader()
        {
            SceneLoader.LevelName = GetSceneNameFromID(CurLevelID);
            LBL_LevelName.text = GetDisplayNameFromID(CurLevelID);
            LBL_LevelAuthor.text = GetAuthorFromID(CurLevelID);
            LBL_LevelDescription.text = GetDescriptionFromID(CurLevelID);
            IM_LevelImage.sprite = GetImageFromID(CurLevelID);
            int numSeedsFromID = GetNumSeedsFromID(CurLevelID);
            /*
            for (int i = 0; i < SeedButtons.Count; i++)
            {
                if (i < numSeedsFromID)
                {
                    SeedButtons[i].SetActive(true);
                }
                else
                {
                    SeedButtons[i].SetActive(false);
                }
            }
            if (GM.TNHOptions.TNHSeed > numSeedsFromID - 1)
            {
                GM.TNHOptions.TNHSeed = -1;
                GM.TNHOptions.SaveToFile();
                OBS_RunSeed.SetSelectedButton(GM.TNHOptions.TNHSeed + 1);
            }
            */
        }

        public void BTN_NextLevel()
        {
            m_currentLevelIndex++;
            if (m_currentLevelIndex >= Levels.Count)
            {
                m_currentLevelIndex = 0;
            }
            CurLevelID = Levels[m_currentLevelIndex].LevelID;
            GM.TNHOptions.SavedLevelID = CurLevelID;
            GM.TNHOptions.SaveToFile();
            UpdateLevelSelectDisplayAndLoader();
            PlayButtonSound(2);
            OnButtonChange.Invoke();
        }

        public void BTN_PrevLevel()
        {
            m_currentLevelIndex--;
            if (m_currentLevelIndex < 0)
            {
                m_currentLevelIndex = Levels.Count - 1;
            }
            CurLevelID = Levels[m_currentLevelIndex].LevelID;
            GM.TNHOptions.SavedLevelID = CurLevelID;
            GM.TNHOptions.SaveToFile();
            UpdateLevelSelectDisplayAndLoader();
            PlayButtonSound(2);
            OnButtonChange.Invoke();
        }

        /*
        private void ConfigureButtonStateFromOptions()
        {
            OBS_Progression.SetSelectedButton((int)GM.TNHOptions.ProgressionTypeSetting);
            OBS_EquipmentMode.SetSelectedButton((int)GM.TNHOptions.EquipmentModeSetting);
            OBS_TargetMode.SetSelectedButton((int)GM.TNHOptions.TargetModeSetting);
            OBS_HealthMode.SetSelectedButton((int)GM.TNHOptions.HealthModeSetting);
            OBS_RunSeed.SetSelectedButton(GM.TNHOptions.TNHSeed + 1);
            OBS_AIDifficulty.SetSelectedButton((int)GM.TNHOptions.AIDifficultyModifier);
            OBS_AIRadarMode.SetSelectedButton((int)GM.TNHOptions.RadarModeModifier);
            OBS_HealthMult.SetSelectedButton((int)GM.TNHOptions.HealthMult);
            OBS_ItemSpawner.SetSelectedButton((int)GM.TNHOptions.ItemSpawnerMode);
            OBS_Backpack.SetSelectedButton((int)GM.TNHOptions.BackpackMode);
            OBS_SosiggunReloading.SetSelectedButton((int)GM.TNHOptions.SosiggunShakeReloading);
            OBS_BGAudio.SetSelectedButton((int)GM.TNHOptions.BGAudioMode);
            OBS_AINarration.SetSelectedButton((int)GM.TNHOptions.AIVoiceMode);
            OBS_RadarHand.SetSelectedButton((int)GM.TNHOptions.RadarHand);
            if (GM.TNHOptions.VerboseLogging)
            {
                OBS_VerboseDebugging.SetSelectedButton(1);
            }
            else
            {
                OBS_VerboseDebugging.SetSelectedButton(0);
            }
            try
            {
                SetCharacter((TNH_Char)GM.TNHOptions.LastPlayedChar);
            }
            catch
            {
                SetCharacter(TNH_Char.DD_BeginnerBlake);
            }
            UpdateTableBasedOnOptions();
        }

        private void UpdateTableBasedOnOptions()
        {
            TNH_Char lastPlayedChar = (TNH_Char)GM.TNHOptions.LastPlayedChar;
            TNH_CharacterDef def = CharDatabase.GetDef(lastPlayedChar);
            string tableID = ScoreDisplay.GetTableID(CurLevelID, def.TableID, GM.TNHOptions.ProgressionTypeSetting, GM.TNHOptions.EquipmentModeSetting, GM.TNHOptions.HealthModeSetting);
            ScoreDisplay.SwitchToModeID(tableID);
        }

        private void UpdateOptionVis()
        {
            if (GM.TNHOptions.HealthModeSetting == TNHSetting_HealthMode.CustomHealth)
            {
                CustomHealthOptions.SetActive(true);
            }
            else
            {
                CustomHealthOptions.SetActive(false);
            }
        }
        */

        private void PlayButtonSound(int i)
        {
            if (ButtonSoundEvents.Count > i)
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, ButtonSoundEvents[i], base.transform.position);
            }
        }

        /*
        private TNH_CharacterDef SetCharacter(TNH_Char c)
        {
            GM.TNHOptions.LastPlayedChar = (int)c;
            GM.TNHOptions.SaveToFile();
            TNH_CharacterDef def = CharDatabase.GetDef(c);
            SelectedCharacter_Image.sprite = def.Picture;
            SelectedCharacter_Title.text = def.DisplayName;
            SelectedCharacter_Description.text = def.Description;
            UpdateTableBasedOnOptions();
            return def;
        }

        public void SetSelectedCharacter(int i)
        {
            m_selectedCharacter = i;
            SetCharacter(Categories[m_selectedCategory].Characters[m_selectedCharacter]);
            PlayButtonSound(1);
        }
        */

        public void AddOptionsButton(string category, string internalName, TNHBaseManager.OptionsData options)
        {
            /*
            GameObject newOption = Instantiate(OptionsListPrefab);
            Signpost signpost = newOption.GetComponent<Signpost>();
            OptionsPanel_ButtonSet optionsSet = signpost.Signs[1].GetComponent<OptionsPanel_ButtonSet>();

            foreach (string option in options)
            {
                GameObject optionObject = Instantiate(OptionPrefab, signpost.Signs[1].transform);

                optionObject.GetComponent<Text>().text = option;
                optionsSet.ButtonsInSet.AddItem(optionObject.GetComponent<FVRPointableButton>());
            }

            */
            GameObject currentOption = null;
            OptionsComponent signpost;
            bool alreadyExists = false;
            foreach (OptionsComponent option in OptionReserve[category])
            {
                if (option.Identifier == internalName)
                {
                    currentOption = option.gameObject;
                    alreadyExists = true;
                    break;
                }
            }

            if (currentOption == null)
            {
                currentOption = Instantiate(OptionsListPrefab, OptionReserveHolder.transform);
            }

            if (currentOption != null)
            {
                signpost = currentOption.GetComponent<OptionsComponent>();
                signpost.AddOption(this, options);

                if (!alreadyExists)
                {
                    OptionReserve[category].Add(signpost);
                }
            }
        }

        [HideInInspector]
        public int PageMax = 0;

        public void RefreshOptions()
        {
            float[] PageHeights = [];
            int currentPage = 0;
            List<GameObject> options = [];

            // Clean the options display.
            for (int i = 0; i < OptionDisplay.transform.childCount - 1; i++)
            {
                Destroy(OptionDisplay.transform.GetChild(i));
            }
            for (int i = 0; i < OptionReserveHolder.transform.childCount - 1; i++)
            {
                options.Add(OptionReserveHolder.transform.GetChild(i).gameObject);
            }

            foreach (OptionsComponent option in OptionReserve[CurrentManager.OptionType])
            {
                float height = option.GetComponent<RectTransform>().rect.height;
                if ((height + PageHeights[currentPage]) > 900)
                {
                    currentPage++;
                }
                PageHeights[currentPage] += height;

                if (currentPage == OptionsPage)
                {
                    Instantiate(option, OptionDisplay.transform).transform.localScale = new Vector3(1, 1, 1);
                }
            }

            if (currentPage > PageMax)
            {
                PageMax = currentPage;
            }
            currentPage = 0;

            foreach (OptionsComponent option in OptionReserve[CurrentManager.OptionType])
            {
                float height = option.GetComponent<RectTransform>().rect.height;
                if ((height + PageHeights[currentPage]) > 900)
                {
                    currentPage++;
                }
                PageHeights[currentPage] += height;

                if (currentPage == OptionsPage)
                {
                    Instantiate(option.gameObject, OptionDisplay.transform).transform.localScale = new Vector3(1, 1, 1);
                }
            }

            if (currentPage > PageMax)
            {
                PageMax = currentPage;
            }
        }
    }
}