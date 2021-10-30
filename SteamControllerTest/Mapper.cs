using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Sensorit.Base;

using SteamControllerTest.SteamControllerLibrary;
using FakerInputWrapper;

namespace SteamControllerTest
{
    public class Mapper
    {
        public enum DpadDirections : uint
        {
            Centered,
            Up = 1,
            Right = 2,
            UpRight = 3,
            Down = 4,
            DownRight = 6,
            Left = 8,
            UpLeft = 9,
            DownLeft = 12,
        }

        public struct ButtonKeyAssociation
        {
            public ushort A;
            public ushort B;
            public ushort X;
            public ushort Y;
            public ushort LB;
            public ushort RB;
            public ushort Back;
            public ushort Guide;
            public ushort Start;
            public ushort LSClick;
            public ushort LGrip;
            public ushort RGrip;
            public ushort LeftTouchClick;
            public ushort RightTouchClick;
        }

        public struct TouchpadKeyAssociation
        {
            public ushort Up;
            public ushort Down;
            public ushort Left;
            public ushort Right;
            public ushort Click;
        }

        private const short STICK_MAX = 30000;
        private const short STICK_MIN = -30000;

        private const int inputResolution = STICK_MAX - STICK_MIN;
        private const float reciprocalInputResolution = 1 / (float)inputResolution;

        private const ushort STICK_MIDRANGE = inputResolution / 2;
        private const ushort STICK_NEUTRAL = STICK_MAX - STICK_MIDRANGE;

        private const int X360_STICK_MAX = 32767;
        private const int X360_STICK_MIN = -32768;
        private const int OUTPUT_X360_RESOLUTION = X360_STICK_MAX - X360_STICK_MIN;

        private const int LT_DEADZONE = 80;
        private const int RT_DEADZONE = 80;

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

        private bool mouseLBDown;
        private bool mouseRBDown;
        private ButtonKeyAssociation buttonBindings;
        private TouchpadKeyAssociation leftTouchBindings;
        private TouchpadKeyAssociation rightTouchBindings;
        private DpadDirections currentLeftDir;
        private DpadDirections previousLeftDir;

        private const int TRACKBALL_INIT_FRICTION = 10;
        private const int TRACKBALL_JOY_FRICTION = 8;
        private const int TRACKBALL_MASS = 45;
        private const double TRACKBALL_RADIUS = 0.0245;

        private double TRACKBALL_INERTIA = 2.0 * (TRACKBALL_MASS * TRACKBALL_RADIUS * TRACKBALL_RADIUS) / 5.0;
        //private double TRACKBALL_SCALE = 0.000023;
        private double TRACKBALL_SCALE = 0.000023;
        private const int TRACKBALL_BUFFER_LEN = 8;
        private double[] trackballXBuffer = new double[TRACKBALL_BUFFER_LEN];
        private double[] trackballYBuffer = new double[TRACKBALL_BUFFER_LEN];
        private int trackballBufferTail = 0;
        private int trackballBufferHead = 0;
        private double trackballAccel = 0.0;
        private double trackballXVel = 0.0;
        private double trackballYVel = 0.0;
        private bool trackballActive = false;
        private double trackballDXRemain = 0.0;
        private double trackballDYRemain = 0.0;

        private OneEuroFilter filterX = new OneEuroFilter(1.0, 0.5);
        private OneEuroFilter filterY = new OneEuroFilter(1.0, 0.5);
        //private OneEuroFilter filterX = new OneEuroFilter(2.0, 0.8);
        //private OneEuroFilter filterY = new OneEuroFilter(2.0, 0.8);
        private double currentRate = 0.0;

        private FakerInput fakerInput = new FakerInput();
        private KeyboardReport keyboardReport = new KeyboardReport();
        private RelativeMouseReport mouseReport = new RelativeMouseReport();
        private KeyboardEnhancedReport mediaKeyboardReport = new KeyboardEnhancedReport();

        private bool keyboardSync = false;
        private bool keyboardEnhancedSync = false;
        //private bool mouseSync = false;

        public Mapper(SteamControllerDevice device)
        {
            this.device = device;

            //trackballAccel = TRACKBALL_RADIUS * TRACKBALL_INIT_FRICTION / TRACKBALL_INERTIA;
            trackballAccel = TRACKBALL_RADIUS * TRACKBALL_JOY_FRICTION / TRACKBALL_INERTIA;
        }

        //public void Start(ViGEmClient vigemTestClient,
        //    SwitchProDevice proDevice, SwitchProReader proReader)

        public void Start(ViGEmClient vigemTestClient,
            SteamControllerDevice device, SteamControllerReader reader)
        {
            bool checkConnect = fakerInput.Connect();
            //Trace.WriteLine(checkConnect);

            PopulateKeyBindings();

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

        public void PopulateKeyBindings()
        {
            /*buttonBindings.A = (ushort)KeyInterop.VirtualKeyFromKey(Key.Space);
            buttonBindings.B = (ushort)KeyInterop.VirtualKeyFromKey(Key.S);
            buttonBindings.X = (ushort)KeyInterop.VirtualKeyFromKey(Key.Return);
            buttonBindings.Y = (ushort)KeyInterop.VirtualKeyFromKey(Key.R);
            buttonBindings.LB = (ushort)KeyInterop.VirtualKeyFromKey(Key.Q);
            buttonBindings.RB = (ushort)KeyInterop.VirtualKeyFromKey(Key.Z);
            buttonBindings.Back = (ushort)KeyInterop.VirtualKeyFromKey(Key.Tab);
            buttonBindings.Start = (ushort)KeyInterop.VirtualKeyFromKey(Key.Escape);
            buttonBindings.Guide = (ushort)KeyInterop.VirtualKeyFromKey(Key.Tab);
            buttonBindings.LGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.X);
            //buttonBindings.RGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.F);
            buttonBindings.RGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.VolumeMute);
            */

            /*leftTouchBindings.Up = (ushort)KeyInterop.VirtualKeyFromKey(Key.W);
            leftTouchBindings.Left = (ushort)KeyInterop.VirtualKeyFromKey(Key.A);
            leftTouchBindings.Down = (ushort)KeyInterop.VirtualKeyFromKey(Key.S);
            leftTouchBindings.Right = (ushort)KeyInterop.VirtualKeyFromKey(Key.D);
            */

            //leftTouchBindings.Up = (ushort)KeyInterop.VirtualKeyFromKey(Key.Up);
            //leftTouchBindings.Left = (ushort)KeyInterop.VirtualKeyFromKey(Key.Left);
            //leftTouchBindings.Down = (ushort)KeyInterop.VirtualKeyFromKey(Key.Down);
            //leftTouchBindings.Right = (ushort)KeyInterop.VirtualKeyFromKey(Key.Right);

            buttonBindings.A = (ushort)KeyboardKey.Spacebar;
            buttonBindings.B = (ushort)KeyboardKey.C;
            buttonBindings.X = (ushort)KeyboardKey.R;
            buttonBindings.Y = (ushort)KeyboardKey.E;
            buttonBindings.LB = (ushort)KeyboardKey.Q;
            buttonBindings.RB = (ushort)KeyboardKey.Z;
            buttonBindings.Back = (ushort)KeyboardKey.Tab;
            buttonBindings.Start = (ushort)KeyboardKey.Escape;
            buttonBindings.Guide = (ushort)KeyboardKey.Tab;
            buttonBindings.LGrip = (ushort)KeyboardKey.X;
            //buttonBindings.RGrip = (ushort)KeyboardKey.F;
            buttonBindings.RGrip = (ushort)KeyboardKey.Spacebar;

            leftTouchBindings.Up = (ushort)KeyboardKey.W;
            leftTouchBindings.Left = (ushort)KeyboardKey.A;
            leftTouchBindings.Down = (ushort)KeyboardKey.S;
            leftTouchBindings.Right = (ushort)KeyboardKey.D;
            //leftTouchBindings.Up = (ushort)KeyboardKey.UpArrow;
            //leftTouchBindings.Left = (ushort)KeyboardKey.LeftArrow;
            //leftTouchBindings.Down = (ushort)KeyboardKey.DownArrow;
            //leftTouchBindings.Right = (ushort)KeyboardKey.RightArrow;
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
            mouseSync = keyboardSync = keyboardEnhancedSync = false;

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

                /*if (current.DPadUp) tempButtons |= Xbox360Button.Up.Value;
                if (current.DPadDown) tempButtons |= Xbox360Button.Down.Value;
                if (current.DPadLeft) tempButtons |= Xbox360Button.Left.Value;
                if (current.DPadRight) tempButtons |= Xbox360Button.Right.Value;
                */

                current.LeftPad.Rotate(-18.0 * Math.PI / 180.0);
                current.RightPad.Rotate(8.0 * Math.PI / 180.0);
                TouchDPad(ref current, ref previous, ref tempButtons);

                outputX360.SetButtonsFull(tempButtons);
            }

            short temp;
            currentRate = current.timeElapsed;
            //Console.WriteLine(currentRate);
            /*if (current.LeftPad.Touch)
            {
                temp = Math.Min(Math.Max(current.LeftPad.X, STICK_MIN), STICK_MAX);
                temp = AxisScale(temp, false);
                outputX360.LeftThumbX = temp;

                temp = Math.Min(Math.Max(current.LeftPad.Y, STICK_MIN), STICK_MAX);
                temp = AxisScale(temp, false);
                outputX360.LeftThumbY = temp;
            }
            */

            //*
            // Normal Left Stick
            temp = Math.Min(Math.Max(current.LX, STICK_MIN), STICK_MAX);
            temp = AxisScale(temp, false);
            outputX360.LeftThumbX = temp;

            temp = Math.Min(Math.Max(current.LY, STICK_MIN), STICK_MAX);
            temp = AxisScale(temp, false);
            outputX360.LeftThumbY = temp;
            //*/

            // Right Touchpad Mouse-like Joystick
            outputX360.RightThumbX = 0;
            outputX360.RightThumbY = 0;
            //TouchMouseJoystickPad(ref current, ref previous, ref outputX360);
            TrackballMouseJoystickProcess(ref current, ref previous, ref outputX360);

            outputX360.LeftTrigger = current.LT;
            outputX360.RightTrigger = current.RT;

            //if (current.A != previous.A)
            //{
            //    ushort tempKey = buttonBindings.A;
            //    if (current.A)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //keyboardReport.KeyDown(KeyboardKey.Spacebar);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //keyboardReport.KeyUp(KeyboardKey.Spacebar);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.B != previous.B)
            //{
            //    ushort tempKey = buttonBindings.B;
            //    if (current.B)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //keyboardReport.KeyDown(KeyboardKey.S);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //keyboardReport.KeyUp(KeyboardKey.S);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.X != previous.X)
            //{
            //    ushort tempKey = buttonBindings.X;
            //    if (current.X)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.Y != previous.Y)
            //{
            //    ushort tempKey = buttonBindings.Y;
            //    if (current.Y)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //keyboardReport.KeyDown(KeyboardKey.R);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //keyboardReport.KeyUp(KeyboardKey.R);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.LB != previous.LB)
            //{
            //    if (current.LB)
            //    {
            //        // Wheel Up
            //        //InputMethods.MouseWheel(128, 0);
            //        //int click = 1;
            //        mouseReport.WheelPosition = 1;
            //        mouseSync = true;
            //    }

            //    /*ushort tempKey = buttonBindings.LB;
            //    if (current.LB)
            //    {
            //        InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        InputMethods.performKeyRelease(tempKey);
            //    }
            //    */
            //}

            //if (current.RB != previous.RB)
            //{
            //    if (current.RB)
            //    {
            //        // Wheel Down
            //        //InputMethods.MouseWheel(-128, 0);
            //        int click = -1;
            //        mouseReport.WheelPosition = (byte)click;
            //        mouseSync = true;
            //    }

            //    /*ushort tempKey = buttonBindings.RB;
            //    if (current.RB)
            //    {
            //        InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        InputMethods.performKeyRelease(tempKey);
            //    }
            //    */
            //}

            //if (current.LGrip != previous.LGrip)
            //{
            //    ushort tempKey = buttonBindings.LGrip;
            //    if (current.LGrip)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //keyboardReport.KeyDown(KeyboardKey.X);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //keyboardReport.KeyUp(KeyboardKey.X);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.RGrip != previous.RGrip)
            //{
            //    ushort tempKey = buttonBindings.RGrip;
            //    if (current.RGrip)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //    //keyboardEnhancedSync = true;
            //}

            //if (current.Back != previous.Back)
            //{
            //    ushort tempKey = buttonBindings.Back;
            //    if (current.Back)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.Start != previous.Start)
            //{
            //    ushort tempKey = buttonBindings.Start;
            //    if (current.Start)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //keyboardReport.KeyDown(KeyboardKey.Escape);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //keyboardReport.KeyUp(KeyboardKey.Escape);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.Guide != previous.Guide)
            //{
            //    ushort tempKey = buttonBindings.Guide;
            //    if (current.Guide)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            //if (current.LSClick != previous.LSClick)
            //{
            //    ushort tempKey = buttonBindings.LSClick;
            //    if (current.LSClick)
            //    {
            //        keyboardReport.KeyDown((KeyboardKey)tempKey);
            //        //InputMethods.performKeyPress(tempKey);
            //    }
            //    else
            //    {
            //        keyboardReport.KeyUp((KeyboardKey)tempKey);
            //        //InputMethods.performKeyRelease(tempKey);
            //    }

            //    keyboardSync = true;
            //}

            ////if (current.RTClick != previous.RTClick)
            //if ((current.RT > RT_DEADZONE && !mouseLBDown) || (current.RT <= RT_DEADZONE && mouseLBDown))
            //{
            //    mouseLBDown = current.RT > RT_DEADZONE;
            //    //Console.WriteLine("RT: {0} {1}", current.RT, mouseLBDown);
            //    if (mouseLBDown)
            //    {
            //        mouseReport.ButtonDown(FakerInputWrapper.MouseButton.LeftButton);
            //    }
            //    else
            //    {
            //        mouseReport.ButtonUp(FakerInputWrapper.MouseButton.LeftButton);
            //    }

            //    mouseSync = true;
            //    //InputMethods.MouseEvent(mouseLBDown ? InputMethods.MOUSEEVENTF_LEFTDOWN :
            //    //    InputMethods.MOUSEEVENTF_LEFTUP);
            //}

            ////if (current.LTClick != previous.LTClick)
            ////if ((current.LT > 50) != (previous.LT <= 50))
            //if ((current.LT > LT_DEADZONE && !mouseRBDown) || (current.LT <= LT_DEADZONE && mouseRBDown))
            //{
            //    mouseRBDown = current.LT > LT_DEADZONE;
            //    if (mouseRBDown)
            //    {
            //        mouseReport.ButtonDown(FakerInputWrapper.MouseButton.RightButton);
            //    }
            //    else
            //    {
            //        mouseReport.ButtonUp(FakerInputWrapper.MouseButton.RightButton);
            //    }

            //    mouseSync = true;
            //    //InputMethods.MouseEvent(mouseRBDown ? InputMethods.MOUSEEVENTF_RIGHTDOWN :
            //    //    InputMethods.MOUSEEVENTF_RIGHTUP);
            //}

            ///*if (current.RightPad.Touch && previous.RightPad.Touch)
            //{
            //    // Process normal mouse
            //    RightTouchMouse(ref current, ref previous);
            //    Console.WriteLine("NORMAL");
            //}
            //*/

            //if (current.LeftPad.Touch || previous.LeftPad.Touch)
            //{
            //    current.LeftPad.Rotate(-18.0 * Math.PI / 180.0);
            //    TouchActionPad(ref current, ref previous);
            //}

            //current.RightPad.Rotate(8.0 * Math.PI / 180.0);
            //TrackballMouseProcess(ref current, ref previous);

            //if (mouseX != 0.0 || mouseY != 0.0)
            //{
            //    //Console.WriteLine("MOVE: {0}, {1}", (int)mouseX, (int)mouseY);
            //    GenerateMouseMoveEvent();
            //}
            //else
            //{
            //    // Probably not needed here. Leave as a temporary precaution
            //    mouseXRemainder = mouseYRemainder = 0.0;

            //    filterX.Filter(0.0, 1.0 / currentRate); // Smooth on output
            //    filterY.Filter(0.0, 1.0 / currentRate); // Smooth on output
            //}

            //if (keyboardSync)
            //{
            //    fakerInput.UpdateKeyboard(keyboardReport);
            //}

            //if (keyboardEnhancedSync)
            //{
            //    fakerInput.UpdateKeyboardEnhanced(mediaKeyboardReport);
            //}

            //if (mouseSync)
            //{
            //    //fakerInput.UpdateAbsoluteMouse(new AbsoluteMouseReport() { MouseX = 30000, MouseY = 20000, });
            //    fakerInput.UpdateRelativeMouse(mouseReport);
            //    mouseReport.ResetMousePos();
            //}
            
            outputX360.SubmitReport();
        }

        private void TouchMouseJoystickPad(int dx, int dy,
            ref SteamControllerState current,
            ref SteamControllerState previous, ref IXbox360Controller xbox)
        {
            const int deadZone = 60;
            const int maxDeadZoneAxial = 200;
            const int minDeadZoneAxial = 40;

            //int dx = 0;
            //if (current.RightPad.Touch)
            //    dx = current.RightPad.X - previous.RightPad.X;

            //int dy = 0;
            //if (current.RightPad.Touch)
            //    dy = (current.RightPad.Y - previous.RightPad.Y);

            //Trace.WriteLine(current.RightPad.Y);

            int maxDirX = dx >= 0 ? 32767 : -32768;
            int maxDirY = dy >= 0 ? 32767 : -32768;

            double tempAngle = Math.Atan2(dy, dx);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(dx);
            int signY = Math.Sign(dy);

            //double timeElapsed = current.timeElapsed;
            // Base speed 8 ms
            //double tempDouble = timeElapsed * 125.0;

            int maxValX = signX * 550;
            int maxValY = signY * 550;

            double xratio = 0.0, yratio = 0.0;
            double antiX = 0.52 * normX;
            double antiY = 0.52 * normY;

            int deadzoneX = (int)Math.Abs(normX * deadZone);
            int radialDeadZoneY = (int)(Math.Abs(normY * deadZone));
            //int deadzoneY = (int)(Math.Abs(normY * deadZone));

            int absDX = Math.Abs(dx);
            int absDY = Math.Abs(dy);

            // Check for radial dead zone first
            double mag = (dx * dx) + (dy * dy);
            if (mag <= (deadZone * deadZone))
            {
                dx = 0;
                dy = 0;
            }
            // Past radial. Check for bowtie
            else
            {
                //Trace.WriteLine($"X ({dx}) | Y ({dy})");

                // X axis calculated with scaled radial
                if (absDX > deadzoneX)
                {
                    dx -= signX * deadzoneX;
                    dx = (dx < 0 && dx < maxValX) ? maxValX :
                        (dx > 0 && dx > maxValX) ? maxValX : dx;
                }
                else
                {
                    dx = 0;
                }

                if (absDY > radialDeadZoneY)
                {
                    dy -= signY * radialDeadZoneY;
                    dy = (dy < 0 && dy < maxValY) ? maxValY :
                        (dy > 0 && dy > maxValY) ? maxValY : dy;
                }
                else
                {
                    dy = 0;
                }

                // Need to adjust Y axis dead zone based on X axis input. Bowtie
                //int deadzoneY = Math.Max(radialDeadZoneY,
                //    (int)(Math.Min(1.0, absDX / (double)maxValX) * maxDeadZoneAxialY));
                double tempRangeRatioX = Math.Abs(dx) / Math.Abs((double)maxValX);
                double tempRangeRatioY = Math.Abs(dy) / Math.Abs((double)maxValY);

                int axialDeadX = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
                    Math.Min(1.0, tempRangeRatioY) + minDeadZoneAxial);
                int deadzoneY = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
                    Math.Min(1.0, tempRangeRatioX) + minDeadZoneAxial);

                //Trace.WriteLine($"Axial {axialDeadX} DX: {dx}");
                //Trace.WriteLine(deadzoneY);
                //int deadzoneY = Math.Min(maxValX, Math.Abs(dx)) * maxDeadZoneAxialY;
                //if (absDY > radialDeadZoneY)
                //{
                //    dy -= signY * radialDeadZoneY;
                //    dy = (dy < 0 && dy < maxValY) ? maxValY :
                //        (dy > 0 && dy > maxValY) ? maxValY : dy;
                //}
                //else if (absDY > deadzoneY)

                if (Math.Abs(dx) > axialDeadX)
                {
                    dx -= signX * axialDeadX;
                    double newMaxValX = Math.Abs(maxValX) - axialDeadX;
                    double scaleX = Math.Abs(dx) / (double)(newMaxValX);
                    dx = (int)(maxValX * scaleX);
                    //Trace.WriteLine($"{scaleX} {dx}");
                }
                else
                {
                    dx = 0;
                    //Trace.WriteLine("dx zero");
                }

                if (Math.Abs(dy) > deadzoneY)
                {
                    dy -= signY * deadzoneY;
                    double newMaxValY = Math.Abs(maxValY) - deadzoneY;
                    double scaleY = Math.Abs(dy) / (double)(newMaxValY);
                    dy = (int)(maxValY * scaleY);
                }
                else
                {
                    dy = 0;
                }

                /*
                if (absDY > deadzoneY)
                {
                    int newMaxValY = signY * (Math.Abs(maxValY) - deadzoneY);
                    dy -= signY * deadzoneY;
                    //dy = (dy < 0 && dy < maxValY) ? maxValY :
                    //    (dy > 0 && dy > maxValY) ? maxValY : dy;
                    dy = (dy < 0 && dy < newMaxValY) ? newMaxValY :
                          (dy > 0 && dy > newMaxValY) ? newMaxValY : dy;

                    double scaleY;
                    if (dy == newMaxValY)
                    {
                        scaleY = 1.0;
                    }
                    else
                    {
                        scaleY = (double)(dy - 0.0) / (double)(newMaxValY - 0.0);
                    }
                    //scaleY = (Math.Abs(newMaxValY) - Math.Abs(dy)) /
                    //    (double)(Math.Abs(maxValY) - 0);
                    dy = (int)(scaleY * maxValY);
                    
                    //Trace.WriteLine($"SCALE: ({scaleY}) | NEW: ({newMaxValY}) | DY({dy})");
                }
                else
                {
                    dy = 0;
                }
                //*/
            }

            if (dx != 0) xratio = dx / (double)maxValX;
            if (dy != 0) yratio = dy / (double)maxValY;

            double maxOutRatio = 1.0;
            double maxOutXRatio = Math.Abs(Math.Cos(tempAngle)) * maxOutRatio;
            double maxOutYRatio = Math.Abs(Math.Sin(tempAngle)) * maxOutRatio;

            //Trace.WriteLine($"{maxOutXRatio} {maxOutYRatio}");
            // Expand output a bit
            maxOutXRatio = Math.Min(maxOutXRatio / 0.95, 1.0);
            // Expand output a bit
            maxOutYRatio = Math.Min(maxOutYRatio / 0.95, 1.0);

            xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
            yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);

            //Trace.WriteLine($"X ({dx}) | Y ({dy})");

            //double maxOutRatio = 0.98;
            //// Expand output a bit. Likely not going to get a straight line with Gyro
            //double maxOutXRatio = Math.Min(normX / 1.0, 1.0) * maxOutRatio;
            //double maxOutYRatio = Math.Min(normY / 1.0, 1.0) * maxOutRatio;

            //xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
            //yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);

            // QuadOut
            xratio = 1.0 - ((1.0 - xratio) * (1.0 - xratio));
            yratio = 1.0 - ((1.0 - yratio) * (1.0 - yratio));

            double xNorm = 0.0, yNorm = 0.0;
            if (xratio != 0.0)
            {
                xNorm = (1.0 - antiX) * xratio + antiX;
            }

            if (yratio != 0.0)
            {
                yNorm = (1.0 - antiY) * yratio + antiY;
            }


            short axisXOut = (short)filterX.Filter(xNorm * maxDirX,
                1.0 / currentRate);
            short axisYOut = (short)filterY.Filter(yNorm * maxDirY,
                1.0 / currentRate);

            //Trace.WriteLine($"OutX ({axisXOut}) | OutY ({axisYOut})");

            xbox.RightThumbX = axisXOut;
            xbox.RightThumbY = axisYOut;
        }

        private void TouchActionPad(ref SteamControllerState current,
            ref SteamControllerState previous)
        {
            const double DIAGONAL_RANGE = 50.0;
            const double CARDINAL_RANGE = 90.0 - DIAGONAL_RANGE;
            const double CARDINAL_HALF_RANGE = CARDINAL_RANGE / 2.0;
            //const double CARDINAL_HALF_RANGE = 22.5;

            const double upLeftEnd = 360 - CARDINAL_HALF_RANGE;
            const double upRightBegin = CARDINAL_HALF_RANGE;
            const double rightBegin = upRightBegin + DIAGONAL_RANGE;
            const double downRightBegin = rightBegin + CARDINAL_RANGE;
            const double downBegin = downRightBegin + DIAGONAL_RANGE;
            const double downLeftBegin = downBegin + CARDINAL_RANGE;
            const double leftBegin = downLeftBegin + DIAGONAL_RANGE;
            const double upLeftBegin = leftBegin + CARDINAL_RANGE;

            const int deadzoneSquared = 8000 * 8000;

            int dist = (current.LeftPad.X * current.LeftPad.X) + (current.LeftPad.Y * current.LeftPad.Y);
            if (dist < deadzoneSquared)
            {
                currentLeftDir = DpadDirections.Centered;
            }
            else
            {
                double angleRad = Math.Atan2(current.LeftPad.X, current.LeftPad.Y);
                double angle = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;
                //Console.WriteLine("{0} {1}", angle, current.LeftPad.Touch);
                /*double normX = Math.Abs(Math.Cos(tempAngle));
                double normY = Math.Abs(Math.Sin(tempAngle));
                int signX = Math.Sign(current.LeftPad.X);
                int signY = Math.Sign(current.LeftPad.Y);
                */

                if (angle == 0.0)
                {
                    currentLeftDir = DpadDirections.Centered;
                }
                else if (angle > upLeftEnd || angle < upRightBegin)
                {
                    currentLeftDir = DpadDirections.Up;
                }
                else if (angle >= upRightBegin && angle < rightBegin)
                {
                    currentLeftDir = DpadDirections.UpRight;
                }
                else if (angle >= rightBegin && angle < downRightBegin)
                {
                    currentLeftDir = DpadDirections.Right;
                }
                else if (angle >= downRightBegin && angle < downBegin)
                {
                    currentLeftDir = DpadDirections.DownRight;
                }
                else if (angle >= downBegin && angle < downLeftBegin)
                {
                    currentLeftDir = DpadDirections.Down;
                }
                else if (angle >= downLeftBegin && angle < leftBegin)
                {
                    currentLeftDir = DpadDirections.DownLeft;
                }
                else if (angle >= leftBegin && angle < upLeftBegin)
                {
                    currentLeftDir = DpadDirections.Left;
                }
                else if (angle >= upLeftBegin && angle <= upLeftEnd)
                {
                    currentLeftDir = DpadDirections.UpLeft;
                }
            }

            if (currentLeftDir != previousLeftDir)
            {
                DpadDirections xor = previousLeftDir ^ currentLeftDir;
                DpadDirections remDirs = xor & previousLeftDir;
                DpadDirections addDirs = xor & currentLeftDir;
                //Console.WriteLine("RELEASED: {0} CURRENT: {1}", remDirs, currentLeftDir);

                if ((remDirs & DpadDirections.Up) != 0)
                {
                    ushort tempKey = leftTouchBindings.Up;
                    //InputMethods.performKeyRelease(tempKey);
                    keyboardReport.KeyUp((KeyboardKey)tempKey);
                    keyboardSync = true;
                }
                else if ((remDirs & DpadDirections.Down) != 0)
                {
                    ushort tempKey = leftTouchBindings.Down;
                    //InputMethods.performKeyRelease(tempKey);
                    keyboardReport.KeyUp((KeyboardKey)tempKey);
                    keyboardSync = true;
                }

                if ((remDirs & DpadDirections.Left) != 0)
                {
                    ushort tempKey = leftTouchBindings.Left;
                    //InputMethods.performKeyRelease(tempKey);
                    keyboardReport.KeyUp((KeyboardKey)tempKey);
                    keyboardSync = true;
                }
                else if ((remDirs & DpadDirections.Right) != 0)
                {
                    ushort tempKey = leftTouchBindings.Right;
                    //InputMethods.performKeyRelease(tempKey);
                    keyboardReport.KeyUp((KeyboardKey)tempKey);
                    keyboardSync = true;
                }

                if ((addDirs & DpadDirections.Up) != 0)
                {
                    ushort tempKey = leftTouchBindings.Up;
                    //InputMethods.performKeyPress(tempKey);
                    keyboardReport.KeyDown((KeyboardKey)tempKey);
                    keyboardSync = true;
                }
                else if ((addDirs & DpadDirections.Down) != 0)
                {
                    ushort tempKey = leftTouchBindings.Down;
                    //InputMethods.performKeyPress(tempKey);
                    keyboardReport.KeyDown((KeyboardKey)tempKey);
                    keyboardSync = true;
                }

                if ((addDirs & DpadDirections.Left) != 0)
                {
                    ushort tempKey = leftTouchBindings.Left;
                    //InputMethods.performKeyPress(tempKey);
                    keyboardReport.KeyDown((KeyboardKey)tempKey);
                    keyboardSync = true;
                }
                else if ((addDirs & DpadDirections.Right) != 0)
                {
                    ushort tempKey = leftTouchBindings.Right;
                    //InputMethods.performKeyPress(tempKey);
                    keyboardReport.KeyDown((KeyboardKey)tempKey);
                    keyboardSync = true;
                }

                previousLeftDir = currentLeftDir;
            }
        }

        private void TouchDPad(ref SteamControllerState current,
            ref SteamControllerState previous, ref ushort tempButtons)
        {
            const double DIAGONAL_RANGE = 55.0;
            const double CARDINAL_RANGE = 90.0 - DIAGONAL_RANGE;
            const double CARDINAL_HALF_RANGE = CARDINAL_RANGE / 2.0;
            //const double CARDINAL_HALF_RANGE = 22.5;

            const double upLeftEnd = 360 - CARDINAL_HALF_RANGE;
            const double upRightBegin = CARDINAL_HALF_RANGE;
            const double rightBegin = upRightBegin + DIAGONAL_RANGE;
            const double downRightBegin = rightBegin + CARDINAL_RANGE;
            const double downBegin = downRightBegin + DIAGONAL_RANGE;
            const double downLeftBegin = downBegin + CARDINAL_RANGE;
            const double leftBegin = downLeftBegin + DIAGONAL_RANGE;
            const double upLeftBegin = leftBegin + CARDINAL_RANGE;

            const int deadzoneSquared = 8000 * 8000;

            unchecked
            {
                if (current.LeftPad.Touch)
                {
                    int dist = (current.LeftPad.X * current.LeftPad.X) + (current.LeftPad.Y * current.LeftPad.Y);
                    if (dist > deadzoneSquared)
                    {
                        double angleRad = Math.Atan2(current.LeftPad.X, current.LeftPad.Y);
                        double angle = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;
                        //Console.WriteLine(angle);
                        /*double normX = Math.Abs(Math.Cos(tempAngle));
                        double normY = Math.Abs(Math.Sin(tempAngle));
                        int signX = Math.Sign(current.LeftPad.X);
                        int signY = Math.Sign(current.LeftPad.Y);
                        */
                        if (angle == 0.0)
                        {
                        }
                        else if (angle > upLeftEnd || angle < upRightBegin)
                        {
                            tempButtons |= Xbox360Button.Up.Value;
                            //currentDir = DpadDirections.Up;
                        }
                        else if (angle >= upRightBegin && angle < rightBegin)
                        {
                            tempButtons |= Xbox360Button.Up.Value;
                            tempButtons |= Xbox360Button.Right.Value;
                            //currentDir = DpadDirections.UpRight;
                        }
                        else if (angle >= rightBegin && angle < downRightBegin)
                        {
                            tempButtons |= Xbox360Button.Right.Value;
                            //currentDir = DpadDirections.Right;
                        }
                        else if (angle >= downRightBegin && angle < downBegin)
                        {
                            tempButtons |= Xbox360Button.Down.Value;
                            tempButtons |= Xbox360Button.Right.Value;
                            //currentDir = DpadDirections.DownRight;
                        }
                        else if (angle >= downBegin && angle < downLeftBegin)
                        {
                            tempButtons |= Xbox360Button.Down.Value;
                            //currentDir = DpadDirections.Down;
                        }
                        else if (angle >= downLeftBegin && angle < leftBegin)
                        {
                            tempButtons |= Xbox360Button.Down.Value;
                            tempButtons |= Xbox360Button.Left.Value;
                            //currentDir = DpadDirections.DownLeft;
                        }
                        else if (angle >= leftBegin && angle < upLeftBegin)
                        {
                            tempButtons |= Xbox360Button.Left.Value;
                            //currentDir = DpadDirections.Left;
                        }
                        else if (angle >= upLeftBegin && angle <= upLeftEnd)
                        {
                            tempButtons |= Xbox360Button.Up.Value;
                            tempButtons |= Xbox360Button.Left.Value;
                            //currentDir = DpadDirections.UpLeft;
                        }
                    }
                }
            }
        }

        private void TrackballMouseProcess(ref SteamControllerState current,
            ref SteamControllerState previous)
        {
            if (current.RightPad.Touch && !previous.RightPad.Touch)
            {
                if (trackballActive)
                {
                    //Console.WriteLine("CHECKING HERE");
                }

                // Initial touch
                Array.Clear(trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
                Array.Clear(trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
                trackballXVel = 0.0;
                trackballYVel = 0.0;
                trackballActive = false;
                trackballBufferTail = 0;
                trackballBufferHead = 0;
                trackballDXRemain = 0.0;
                trackballDYRemain = 0.0;

                //Console.WriteLine("INITIAL");
            }
            else if (current.RightPad.Touch && previous.RightPad.Touch)
            {
                // Process normal mouse
                RightTouchMouse(ref current, ref previous);
                //Console.WriteLine("NORMAL");
            }
            else if (!current.RightPad.Touch && previous.RightPad.Touch)
            {
                // Initially released. Calculate velocity and start Trackball
                double currentWeight = 1.0;
                double finalWeight = 0.0;
                double x_out = 0.0, y_out = 0.0;
                int idx = -1;
                for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != trackballBufferHead; i++)
                {
                    idx = (trackballBufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
                    x_out += trackballXBuffer[idx] * currentWeight;
                    y_out += trackballYBuffer[idx] * currentWeight;
                    finalWeight += currentWeight;
                    currentWeight *= 1.0;
                }

                x_out /= finalWeight;
                trackballXVel = x_out;
                y_out /= finalWeight;
                trackballYVel = y_out;

                double dist = Math.Sqrt(trackballXVel * trackballXVel + trackballYVel * trackballYVel);
                if (dist >= 1.0)
                {
                    trackballActive = true;

                    //Debug.WriteLine("START TRACK {0}", dist);
                    ProcessTrackballFrame(ref current, ref previous);
                }
            }
            else if (!current.RightPad.Touch && trackballActive)
            {
                //Console.WriteLine("CONTINUE TRACK");
                // Trackball Running
                ProcessTrackballFrame(ref current, ref previous);
            }
        }

        private void TrackballMouseJoystickProcess(ref SteamControllerState current,
            ref SteamControllerState previous, ref IXbox360Controller xbox)
        {
            if (current.RightPad.Touch && !previous.RightPad.Touch)
            {
                if (trackballActive)
                {
                    //Console.WriteLine("CHECKING HERE");
                }

                // Initial touch
                Array.Clear(trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
                Array.Clear(trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
                trackballXVel = 0.0;
                trackballYVel = 0.0;
                trackballActive = false;
                trackballBufferTail = 0;
                trackballBufferHead = 0;
                trackballDXRemain = 0.0;
                trackballDYRemain = 0.0;

                //Console.WriteLine("INITIAL");
            }
            else if (current.RightPad.Touch && previous.RightPad.Touch)
            {
                // Process normal mouse
                //RightTouchMouse(ref current, ref previous);
                //Console.WriteLine("NORMAL");
                RightTouchMouseJoystick(ref current, ref previous, ref xbox);

            }
            else if (!current.RightPad.Touch && previous.RightPad.Touch)
            {
                // Initially released. Calculate velocity and start Trackball
                double currentWeight = 1.0;
                double finalWeight = 0.0;
                double x_out = 0.0, y_out = 0.0;
                int idx = -1;
                for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != trackballBufferHead; i++)
                {
                    idx = (trackballBufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
                    x_out += trackballXBuffer[idx] * currentWeight;
                    y_out += trackballYBuffer[idx] * currentWeight;
                    finalWeight += currentWeight;
                    currentWeight *= 1.0;
                }

                x_out /= finalWeight;
                trackballXVel = x_out;
                y_out /= finalWeight;
                trackballYVel = y_out;

                double dist = Math.Sqrt(trackballXVel * trackballXVel + trackballYVel * trackballYVel);
                if (dist >= 1.0)
                {
                    trackballActive = true;

                    //Debug.WriteLine("START TRACK {0}", dist);
                    ProcessTrackballJoystickFrame(ref current, ref previous, ref xbox);
                }
            }
            else if (!current.RightPad.Touch && trackballActive)
            {
                //Console.WriteLine("CONTINUE TRACK");
                // Trackball Running
                ProcessTrackballJoystickFrame(ref current, ref previous, ref xbox);
            }
        }

        private void RightTouchMouseJoystick(ref SteamControllerState current,
            ref SteamControllerState previous, ref IXbox360Controller xbox)
        {
            int dx = current.RightPad.X - previous.RightPad.X;
            int dy = -(current.RightPad.Y - previous.RightPad.Y);
            //int rawDeltaX = dx, rawDeltaY = dy;

            //Console.WriteLine("DELTA X: {0} Y: {1}", dx, dy);

            // Fill trackball entry
            int iIndex = trackballBufferTail;
            trackballXBuffer[iIndex] = (dx * TRACKBALL_SCALE) / current.timeElapsed;
            trackballYBuffer[iIndex] = (dy * TRACKBALL_SCALE) / current.timeElapsed;
            trackballBufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
            if (trackballBufferHead == trackballBufferTail)
                trackballBufferHead = (trackballBufferHead + 1) % TRACKBALL_BUFFER_LEN;

            TouchMouseJoystickPad(dx, -dy, ref current, ref previous, ref xbox);
        }

        private void ProcessTrackballFrame(ref SteamControllerState current,
            ref SteamControllerState _)
        {
            double tempAngle = Math.Atan2(-trackballYVel, trackballXVel);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(trackballXVel);
            int signY = Math.Sign(trackballYVel);

            double trackXvDecay = Math.Min(Math.Abs(trackballXVel), trackballAccel * current.timeElapsed * normX);
            double trackYvDecay = Math.Min(Math.Abs(trackballYVel), trackballAccel * current.timeElapsed * normY);
            double xVNew = trackballXVel - (trackXvDecay * signX);
            double yVNew = trackballYVel - (trackYvDecay * signY);
            double xMotion = (xVNew * current.timeElapsed) / TRACKBALL_SCALE;
            double yMotion = (yVNew * current.timeElapsed) / TRACKBALL_SCALE;
            if (xMotion != 0.0)
            {
                xMotion += trackballDXRemain;
            }
            else
            {
                trackballDXRemain = 0.0;
            }

            int dx = (int)xMotion;
            trackballDXRemain = xMotion - dx;

            if (yMotion != 0.0)
            {
                yMotion += trackballDYRemain;
            }
            else
            {
                trackballDYRemain = 0.0;
            }

            int dy = (int)yMotion;
            trackballDYRemain = yMotion - dy;

            trackballXVel = xVNew;
            trackballYVel = yVNew;

            //Console.WriteLine("DX: {0} DY: {1}", dx, dy);

            if (dx == 0 && dy == 0)
            {
                trackballActive = false;
                //Console.WriteLine("ENDING TRACK");
            }
            else
            {
                TouchMoveMouse(dx, dy, ref current);
            }
        }

        private void ProcessTrackballJoystickFrame(ref SteamControllerState current,
            ref SteamControllerState previous, ref IXbox360Controller xbox)
        {
            double tempAngle = Math.Atan2(-trackballYVel, trackballXVel);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(trackballXVel);
            int signY = Math.Sign(trackballYVel);

            double trackXvDecay = Math.Min(Math.Abs(trackballXVel), trackballAccel * current.timeElapsed * normX);
            double trackYvDecay = Math.Min(Math.Abs(trackballYVel), trackballAccel * current.timeElapsed * normY);
            double xVNew = trackballXVel - (trackXvDecay * signX);
            double yVNew = trackballYVel - (trackYvDecay * signY);
            double xMotion = (xVNew * current.timeElapsed) / TRACKBALL_SCALE;
            double yMotion = (yVNew * current.timeElapsed) / TRACKBALL_SCALE;
            if (xMotion != 0.0)
            {
                xMotion += trackballDXRemain;
            }
            else
            {
                trackballDXRemain = 0.0;
            }

            int dx = (int)xMotion;
            trackballDXRemain = xMotion - dx;

            if (yMotion != 0.0)
            {
                yMotion += trackballDYRemain;
            }
            else
            {
                trackballDYRemain = 0.0;
            }

            int dy = (int)yMotion;
            trackballDYRemain = yMotion - dy;

            trackballXVel = xVNew;
            trackballYVel = yVNew;

            //Console.WriteLine("DX: {0} DY: {1}", dx, dy);

            if (dx == 0 && dy == 0)
            {
                trackballActive = false;
                //Console.WriteLine("ENDING TRACK");
            }
            else
            {
                TouchMouseJoystickPad(dx, -dy, ref current, ref previous, ref xbox);
            }
        }

        private void TouchMoveMouse(int dx, int dy, ref SteamControllerState current)
        {
            //const int deadZone = 18;
            //const int deadZone = 12;
			const int deadZone = 8;

            double tempAngle = Math.Atan2(-dy, dx);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(dx);
            int signY = Math.Sign(dy);

            double timeElapsed = current.timeElapsed;
            double coefficient = TOUCHPAD_COEFFICIENT;
            double offset = TOUCHPAD_MOUSE_OFFSET;
            // Base speed 8 ms
            double tempDouble = timeElapsed * 125.0;

            int deadzoneX = (int)Math.Abs(normX * deadZone);
            int deadzoneY = (int)Math.Abs(normY * deadZone);

            if (Math.Abs(dx) > deadzoneX)
            {
                dx -= signX * deadzoneX;
            }
            else
            {
                dx = 0;
            }

            if (Math.Abs(dy) > deadzoneY)
            {
                dy -= signY * deadzoneY;
            }
            else
            {
                dy = 0;
            }

            double xMotion = dx != 0 ? coefficient * (dx * tempDouble)
                + (normX * (offset * signX)) : 0;

            double yMotion = dy != 0 ? coefficient * (dy * tempDouble)
                + (normY * (offset * signY)) : 0;

            double throttla = 1.428;
            //double offman = 10;
            //double throttla = 1.4;
            double offman = 12;

            double absX = Math.Abs(xMotion);
            if (absX <= normX * offman)
            {
                //double before = xMotion;
                //double adjOffman = normX != 0.0 ? normX * offman : offman;
                xMotion = signX * Math.Pow(absX / offman, throttla) * offman;
                //Console.WriteLine("Before: {0} After {1}", before, xMotion);
                //Console.WriteLine(absX / adjOffman);
            }

            double absY = Math.Abs(yMotion);
            if (absY <= normY * offman)
            {
                //double adjOffman = normY != 0.0 ? normY * offman : offman;
                yMotion = signY * Math.Pow(absY / offman, throttla) * offman;
                //Console.WriteLine(absY / adjOffman);
            }

            mouseX = xMotion; mouseY = yMotion;
        }

        private const double TOUCHPAD_MOUSE_OFFSET = 0.375;
        //private const double TOUCHPAD_COEFFICIENT = 0.012;
        private const double TOUCHPAD_COEFFICIENT = 0.012 * 1.1;
        private void RightTouchMouse(ref SteamControllerState current,
            ref SteamControllerState previous)
        {
            int dx = current.RightPad.X - previous.RightPad.X;
            int dy = -(current.RightPad.Y - previous.RightPad.Y);
            //int rawDeltaX = dx, rawDeltaY = dy;

            //Console.WriteLine("DELTA X: {0} Y: {1}", dx, dy);

            // Fill trackball entry
            int iIndex = trackballBufferTail;
            trackballXBuffer[iIndex] = (dx * TRACKBALL_SCALE) / current.timeElapsed;
            trackballYBuffer[iIndex] = (dy * TRACKBALL_SCALE) / current.timeElapsed;
            trackballBufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
            if (trackballBufferHead == trackballBufferTail)
                trackballBufferHead = (trackballBufferHead + 1) % TRACKBALL_BUFFER_LEN;

            TouchMoveMouse(dx, dy, ref current);
        }

        private void GenerateMouseMoveEvent()
        {
            if (mouseX != 0.0 || mouseY != 0.0)
            {
                if ((mouseX > 0.0 && mouseXRemainder > 0.0) || (mouseX < 0.0 && mouseXRemainder < 0.0))
                {
                    mouseX += mouseXRemainder;
                }
                else
                {
                    mouseXRemainder = 0.0;
                }

                if ((mouseY > 0.0 && mouseYRemainder > 0.0) || (mouseY < 0.0 && mouseYRemainder < 0.0))
                {
                    mouseY += mouseYRemainder;
                }
                else
                {
                    mouseYRemainder = 0.0;
                }

                //mouseX = filterX.Filter(mouseX, 1.0 / 0.016);
                //mouseY = filterY.Filter(mouseY, 1.0 / 0.016);
                mouseX = filterX.Filter(mouseX, 1.0 / currentRate);
                mouseY = filterY.Filter(mouseY, 1.0 / currentRate);

                double mouseXTemp = mouseX - (remainderCutoff(mouseX * 100.0, 1.0) / 100.0);
                int mouseXInt = (int)(mouseXTemp);
                mouseXRemainder = mouseXTemp - mouseXInt;

                double mouseYTemp = mouseY - (remainderCutoff(mouseY * 100.0, 1.0) / 100.0);
                int mouseYInt = (int)(mouseYTemp);
                mouseYRemainder = mouseYTemp - mouseYInt;
                mouseReport.MouseX = (short)mouseXInt;
                mouseReport.MouseY = (short)mouseYInt;
                mouseSync = true;
                //fakerInput.UpdateRelativeMouse(mouseReport);
                //InputMethods.MoveCursorBy(mouseXInt, mouseYInt);
            }
            else
            {
                mouseXRemainder = mouseYRemainder = 0.0;
                //mouseX = filterX.Filter(0.0, 1.0 / 0.016);
                //mouseY = filterY.Filter(0.0, 1.0 / 0.016);
                filterX.Filter(mouseX, 1.0 / currentRate);
                filterY.Filter(mouseY, 1.0 / currentRate);
            }

            mouseX = mouseY = 0.0;
        }

        private double remainderCutoff(double dividend, double divisor)
        {
            return dividend - (divisor * (int)(dividend / divisor));
        }

        private short AxisScale(int value, bool flip)
        {
            unchecked
            {
                float temp = (value - STICK_MIN) * reciprocalInputResolution;
                if (flip) temp = (temp - 0.5f) * -1.0f + 0.5f;
                return (short)(temp * OUTPUT_X360_RESOLUTION + X360_STICK_MIN);
            }
        }

        public void Stop()
        {
            reader.StopUpdate();
            fakerInput.UpdateKeyboard(new KeyboardReport());
            fakerInput.Disconnect();
            fakerInput.Free();

            quit = true;

            outputX360?.Disconnect();
            outputX360 = null;
        }
    }
}
