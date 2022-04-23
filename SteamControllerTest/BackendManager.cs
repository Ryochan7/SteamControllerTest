﻿using System;
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
        private Thread vbusThr;

        private List<Mapper> mapperList;
        private SteamControllerEnumerator enumerator;
        private Dictionary<SteamControllerDevice, SteamControllerReader> deviceReadersMap;
        private ViGEmClient vigemTestClient = null;
        private string profileFile;

        public event EventHandler<Mapper.RequestOSDArgs> RequestOSD;

        public BackendManager(string profileFile)
        {
            this.profileFile = profileFile;
            mapperList = new List<Mapper>();
            enumerator = new SteamControllerEnumerator();
            deviceReadersMap = new Dictionary<SteamControllerDevice, SteamControllerReader>();
        }

        public void Start()
        {
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

            foreach (SteamControllerDevice device in enumerator.GetFoundDevices())
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

                device.SetOperational();
                deviceReadersMap.Add(device, reader);

                Mapper testMapper = new Mapper(device, profileFile);
                //testMapper.Start(device, reader);
                testMapper.Start(vigemTestClient, device, reader);
                testMapper.RequestOSD += TestMapper_RequestOSD;
                mapperList.Add(testMapper);
            }
        }

        private void TestMapper_RequestOSD(object sender, Mapper.RequestOSDArgs e)
        {
            RequestOSD?.Invoke(this, e);
        }

        public void Stop()
        {
            foreach (SteamControllerReader readers in deviceReadersMap.Values)
            {
                //readers.StopUpdate();
            }

            foreach (Mapper mapper in mapperList)
            {
                mapper.Stop();
            }

            deviceReadersMap.Clear();
            enumerator.StopControllers();

            vigemTestClient?.Dispose();
            vigemTestClient = null;
        }
    }
}
