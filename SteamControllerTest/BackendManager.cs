using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Nefarius.ViGEm.Client;
using SteamControllerTest.SteamControllerLibrary;

namespace SteamControllerTest
{
    public class BackendManager
    {
        public const int CONTROLLER_LIMIT = 8;

        private Thread vbusThr;
        private bool isRunning;
        public bool IsRunning
        {
            get => isRunning;
        }

        private bool changingService;
        public bool ChangingService
        {
            get => changingService;
        }

        public event EventHandler ServiceStarted;
        public event EventHandler PreServiceStop;
        public event EventHandler ServiceStopped;
        //public event EventHandler HotplugFinished;

        private FakerInputHandler fakerInputHandler = new FakerInputHandler();

        private Dictionary<int, Mapper> mapperDict;
        public Dictionary<int, Mapper> MapperDict
        {
            get => mapperDict;
        }
        private SteamControllerEnumerator enumerator;
        private Dictionary<SteamControllerDevice, SteamControllerReader> deviceReadersMap;
        private ViGEmClient vigemTestClient = null;
        private string profileFile;
        public string ProfileFile
        {
            get => profileFile;
            set
            {
                profileFile = value;
                ProfileFileChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ProfileFileChanged;

        public event EventHandler<Mapper.RequestOSDArgs> RequestOSD;

        private AppGlobalData appGlobal;
        private InputControllerDeviceOptions inputDevOpts =
            new InputControllerDeviceOptions();
        private SteamControllerDevice[] controllerList =
            new SteamControllerDevice[CONTROLLER_LIMIT];
        public SteamControllerDevice[] ControllerList
        {
            get => controllerList;
        }

        private ProfileList deviceProfileList;
        public ProfileList DeviceProfileList
        {
            get => deviceProfileList;
        }

        private Thread eventDispatchThread;
        private Dispatcher eventDispatcher;
        public Dispatcher EventDispatcher
        {
            get => eventDispatcher;
        }

        public delegate void HotplugControllerHandler(SteamControllerDevice device, int ind);
        public event HotplugControllerHandler HotplugController;
        public event HotplugControllerHandler UnplugController;

        public BackendManager(string profileFile, AppGlobalData appGlobal)
        {
            this.profileFile = profileFile;
            this.appGlobal = appGlobal;

            mapperDict = new Dictionary<int, Mapper>();
            enumerator = new SteamControllerEnumerator();
            deviceReadersMap = new Dictionary<SteamControllerDevice, SteamControllerReader>();
            deviceProfileList = new ProfileList(InputDeviceType.SteamController);
            deviceProfileList.Refresh();

            // Bad check if profile exists in proper Profiles folder
            if (!string.IsNullOrWhiteSpace(profileFile) &&
                deviceProfileList.ProfileListCol.Where((item) => item.ProfilePath == profileFile).FirstOrDefault() == null)
            {
                profileFile = string.Empty;
            }
            //List<ProfileEntity> ham = deviceProfileList.ProfileListCol.ToList();

            //ProfileFileChanged += BackendManager_ProfileFileChanged;

            eventDispatchThread = new Thread(() =>
            {
                Dispatcher currentDis = Dispatcher.CurrentDispatcher;
                eventDispatcher = currentDis;
                Dispatcher.Run();
            });
            eventDispatchThread.IsBackground = true;
            eventDispatchThread.Priority = ThreadPriority.Normal;
            eventDispatchThread.Name = "BackendManager Events";
            eventDispatchThread.Start();

            while (eventDispatcher == null)
            {
                Thread.SpinWait(500);
            }
        }

        //private void BackendManager_ProfileFileChanged(object sender, EventArgs e)
        //{
        //    if (isRunning)
        //    {
        //        foreach(Mapper mapper in mapperList)
        //        {
        //            Task.Run(() =>
        //            {
        //                mapper.ChangeProfile(profileFile);
        //            });
        //        }
        //    }
        //}

        public void Start()
        {
            changingService = true;
            bool checkConnect = fakerInputHandler.Connect();

            // Change thread affinity of bus object to not be tied
            // to GUI thread
            vbusThr = new Thread(() =>
            {
                vigemTestClient = new ViGEmClient();
            });

            vbusThr.Priority = ThreadPriority.AboveNormal;
            vbusThr.IsBackground = true;
            vbusThr.Start();
            vbusThr.Join(); // Wait for bus object start

            Thread temper = new Thread(() =>
            {
                enumerator.FindControllers();
            });
            temper.IsBackground = true;
            temper.Priority = ThreadPriority.Normal;
            temper.Name = "HID Device Opener";
            temper.Start();
            temper.Join();

            int ind = 0;
            foreach (SteamControllerDevice device in enumerator.GetFoundDevices())
            {
                if (ind >= CONTROLLER_LIMIT)
                {
                    break;
                }

                SteamControllerReader reader;
                if (device.ConType == SteamControllerDevice.ConnectionType.Bluetooth)
                {
                    reader = new SteamControllerBTReader(device as SteamControllerBTDevice);
                }
                else
                {
                    reader = new SteamControllerReader(device);
                }

                if (!device.Synced)
                {
                    //device.SetOperational();
                    deviceReadersMap.Add(device, reader);
                    device.Removal += Device_Removal;
                    device.SyncedChanged += Device_SyncedChanged;
                    // Attempt to run reader early
                    reader.StartUpdate();

                    continue;
                }

                device.Index = ind;
                device.SetOperational();
                deviceReadersMap.Add(device, reader);

                device.Removal += Device_Removal;
                if (device.CheckForSyncChange)
                {
                    device.SyncedChanged += Device_SyncedChanged;
                }

                appGlobal.LoadControllerDeviceSettings(device, device.DeviceOptions);

                //string tempProfilePath = profileFile;
                //if (string.IsNullOrEmpty(profileFile) &&
                //    deviceProfileList.ProfileListCol.Count > 0)
                //{
                //    tempProfilePath = deviceProfileList.ProfileListCol[0].ProfilePath;
                //}

                string tempProfilePath = string.Empty;
                if (appGlobal.activeProfiles.TryGetValue(ind, out tempProfilePath))
                {
                }
                else if (deviceProfileList.ProfileListCol.Count > 0)
                {
                    tempProfilePath = deviceProfileList.ProfileListCol[0].ProfilePath;
                }

                Mapper testMapper = new Mapper(device, tempProfilePath, appGlobal);
                mapperDict.Add(ind, testMapper);
                //testMapper.Start(device, reader);
                testMapper.Start(vigemTestClient, fakerInputHandler, device, reader);
                testMapper.RequestOSD += TestMapper_RequestOSD;

                int tempInd = ind;
                testMapper.ProfileChanged += (object sender, string e) => {
                    appGlobal.activeProfiles[tempInd] = e;
                    appGlobal.SaveControllerDeviceSettings(device, device.DeviceOptions);
                };

                controllerList[ind] = device;
                ind++;
            }

            isRunning = true;
            changingService = false;

            ServiceStarted?.Invoke(this, EventArgs.Empty);
        }

        private void Device_SyncedChanged(object sender, EventArgs e)
        {
            SteamControllerDevice device = sender as SteamControllerDevice;
            if (device.Synced)
            {
                Func<bool> tempFoundDevFunc = () =>
                {
                    bool found = false;
                    for (int i = 0, arlen = controllerList.Length; i < arlen; i++)
                    {
                        if (controllerList[i] != null &&
                            controllerList[i].Serial == device.Serial)
                        {
                            found = true;
                        }
                    }

                    return found;
                };

                bool alreadyExists = tempFoundDevFunc();
                if (!alreadyExists)
                {
                    for (int ind = 0, arlen = controllerList.Length; ind < arlen; ind++)
                    {
                        // No controller in input slot. Insert newly created
                        // device in slot
                        if (controllerList[ind] == null)
                        {
                            if (deviceReadersMap.TryGetValue(device,
                                out SteamControllerReader reader))
                            {
                                PrepareSyncedInputDevice(device, reader, ind);
                                HotplugController?.Invoke(device, ind);
                            }

                            break;
                        }
                    }
                }
            }
            else
            {
                UnplugController?.Invoke(device, device.Index);

                if (mapperDict.TryGetValue(device.Index, out Mapper tempMapper))
                {
                    Task tempTask = Task.Run(() =>
                    {
                        tempMapper.Stop();
                        tempMapper = null;
                    });
                    //tempTask.Wait();

                    mapperDict.Remove(device.Index);
                }

                eventDispatcher.Invoke(() =>
                {
                    if (device.Index >= 0)
                    {
                        controllerList[device.Index] = null;
                        if (appGlobal.activeProfiles.ContainsKey(device.Index))
                        {
                            appGlobal.activeProfiles.Remove(device.Index);
                        }
                    }
                });
            }
        }

        private void Device_Removal(object sender, EventArgs e)
        {
            SteamControllerDevice device = sender as SteamControllerDevice;
            deviceReadersMap.Remove(device);
            if (mapperDict.TryGetValue(device.Index, out Mapper tempMapper))
            {
                Task tempTask = Task.Run(() =>
                {
                    tempMapper.Stop();
                    tempMapper = null;
                });
                //tempTask.Wait();

                mapperDict.Remove(device.Index);
            }

            enumerator.RemoveDevice(device);
            eventDispatcher.Invoke(() =>
            {
                if (device.Index >= 0)
                {
                    controllerList[device.Index] = null;
                    if (appGlobal.activeProfiles.ContainsKey(device.Index))
                    {
                        appGlobal.activeProfiles.Remove(device.Index);
                    }
                }
            });
        }

        private void TestMapper_RequestOSD(object sender, Mapper.RequestOSDArgs e)
        {
            RequestOSD?.Invoke(this, e);
        }

        public void Stop()
        {
            changingService = true;
            isRunning = false;

            PreServiceStop?.Invoke(this, EventArgs.Empty);

            foreach (Mapper mapper in mapperDict.Values)
            {
                mapper.Stop();
            }

            foreach (SteamControllerReader readers in deviceReadersMap.Values)
            {
                readers.StopUpdate();
            }

            mapperDict.Clear();
            deviceReadersMap.Clear();
            enumerator.StopControllers();
            Array.Clear(controllerList, 0, CONTROLLER_LIMIT);
            //controllerList.Clear();

            vigemTestClient?.Dispose();
            vigemTestClient = null;

            fakerInputHandler.Sync();
            fakerInputHandler.Disconnect();

            changingService = false;

            ServiceStopped?.Invoke(this, EventArgs.Empty);
        }

        public void ShutDown()
        {
            eventDispatchThread.Join();

            eventDispatcher = null;
            eventDispatchThread = null;
        }

        public void PreAppStopDown()
        {
            PreServiceStop = null;
            ServiceStopped = null;
            eventDispatcher.InvokeShutdown();
        }

        public void Hotplug()
        {
            if (isRunning)
            {
                Task temp = Task.Run(() =>
                {
                    enumerator.FindControllers();
                });
                temp.Wait();

                IEnumerable<SteamControllerDevice> devices =
                    enumerator.GetFoundDevices();

                for (var devEnum = devices.GetEnumerator(); devEnum.MoveNext();)
                {
                    SteamControllerDevice device = devEnum.Current;
                    if (!device.Synced)
                    {
                        continue;
                    }

                    Func<bool> tempFoundDevFunc = () =>
                    {
                        bool found = false;
                        for (int i = 0, arlen = controllerList.Length; i < arlen; i++)
                        {
                            if (controllerList[i] != null &&
                                controllerList[i].Serial == device.Serial)
                            {
                                found = true;
                            }
                        }

                        return found;
                    };

                    if (tempFoundDevFunc())
                    {
                        continue;
                    }

                    for (int ind = 0, arlen = controllerList.Length; ind < arlen; ind++)
                    {
                        // No controller in input slot. Insert newly created
                        // device in slot
                        if (controllerList[ind] == null)
                        {
                            PrepareAddInputDevice(device, ind);
                            HotplugController?.Invoke(device, ind);
                            break;
                        }
                    }
                }
            }
        }

        private void PrepareSyncedInputDevice(SteamControllerDevice device,
            SteamControllerReader reader, int ind)
        {
            device.Index = ind;
            //device.Removal += Device_Removal;

            string tempProfilePath = string.Empty;
            if (appGlobal.activeProfiles.TryGetValue(ind, out tempProfilePath))
            {
            }
            if (deviceProfileList.ProfileListCol.Count > 0)
            {
                tempProfilePath = deviceProfileList.ProfileListCol[0].ProfilePath;
            }

            Mapper testMapper = new Mapper(device, tempProfilePath, appGlobal);
            //testMapper.Start(device, reader);
            testMapper.Start(vigemTestClient, fakerInputHandler, device, reader);
            testMapper.RequestOSD += TestMapper_RequestOSD;
            int tempInd = ind;
            testMapper.ProfileChanged += (object sender, string e) => {
                appGlobal.activeProfiles[tempInd] = e;
                appGlobal.SaveControllerDeviceSettings(device, device.DeviceOptions);
            };

            mapperDict.Add(ind, testMapper);

            controllerList[ind] = device;
        }

        private void PrepareAddInputDevice(SteamControllerDevice device, int ind)
        {
            SteamControllerReader reader;
            if (device.ConType == SteamControllerDevice.ConnectionType.Bluetooth)
            {
                reader = new SteamControllerBTReader(device as SteamControllerBTDevice);
            }
            else
            {
                reader = new SteamControllerReader(device);
            }

            device.Index = ind;
            //device.SetOperational();
            deviceReadersMap.Add(device, reader);

            device.Removal += Device_Removal;
            if (device.CheckForSyncChange)
            {
                device.SyncedChanged += Device_SyncedChanged;
            }

            appGlobal.LoadControllerDeviceSettings(device, device.DeviceOptions);

            //string tempProfilePath = profileFile;
            //if (string.IsNullOrEmpty(profileFile) &&
            //    deviceProfileList.ProfileListCol.Count > 0)
            //{
            //    tempProfilePath = deviceProfileList.ProfileListCol[0].ProfilePath;
            //}

            string tempProfilePath = string.Empty;
            if (appGlobal.activeProfiles.TryGetValue(ind, out tempProfilePath))
            {
            }
            else if (deviceProfileList.ProfileListCol.Count > 0)
            {
                tempProfilePath = deviceProfileList.ProfileListCol[0].ProfilePath;
            }

            Mapper testMapper = new Mapper(device, tempProfilePath, appGlobal);
            //testMapper.Start(device, reader);
            testMapper.Start(vigemTestClient, fakerInputHandler, device, reader);
            testMapper.RequestOSD += TestMapper_RequestOSD;
            int tempInd = ind;
            testMapper.ProfileChanged += (object sender, string e) => {
                appGlobal.activeProfiles[tempInd] = e;
                appGlobal.SaveControllerDeviceSettings(device, device.DeviceOptions);
            };

            mapperDict.Add(ind, testMapper);

            controllerList[ind] = device;
        }
    }
}
