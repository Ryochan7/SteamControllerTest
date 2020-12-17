using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.SteamControllerLibrary;

namespace SteamControllerTest
{
    public class Mapper
    {
        private SteamControllerDevice device;
        private SteamControllerReader reader;

        private bool quit = false;
        public bool Quit { get => quit; set => quit = value; }

        public Mapper(SteamControllerDevice device)
        {
            this.device = device;
        }

        //public void Start(ViGEmClient vigemTestClient,
        //    SwitchProDevice proDevice, SwitchProReader proReader)

        public void Start(SteamControllerDevice device, SteamControllerReader reader)
        {
            this.reader = reader;
            reader.Report += ControllerReader_Report;
            reader.StartUpdate();
        }

        private void ControllerReader_Report(SteamControllerReader sender,
            SteamControllerDevice device)
        {
            ref SteamControllerState current = ref device.CurrentStateRef;
        }

        public void Stop()
        {
            reader.StopUpdate();

            quit = true;
        }
    }
}
