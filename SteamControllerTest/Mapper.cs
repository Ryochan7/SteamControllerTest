using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

using SteamControllerTest.SteamControllerLibrary;

namespace SteamControllerTest
{
    public class Mapper
    {
        private ViGEmClient vigemTestClient = null;
        private IXbox360Controller outputX360 = null;
        private Thread contThr;

        private SteamControllerDevice device;
        private SteamControllerReader reader;

        private bool quit = false;
        public bool Quit { get => quit; set => quit = value; }


        private double mouseX = 0.0;
        private double mouseY = 0.0;
        private bool mouseSync;
        public bool MouseSync { get => mouseSync; set => mouseSync = value; }

        public double MouseX { get => mouseX; set => mouseX = value; }
        public double MouseY { get => mouseY; set => mouseY = value; }
        private double mouseXRemainder = 0.0;
        private double mouseYRemainder = 0.0;
        public double MouseXRemainder { get => mouseXRemainder; set => mouseXRemainder = value; }
        public double MouseYRemainder { get => mouseYRemainder; set => mouseYRemainder = value; }

        public Mapper(SteamControllerDevice device)
        {
            this.device = device;
        }

        //public void Start(ViGEmClient vigemTestClient,
        //    SwitchProDevice proDevice, SwitchProReader proReader)

        public void Start(ViGEmClient vigemTestClient,
            SteamControllerDevice device, SteamControllerReader reader)
        {
            contThr = new Thread(() =>
            {
                outputX360 = vigemTestClient.CreateXbox360Controller();
                outputX360.AutoSubmitReport = false;
                outputX360.Connect();
            });
            contThr.Priority = ThreadPriority.Normal;
            contThr.IsBackground = true;
            contThr.Start();
            contThr.Join(); // Wait for bus object start

            this.reader = reader;
            reader.Report += ControllerReader_Report;
            reader.StartUpdate();
        }

        /*public void Start(SteamControllerDevice device, SteamControllerReader reader)
        {
            this.reader = reader;
            reader.Report += ControllerReader_Report;
            reader.StartUpdate();
        }
        */

        private void ControllerReader_Report(SteamControllerReader sender,
            SteamControllerDevice device)
        {
            ref SteamControllerState current = ref device.CurrentStateRef;
            ref SteamControllerState previous = ref device.PreviousStateRef;


            outputX360.ResetReport();
            unchecked
            {
                ushort tempButtons = 0;
                if (current.A) tempButtons |= Xbox360Button.A.Value;
                if (current.B) tempButtons |= Xbox360Button.B.Value;
                if (current.X) tempButtons |= Xbox360Button.X.Value;
                if (current.Y) tempButtons |= Xbox360Button.Y.Value;
                if (current.Back) tempButtons |= Xbox360Button.Back.Value;
                if (current.Start) tempButtons |= Xbox360Button.Start.Value;
                if (current.Guide) tempButtons |= Xbox360Button.Guide.Value;
                if (current.LB) tempButtons |= Xbox360Button.LeftShoulder.Value;
                if (current.RB) tempButtons |= Xbox360Button.RightShoulder.Value;

                if (current.DPadUp) tempButtons |= Xbox360Button.Up.Value;
                if (current.DPadDown) tempButtons |= Xbox360Button.Down.Value;
                if (current.DPadLeft) tempButtons |= Xbox360Button.Left.Value;
                if (current.DPadRight) tempButtons |= Xbox360Button.Right.Value;

                outputX360.SetButtonsFull(tempButtons);
            }

            outputX360.LeftTrigger = current.LT;
            outputX360.RightTrigger = current.RT;

            outputX360.SubmitReport();
        }

        public void Stop()
        {
            reader.StopUpdate();

            quit = true;

            outputX360?.Disconnect();
            outputX360 = null;
        }
    }
}
