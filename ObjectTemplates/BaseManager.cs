using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;

namespace TNHFramework.ObjectTemplates
{
    public abstract class TNHBaseManager
    {
        public string managerName;
        public string charType;

        public abstract bool InitTNH(TNH_Manager baseManager);
    }
}
