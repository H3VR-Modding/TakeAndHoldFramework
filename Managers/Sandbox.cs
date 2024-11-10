using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using TNHFramework.ObjectTemplates;
using UnityEngine;

namespace TNHFramework.Managers
{
    public class TNHManagerSandbox : TNHBaseManager
    {
        public TNHManagerSandbox() 
        {
            managerName = "Sandbox";
        }

        public override bool InitTNH(TNH_Manager baseManager)
        {
            if (GM.TNHOptions.TNHSeed < 0)
            {
                baseManager.m_seed = global::UnityEngine.Random.Range(0, baseManager.PossibleSequnces.Count);
                baseManager.m_curPointSequence = baseManager.PossibleSequnces[baseManager.m_seed];
            }
            else
            {
                int num = GM.TNHOptions.TNHSeed;
                num %= baseManager.PossibleSequnces.Count;
                baseManager.m_seed = num;
                baseManager.m_curPointSequence = baseManager.PossibleSequnces[num];
            }

            TNH_SupplyPoint startSupplyPoint = baseManager.SupplyPoints[baseManager.m_curPointSequence.StartSupplyPointIndex];

            baseManager.LoadFromSettings();

            baseManager.ItemSpawner.transform.position = startSupplyPoint.SpawnPoints_Panels[startSupplyPoint.SpawnPoints_Panels.Count - 1].position + Vector3.up * 0.8f;
            baseManager.ItemSpawner.transform.rotation = startSupplyPoint.SpawnPoints_Panels[startSupplyPoint.SpawnPoints_Panels.Count - 1].rotation;
            baseManager.ItemSpawner.SetActive(true);

            return false;
        }
    }
}
