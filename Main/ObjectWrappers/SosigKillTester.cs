using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TNHFramework.ObjectTemplates;
using UnityEngine;

namespace TNHFramework.Main.ObjectWrappers
{
    public class SosigKillTester : MonoBehaviour
    {
        public delegate void HowTheHellDoDelegatesWork();

        public event HowTheHellDoDelegatesWork InvokeOnKill;

        public string SosigID;

        public void OnDestroy()
        {
            InvokeOnKill.Invoke();
        }
    }
}
