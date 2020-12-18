using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;

namespace SteamControllerTest.SteamControllerLibrary
{
    public class SteamControllerReader
    {
        private SteamControllerDevice device;
        public SteamControllerDevice Device { get => device; }
        private Thread inputThread;
        private bool activeInputLoop = false;
        private byte[] inputReportBuffer;
        private byte[] outputReportBuffer;
        private byte[] rumbleReportBuffer;

        public delegate void SteamControllerReportDelegate(SteamControllerReader sender,
            SteamControllerDevice device);
        public event SteamControllerReportDelegate Report;

        public SteamControllerReader(SteamControllerDevice inputDevice)
        {
            this.device = inputDevice;

            inputReportBuffer = new byte[device.InputReportLen];
            outputReportBuffer = new byte[device.OutputReportLen];
            rumbleReportBuffer = new byte[SteamControllerDevice.RUMBLE_REPORT_LEN];
        }

        public void PrepareDevice()
        {
            NativeMethods.HidD_SetNumInputBuffers(device.HidDevice.safeReadHandle.DangerousGetHandle(),
                2);

            device.SetOperational();
        }

        public void StartUpdate()
        {
            PrepareDevice();

            inputThread = new Thread(ReadInput);
            inputThread.IsBackground = true;
            inputThread.Priority = ThreadPriority.AboveNormal;
            inputThread.Name = "Steam Controller Reader Thread";
            inputThread.Start();
        }

        public void StopUpdate()
        {
            activeInputLoop = false;
            Report = null;
            //inputThread.Interrupt();
            if (inputThread != null && inputThread.IsAlive)
            {
                inputThread.Join();
            }
        }

        private void ReadInput()
        {
            activeInputLoop = true;

            bool lsCoorConflict = false;
            short tempAxisX;
            short tempAxisY;
            byte tempByte;

            long currentTime = 0;
            long previousTime = 0;
            long deltaElapsed = 0;
            double lastElapsed;
            double tempTimeElapsed;
            bool firstReport = true;

            unchecked
            {
                while (activeInputLoop)
                {
                    HidDevice.ReadStatus res = device.HidDevice.ReadWithFileStream(inputReportBuffer);
                    if (res == HidDevice.ReadStatus.Success)
                    {
                        tempByte = inputReportBuffer[3];

                        ref SteamControllerState current = ref device.CurrentStateRef;
                        ref SteamControllerState previous = ref device.PreviousStateRef;

                        if (tempByte != SteamControllerDevice.SCPacketType.PT_INPUT &&
                            tempByte != SteamControllerDevice.SCPacketType.PT_IDLE)
                        {
                            Console.WriteLine("Got unexpected input report id 0x{0:X2}. Try again",
                                inputReportBuffer[3]);

                            continue;
                        }
                        else if (tempByte == SteamControllerDevice.SCPacketType.PT_IDLE && !firstReport)
                        {
                            tempByte = 0;

                            // Repeat previously grabbed state with updated timestamp
                            currentTime = Stopwatch.GetTimestamp();
                            deltaElapsed = currentTime - previousTime;
                            lastElapsed = deltaElapsed * (1.0 / Stopwatch.Frequency) * 1000.0;
                            tempTimeElapsed = lastElapsed * .001;

                            current.timeElapsed = tempTimeElapsed;
                            previousTime = currentTime;

                            Report?.Invoke(this, device);
                            device.SyncStates();

                            continue;
                        }
                        else if (firstReport)
                        {
                            Console.WriteLine("CAN READ REPORTS. NICE");
                        }

                        //Console.WriteLine("Got unexpected input report id 0x{0:X2}. Try again",
                        //        inputReportBuffer[3]);

                        //ref SteamControllerState current = ref device.CurrentStateRef;
                        //ref SteamControllerState previous = ref device.PreviousStateRef;
                        tempByte = 0;

                        currentTime = Stopwatch.GetTimestamp();
                        deltaElapsed = currentTime - previousTime;
                        lastElapsed = deltaElapsed * (1.0 / Stopwatch.Frequency) * 1000.0;
                        tempTimeElapsed = lastElapsed * .001;

                        current.timeElapsed = tempTimeElapsed;
                        previousTime = currentTime;

                        /*if (inputReportBuffer[3] != SteamControllerDevice.SCPacketType.PT_INPUT)
                        {
                            Console.WriteLine("GOT INPUT REPORT {0} 0x{1:X2}", res, inputReportBuffer[3]);
                        }
                        */

                        /*
                        if (!firstReport)
                        {
                            Console.WriteLine("Poll Time: {0}", tempTimeElapsed);
                        }
                        //*/

                        //Console.WriteLine("BUTTONS?: {0} {1} {2} {3}", inputReportBuffer[8], inputReportBuffer[9], inputReportBuffer[10], inputReportBuffer[11]);

                        // ???
                        //tempByte = inputReportBuffer[8];

                        // Buttons
                        tempByte = inputReportBuffer[9];
                        current.RTClick = (tempByte & 0x01) != 0;
                        current.LTClick = (tempByte & 0x02) != 0;
                        current.RB = (tempByte & 0x04) != 0;
                        current.LB = (tempByte & 0x08) != 0;
                        current.Y = (tempByte & 0x10) != 0;
                        current.B = (tempByte & 0x20) != 0;
                        current.X = (tempByte & 0x40) != 0;
                        current.A = (tempByte & 0x80) != 0;

                        /*if (inputReportBuffer[3] != SteamControllerDevice.SCPacketType.PT_IDLE)
                        {
                            Console.WriteLine("LKJDLKDLK: {0}", current.A);
                        }
                        */

                        // Buttons
                        tempByte = inputReportBuffer[10];
                        current.DPadUp = (tempByte & 0x01) != 0;
                        current.DPadRight = (tempByte & 0x02) != 0;
                        current.DPadLeft = (tempByte & 0x04) != 0;
                        current.DPadDown = (tempByte & 0x08) != 0;
                        current.Back = (tempByte & 0x10) != 0;
                        current.Guide = (tempByte & 0x20) != 0;
                        current.Start = (tempByte & 0x40) != 0;
                        current.LGrip = (tempByte & 0x80) != 0;

                        // Buttons
                        tempByte = inputReportBuffer[11];
                        current.RGrip = (tempByte & 0x01) != 0;
                        current.LeftPad.Click = (tempByte & 0x02) != 0;
                        current.RightPad.Click = (tempByte & 0x04) != 0;
                        current.LeftPad.Touch = (tempByte & 0x08) != 0;
                        current.RightPad.Touch = (tempByte & 0x10) != 0;
                        current.LSClick = (tempByte & 0x40) != 0;
                        lsCoorConflict = (tempByte & 0x80) != 0;

                        current.LT = inputReportBuffer[12];
                        current.RT = inputReportBuffer[13];

                        //Console.WriteLine(current.LT);
                        //Console.WriteLine(current.RT);

                        tempAxisX = (short)((inputReportBuffer[18] << 8) | inputReportBuffer[17]);
                        tempAxisY = (short)((inputReportBuffer[20] << 8) | inputReportBuffer[19]);

                        if (!lsCoorConflict)
                        {
                            if (current.LeftPad.Touch)
                            {
                                // Only Touchpad is active
                                current.LeftPad.X = tempAxisX;
                                current.LeftPad.Y = tempAxisY;
                            }
                            else
                            {
                                // Only LS is active
                                current.LX = tempAxisX;
                                current.LY = tempAxisY;
                            }
                        }
                        else
                        {
                            // Preserve previous coordinate of unused input
                            if (current.LeftPad.Touch)
                            {
                                // Preserve previous LS state
                                current.LeftPad.X = tempAxisX;
                                current.LeftPad.Y = tempAxisY;
                                current.LX = previous.LX;
                                current.LY = previous.LY;
                            }
                            else
                            {
                                // Preserve previous Left Touchpad state
                                current.LX = tempAxisX;
                                current.LY = tempAxisY;
                                current.LeftPad.X = previous.LeftPad.X;
                                current.LeftPad.Y = previous.LeftPad.Y;
                            }
                        }

                        current.RightPad.X = (short)((inputReportBuffer[22] << 8) | inputReportBuffer[21]);
                        current.RightPad.Y = (short)((inputReportBuffer[24] << 8) | inputReportBuffer[23]);

                        //Console.WriteLine("X: {0} Y: {1}", tempAxisX, tempAxisY);

                        current.Motion.AccelX = (short)(-1 * (inputReportBuffer[30] << 8) | inputReportBuffer[29]);
                        current.Motion.AccelY = (short)((inputReportBuffer[32] << 8) | inputReportBuffer[31]);
                        current.Motion.AccelZ = (short)((inputReportBuffer[34] << 8) | inputReportBuffer[33]);

                        current.Motion.GyroPitch = (short)(-1 * (inputReportBuffer[36] << 8) | inputReportBuffer[35]);
                        current.Motion.GyroRoll = (short)(-1 * (inputReportBuffer[38] << 8) | inputReportBuffer[37]);
                        current.Motion.GyroYaw = (short)(-1 * (inputReportBuffer[40] << 8) | inputReportBuffer[39]);

                        //Console.WriteLine("AccelZ: {0}", current.Motion.AccelZ);

                        Report?.Invoke(this, device);
                        device.SyncStates();

                        firstReport = false;
                    }
                    else
                    {
                        activeInputLoop = false;
                    }
                }
            }
        }
    }
}
