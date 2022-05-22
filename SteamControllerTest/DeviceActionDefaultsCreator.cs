using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.TouchpadActions;

namespace SteamControllerTest
{
    public abstract class DeviceActionDefaultsCreator
    {
        public struct TouchJoystickActionValues
        {
            public double deadZone;
            public double antiDeadZone;
            public double maxZone;

            public void Process(TouchpadStickAction action)
            {
                action.DeadMod.DeadZone = deadZone;
                action.DeadMod.AntiDeadZone = antiDeadZone;
                action.DeadMod.MaxZone = maxZone;
            }
        }

        public struct TouchMouseActionValues
        {
            public int deadZone;

            public void Process(TouchpadMouse action)
            {
                action.DeadZone = deadZone;
            }
        }

        public abstract TouchJoystickActionValues GrabTouchJoystickDefaults();
        public abstract TouchMouseActionValues GrabTouchMouseDefaults();
    }

    public class SteamControllerActionDefaultsCreator : DeviceActionDefaultsCreator
    {
        public override TouchJoystickActionValues GrabTouchJoystickDefaults()
        {
            TouchJoystickActionValues result = new TouchJoystickActionValues()
            {
                deadZone = 0.05,
                antiDeadZone = 0.35,
                maxZone = 0.7,
            };

            return result;
        }

        public override TouchMouseActionValues GrabTouchMouseDefaults()
        {
            TouchMouseActionValues result = new TouchMouseActionValues()
            {
                deadZone = 8,
            };

            return result;
        }
    }
}
