using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.MapperUtil;
using FakerInputWrapper;

namespace SteamControllerTest
{
    public static class OutputDataAliasUtil
    {
        public static string GetStringForKeyboardKey(int keyCode)
        {
            string result = "";
            KeyboardKey tempKeyCode = (KeyboardKey)keyCode;
            switch (tempKeyCode)
            {
                case KeyboardKey.A:
                    result = "A";
                    break;
                case KeyboardKey.B:
                    result = "B";
                    break;
                case KeyboardKey.C:
                    result = "C";
                    break;
                case KeyboardKey.D:
                    result = "D";
                    break;
                case KeyboardKey.E:
                    result = "E";
                    break;
                case KeyboardKey.F:
                    result = "F";
                    break;
                case KeyboardKey.G:
                    result = "G";
                    break;
                case KeyboardKey.H:
                    result = "H";
                    break;
                case KeyboardKey.I:
                    result = "I";
                    break;
                case KeyboardKey.J:
                    result = "J";
                    break;
                case KeyboardKey.K:
                    result = "K";
                    break;
                case KeyboardKey.L:
                    result = "L";
                    break;
                case KeyboardKey.M:
                    result = "M";
                    break;
                case KeyboardKey.N:
                    result = "N";
                    break;
                case KeyboardKey.O:
                    result = "O";
                    break;
                case KeyboardKey.P:
                    result = "P";
                    break;
                case KeyboardKey.Q:
                    result = "Q";
                    break;
                case KeyboardKey.R:
                    result = "R";
                    break;
                case KeyboardKey.S:
                    result = "S";
                    break;
                case KeyboardKey.T:
                    result = "T";
                    break;
                case KeyboardKey.U:
                    result = "U";
                    break;
                case KeyboardKey.V:
                    result = "V";
                    break;
                case KeyboardKey.W:
                    result = "W";
                    break;
                case KeyboardKey.X:
                    result = "X";
                    break;
                case KeyboardKey.Y:
                    result = "Y";
                    break;
                case KeyboardKey.Z:
                    result = "Z";
                    break;
                case KeyboardKey.Tilde:
                    result = "Tilde";
                    break;
                default:
                    break;
            }

            return result;
        }

        public static string GetStringForMouseButton(int mouseBtnCode)
        {
            string result = "";
            MouseButtonCodes btn;
            switch(mouseBtnCode)
            {
                case MouseButtonCodes.MOUSE_LEFT_BUTTON:
                    result = "LMB";
                    break;
                case MouseButtonCodes.MOUSE_MIDDLE_BUTTON:
                    result = "MMB";
                    break;
                case MouseButtonCodes.MOUSE_RIGHT_BUTTON:
                    result = "RMB";
                    break;
                case MouseButtonCodes.MOUSE_XBUTTON1:
                    result = "XButton1";
                    break;
                case MouseButtonCodes.MOUSE_XBUTTON2:
                    result = "XButton2";
                    break;
                default:
                    break;
            }

            return result;
        }

        public static string GetStringForX360GamepadCode(JoypadActionCodes code)
        {
            string result = "";
            switch(code)
            {
                case JoypadActionCodes.X360_A:
                    result = "A";
                    break;
                case JoypadActionCodes.X360_B:
                    result = "B";
                    break;
                case JoypadActionCodes.X360_X:
                    result = "X";
                    break;
                case JoypadActionCodes.X360_Y:
                    result = "Y";
                    break;
                case JoypadActionCodes.X360_LB:
                    result = "LB";
                    break;
                case JoypadActionCodes.X360_RB:
                    result = "RB";
                    break;
                case JoypadActionCodes.X360_Back:
                    result = "Back";
                    break;
                case JoypadActionCodes.X360_Start:
                    result = "Start";
                    break;
                case JoypadActionCodes.X360_Guide:
                    result = "Guide";
                    break;
                case JoypadActionCodes.X360_LT:
                    result = "Left Trigger";
                    break;
                case JoypadActionCodes.X360_RT:
                    result = "Right Trigger";
                    break;
                case JoypadActionCodes.X360_ThumbL:
                    result = "LStick Click";
                    break;
                case JoypadActionCodes.X360_ThumbR:
                    result = "RStick Click";
                    break;
                case JoypadActionCodes.X360_DPAD_UP:
                    result = "DPad Up";
                    break;
                case JoypadActionCodes.X360_DPAD_DOWN:
                    result = "DPad Down";
                    break;
                case JoypadActionCodes.X360_DPAD_LEFT:
                    result = "DPad Left";
                    break;
                case JoypadActionCodes.X360_DPAD_RIGHT:
                    result = "DPad Right";
                    break;
                case JoypadActionCodes.X360_LX_NEG:
                    result = "LX-";
                    break;
                case JoypadActionCodes.X360_LX_POS:
                    result = "LX+";
                    break;
                case JoypadActionCodes.X360_LY_NEG:
                    result = "LY-";
                    break;
                case JoypadActionCodes.X360_LY_POS:
                    result = "LY+";
                    break;

                case JoypadActionCodes.X360_RX_NEG:
                    result = "RX-";
                    break;
                case JoypadActionCodes.X360_RX_POS:
                    result = "RX+";
                    break;
                case JoypadActionCodes.X360_RY_NEG:
                    result = "RY-";
                    break;
                case JoypadActionCodes.X360_RY_POS:
                    result = "RY+";
                    break;
                default:
                    break;
            }

            return result;
        }

        public static string GetStringForGamepadStick(StickActionCodes code)
        {
            string result = "";
            switch(code)
            {
                case StickActionCodes.X360_LS:
                    result = "LS";
                    break;
                case StickActionCodes.X360_RS:
                    result = "RS";
                    break;
                default:
                    break;
            }

            return result;
        }
    }
}
