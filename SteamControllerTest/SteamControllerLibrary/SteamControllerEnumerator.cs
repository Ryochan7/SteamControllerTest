using System.Collections.Generic;
using System.Linq;
using HidLibrary;

namespace SteamControllerTest.SteamControllerLibrary
{
    public class SteamControllerEnumerator
    {
        private const int STEAM_CONTROLLER_VENDOR_ID = 0x28DE;
        private const int STEAM_CONTROLLER_PRODUCT_ID = 0x1102;
        public const int STEAM_DONGLE_CONTROLLER_PRODUCT_ID = 0x1142;

        private Dictionary<string, SteamControllerDevice> foundDevices;

        public SteamControllerEnumerator()
        {
            foundDevices = new Dictionary<string, SteamControllerDevice>();
        }

        public void FindControllers()
        {
            int endpointIdx = 1;

            IEnumerable<HidDevice> hDevices = HidDevices.Enumerate(STEAM_CONTROLLER_VENDOR_ID,
                STEAM_CONTROLLER_PRODUCT_ID, STEAM_DONGLE_CONTROLLER_PRODUCT_ID);
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
                    //string serial = hDevice.ReadSerial();
                    if (hDevice.Attributes.ProductId == STEAM_CONTROLLER_PRODUCT_ID)
                    {
                        SteamControllerDevice tempDev = new SteamControllerDevice(hDevice);
                        foundDevices.Add(hDevice.DevicePath, tempDev);
                    }
                    else if (hDevice.Attributes.ProductId == STEAM_DONGLE_CONTROLLER_PRODUCT_ID)
                    {
                        // Only care about interface 1 for this test
                        if (endpointIdx == 1)
                        {
                            SteamControllerDevice tempDev = new SteamControllerDevice(hDevice);
                            foundDevices.Add(hDevice.DevicePath, tempDev);
                            endpointIdx++;
                        }
                    }
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
