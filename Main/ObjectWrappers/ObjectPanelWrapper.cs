using FistVR;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TNHFramework
{
    public enum PanelType
    {
        MagDuplicator,
        Recycler,
        AmmoReloader,
        MagUpgrader,
        AddFullAuto,
        AmmoPurchase,
        FireRateUp,
        FireRateDown,
        MagPurchase,
    }

    public class MagazinePanel : MonoBehaviour
    {
        public TNH_MagDuplicator original;

        public Dictionary<string, MagazineBlacklistEntry> blacklist;
        public int DupeCost = 2;
        public int UpgradeCost = 3;
        public int PurchaseCost = 2;
        private int numTokensSelected = 0;

        public static Sprite background;

        private TNH_ObjectConstructorIcon DupeIcon;
        private TNH_ObjectConstructorIcon UpgradeIcon;
        private TNH_ObjectConstructorIcon PurchaseIcon;

        private Text priceText_0;
        private Text priceText_1;
        private Text priceText_2;

        private FVRPhysicalObject selectedObject = null;
        private FVRFireArmMagazine detectedMag = null;
        private Speedloader detectedSpeedLoader = null;
        private FVRObject purchaseMag = null;
        private FVRObject upgradeMag = null;

        private Collider[] colBuffer = new Collider[50];

        public void Awake()
        {
            original = gameObject.GetComponent<TNH_MagDuplicator>();

            if (original == null)
                TNHFrameworkLogger.LogError("Mag Upgrader failed, original Mag Duplicator was null!");

            original.enabled = false;
            blacklist = LoadedTemplateManager.LoadedCharactersDict[original.M.C].GetMagazineBlacklist();

            InitPanel();
            UpdateIcons();

            GM.TNH_Manager.TokenCountChangeEvent += UpdateTokenDisplay;
        }

        private void OnDestroy()
        {
            if (original.M != null)
                original.M.TokenCountChangeEvent -= UpdateTokenDisplay;
        }

        private void InitPanel()
        {
            Transform titleTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/AmmoReloaderTitle (3)");

            Text titleText = titleTransform.gameObject.GetComponent<Text>();
            titleText.text = "-- Mag Dupe / Upgrade / Buy --";

            Transform backingTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/Backing");

            Transform canvasHolder = original.transform.Find("_CanvasHolder/_UITest_Canvas");

            Transform iconTransform_0 = canvasHolder.Find("Icon_0");
            iconTransform_0.localPosition = new Vector3(-270, -200, 0);

            Transform iconTransform_1 = canvasHolder.Find("Icon_1");
            iconTransform_1.localPosition = new Vector3(0, -200, 0);

            Transform iconTransform_2 = Instantiate(iconTransform_1.gameObject, canvasHolder).transform;
            iconTransform_2.localPosition = new Vector3(275, -200, 0);

            Transform buttonTransform_0 = original.transform.Find("PointableButton_0");
            buttonTransform_0.position = iconTransform_0.position;

            Transform buttonTransform_1 = original.transform.Find("PointableButton_1");
            buttonTransform_1.position = iconTransform_1.position;

            Transform buttonTransform_2 = Instantiate(buttonTransform_1.gameObject, buttonTransform_1.parent).transform;
            buttonTransform_2.position = iconTransform_2.position;

            Image backgroundImage = backingTransform.gameObject.GetComponent<Image>();

            backgroundImage.sprite = background;

            DupeIcon = iconTransform_0.gameObject.GetComponent<TNH_ObjectConstructorIcon>();
            UpgradeIcon = iconTransform_1.gameObject.GetComponent<TNH_ObjectConstructorIcon>();
            PurchaseIcon = iconTransform_2.gameObject.GetComponent<TNH_ObjectConstructorIcon>();

            Button button_0 = buttonTransform_0.gameObject.GetComponent<Button>();
            button_0.onClick = new Button.ButtonClickedEvent();
            button_0.onClick.AddListener(() => { DupeMagButton(); });

            Button button_1 = buttonTransform_1.gameObject.GetComponent<Button>();
            button_1.onClick = new Button.ButtonClickedEvent();
            button_1.onClick.AddListener(() => { UpgradeMagButton(); });

            Button button_2 = buttonTransform_2.gameObject.GetComponent<Button>();
            button_2.onClick = new Button.ButtonClickedEvent();
            button_2.onClick.AddListener(() => { PurchaseMagButton(); });

            priceText_0 = AddPriceText(iconTransform_0, new Vector3(-235, 155, 0));
            priceText_1 = AddPriceText(iconTransform_1, new Vector3(40, 155, 0));
            priceText_2 = AddPriceText(iconTransform_2, new Vector3(355, 150, 0));
            priceText_2.alignment = TextAnchor.MiddleLeft;

            priceText_0.text = "x" + DupeCost;
            priceText_1.text = "x" + UpgradeCost;
            priceText_2.text = "x" + PurchaseCost;
        }


        private Text AddPriceText(Transform iconTransform, Vector3 localPosition)
        {
            GameObject canvas = new GameObject("PriceCanvas");
            canvas.transform.SetParent(iconTransform.parent);
            canvas.transform.rotation = iconTransform.rotation;
            canvas.transform.localPosition = localPosition;

            Canvas canvasComp = canvas.AddComponent<Canvas>();
            RectTransform rect = canvasComp.GetComponent<RectTransform>();
            canvasComp.renderMode = RenderMode.WorldSpace;
            rect.sizeDelta = new Vector2(1, 1);

            GameObject text = new GameObject("Text");
            text.transform.SetParent(canvas.transform);
            text.transform.rotation = canvas.transform.rotation;
            text.transform.localPosition = Vector3.zero;

            text.AddComponent<CanvasRenderer>();
            Text textComp = text.AddComponent<Text>();
            Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

            textComp.text = "x?";
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.fontSize = 30;
            textComp.fontStyle = FontStyle.Bold;
            text.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
            textComp.font = ArialFont;
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;

            return textComp;
        }

        private void DupeMagButton()
        {
            if ((detectedMag == null && detectedSpeedLoader == null) || original.M.GetNumTokens() < DupeCost)
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Fail, transform.position);
            }

            else
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Spawn, transform.position);
                original.M.SubtractTokens(DupeCost);

                if (detectedMag != null)
                {
                    TNHFramework.HoldActions[original.M.m_level].Add($"Duplicated {detectedMag.ObjectWrapper.DisplayName}");
                    FirearmUtils.SpawnDuplicateMagazine(original.M, detectedMag, original.Spawnpoint_Mag.position, original.Spawnpoint_Mag.rotation);
                }

                else
                {
                    TNHFramework.HoldActions[original.M.m_level].Add($"Duplicated {detectedSpeedLoader.ObjectWrapper.DisplayName}");
                    FirearmUtils.SpawnDuplicateSpeedloader(original.M, detectedSpeedLoader, original.Spawnpoint_Mag.position, original.Spawnpoint_Mag.rotation);
                }

                ClearSelection();
                UpdateIcons();
            }
        }

        private void UpgradeMagButton()
        {
            if (upgradeMag == null || original.M.GetNumTokens() < UpgradeCost)
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Fail, transform.position);
            }

            else
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Spawn, transform.position);
                original.M.SubtractTokens(UpgradeCost);

                TNHFramework.HoldActions[original.M.m_level].Add($"Upgraded {detectedMag.ObjectWrapper.DisplayName} To {upgradeMag.DisplayName}");

                Destroy(detectedMag.GameObject);
                Instantiate(upgradeMag.GetGameObject(), original.Spawnpoint_Mag.position, original.Spawnpoint_Mag.rotation);

                ClearSelection();
                UpdateIcons();
            }
        }

        private void PurchaseMagButton()
        {
            if (purchaseMag == null || original.M.GetNumTokens() < PurchaseCost)
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Fail, transform.position);
            }

            else
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Spawn, transform.position);
                original.M.SubtractTokens(PurchaseCost);

                TNHFramework.HoldActions[original.M.m_level].Add($"Purchased {purchaseMag.DisplayName}");

                Instantiate(purchaseMag.GetGameObject(), original.Spawnpoint_Mag.position, original.Spawnpoint_Mag.rotation);

                ClearSelection();
                UpdateIcons();
            }
        }


        private void Update()
        {
            if ((float)Vector3.Distance(original.ScanningVolume.position, GM.CurrentPlayerBody.Head.transform.position) < 2f)
            {
                Scan();
                UpdateIcons();
            }
            else
            {
                ClearSelection();
            }
        }

        private void ClearSelection()
        {
            detectedMag = null;
            detectedSpeedLoader = null;
            purchaseMag = null;
            upgradeMag = null;
            selectedObject = null;

            original.PlacementBoundsRenderer?.gameObject.SetActive(false);
        }

        private void Scan()
        {
            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[original.M.C];

            if (selectedObject != null)
            {
                // Find the bounds of the selected object
                Bounds objBounds = new(selectedObject.transform.position, new Vector3(0.01f, 0.01f, 0.01f));
                Collider[] objColliders = selectedObject.transform.GetComponentsInChildren<Collider>();

                foreach (Collider collider in objColliders)
                {
                    if (collider != null && collider.isTrigger)
                    {
                        objBounds.Encapsulate(collider.bounds);
                    }
                }

                // Get the selected object's position
                Vector3 objPosition = selectedObject.RootRigidbody.position;
                Vector3 b = objBounds.center - selectedObject.transform.position;

                // Create the scanning box effect
                if (original.PlacementBoundsRenderer == null)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original.PlacementBoundsPrefab, objPosition, Quaternion.identity);
                    original.PlacementBoundsRenderer = gameObject.GetComponent<TG_Bounds>();
                }

                // Place the scanning box effect over the selected object
                original.PlacementBoundsRenderer.gameObject.SetActive(true);
                original.PlacementBoundsRenderer.PoseAndRescale(objPosition + b, objBounds.extents * 2f, original.EmitterPoint.position);

                // Find the bounds of the scanning volume
                Bounds scanBounds = new(original.ScanningVolume.position, new Vector3(0.001f, 0.001f, 0.001f));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(0.5f, 0.5f, 0.5f)));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(-0.5f, -0.5f, -0.5f)));

                // If the selected object is no longer within the scanning volume, deselect it
                if (!objBounds.Intersects(scanBounds))
                {
                    ClearSelection();
                }
            }

            if (selectedObject == null)
            {
                // Get a list of colliders within the scanning volume
                int colliderCount = Physics.OverlapBoxNonAlloc(original.ScanningVolume.position, original.ScanningVolume.localScale * 0.5f, colBuffer, original.ScanningVolume.rotation, original.ScanningLM, QueryTriggerInteraction.Collide);

                detectedMag = null;
                detectedSpeedLoader = null;
                purchaseMag = null;
                upgradeMag = null;

                for (int i = 0; i < colliderCount; i++)
                {
                    if (colBuffer[i].attachedRigidbody != null)
                    {
                        // Detect firearms
                        FVRFireArm firearm = colBuffer[i].GetComponent<FVRFireArm>();
                        if (purchaseMag == null && firearm != null && !firearm.IsHeld && firearm.QuickbeltSlot == null)
                        {
                            MagazineBlacklistEntry entry = (blacklist.ContainsKey(firearm.ObjectWrapper.ItemID) ? blacklist[firearm.ObjectWrapper.ItemID] : null);
                            List<FVRObject> spawnableMags = FirearmUtils.GetCompatibleMagazines(firearm.ObjectWrapper, -1, -1, false, character.GlobalObjectBlacklist, entry);

                            if (spawnableMags.Count > 0)
                            {
                                purchaseMag = FirearmUtils.GetSmallestCapacityMagazine(spawnableMags, character.GlobalObjectBlacklist, entry);
                                selectedObject = firearm;
                            }
                        }

                        // Detect magazines
                        FVRFireArmMagazine mag = colBuffer[i].attachedRigidbody.GetComponent<FVRFireArmMagazine>();
                        if (mag != null && mag.FireArm == null && !mag.IsHeld && mag.QuickbeltSlot == null && !mag.IsIntegrated)
                        {
                            detectedMag = mag;
                            selectedObject = mag;
                        }

                        // Detect speedloaders
                        Speedloader speedloader = colBuffer[i].attachedRigidbody.GetComponent<Speedloader>();
                        if (speedloader != null && !speedloader.IsHeld && speedloader.QuickbeltSlot == null && speedloader.IsPretendingToBeAMagazine)
                        {
                            detectedSpeedLoader = speedloader;
                            selectedObject = speedloader;
                        }

                        // If at this point we have a valid ammo container and firearm, we can stop looping
                        if (purchaseMag != null && (detectedMag != null || detectedSpeedLoader != null)) break;
                    }
                }
            }
        }

        private void UpdateIcons()
        {
            TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[original.M.C];

            DupeIcon.State = TNH_ObjectConstructorIcon.IconState.Cancel;
            UpgradeIcon.State = TNH_ObjectConstructorIcon.IconState.Cancel;
            PurchaseIcon.State = TNH_ObjectConstructorIcon.IconState.Cancel;
            int numTokens = GM.TNH_Manager.GetNumTokens();
            numTokensSelected = 0;

            if (detectedMag != null || detectedSpeedLoader != null)
            {
                DupeIcon.State = TNH_ObjectConstructorIcon.IconState.Accept;
                if (numTokens >= DupeCost) numTokensSelected = DupeCost;
            }

            if (purchaseMag != null)
            {
                PurchaseIcon.State = TNH_ObjectConstructorIcon.IconState.Accept;
                if (numTokens >= PurchaseCost) numTokensSelected = PurchaseCost;
            }

            if (detectedMag != null)
            {
                upgradeMag = FirearmUtils.GetNextHighestCapacityMagazine(detectedMag.ObjectWrapper);
                if (upgradeMag != null)
                {
                    UpgradeIcon.State = TNH_ObjectConstructorIcon.IconState.Accept;
                    if (numTokens >= UpgradeCost) numTokensSelected = UpgradeCost;
                }
            }

            DupeIcon.UpdateIconDisplay();
            UpgradeIcon.UpdateIconDisplay();
            PurchaseIcon.UpdateIconDisplay();
            UpdateTokenDisplay(numTokens);
        }

        private void UpdateTokenDisplay(int numTokens)
        {
            // Display the total number of tokens
            for (int i = 0; i < original.TokenList.Count; i++)
            {
                if (i < numTokens)
                {
                    if (i < numTokens - numTokensSelected)
                    {
                        original.TokenList[i].color = original.Token_Unselected;
                    }
                    else
                    {
                        original.TokenList[i].color = original.Token_Selected;
                    }
                }
                else
                {
                    original.TokenList[i].color = original.Token_Empty;
                }
            }
        }
    }




    public class AmmoPurchasePanel : MonoBehaviour
    {
        public TNH_MagDuplicator original;
        public Dictionary<string, MagazineBlacklistEntry> blacklist;
        public int PanelCost = 1;
        private int numTokensSelected = 0;

        public static Sprite background;

        private TNH_ObjectConstructorIcon PurchaseIcon;

        private FVRPhysicalObject selectedObject = null;
        private FVRFireArm detectedFirearm = null;
        private Collider[] colBuffer = new Collider[50];

        public void Awake()
        {
            original = gameObject.GetComponent<TNH_MagDuplicator>();

            if (original == null)
                TNHFrameworkLogger.LogError("Ammo Purchaser failed, original mag duplicator was null!");
            
            original.enabled = false;

            InitPanel();
            UpdateIcons();

            GM.TNH_Manager.TokenCountChangeEvent += UpdateTokenDisplay;
        }

        private void OnDestroy()
        {
            if (original.M != null)
                original.M.TokenCountChangeEvent -= UpdateTokenDisplay;
        }

        private void InitPanel()
        {
            Transform titleTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/AmmoReloaderTitle (3)");

            Text titleText = titleTransform.gameObject.GetComponent<Text>();
            titleText.text = "-- Ammo Purchaser --";

            Transform backingTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/Backing");

            Transform canvasHolder = original.transform.Find("_CanvasHolder/_UITest_Canvas");

            Transform iconTransform_0 = canvasHolder.Find("Icon_0");
            iconTransform_0.localPosition = new Vector3(0, -290, 0);

            Transform iconTransform_1 = canvasHolder.Find("Icon_1");
            Destroy(iconTransform_1.gameObject);

            Transform buttonTransform_0 = original.transform.Find("PointableButton_0");
            buttonTransform_0.position = iconTransform_0.position;

            Transform buttonTransform_1 = original.transform.Find("PointableButton_1");
            Destroy(buttonTransform_1.gameObject);

            Image backgroundImage = backingTransform.gameObject.GetComponent<Image>();
            backgroundImage.sprite = background;
;
            PurchaseIcon = iconTransform_0.gameObject.GetComponent<TNH_ObjectConstructorIcon>();

            Button button_0 = buttonTransform_0.gameObject.GetComponent<Button>();
            button_0.onClick = new Button.ButtonClickedEvent();
            button_0.onClick.AddListener(() => { PurchaseAmmoButton(); });
        }


        public void PurchaseAmmoButton()
        {
            if (detectedFirearm == null || original.M.GetNumTokens() < PanelCost)
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Fail, transform.position);
                return;
            }

            else
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Spawn, transform.position);
                original.M.SubtractTokens(PanelCost);

                FVRObject.OTagFirearmRoundPower roundPower = AM.GetRoundPower(detectedFirearm.RoundType);
                int numSpawned = GetRoundsToSpawn(roundPower);

                TNHFrameworkLogger.Log("Compatible rounds count for " + detectedFirearm.ObjectWrapper.ItemID + ": " + IM.OD[detectedFirearm.ObjectWrapper.ItemID].CompatibleSingleRounds.Count, TNHFrameworkLogger.LogType.General);

                TakeAndHoldCharacter character = LoadedTemplateManager.LoadedCharactersDict[original.M.C];
                MagazineBlacklistEntry blacklistEntry = null;
                if (character.GetMagazineBlacklist().ContainsKey(detectedFirearm.ObjectWrapper.ItemID)) blacklistEntry = character.GetMagazineBlacklist()[detectedFirearm.ObjectWrapper.ItemID];

                FVRObject compatibleRound = FirearmUtils.GetCompatibleRounds(detectedFirearm.ObjectWrapper, character.ValidAmmoEras, character.ValidAmmoSets, character.GlobalAmmoBlacklist, character.GlobalObjectBlacklist, blacklistEntry).GetRandom();

                AnvilManager.Run(SpawnRounds(original.M, compatibleRound, numSpawned));

                ClearSelection();
                UpdateIcons();
            }
        }

        public IEnumerator SpawnRounds(TNH_Manager M, FVRObject bullet, int count)
        {
            GameObject bulletObject = bullet.GetGameObject();
            return TNHFrameworkUtils.InstantiateMultiple(M, bulletObject, original.Spawnpoint_Mag.position, count);
        }

        public int GetRoundsToSpawn(FVRObject.OTagFirearmRoundPower roundPower)
        {
            if (roundPower == FVRObject.OTagFirearmRoundPower.Shotgun) return 18;
            if (roundPower == FVRObject.OTagFirearmRoundPower.Pistol) return 30;
            if (roundPower == FVRObject.OTagFirearmRoundPower.Tiny) return 30;
            if (roundPower == FVRObject.OTagFirearmRoundPower.Intermediate) return 20;
            if (roundPower == FVRObject.OTagFirearmRoundPower.FullPower) return 16;
            if (roundPower == FVRObject.OTagFirearmRoundPower.Exotic) return 16;
            if (roundPower == FVRObject.OTagFirearmRoundPower.AntiMaterial) return 6;
            if (roundPower == FVRObject.OTagFirearmRoundPower.Ordnance) return 3;

            return 10;
        }

        private void Update()
        {
            if ((float)Vector3.Distance(original.ScanningVolume.position, GM.CurrentPlayerBody.Head.transform.position) < 2f)
            {
                Scan();
                UpdateIcons();
            }
            else
            {
                ClearSelection();
            }
        }

        private void ClearSelection()
        {
            detectedFirearm = null;
            selectedObject = null;

            original.PlacementBoundsRenderer?.gameObject.SetActive(false);
        }

        private void Scan()
        {
            if (selectedObject != null)
            {
                // Find the bounds of the selected object
                Bounds objBounds = new(selectedObject.transform.position, new Vector3(0.01f, 0.01f, 0.01f));
                Collider[] objColliders = selectedObject.transform.GetComponentsInChildren<Collider>();

                foreach (Collider collider in objColliders)
                {
                    if (collider != null && collider.isTrigger)
                    {
                        objBounds.Encapsulate(collider.bounds);
                    }
                }

                // Get the selected object's position
                Vector3 objPosition = selectedObject.RootRigidbody.position;
                Vector3 b = objBounds.center - selectedObject.transform.position;

                // Create the scanning box effect
                if (original.PlacementBoundsRenderer == null)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original.PlacementBoundsPrefab, objPosition, Quaternion.identity);
                    original.PlacementBoundsRenderer = gameObject.GetComponent<TG_Bounds>();
                }

                // Place the scanning box effect over the selected object
                original.PlacementBoundsRenderer.gameObject.SetActive(true);
                original.PlacementBoundsRenderer.PoseAndRescale(objPosition + b, objBounds.extents * 2f, original.EmitterPoint.position);

                // Find the bounds of the scanning volume
                Bounds scanBounds = new(original.ScanningVolume.position, new Vector3(0.001f, 0.001f, 0.001f));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(0.5f, 0.5f, 0.5f)));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(-0.5f, -0.5f, -0.5f)));

                // If the selected object is no longer within the scanning volume, deselect it
                if (!objBounds.Intersects(scanBounds))
                {
                    ClearSelection();
                }
            }

            if (selectedObject == null)
            {
                int colliderCount = Physics.OverlapBoxNonAlloc(original.ScanningVolume.position, original.ScanningVolume.localScale * 0.5f, colBuffer, original.ScanningVolume.rotation, original.ScanningLM, QueryTriggerInteraction.Collide);

                detectedFirearm = null;

                for (int i = 0; i < colliderCount; i++)
                {
                    if (colBuffer[i].attachedRigidbody != null)
                    {
                        FVRFireArm firearm = colBuffer[i].attachedRigidbody.GetComponent<FVRFireArm>();

                        if (firearm != null && !firearm.IsHeld && firearm.QuickbeltSlot == null)
                        {
                            detectedFirearm = firearm;
                            selectedObject = firearm;
                            return;
                        }
                    }
                }
            }
        }

        private void UpdateIcons()
        {
            PurchaseIcon.State = TNH_ObjectConstructorIcon.IconState.Cancel;
            int numTokens = GM.TNH_Manager.GetNumTokens();
            numTokensSelected = 0;

            if (detectedFirearm != null)
            {
                PurchaseIcon.State = TNH_ObjectConstructorIcon.IconState.Accept;
                if (numTokens >= PanelCost) numTokensSelected = PanelCost;
            }

            PurchaseIcon.UpdateIconDisplay();
            UpdateTokenDisplay(numTokens);
        }

        private void UpdateTokenDisplay(int numTokens)
        {
            // Display the total number of tokens
            for (int i = 0; i < original.TokenList.Count; i++)
            {
                if (i < numTokens)
                {
                    if (i < numTokens - numTokensSelected)
                    {
                        original.TokenList[i].color = original.Token_Unselected;
                    }
                    else
                    {
                        original.TokenList[i].color = original.Token_Selected;
                    }
                }
                else
                {
                    original.TokenList[i].color = original.Token_Empty;
                }
            }
        }
    }


    public class FullAutoPanel : MonoBehaviour
    {
        public TNH_MagDuplicator original;
        public int PanelCost = 4;
        private int numTokensSelected = 0;

        public static Sprite background;

        private TNH_ObjectConstructorIcon PurchaseIcon;

        private FVRPhysicalObject selectedObject = null;
        public Handgun detectedHandgun = null;
        public ClosedBoltWeapon detectedClosedBolt = null;
        public OpenBoltReceiver detectedOpenBolt = null;

        private Collider[] colBuffer = new Collider[50];

        public void Awake()
        {
            original = gameObject.GetComponent<TNH_MagDuplicator>();

            if (original == null)
                TNHFrameworkLogger.LogError("Full Auto Enabler failed, original Mag Duplicator was null!");
            
            original.enabled = false;

            InitPanel();
            UpdateIcons();

            GM.TNH_Manager.TokenCountChangeEvent += UpdateTokenDisplay;
        }

        private void OnDestroy()
        {
            if (original.M != null)
                original.M.TokenCountChangeEvent -= UpdateTokenDisplay;
        }

        private void InitPanel()
        {
            Transform titleTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/AmmoReloaderTitle (3)");

            Text titleText = titleTransform.gameObject.GetComponent<Text>();
            titleText.text = "-- Full Auto Upgrader --";

            Transform backingTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/Backing");

            Transform canvasHolder = original.transform.Find("_CanvasHolder/_UITest_Canvas");

            Transform iconTransform_0 = canvasHolder.Find("Icon_0");
            iconTransform_0.localPosition = new Vector3(0, -290, 0);

            Transform iconTransform_1 = canvasHolder.Find("Icon_1");
            Destroy(iconTransform_1.gameObject);

            Transform buttonTransform_0 = original.transform.Find("PointableButton_0");
            buttonTransform_0.position = iconTransform_0.position;

            Transform buttonTransform_1 = original.transform.Find("PointableButton_1");
            Destroy(buttonTransform_1.gameObject);

            Image backgroundImage = backingTransform.gameObject.GetComponent<Image>();
            backgroundImage.sprite = background;

            PurchaseIcon = iconTransform_0.gameObject.GetComponent<TNH_ObjectConstructorIcon>();

            Button button_0 = buttonTransform_0.gameObject.GetComponent<Button>();
            button_0.onClick = new Button.ButtonClickedEvent();
            button_0.onClick.AddListener(() => { AddFullAutoButton(); });
        }


        public void AddFullAutoButton()
        {
            if ((detectedHandgun == null && detectedClosedBolt == null && detectedOpenBolt == null) || PanelCost > original.M.GetNumTokens())
            {
                //Debug.Log("Can't add full auto!");
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Fail, transform.position);
                return;
            }

            else
            {
                //Debug.Log("Adding full auto!");
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Spawn, transform.position);
                original.M.SubtractTokens(PanelCost);

                if(detectedHandgun != null)
                {
                    TNHFramework.HoldActions[original.M.m_level].Add($"Added Full-Auto To {detectedHandgun.ObjectWrapper.DisplayName}");
                    AddFullAutoToHandgun(detectedHandgun);
                    ClearSelection();
                }

                else if (detectedClosedBolt != null)
                {
                    TNHFramework.HoldActions[original.M.m_level].Add($"Added Full-Auto To {detectedClosedBolt.ObjectWrapper.DisplayName}");
                    AddFullAutoToClosedBolt(detectedClosedBolt);
                    ClearSelection();
                }

                else if(detectedOpenBolt != null)
                {
                    TNHFramework.HoldActions[original.M.m_level].Add($"Added Full-Auto To {detectedOpenBolt.ObjectWrapper.DisplayName}");
                    AddFullAutoToOpenBolt(detectedOpenBolt);
                    ClearSelection();
                }
            }
        }


        private void AddFullAutoToHandgun(Handgun gun)
        {
            Handgun.FireSelectorMode fullAuto = new Handgun.FireSelectorMode();
            fullAuto.SelectorPosition = 0;
            fullAuto.ModeType = Handgun.FireSelectorModeType.FullAuto;

            if (gun.FireSelectorModes != null)
            {
                //Debug.Log("Fire Selector options before addition:");
                foreach (Handgun.FireSelectorMode mode in gun.FireSelectorModes)
                {
                    Debug.Log(mode.ModeType);
                }
            }
            
            if (gun.FireSelectorModes == null || gun.FireSelectorModes.Length == 0)
            {
                //Debug.Log("Handgun did not have any fire selector modes, so we're adding defaults");

                Handgun.FireSelectorMode single = new Handgun.FireSelectorMode();
                single.SelectorPosition = 0;
                single.ModeType = Handgun.FireSelectorModeType.Single;

                gun.FireSelectorModes = [single, fullAuto];
            }
            else
            {
                //Debug.Log("Handgun had atleast one fire selector mode, Adding full auto");
                List<Handgun.FireSelectorMode> modes = new(gun.FireSelectorModes)
                {
                    fullAuto
                };
                gun.FireSelectorModes = modes.ToArray();

                //Debug.Log("Array count: " + gun.FireSelectorModes.Length + ", List count: " + modes.Count);
            }

            
            if (!gun.HasFireSelector)
            {
                //Debug.Log("Handgun was not originally configured to have fire selector. Configuring");

                gun.HasFireSelector = true;

                if (gun.HasSafety)
                {
                    //Debug.Log("Using safety object as fire selector");
                    gun.FireSelectorAxis = gun.SafetyAxis;
                    gun.FireSelectorInterpStyle = gun.Safety_Interp;
                    gun.FireSelector = gun.Safety;
                }

                else if (gun.FireSelector == null)
                {
                    //Debug.Log("Creating dummy game object for fire selector");
                    gun.FireSelector = Instantiate(new GameObject(), gun.GameObject.transform).transform;
                }
            }

            if (gun.HasSafety)
            {
                //Debug.Log("Handgun originally had a safety, so we're removing that");
                //gun.SetSafetyState(false);
                //gun.FireSelectorModes = gun.FireSelectorModes.Where(o => o.ModeType != Handgun.FireSelectorModeType.Safe).ToArray();
                gun.AudioClipSet.FireSelector = gun.AudioClipSet.Safety;
                gun.HasSafety = false;
            }
        }


        private void AddFullAutoToClosedBolt(ClosedBoltWeapon gun)
        {
            ClosedBoltWeapon.FireSelectorMode fullAuto = new ClosedBoltWeapon.FireSelectorMode();
            fullAuto.ModeType = ClosedBoltWeapon.FireSelectorModeType.FullAuto;
            fullAuto.SelectorPosition = 0;

            if (gun.FireSelector_Modes == null || gun.FireSelector_Modes.Length == 0)
            {
                //Debug.Log("Gun did not have fire selector, adding full");

                ClosedBoltWeapon.FireSelectorMode single = new ClosedBoltWeapon.FireSelectorMode();
                single.ModeType = ClosedBoltWeapon.FireSelectorModeType.Single;
                single.SelectorPosition = 0;

                gun.FireSelector_Modes = [single, fullAuto];
            }
            else
            {
                //Debug.Log("Gun had fire selector, adding full");
                List<ClosedBoltWeapon.FireSelectorMode> modes = new(gun.FireSelector_Modes)
                {
                    fullAuto
                };
                gun.FireSelector_Modes = modes.ToArray();
            }
        }

        private void AddFullAutoToOpenBolt(OpenBoltReceiver gun)
        {
            OpenBoltReceiver.FireSelectorMode fullAuto = new OpenBoltReceiver.FireSelectorMode();
            fullAuto.ModeType = OpenBoltReceiver.FireSelectorModeType.FullAuto;
            fullAuto.SelectorPosition = 0;

            if (gun.FireSelector_Modes == null || gun.FireSelector_Modes.Length == 0)
            {
                //Debug.Log("Gun did not have fire selector, adding full");

                OpenBoltReceiver.FireSelectorMode single = new OpenBoltReceiver.FireSelectorMode();
                single.ModeType = OpenBoltReceiver.FireSelectorModeType.Single;
                single.SelectorPosition = 0;

                gun.FireSelector_Modes = [single, fullAuto];
            }
            else
            {
                //Debug.Log("Gun had fire selector, adding full");
                List<OpenBoltReceiver.FireSelectorMode> modes = new(gun.FireSelector_Modes)
                {
                    fullAuto
                };
                gun.FireSelector_Modes = modes.ToArray();
            }
        }


        private void Update()
        {
            if ((float)Vector3.Distance(original.ScanningVolume.position, GM.CurrentPlayerBody.Head.transform.position) < 2f)
            {
                Scan();
                UpdateIcons();
            }
            else
            {
                ClearSelection();
            }
        }

        private void ClearSelection()
        {
            detectedHandgun = null;
            detectedClosedBolt = null;
            detectedOpenBolt = null;
            selectedObject = null;

            original.PlacementBoundsRenderer?.gameObject.SetActive(false);
        }

        private void Scan()
        {
            if (selectedObject != null)
            {
                // Find the bounds of the selected object
                Bounds objBounds = new(selectedObject.transform.position, new Vector3(0.01f, 0.01f, 0.01f));
                Collider[] objColliders = selectedObject.transform.GetComponentsInChildren<Collider>();

                foreach (Collider collider in objColliders)
                {
                    if (collider != null && collider.isTrigger)
                    {
                        objBounds.Encapsulate(collider.bounds);
                    }
                }

                // Get the selected object's position
                Vector3 objPosition = selectedObject.RootRigidbody.position;
                Vector3 b = objBounds.center - selectedObject.transform.position;

                // Create the scanning box effect
                if (original.PlacementBoundsRenderer == null)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original.PlacementBoundsPrefab, objPosition, Quaternion.identity);
                    original.PlacementBoundsRenderer = gameObject.GetComponent<TG_Bounds>();
                }

                // Place the scanning box effect over the selected object
                original.PlacementBoundsRenderer.gameObject.SetActive(true);
                original.PlacementBoundsRenderer.PoseAndRescale(objPosition + b, objBounds.extents * 2f, original.EmitterPoint.position);

                // Find the bounds of the scanning volume
                Bounds scanBounds = new(original.ScanningVolume.position, new Vector3(0.001f, 0.001f, 0.001f));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(0.5f, 0.5f, 0.5f)));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(-0.5f, -0.5f, -0.5f)));

                // If the selected object is no longer within the scanning volume, deselect it
                if (!objBounds.Intersects(scanBounds))
                {
                    ClearSelection();
                }
            }

            if (selectedObject == null)
            {
                detectedHandgun = null;
                detectedClosedBolt = null;
                detectedOpenBolt = null;

                int colliderCount = Physics.OverlapBoxNonAlloc(original.ScanningVolume.position, original.ScanningVolume.localScale * 0.5f, colBuffer, original.ScanningVolume.rotation, original.ScanningLM, QueryTriggerInteraction.Collide);

                for (int i = 0; i < colliderCount; i++)
                {
                    if (colBuffer[i].attachedRigidbody != null)
                    {
                        Handgun handgun = colBuffer[i].attachedRigidbody.GetComponent<Handgun>();
                        if (handgun != null)
                        {
                            if (handgun.FireSelectorModes == null || !handgun.FireSelectorModes.Any(o => o.ModeType == Handgun.FireSelectorModeType.FullAuto))
                            {
                                //Debug.Log("Hand gun detected!");
                                detectedHandgun = handgun;
                                selectedObject = handgun;
                                return;
                            }
                        }

                        ClosedBoltWeapon closedBolt = colBuffer[i].attachedRigidbody.GetComponent<ClosedBoltWeapon>();
                        if (closedBolt != null)
                        {
                            if (closedBolt.FireSelector_Modes == null || !closedBolt.FireSelector_Modes.Any(o => o.ModeType == ClosedBoltWeapon.FireSelectorModeType.FullAuto))
                            {
                                //Debug.Log("Closed bolt detected!");
                                detectedClosedBolt = closedBolt;
                                selectedObject = closedBolt;
                                return;
                            }
                        }

                        OpenBoltReceiver openBolt = colBuffer[i].attachedRigidbody.GetComponent<OpenBoltReceiver>();
                        if (openBolt != null)
                        {
                            if (openBolt.FireSelector_Modes == null || !openBolt.FireSelector_Modes.Any(o => o.ModeType == OpenBoltReceiver.FireSelectorModeType.FullAuto))
                            {
                                //Debug.Log("Open bolt detected!");
                                detectedOpenBolt = openBolt;
                                selectedObject = openBolt;
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void UpdateIcons()
        {
            PurchaseIcon.State = TNH_ObjectConstructorIcon.IconState.Cancel;
            int numTokens = GM.TNH_Manager.GetNumTokens();
            numTokensSelected = 0;

            if (detectedHandgun != null || detectedClosedBolt != null || detectedOpenBolt != null)
            {
                PurchaseIcon.State = TNH_ObjectConstructorIcon.IconState.Accept;
                if (numTokens >= PanelCost) numTokensSelected = PanelCost;
            }

            PurchaseIcon.UpdateIconDisplay();
            UpdateTokenDisplay(numTokens);
        }

        private void UpdateTokenDisplay(int numTokens)
        {
            // Display the total number of tokens
            for (int i = 0; i < original.TokenList.Count; i++)
            {
                if (i < numTokens)
                {
                    if (i < numTokens - numTokensSelected)
                    {
                        original.TokenList[i].color = original.Token_Unselected;
                    }
                    else
                    {
                        original.TokenList[i].color = original.Token_Selected;
                    }
                }
                else
                {
                    original.TokenList[i].color = original.Token_Empty;
                }
            }
        }
    }


    public class FireRatePanel : MonoBehaviour
    {
        public TNH_MagDuplicator original;

        public static Sprite background;
        public static Sprite plusSprite;
        public static Sprite minusSprite;

        private TNH_ObjectConstructorIcon PlusIcon;
        private TNH_ObjectConstructorIcon MinusIcon;

        private FVRPhysicalObject selectedObject = null;
        public Handgun detectedHandgun = null;
        public ClosedBoltWeapon detectedClosedBolt = null;
        public OpenBoltReceiver detectedOpenBolt = null;

        private int PanelCost = 1;
        private int numTokensSelected = 0;
        private Collider[] colBuffer = new Collider[50];

        private float fireRateMultiplier = 1.5f;

        public void Awake()
        {
            original = gameObject.GetComponent<TNH_MagDuplicator>();

            if (original == null)
                TNHFrameworkLogger.LogError("Fire Rate Modifier failed, original Mag Duplicator was null!");

            original.enabled = false;

            InitPanel();
            UpdateIcons();

            GM.TNH_Manager.TokenCountChangeEvent += UpdateTokenDisplay;
        }

        private void OnDestroy()
        {
            if (original.M != null)
                original.M.TokenCountChangeEvent -= UpdateTokenDisplay;
        }

        public void InitPanel()
        {
            Transform titleTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/AmmoReloaderTitle (3)");

            Text titleText = titleTransform.gameObject.GetComponent<Text>();
            titleText.text = "-- Fire Rate Up / Down --";

            Transform backingTransform = original.transform.Find("_CanvasHolder/_UITest_Canvas/Backing");

            Transform canvasHolder = original.transform.Find("_CanvasHolder/_UITest_Canvas");

            Transform iconTransform_0 = canvasHolder.Find("Icon_0");
            iconTransform_0.localPosition = new Vector3(-165, -290, 0);

            Transform iconTransform_1 = canvasHolder.Find("Icon_1");
            iconTransform_1.localPosition = new Vector3(165, -290, 0);

            Transform buttonTransform_0 = original.transform.Find("PointableButton_0");
            buttonTransform_0.position = iconTransform_0.position;

            Transform buttonTransform_1 = original.transform.Find("PointableButton_1");
            buttonTransform_1.position = iconTransform_1.position;

            Image backgroundImage = backingTransform.gameObject.GetComponent<Image>();

            backgroundImage.sprite = background;

            MinusIcon = iconTransform_0.gameObject.GetComponent<TNH_ObjectConstructorIcon>();
            PlusIcon = iconTransform_1.gameObject.GetComponent<TNH_ObjectConstructorIcon>();

            MinusIcon.Sprite_Accept = minusSprite;
            PlusIcon.Sprite_Accept = plusSprite;

            Button button_0 = buttonTransform_0.gameObject.GetComponent<Button>();
            button_0.onClick = new Button.ButtonClickedEvent();
            button_0.onClick.AddListener(() => { DecreaseFireRateButton(); });

            Button button_1 = buttonTransform_1.gameObject.GetComponent<Button>();
            button_1.onClick = new Button.ButtonClickedEvent();
            button_1.onClick.AddListener(() => { IncreaseFireRateButton(); });
        }


        public void IncreaseFireRateButton()
        {
            if ((detectedHandgun == null && detectedClosedBolt == null && detectedOpenBolt == null) || PanelCost > original.M.GetNumTokens())
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Fail, transform.position);
                return;
            }

            else
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Spawn, transform.position);
                original.M.SubtractTokens(PanelCost);

                IncreaseFireRate();
                ClearSelection();
            }
        }

        public void DecreaseFireRateButton()
        {
            if ((detectedHandgun == null && detectedClosedBolt == null && detectedOpenBolt == null) || PanelCost > original.M.GetNumTokens())
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Fail, transform.position);
                return;
            }

            else
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, original.AudEvent_Spawn, transform.position);
                original.M.SubtractTokens(PanelCost);

                DecreaseFireRate();
                ClearSelection();
            }
        }


        public void IncreaseFireRate()
        {
            if (detectedHandgun != null)
            {
                TNHFramework.HoldActions[original.M.m_level].Add($"Increased Fire-Rate For {detectedHandgun.ObjectWrapper.DisplayName}");
                detectedHandgun.Slide.SpringStiffness *= fireRateMultiplier;
                detectedHandgun.Slide.Speed_Rearward *= fireRateMultiplier;
                detectedHandgun.Slide.Speed_Forward *= fireRateMultiplier;
                return;
            }

            else if (detectedClosedBolt != null)
            {
                TNHFramework.HoldActions[original.M.m_level].Add($"Increased Fire-Rate For {detectedClosedBolt.ObjectWrapper.DisplayName}");
                detectedClosedBolt.Bolt.SpringStiffness *= fireRateMultiplier;
                detectedClosedBolt.Bolt.Speed_Forward *= fireRateMultiplier;
                detectedClosedBolt.Bolt.Speed_Rearward *= fireRateMultiplier;
                return;
            }

            else
            {
                TNHFramework.HoldActions[original.M.m_level].Add($"Increased Fire-Rate For {detectedOpenBolt.ObjectWrapper.DisplayName}");
                detectedOpenBolt.Bolt.BoltSpringStiffness *= fireRateMultiplier;
                detectedOpenBolt.Bolt.BoltSpeed_Forward *= fireRateMultiplier;
                detectedOpenBolt.Bolt.BoltSpeed_Rearward *= fireRateMultiplier;
                return;
            }


        }

        public void DecreaseFireRate()
        {
            if(detectedHandgun != null)
            {
                TNHFramework.HoldActions[original.M.m_level].Add($"Decreased Fire-Rate For {detectedHandgun.ObjectWrapper.DisplayName}");
                detectedHandgun.Slide.SpringStiffness *= (1f / fireRateMultiplier);
                detectedHandgun.Slide.Speed_Rearward *= (1f / fireRateMultiplier);
                detectedHandgun.Slide.Speed_Forward *= (1f / fireRateMultiplier);
                return;
            }

            else if(detectedClosedBolt != null)
            {
                TNHFramework.HoldActions[original.M.m_level].Add($"Decreased Fire-Rate For {detectedClosedBolt.ObjectWrapper.DisplayName}");
                detectedClosedBolt.Bolt.SpringStiffness *= (1f / fireRateMultiplier);
                detectedClosedBolt.Bolt.Speed_Rearward *= (1f / fireRateMultiplier);
                detectedClosedBolt.Bolt.Speed_Forward *= (1f / fireRateMultiplier);
                return;
            }

            else
            {
                TNHFramework.HoldActions[original.M.m_level].Add($"Decreased Fire-Rate For {detectedOpenBolt.ObjectWrapper.DisplayName}");
                detectedOpenBolt.Bolt.BoltSpringStiffness *= (1f / fireRateMultiplier);
                detectedOpenBolt.Bolt.BoltSpeed_Forward *= (1f / fireRateMultiplier);
                detectedOpenBolt.Bolt.BoltSpeed_Rearward *= (1f / fireRateMultiplier);
                return;
            }
        }


        private void Update()
        {
            if ((float)Vector3.Distance(original.ScanningVolume.position, GM.CurrentPlayerBody.Head.transform.position) < 2f)
            {
                Scan();
                UpdateIcons();
            }
            else
            {
                ClearSelection();
            }
        }

        private void ClearSelection()
        {
            detectedHandgun = null;
            detectedClosedBolt = null;
            detectedOpenBolt = null;
            selectedObject = null;

            original.PlacementBoundsRenderer?.gameObject.SetActive(false);
        }

        private void Scan()
        {
            if (selectedObject != null)
            {
                // Find the bounds of the selected object
                Bounds objBounds = new(selectedObject.transform.position, new Vector3(0.01f, 0.01f, 0.01f));
                Collider[] objColliders = selectedObject.transform.GetComponentsInChildren<Collider>();

                foreach (Collider collider in objColliders)
                {
                    if (collider != null && collider.isTrigger)
                    {
                        objBounds.Encapsulate(collider.bounds);
                    }
                }

                // Get the selected object's position
                Vector3 objPosition = selectedObject.RootRigidbody.position;
                Vector3 b = objBounds.center - selectedObject.transform.position;

                // Create the scanning box effect
                if (original.PlacementBoundsRenderer == null)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original.PlacementBoundsPrefab, objPosition, Quaternion.identity);
                    original.PlacementBoundsRenderer = gameObject.GetComponent<TG_Bounds>();
                }

                // Place the scanning box effect over the selected object
                original.PlacementBoundsRenderer.gameObject.SetActive(true);
                original.PlacementBoundsRenderer.PoseAndRescale(objPosition + b, objBounds.extents * 2f, original.EmitterPoint.position);

                // Find the bounds of the scanning volume
                Bounds scanBounds = new(original.ScanningVolume.position, new Vector3(0.001f, 0.001f, 0.001f));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(0.5f, 0.5f, 0.5f)));
                scanBounds.Encapsulate(original.ScanningVolume.TransformPoint(new Vector3(-0.5f, -0.5f, -0.5f)));

                // If the selected object is no longer within the scanning volume, deselect it
                if (!objBounds.Intersects(scanBounds))
                {
                    ClearSelection();
                }
            }

            if (selectedObject == null)
            {
                detectedHandgun = null;
                detectedClosedBolt = null;
                detectedOpenBolt = null;

                int colliderCount = Physics.OverlapBoxNonAlloc(original.ScanningVolume.position, original.ScanningVolume.localScale * 0.5f, colBuffer, original.ScanningVolume.rotation, original.ScanningLM, QueryTriggerInteraction.Collide);

                for (int i = 0; i < colliderCount; i++)
                {
                    if (colBuffer[i].attachedRigidbody != null)
                    {
                        Handgun handgun = colBuffer[i].attachedRigidbody.GetComponent<Handgun>();
                        if (handgun != null)
                        {
                            //Debug.Log("Hand gun detected!");
                            detectedHandgun = handgun;
                            selectedObject = handgun;
                            return;
                        }

                        ClosedBoltWeapon closedBolt = colBuffer[i].attachedRigidbody.GetComponent<ClosedBoltWeapon>();
                        if (closedBolt != null)
                        {
                            //Debug.Log("Closed bolt detected!");
                            detectedClosedBolt = closedBolt;
                            selectedObject = closedBolt;
                            return;
                        }

                        OpenBoltReceiver openBolt = colBuffer[i].attachedRigidbody.GetComponent<OpenBoltReceiver>();
                        if (openBolt != null)
                        {
                            //Debug.Log("Open bolt detected!");
                            detectedOpenBolt = openBolt;
                            selectedObject = openBolt;
                            return;
                        }
                    }
                }
            }
        }

        private void UpdateIcons()
        {
            PlusIcon.State = TNH_ObjectConstructorIcon.IconState.Cancel;
            MinusIcon.State = TNH_ObjectConstructorIcon.IconState.Cancel;
            int numTokens = GM.TNH_Manager.GetNumTokens();
            numTokensSelected = 0;

            if (detectedHandgun != null || detectedClosedBolt != null || detectedOpenBolt != null)
            {
                PlusIcon.State = TNH_ObjectConstructorIcon.IconState.Accept;
                MinusIcon.State = TNH_ObjectConstructorIcon.IconState.Accept;
                if (numTokens >= PanelCost) numTokensSelected = PanelCost;
            }

            PlusIcon.UpdateIconDisplay();
            MinusIcon.UpdateIconDisplay();
            UpdateTokenDisplay(numTokens);
        }

        private void UpdateTokenDisplay(int numTokens)
        {
            // Display the total number of tokens
            for (int i = 0; i < original.TokenList.Count; i++)
            {
                if (i < numTokens)
                {
                    if (i < numTokens - numTokensSelected)
                    {
                        original.TokenList[i].color = original.Token_Unselected;
                    }
                    else
                    {
                        original.TokenList[i].color = original.Token_Selected;
                    }
                }
                else
                {
                    original.TokenList[i].color = original.Token_Empty;
                }
            }
        }
    }
}
