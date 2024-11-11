using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ADepIn;
using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace TNHFramework.Editor
{
    public class OptionsComponentManager : OptionsComponent
    {
        public override void AddOption(Framework_UIManager manager, TNHBaseManager.OptionsData options)
        {
            OptionsPanel_ButtonSet buttonSet = GetComponent<OptionsPanel_ButtonSet>();

            text.text = options.Label;
            GameObject currentRow = Instantiate(manager.RowPrefab, transform);

            for (int i = 0; i < options.Options.Count; i++)
            {
                GameObject optionObject = Instantiate(manager.OptionPrefab, currentRow.transform);

                optionObject.GetComponent<Text>().text = options.Options.ElementAt(i).Key;

                Array.Resize(ref buttonSet.ButtonsInSet, buttonSet.ButtonsInSet.Length + 1);
                buttonSet.ButtonsInSet[buttonSet.ButtonsInSet.Length - 1] = optionObject.GetComponent<FVRPointableButton>();

                optionObject.GetComponent<Button>().onClick.AddListener(delegate { buttonSet.SetSelectedButton(i); });
                optionObject.GetComponent<Button>().onClick.AddListener(delegate { options.Options.ElementAt(i).Value.Invoke(); });
            }
        }
    }
}
