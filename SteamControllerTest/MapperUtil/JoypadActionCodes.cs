using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.MapperUtil
{
    public enum JoypadActionCodes : uint
    {
        Empty, AlwaysOn = Empty,
        Btn1, BtnNorth = Btn1, X360_Y = Btn1,
        Btn2, BtnEast = Btn2, X360_B = Btn2,
        Btn3, BtnSouth = Btn3, X360_A = Btn3,
        Btn4, BtnWest = Btn4, X360_X = Btn4,
        Btn5, BtnLShoulder = Btn5, X360_LB = Btn5,
        Btn6, BtnRShoulder = Btn6, X360_RB = Btn6,
        Btn7, BtnMode = Btn7, X360_Guide = Btn7,
        Btn8, BtnStart = Btn8, X360_Start = Btn8,
        Btn9, BtnSelect = Btn9, X360_Back = Btn9,
        Btn10, BtnThumbL = Btn10, X360_ThumbL = Btn10,
        Btn11, BtnThumbR = Btn11, X360_ThumbR = Btn11,
        Btn12, BtnDPadUp = Btn12, X360_DPAD_UP = Btn12,
        Btn13, BtnDPadDown = Btn13, X360_DPAD_DOWN = Btn13,
        Btn14, BtnDPadLeft = Btn14, X360_DPAD_LEFT = Btn14,
        Btn15, BtnDPadRight = Btn15, X360_DPAD_RIGHT = Btn15,

        Btn16, BtnHome = Btn16,
        Btn17, BtnCapture = Btn17,

        BtnMax = 255,

        Axis1 = 1 << 8, AxisLX = Axis1, X360_LX = Axis1,
        Axis2, AxisLY = Axis2, X360_LY = Axis2,
        Axis3, AxisRX = Axis3, X360_RX = Axis3,
        Axis4, AxisRY = Axis4, X360_RY = Axis4,
        Axis5, AxisLTrigger = Axis5, X360_LT = Axis5,
        Axis6, AxisRTrigger = Axis6, X360_RT = Axis6,

        AxisMax = 511,

        // Apply axis direction aliases. Mainly for button to axis bindings
        Axis1Neg, AxisLXNeg = Axis1Neg, X360_LX_NEG = Axis1Neg,
        Axis1Pos, AxisLXPos = Axis1Pos, X360_LX_POS = Axis1Pos,

        Axis2Neg, AxisLYNeg = Axis2Neg, X360_LY_NEG = Axis2Neg,
        Axis2Pos, AxisLYPos = Axis2Pos, X360_LY_POS = Axis2Pos,

        Axis3Neg, AxisRXNeg = Axis3Neg, X360_RX_NEG = Axis3Neg,
        Axis3Pos, AxisRXPos = Axis3Pos, X360_RX_POS = Axis3Pos,

        Axis4Neg, AxisRYNeg = Axis4Neg, X360_RY_NEG = Axis4Neg,
        Axis4Pos, AxisRYPos = Axis4Pos, X360_RY_POS = Axis4Pos,
    }

    public enum StickActionCodes : uint
    {
        Empty,
        Stick1, LS = Stick1, X360_LS = Stick1,
        Stick2, RS = Stick2, X360_RS = Stick2,

        StickMax = 127,
    }

    public enum DPadActionCodes : uint
    {
        Empty,
        DPad, DPad1 = DPad, X360_DPAD = DPad,
        DPad2,

        DPadMax = 127,
    }

    public enum TouchpadActionCodes : uint
    {
        Empty,
        Touch1, Touch = Touch1, TouchL = Touch1,
        Touch2, TouchR = Touch2,
    }

    public enum TriggerActionCodes : uint
    {
        Empty,
        Trigger1, Trigger = Trigger1, LeftTrigger = Trigger1, LT = Trigger1,
        Trigger2, RightTrigger = Trigger2, RT = RightTrigger,
    }

    public static class MouseButtonCodes
    {
        public const int MOUSE_LEFT_BUTTON = 1;
        public const int MOUSE_MIDDLE_BUTTON = 2;
        public const int MOUSE_RIGHT_BUTTON = 3;

        public const int MOUSE_XBUTTON1 = 6;
        public const int MOUSE_XBUTTON2 = 7;
    }

    public enum MouseWheelCodes : uint
    {
        None = 0,
        WheelUp,
        WheelDown,
        WheelLeft,
        WheelRight,
    }

    public enum JoypadAxesCodes : uint
    {
        Axis1 = 1 << 8, AxisLX = Axis1, X360_LX = Axis1,
        Axis2, AxisLY = Axis2, X360_LY = Axis2,
        Axis3, AxisRX = Axis3, X360_RX = Axis3,
        Axis4, AxisRY = Axis4, X360_RY = Axis4,
        Axis5, AxisLTrigger = Axis5, X360_LT = Axis5,
        Axis6, AxisRTrigger = Axis6, X360_RT = Axis6,
    }

    public enum SplitAxesAnalogBindingCodes : uint
    {
        None,
        Axis1 = 1 << 8, AxisLX = Axis1, X360_LX = Axis1,
        Axis2, AxisLY = Axis2, X360_LY = Axis2,
        Axis3, AxisRX = Axis3, X360_RX = Axis3,
        Axis4, AxisRY = Axis4, X360_RY = Axis4,
        Axis5, AxisLTrigger = Axis5, X360_LT = Axis5,
        Axis6, AxisRTrigger = Axis6, X360_RT = Axis6, AxisMax = Axis6,
        MouseX,
        MouseY,
        MouseWheelX,
        MouseWheelY,
    }
}
