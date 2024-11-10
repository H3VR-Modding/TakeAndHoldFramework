using ADepIn;
using BepInEx;
using Deli;
using Deli.Immediate;
using Deli.Runtime;
using Deli.Runtime.Yielding;
using Deli.Setup;
using Deli.VFS;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;
using Stratum;
using Stratum.Extensions;

namespace TNHFramework
{
    public class SosigLoaderDeli
    {
        public void LoadAsset(SetupStage stage, Mod mod, IHandle handle)
        {

            if(handle is not IFileHandle file)
            {
                throw new ArgumentException("Could not load sosig! Make sure you're pointing to a sosig template json file in the manifest");
            }

            try
            {
                string charString = stage.ImmediateReaders.Get<string>()(file);
                JsonSerializerSettings settings = new()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                };

                SosigTemplate sosig = JsonConvert.DeserializeObject<SosigTemplate>(charString, settings);
                TNHFrameworkLogger.Log("Sosig loaded successfuly : " + sosig.DisplayName, TNHFrameworkLogger.LogType.General);

                LoadedTemplateManager.AddSosigTemplate(sosig);
            }
            catch (Exception e)
            {
                TNHFrameworkLogger.LogError("Failed to load setup assets for sosig file! Caused Error: " + e.ToString());
            }

        }
    }



    public class CharacterLoaderDeli
    {
        public void LoadAsset(SetupStage stage, Mod mod, IHandle handle)
        {
            
            if(handle is not IDirectoryHandle dir)
            {
                throw new ArgumentException("Could not load character! Character should point to a folder holding the character.json and thumb.png");
            }


            try
            {
                ObjectTemplates.V1.CustomCharacter character = null;
                Sprite thumbnail = null;

                foreach (IFileHandle file in dir.GetFiles())
                {
                    if (file.Path.EndsWith("character.json"))
                    {
                        string charString = stage.ImmediateReaders.Get<string>()(file);
                        JsonSerializerSettings settings = new()
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        
                        character = JsonConvert.DeserializeObject<ObjectTemplates.V1.CustomCharacter>(charString, settings);
                    }
                    else if (file.Path.EndsWith("thumb.png"))
                    {
                        thumbnail = TNHFrameworkUtils.LoadSprite(file);
                    }
                }

                if (character == null)
                {
                    TNHFrameworkLogger.LogError("Failed to load custom character! No character.json file found");
                    return;
                }

                else if (thumbnail == null)
                {
                    TNHFrameworkLogger.LogError("Failed to load custom character! No thumb.png file found");
                    return;
                }

                //Now we want to load the icons for each pool
                foreach (IFileHandle iconFile in dir.GetFiles())
                {
                    foreach (ObjectTemplates.V1.EquipmentPool pool in character.EquipmentPools)
                    {
                        if (iconFile.Path.Split('/').Last() == pool.IconName)
                        {
                            pool.GetPoolEntry().TableDef.Icon = TNHFrameworkUtils.LoadSprite(iconFile);
                        }
                    }
                }

                TNHFrameworkLogger.Log("Character loaded successfuly : " + character.DisplayName, TNHFrameworkLogger.LogType.General);

                LoadedTemplateManager.AddCharacterTemplate(new ObjectTemplates.TakeAndHoldCharacter(character), thumbnail);
            }
            catch(Exception e)
            {
                TNHFrameworkLogger.LogError("Failed to load setup assets for character! Caused Error: " + e.ToString());
            }
        }
    }



    public class VaultFileLoaderDeli
    {
        public void LoadAsset(SetupStage stage, Mod mod, IHandle handle)
        {

            if (handle is not IFileHandle file)
            {
                throw new ArgumentException("Could not load vault file! Make sure you're pointing to a vault json file in the manifest");
            }

            try
            {
                string charString = stage.ImmediateReaders.Get<string>()(file);
                JsonSerializerSettings settings = new()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                };

                SavedGunSerializable savedGun = JsonConvert.DeserializeObject<SavedGunSerializable>(charString, settings);
                TNHFrameworkLogger.Log("Vault file loaded successfuly : " + savedGun.FileName, TNHFrameworkLogger.LogType.General);

                LoadedTemplateManager.AddVaultFile(savedGun);
            }
            catch(Exception e)
            {
                TNHFrameworkLogger.LogError("Failed to load setup assets for vault file! Caused Error: " + e.ToString());
            }
        }
    }
}
