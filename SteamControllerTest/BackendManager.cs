using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

                device.Index = ind;
                device.SetOperational();
                deviceReadersMap.Add(device, reader);

                appGlobal.LoadControllerDeviceSettings(device, device.DeviceOptions);

                string tempProfilePath = profileFile;
                if (string.IsNullOrEmpty(profileFile) &&
                    deviceProfileList.ProfileListCol.Count > 0)
                {
                    tempProfilePath = deviceProfileList.ProfileListCol[0].ProfilePath;
                }

                Mapper testMapper = new Mapper(device, tempProfilePath, appGlobal);
                //testMapper.Start(device, reader);
                testMapper.Start(vigemTestClient, fakerInputHandler, device, reader);
                testMapper.RequestOSD += TestMapper_RequestOSD;
                mapperDict.Add(ind, testMapper);

                controllerList[ind] = device;
                ind++;
            }

            isRunning = true;
            changingService = false;

            ServiceStarted?.Invoke(this, EventArgs.Empty);
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

            foreach (SteamControllerReader readers in deviceReadersMap.Values)
            {
                //readers.StopUpdate();
            }

            foreach (Mapper mapper in mapperDict.Values)
            {
                mapper.Stop();
            }

            mapperDict.Clear();
            deviceReadersMap.Clear();
            enumerator.StopControllers();
            Array.Clear(controllerList, 0, CONTROLLER_LIMIT);
            //controllerList.Clear();

            vigemTestClient?.Dispose();
            vigemTestClient = null;

            fakerInputHandler.Disconnect();

            changingService = false;

            ServiceStopped?.Invoke(this, EventArgs.Empty);
        }
    }
}
