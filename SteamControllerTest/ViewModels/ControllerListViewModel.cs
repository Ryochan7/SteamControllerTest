using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json;
using SteamControllerTest.SteamControllerLibrary;

namespace SteamControllerTest.ViewModels
{
    public class ReadProfileFailException : Exception
    {
        private JsonException innerJsonException;
        public JsonException InnerJsonException => innerJsonException;

        private string extraMessage;
        public string ExtraMessage => extraMessage;

        public ReadProfileFailException(JsonException e, string extraMessage)
        {
            innerJsonException = e;
            this.extraMessage = extraMessage;
        }
    }

    public class ControllerListViewModel
    {
        private ReaderWriterLockSlim _colListLocker = new ReaderWriterLockSlim();
        private ObservableCollection<DeviceListItem> controllerList =
            new ObservableCollection<DeviceListItem>();
        public ObservableCollection<DeviceListItem> ControllerList
        {
            get => controllerList;
        }

        private BackendManager backendManager;
        private int selectedIndex;

        public ProfileList DeviceProfileList
        {
            get => backendManager.DeviceProfileList;
        }

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value == selectedIndex) return;
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectedIndexChanged;
        public event EventHandler<ReadProfileFailException> ReadProfileFailure;


        public ControllerListViewModel(BackendManager manager)
        {
            backendManager = manager;

            backendManager.ServiceStarted += BackendManager_ServiceStarted;
            backendManager.ServiceStopped += BackendManager_ServiceStopped;
            backendManager.HotplugController += BackendManager_HotplugController;
            backendManager.UnplugController += BackendManager_UnplugController;
            
            BindingOperations.EnableCollectionSynchronization(controllerList, _colListLocker,
                            ColLockCallback);
        }

        private void BackendManager_UnplugController(SteamControllerDevice device, int ind)
        {
            using (WriteLocker locker = new WriteLocker(_colListLocker))
            {
                //int ind = controllerList.Where((item) => item.ItemIndex == device.Index)
                //    .Select((item) => item.ItemIndex).DefaultIfEmpty(-1).First();
                if (ind >= 0)
                {
                    controllerList.RemoveAt(ind);
                }
            }
        }

        private void BackendManager_ServiceStopped(object sender, EventArgs e)
        {
            using (WriteLocker locker = new WriteLocker(_colListLocker))
            {
                controllerList.Clear();
            }
        }

        private void BackendManager_ServiceStarted(object sender, EventArgs e)
        {
            using (WriteLocker locker = new WriteLocker(_colListLocker))
            {
                int i = 0;
                foreach (SteamControllerDevice device in backendManager.ControllerList)
                {
                    if (device != null)
                    {
                        DeviceListItem devItem = new DeviceListItem(device, i, DeviceProfileList);
                        Mapper map = backendManager.MapperDict[device.Index];
                        if (map.ProfileFile != string.Empty)
                        {
                            devItem.PostInit(map.ProfileFile);
                        }

                        //if (!string.IsNullOrWhiteSpace(backendManager.ProfileFile))
                        //{
                        //    devItem.PostInit(backendManager.ProfileFile);
                        //}

                        devItem.ProfileIndexChanged += DevItem_ProfileIndexChanged;
                        device.Removal += Device_Removal;
                        controllerList.Add(devItem);

                        i++;
                    }
                }
            }
        }

        private void Device_Removal(object sender, EventArgs e)
        {
            SteamControllerDevice device = sender as SteamControllerDevice;
            using (WriteLocker locker = new WriteLocker(_colListLocker))
            {
                int ind = controllerList.Where((item) => item.ItemIndex == device.Index)
                    .Select((item) => item.ItemIndex).DefaultIfEmpty(-1).First();
                if (device.Synced && ind >= 0)
                {
                    controllerList.RemoveAt(ind);
                }
            }
        }

        private void BackendManager_HotplugController(SteamControllerDevice device, int ind)
        {
            // Engage write lock pre-maturely
            using (WriteLocker readLock = new WriteLocker(_colListLocker))
            {
                DeviceListItem devItem = new DeviceListItem(device, ind, DeviceProfileList);
                Mapper map = backendManager.MapperDict[device.Index];
                if (!string.IsNullOrWhiteSpace(map.ProfileFile))
                {
                    devItem.PostInit(map.ProfileFile);
                }

                devItem.ProfileIndexChanged += DevItem_ProfileIndexChanged;
                device.Removal += Device_Removal;
                controllerList.Add(devItem);
            }
        }

        private void DevItem_ProfileIndexChanged(object sender, EventArgs e)
        {
            DeviceListItem item = sender as DeviceListItem;
            Mapper map = backendManager.MapperDict[item.Device.Index];
            string profilePath = DeviceProfileList.ProfileListCol[item.ProfileIndex].ProfilePath;

            map.QueueEvent(() =>
            {
                //map.UseBlankProfile();
                //ReadProfileFailure?.Invoke(this, new ReadProfileFailException(new JsonException(), $"Failed to read profile {profilePath}"));
                try
                {
                    map.ChangeProfile(profilePath);
                }
                catch (JsonException e)
                {
                    ReadProfileFailure?.Invoke(this, new ReadProfileFailException(e, $"Failed to read profile {profilePath}"));
                }
                //backendManager.ProfileFile = DeviceProfileList.ProfileListCol[item.ProfileIndex].ProfilePath;
            });
        }

        private void ColLockCallback(IEnumerable collection, object context,
            Action accessMethod, bool writeAccess)
        {
            if (writeAccess)
            {
                using (WriteLocker locker = new WriteLocker(_colListLocker))
                {
                    accessMethod?.Invoke();
                }
            }
            else
            {
                using (ReadLocker locker = new ReadLocker(_colListLocker))
                {
                    accessMethod?.Invoke();
                }
            }
        }
    }

    public class DeviceListItem
    {
        private int itemIndex;
        private SteamControllerDevice device;
        private ProfileList profileListHolder;
        private int profileIndex = -1;

        public SteamControllerDevice Device
        {
            get => device;
        }

        public string DisplayName
        {
            get => $"{device.DevTypeStr} ({device.Serial})";
        }

        public int DisplayIndex
        {
            get => device.Index + 1;
        }

        public int ItemIndex
        {
            get => itemIndex;
        }

        public int ProfileIndex
        {
            get => profileIndex;
            set
            {
                if (value == profileIndex) return;
                profileIndex = value;
                ProfileIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ProfileIndexChanged;

        public ObservableCollection<ProfileEntity> DevProfileList
        {
            get => profileListHolder.ProfileListCol;
        }

        public DeviceListItem(SteamControllerDevice device, int itemIndex, ProfileList profileListHolder)
        {
            this.device = device;
            this.itemIndex = itemIndex;
            this.profileListHolder = profileListHolder;
        }

        public void PostInit(string profilePath)
        {
            ProfileEntity temp = profileListHolder.ProfileListCol.SingleOrDefault((item) => item.ProfilePath == profilePath);
            if (temp != null)
            {
                int ind = profileListHolder.ProfileListCol.IndexOf(temp);
                ProfileIndex = ind;
            }
            else
            {
                ProfileIndex = -1;
            }
        }
    }
}
