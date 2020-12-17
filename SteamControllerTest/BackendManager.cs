using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamControllerTest.SteamControllerLibrary;

namespace SteamControllerTest
{
    public class BackendManager
    {
        private Thread vbusThr;

        private List<Mapper> mapperList;
        private SteamControllerEnumerator enumerator;
        private Dictionary<SteamControllerDevice, SteamControllerReader> deviceReadersMap;

        public BackendManager()
        {
            mapperList = new List<Mapper>();
            enumerator = new SteamControllerEnumerator();
            deviceReadersMap = new Dictionary<SteamControllerDevice, SteamControllerReader>();
        }

        public void Start()
        {
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
                SteamControllerReader reader = new SteamControllerReader(device);
                device.SetOperational();
                deviceReadersMap.Add(device, reader);

                Mapper testMapper = new Mapper(device);
                testMapper.Start(device, reader);
                //testMapper.Start(vigemTestClient, device, proReader);
                mapperList.Add(testMapper);
            }
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

            //vigemTestClient?.Dispose();
            //vigemTestClient = null;
        }
    }
}
