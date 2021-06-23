using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.SteamControllerLibrary
{
    public class SteamControllerDevice
    {
        public static class SCPacketType
        {
            public const byte PT_INPUT = 0x01;
            public const byte PT_HOTPLUG = 0x03;
            public const byte PT_IDLE = 0x04;
            public const byte PT_OFF = 0x9F;
            public const byte PT_AUDIO = 0xB6;
            public const byte PT_CLEAR_MAPPINGS = 0x81;
            public const byte PT_CONFIGURE = 0x87;
            public const byte PT_LED = 0x87;
            public const byte PT_CALIBRATE_JOYSTICK = 0xBF;
            public const byte PT_CALIBRATE_TRACKPAD = 0xA7;
            public const byte PT_SET_AUDIO_INDICES = 0xC1;
            public const byte PT_LIZARD_BUTTONS = 0x85;
            public const byte PT_LIZARD_MOUSE = 0x8E;
            public const byte PT_FEEDBACK = 0x8F;
            public const byte PT_RESET = 0x95;
            public const byte PT_GET_SERIAL = 0xAE;
        }

        public static class SCPacketLength
        {
            public const byte PL_LED = 0x03;
            public const byte PL_OFF = 0x04;
            public const byte PL_FEEDBACK = 0x07;
            public const byte PL_CONFIGURE = 0x15;
            public const byte PL_CONFIGURE_BT = 0x0F;
            public const byte PL_GET_SERIAL = 0x15;
        }

        public static class SCConfigType
        {
            public const byte CT_LED = 0x2D;
            public const byte CT_CONFIGURE = 0x32;
            public const byte CT_CONFIGURE_BT = 0x18;
        }

        public enum SCControllerState : uint
        {
            SS_NOT_CONFIGURED,
            SS_READY,
            SS_FAILED,
        }

        public struct StickAxisData
        {
            public ushort max;
            public ushort mid;
            public ushort min;
        };

        public enum ConnectionType : uint
        {
            USB,
            SCDongle,
            Bluetooth,
        }

        private ConnectionType conType;

        private HidDevice hidDevice;
        public HidDevice HidDevice { get => hidDevice; }

        private bool modeChangeDone;
        public bool ModeChangeDone { get => modeChangeDone; }

        private SteamControllerState currentState;
        public SteamControllerState CurrentState { get => currentState; }
        public ref SteamControllerState CurrentStateRef { get => ref currentState; }

        private SteamControllerState previousState;
        public SteamControllerState PreviousState { get => previousState; }
        public ref SteamControllerState PreviousStateRef { get => ref previousState; }

        public const int INPUT_REPORT_LEN = 65;
        public const int OUTPUT_REPORT_LEN = 65;
        public const int RUMBLE_REPORT_LEN = 65;
        public const int FEATURE_REPORT_LEN = 65;

        public int InputReportLen { get => INPUT_REPORT_LEN; }
        public int OutputReportLen { get => OUTPUT_REPORT_LEN; }
        public int RumbleReportLen { get => RUMBLE_REPORT_LEN; }

        private SCControllerState controllerModeState;
        public SCControllerState ControllerModeState { get => controllerModeState; }

        public SteamControllerDevice(HidDevice device)
        {
            hidDevice = device;
            conType = DetermineConnectionType(hidDevice);
        }

        public static ConnectionType DetermineConnectionType(HidDevice device)
        {
            // Initially assume a USB connection
            ConnectionType result = ConnectionType.USB;
            if (device.Attributes.ProductId ==
                SteamControllerEnumerator.STEAM_DONGLE_CONTROLLER_PRODUCT_ID)
            //if (device.Attributes.ProductId == 0x1142)
            {
                result = ConnectionType.SCDongle;
            }

            return result;
        }

        public void SetOperational()
        {
            if (modeChangeDone)
            {
                return;
            }

            if (!hidDevice.IsFileStreamOpen())
            {
                hidDevice.OpenFileStream(OutputReportLen);
            }

            ReadSerial();
            ClearMappings();
            Configure();

            controllerModeState = SCControllerState.SS_READY;
            modeChangeDone = true;
        }

        public void Detach()
        {
            if (controllerModeState == SCControllerState.SS_READY)
            {
                ChangeToLizardMode();
            }

            controllerModeState = SCControllerState.SS_NOT_CONFIGURED;
        }

        private void ReadSerial()
        {
            byte[] featureData = new byte[FEATURE_REPORT_LEN];
            featureData[1] = SCPacketType.PT_GET_SERIAL;
            featureData[2] = 0x15;
            featureData[3] = 0x01;
            hidDevice.WriteFeatureReport(featureData);
            hidDevice.fileStream.Flush();

            byte[] retReportData = new byte[FEATURE_REPORT_LEN];
            hidDevice.readFeatureData(retReportData);
            //Console.WriteLine("LKJDKJLLD: {0}", retReportData[1]);
        }

        private void ClearMappings()
        {
            byte[] featureData = new byte[FEATURE_REPORT_LEN];
            featureData[1] = SCPacketType.PT_CLEAR_MAPPINGS;
            featureData[2] = 0x01;
            hidDevice.WriteFeatureReport(featureData);
        }

        private void Configure()
        {
            int timeout = 600;
            int ledLevel = 90;
            byte[] gyroAndTimeoutFeatureData = new byte[FEATURE_REPORT_LEN];
            gyroAndTimeoutFeatureData[1] = SCPacketType.PT_CONFIGURE;
            gyroAndTimeoutFeatureData[2] = SCPacketLength.PL_CONFIGURE;
            gyroAndTimeoutFeatureData[3] = SCConfigType.CT_CONFIGURE;
            gyroAndTimeoutFeatureData[4] = 0; // Idle Timeout
            gyroAndTimeoutFeatureData[5] = 0; // Idle Timeout
            // Unknown Header
            Array.Copy(new byte[] { 0x18, 0x00, 0x00, 0x31, 0x02, 0x00, 0x08, 0x07, 0x00, 0x07, 0x07, 0x00, 0x30 }, 0, gyroAndTimeoutFeatureData, 6, 13);
            gyroAndTimeoutFeatureData[19] = true ? 0x1C : 0x00; // Gyro Enable (0x1C = Enable. 0x00 = Disable)
            gyroAndTimeoutFeatureData[20] = 0x00; // Unknown
            gyroAndTimeoutFeatureData[21] = 0x2E; // Unknown
            hidDevice.WriteFeatureReport(gyroAndTimeoutFeatureData);

            byte[] ledsFeatureData = new byte[FEATURE_REPORT_LEN];
            ledsFeatureData[1] = SCPacketType.PT_CONFIGURE;
            ledsFeatureData[2] = SCPacketLength.PL_LED;
            ledsFeatureData[3] = SCConfigType.CT_LED;
            ledsFeatureData[4] = (byte)(Math.Min(Math.Max(ledLevel, 0), 100)); // LED Level (0-100?)
            hidDevice.WriteFeatureReport(ledsFeatureData);
        }

        private void ChangeToLizardMode()
        {
            byte[] featureData = new byte[FEATURE_REPORT_LEN];
            featureData[1] = SCPacketType.PT_LIZARD_BUTTONS;
            featureData[2] = 0x01;
            hidDevice.WriteFeatureReport(featureData);

            featureData[1] = SCPacketType.PT_LIZARD_MOUSE;
            hidDevice.WriteFeatureReport(featureData);
        }
        public void SyncStates()
        {
            previousState = currentState;
        }
    }
}
