using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;

namespace SteamControllerTest.SteamControllerLibrary
{
    public class SteamControllerEnumerator
    {
        private const int STEAM_CONTROLLER_VENDOR_ID = 0x28DE;
        private const int STEAM_CONTROLLER_PRODUCT_ID = 0x1102;

        private Dictionary<string, SteamControllerDevice> foundDevices;

        public SteamControllerEnumerator()
        {
            foundDevices = new Dictionary<string, SteamControllerDevice>();
        }

        public void FindControllers()
        {
            IEnumerable<HidDevice> hDevices = HidDevices.Enumerate(STEAM_CONTROLLER_VENDOR_ID,
                STEAM_CONTROLLER_PRODUCT_ID);
            hDevices = hDevices.Where(hDevice => hDevice.Capabilities.Usage == 1);
            List<HidDevice> tempList = hDevices.ToList();

            foreach (HidDevice hDevice in tempList)
            {
                if (!hDevice.IsOpen)
                {
                    hDevice.OpenDevice(false);
                }

                if (hDevice.IsOpen)
                {
                    //string serial = hDevice.readSerial();
                    SteamControllerDevice tempDev = new SteamControllerDevice(hDevice);
                    foundDevices.Add(hDevice.DevicePath, tempDev);
                }
            }
        }

        public IEnumerable<SteamControllerDevice> GetFoundDevices()
        {
            return foundDevices.Values.ToList();
        }

        public void RemoveDevice(SteamControllerDevice inputDevice)
        {
            inputDevice.Detach();
            inputDevice.HidDevice.CloseDevice();
            foundDevices.Remove(inputDevice.HidDevice.DevicePath);
        }

        public void StopControllers()
        {
            foreach (SteamControllerDevice inputDevice in foundDevices.Values)
            {
                inputDevice.Detach();
                inputDevice.HidDevice.CloseDevice();
            }

            foundDevices.Clear();
        }
    }
}
