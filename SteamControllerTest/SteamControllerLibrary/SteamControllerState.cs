using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.SteamControllerLibrary
{
    public struct SteamControllerState
    {
        public struct TouchPadInfo
        {
            public short X;
            public short Y;
            public bool Touch;
            public bool Click;
        }

        public struct SteamControllerMotion
        {
            public short AccelX;
            public short AccelY;
            public short AccelZ;
            //public double AccelXG, AccelYG, AccelZG;

            public short GyroYaw;
            public short GyroPitch;
            public short GyroRoll;
            //public double AngGyroYaw, AngGyroPitch, AngGyroRoll;

            // TODO: STUB
            public void Populate()
            {

            }
        }

        public double timeElapsed;

        public bool A;
        public bool B;
        public bool X;
        public bool Y;
        public bool LB;
        public bool RB;
        public bool Back;
        public bool Start;
        public bool Guide;
        public byte LT;
        public byte RT;
        public bool LTClick;
        public bool RTClick;
        public bool LGrip;
        public bool RGrip;
        public TouchPadInfo LeftPad;
        public TouchPadInfo RightPad;
        public short LX;
        public short LY;
        public bool LSClick;
        public bool DPadUp;
        public bool DPadDown;
        public bool DPadLeft;
        public bool DPadRight;
        public SteamControllerMotion Motion;
    }
}
