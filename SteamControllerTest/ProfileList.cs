using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace SteamControllerTest
{
    public class ProfileList
    {
        private object _proLockobj = new object();
        private ObservableCollection<ProfileEntity> profileListCol =
            new ObservableCollection<ProfileEntity>();

        public ObservableCollection<ProfileEntity> ProfileListCol { get => profileListCol; set => profileListCol = value; }

        private InputDeviceType inputDeviceType;

        public ProfileList(InputDeviceType inputDeviceType)
        {
            this.inputDeviceType = inputDeviceType;
            BindingOperations.EnableCollectionSynchronization(profileListCol, _proLockobj);
        }

        public void Refresh()
        {
            profileListCol.Clear();
            string[] profiles = Directory.GetFiles(Path.Combine(AppGlobalDataSingleton.Instance.baseProfilesPath,
                AppGlobalData.STEAM_CONTROLLER_PROFILE_DIR));
            foreach (string s in profiles)
            {
                if (s.EndsWith(".json"))
                {
                    string json = File.ReadAllText(s);

                    ProfilePreview tempPreview =
                        JsonConvert.DeserializeObject<ProfilePreview>(json);
                    ProfileEntity item = new ProfileEntity(path: s, name: tempPreview.Name, inputDeviceType);
                    profileListCol.Add(item);
                }
            }
        }
    }

    public class ProfilePreview
    {
        private string name;
        public string Name
        {
            get => name;
            set => name = value;
        }

        private string controllerType;
        public string ControllerType
        {
            get => controllerType;
            set => controllerType = value;
        }
    }
}
