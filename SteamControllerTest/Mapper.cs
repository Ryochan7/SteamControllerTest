using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

using SteamControllerTest.SteamControllerLibrary;

namespace SteamControllerTest
{
    public class Mapper
    {
        public struct KeyAssociation
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

        private const short STICK_MAX = 30000;
        private const short STICK_MIN = -30000;

        private const int inputResolution = STICK_MAX - STICK_MIN;
        private const float reciprocalInputResolution = 1 / (float)inputResolution;

        private const ushort STICK_MIDRANGE = inputResolution / 2;
        private const ushort STICK_NEUTRAL = STICK_MAX - STICK_MIDRANGE;

        private const int X360_STICK_MAX = 32767;
        private const int X360_STICK_MIN = -32768;
        private const int OUTPUT_X360_RESOLUTION = X360_STICK_MAX - X360_STICK_MIN;

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
        private KeyAssociation buttonBindings;

        private const int TRACKBALL_INIT_FICTION = 10;
        private const int TRACKBALL_MASS = 45;
        private const double TRACKBALL_RADIUS = 0.0245;

        private double TRACKBALL_INERTIA = 2.0 * (TRACKBALL_MASS * TRACKBALL_RADIUS * TRACKBALL_RADIUS) / 5.0;
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

        public Mapper(SteamControllerDevice device)
        {
            this.device = device;

            trackballAccel = TRACKBALL_RADIUS * TRACKBALL_INIT_FICTION / TRACKBALL_INERTIA;
        }

        //public void Start(ViGEmClient vigemTestClient,
        //    SwitchProDevice proDevice, SwitchProReader proReader)

        public void Start(ViGEmClient vigemTestClient,
            SteamControllerDevice device, SteamControllerReader reader)
        {
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
            buttonBindings.A = (ushort)KeyInterop.VirtualKeyFromKey(Key.Space);
            buttonBindings.B = (ushort)KeyInterop.VirtualKeyFromKey(Key.C);
            buttonBindings.X = (ushort)KeyInterop.VirtualKeyFromKey(Key.R);
            buttonBindings.Y = (ushort)KeyInterop.VirtualKeyFromKey(Key.E);
            buttonBindings.LB = (ushort)KeyInterop.VirtualKeyFromKey(Key.Q);
            buttonBindings.RB = (ushort)KeyInterop.VirtualKeyFromKey(Key.Z);
            buttonBindings.Back = (ushort)KeyInterop.VirtualKeyFromKey(Key.Tab);
            buttonBindings.Start = (ushort)KeyInterop.VirtualKeyFromKey(Key.Escape);
            buttonBindings.Guide = (ushort)KeyInterop.VirtualKeyFromKey(Key.Tab);
            buttonBindings.LGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.X);
            buttonBindings.RGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.F);
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

            //outputX360.ResetReport();
            //unchecked
            //{
            //    ushort tempButtons = 0;
            //    if (current.A) tempButtons |= Xbox360Button.A.Value;
            //    if (current.B) tempButtons |= Xbox360Button.B.Value;
            //    if (current.X) tempButtons |= Xbox360Button.X.Value;
            //    if (current.Y) tempButtons |= Xbox360Button.Y.Value;
            //    if (current.Back) tempButtons |= Xbox360Button.Back.Value;
            //    if (current.Start) tempButtons |= Xbox360Button.Start.Value;
            //    if (current.Guide) tempButtons |= Xbox360Button.Guide.Value;
            //    if (current.LB) tempButtons |= Xbox360Button.LeftShoulder.Value;
            //    if (current.RB) tempButtons |= Xbox360Button.RightShoulder.Value;

            //    /*if (current.DPadUp) tempButtons |= Xbox360Button.Up.Value;
            //    if (current.DPadDown) tempButtons |= Xbox360Button.Down.Value;
            //    if (current.DPadLeft) tempButtons |= Xbox360Button.Left.Value;
            //    if (current.DPadRight) tempButtons |= Xbox360Button.Right.Value;
            //    */

            //    current.LeftPad.Rotate(-18.0 * Math.PI / 180.0);
            //    //current.RightPad.Rotate(18.0 * Math.PI / 180.0);
            //    TouchDPad(ref current, ref previous, ref tempButtons);

            //    outputX360.SetButtonsFull(tempButtons);
            //}

            short temp;
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

            /*
            temp = Math.Min(Math.Max(current.LX, STICK_MIN), STICK_MAX);
            temp = AxisScale(temp, false);
            outputX360.LeftThumbX = temp;

            temp = Math.Min(Math.Max(current.LY, STICK_MIN), STICK_MAX);
            temp = AxisScale(temp, false);
            outputX360.LeftThumbY = temp;
            //*/

            //outputX360.LeftTrigger = current.LT;
            //outputX360.RightTrigger = current.RT;

            if (current.A != previous.A)
            {
                ushort tempKey = buttonBindings.A;
                if (current.A)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.B != previous.B)
            {
                ushort tempKey = buttonBindings.B;
                if (current.B)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.X != previous.X)
            {
                ushort tempKey = buttonBindings.X;
                if (current.X)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.Y != previous.Y)
            {
                ushort tempKey = buttonBindings.Y;
                if (current.Y)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.LB != previous.LB)
            {
                ushort tempKey = buttonBindings.LB;
                if (current.LB)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.RB != previous.RB)
            {
                ushort tempKey = buttonBindings.RB;
                if (current.RB)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.LGrip != previous.LGrip)
            {
                ushort tempKey = buttonBindings.LGrip;
                if (current.LGrip)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.RGrip != previous.RGrip)
            {
                ushort tempKey = buttonBindings.RGrip;
                if (current.RGrip)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.Back != previous.Back)
            {
                ushort tempKey = buttonBindings.Back;
                if (current.Back)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.Start != previous.Start)
            {
                ushort tempKey = buttonBindings.Start;
                if (current.Start)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.Guide != previous.Guide)
            {
                ushort tempKey = buttonBindings.Guide;
                if (current.Guide)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            if (current.LSClick != previous.LSClick)
            {
                ushort tempKey = buttonBindings.LSClick;
                if (current.LSClick)
                {
                    InputMethods.performKeyPress(tempKey);
                }
                else
                {
                    InputMethods.performKeyRelease(tempKey);
                }
            }

            //if (current.RTClick != previous.RTClick)
            if ((current.RT > 50 && !mouseLBDown) || (current.RT <= 50 && mouseLBDown))
            {
                mouseLBDown = current.RT > 50;
                //Console.WriteLine("RT: {0} {1}", current.RT, mouseLBDown);
                InputMethods.MouseEvent(mouseLBDown ? InputMethods.MOUSEEVENTF_LEFTDOWN :
                    InputMethods.MOUSEEVENTF_LEFTUP);
            }

            //if (current.LTClick != previous.LTClick)
            //if ((current.LT > 50) != (previous.LT <= 50))
            if ((current.LT > 50 && !mouseRBDown) || (current.LT <= 50 && mouseRBDown))
            {
                mouseRBDown = current.LT > 50;
                InputMethods.MouseEvent(mouseRBDown ? InputMethods.MOUSEEVENTF_RIGHTDOWN :
                    InputMethods.MOUSEEVENTF_RIGHTUP);
            }

            /*if (current.RightPad.Touch && previous.RightPad.Touch)
            {
                // Process normal mouse
                RightTouchMouse(ref current, ref previous);
                Console.WriteLine("NORMAL");
            }
            */

            TrackballMouseProcess(ref current, ref previous);

            if (mouseX != 0.0 || mouseY != 0.0)
            {
                //Console.WriteLine("MOVE: {0}, {1}", (int)mouseX, (int)mouseY);
                GenerateMouseMoveEvent();
            }
            else
            {
                // Probably not needed here. Leave as a temporary precaution
                mouseXRemainder = mouseYRemainder = 0.0;

                //filterX.Filter(0.0, currentRate); // Smooth on output
                //filterY.Filter(0.0, currentRate); // Smooth on output
            }


            //outputX360.SubmitReport();
        }

        private void TouchDPad(ref SteamControllerState current,
            ref SteamControllerState previous, ref ushort tempButtons)
        {
            const double CARDINAL_RANGE = 45.0;
            const double DIAGONAL_RANGE = 45.0;
            //const double CARDINAL_HALF_RANGE = CARDINAL_RANGE / 2.0;
            const double CARDINAL_HALF_RANGE = 22.5;

            const double upLeftEnd = 360 - CARDINAL_HALF_RANGE;
            const double upRightBegin = CARDINAL_HALF_RANGE;
            const double rightBegin = upRightBegin + DIAGONAL_RANGE;
            const double downRightBegin = rightBegin + CARDINAL_RANGE;
            const double downBegin = downRightBegin + DIAGONAL_RANGE;
            const double downLeftBegin = downBegin + CARDINAL_RANGE;
            const double leftBegin = downLeftBegin + DIAGONAL_RANGE;
            const double upLeftBegin = leftBegin + CARDINAL_RANGE;

            unchecked
            {
                if (current.LeftPad.Touch)
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

                trackballActive = true;

                //Console.WriteLine("START TRACK {0}", trackballXVel);
                ProcessTrackballFrame(ref current, ref previous);
            }
            else if (!current.RightPad.Touch && trackballActive)
            {
                //Console.WriteLine("CONTINUE TRACK");
                // Trackball Running
                ProcessTrackballFrame(ref current, ref previous);
            }
        }

        private void ProcessTrackballFrame(ref SteamControllerState current,
            ref SteamControllerState previous)
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

        private void TouchMoveMouse(int dx, int dy, ref SteamControllerState current)
        {
            const int deadZone = 22;

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
            double offman = 20;

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

        private const double TOUCHPAD_MOUSE_OFFSET = 0.8;
        private const double TOUCHPAD_COEFFICIENT = 0.009;
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
                //mouseX = filterX.Filter(mouseX, currentRate);
                //mouseY = filterY.Filter(mouseY, currentRate);

                double mouseXTemp = mouseX - (remainderCutoff(mouseX * 1000.0, 1.0) / 1000.0);
                int mouseXInt = (int)(mouseXTemp);
                mouseXRemainder = mouseXTemp - mouseXInt;

                double mouseYTemp = mouseY - (remainderCutoff(mouseY * 1000.0, 1.0) / 1000.0);
                int mouseYInt = (int)(mouseYTemp);
                mouseYRemainder = mouseYTemp - mouseYInt;
                InputMethods.MoveCursorBy(mouseXInt, mouseYInt);
            }
            else
            {
                mouseXRemainder = mouseYRemainder = 0.0;
                //mouseX = filterX.Filter(0.0, 1.0 / 0.016);
                //mouseY = filterY.Filter(0.0, 1.0 / 0.016);
                //filterX.Filter(mouseX, currentRate);
                //filterY.Filter(mouseY, currentRate);
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

            quit = true;

            outputX360?.Disconnect();
            outputX360 = null;
        }
    }
}
