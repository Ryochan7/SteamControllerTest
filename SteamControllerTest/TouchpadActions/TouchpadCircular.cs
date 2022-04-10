using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Diagnostics;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.StickModifiers;

namespace SteamControllerTest.TouchpadActions
{
    public class TouchpadCircular : TouchpadMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
        };

        private enum ClickDirection
        {
            Clockwise,
            CounterClockwise,
        }

        private const double CLICK_ANGLE_THRESHOLD = 12.0;
        private const double CLICK_RAD_THRESHOLD = CLICK_ANGLE_THRESHOLD * Math.PI / 180.0;
        private const double DEFAULT_DEADZONE = 0.25;

        private double startAngleRad;
        private double currentAngleRad;
        //private double remainderAngle;
        private double travelAngleChangeRad;
        private bool click;
        private ClickDirection currentClickDir;

        private OutputActionData clockwiseOutputAction;
        private OutputActionData counterClockwiseOutputAction;
        //public OutputActionData OutputAction
        //{
        //    get => outputAction;
        //}

        private StickDeadZone deadMod;

        public TouchpadCircular()
        {
            this.clockwiseOutputAction =
                new OutputActionData(OutputActionData.ActionType.MouseWheel, (int)MouseWheelCodes.WheelDown);

            this.counterClockwiseOutputAction =
                new OutputActionData(OutputActionData.ActionType.MouseWheel, (int)MouseWheelCodes.WheelUp);

            deadMod = new StickDeadZone(DEFAULT_DEADZONE, 1.0, 0.0);
        }

        public override void Prepare(Mapper mapper, ref TouchEventFrame touchFrame, bool alterState = true)
        {
            active = false;

            ref TouchEventFrame previousTouchFrame =
                    ref mapper.GetPreviousTouchEventFrame(touchpadDefinition.touchCode);

            int axisXMid = touchpadDefinition.xAxis.mid, axisYMid = touchpadDefinition.yAxis.mid;
            int axisXVal = touchFrame.X;
            int axisYVal = touchFrame.Y;
            int axisXDir = axisXVal - axisXMid, axisYDir = axisYVal - axisYMid;
            bool xNegative = axisXDir < 0;
            bool yNegative = axisYDir < 0;
            int maxDirX = (!xNegative ? touchpadDefinition.xAxis.max : touchpadDefinition.xAxis.min) - axisXMid;
            int maxDirY = (!yNegative ? touchpadDefinition.yAxis.max : touchpadDefinition.yAxis.min) - axisYMid;

            bool isActive = touchFrame.Touch;
            bool wasActive = previousTouchFrame.Touch;

            deadMod.CalcOutValues(axisXDir, axisYDir, maxDirX, maxDirY,
                out double xNorm, out double yNorm);

            bool inSafeZone = xNorm != 0.0 || yNorm != 0.0;

            if (!inSafeZone)
            {
                startAngleRad = 0;
                currentAngleRad = 0;
                travelAngleChangeRad = 0;
            }
            else if (isActive && !wasActive)
            {
                double angleRad = Math.Atan2(xNorm, yNorm);
                double angleDeg = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;

                startAngleRad = angleRad;
                currentAngleRad = angleRad;
                travelAngleChangeRad = 0;
            }
            else if (isActive)
            {
                double previousAngleRad = currentAngleRad;

                double angleRad = Math.Atan2(xNorm, yNorm);
                //double angleDeg = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;

                currentAngleRad = angleRad;

                double diffAngle = currentAngleRad - previousAngleRad;
                // Negate wrapping between PI to -PI on lower portion of Touchpad
                if (diffAngle > Math.PI)
                {
                    //Trace.WriteLine("POS FAR");
                    diffAngle -= 2 * Math.PI;
                }
                else if (diffAngle < -Math.PI)
                {
                    //Trace.WriteLine("NEG FAR");
                    diffAngle += 2 * Math.PI;
                }

                travelAngleChangeRad += diffAngle;
                //Trace.WriteLine($"DIFF {diffAngle} RAD: {angleRad} Previous: {previousAngleRad} CURRENT ANG: {currentAngleRad} TRAVEL ANG: {travelAngleChangeRad}");
                //if (Math.Abs(travelAngleChangeRad) > 5 * CLICK_RAD_THRESHOLD)
                //{
                //    Trace.WriteLine($"OUT OF WHACK {travelAngleChangeRad} {diffAngle}");
                //}

                if (Math.Abs(travelAngleChangeRad) > CLICK_RAD_THRESHOLD)
                {
                    //Trace.WriteLine("UP IN HERE");
                    active = true;
                    click = true;

                    currentClickDir = diffAngle > 0 ? ClickDirection.Clockwise : ClickDirection.CounterClockwise;
                    //travelAngleChangeRad = travelAngleChangeRad > 0 ?
                    //    travelAngleChangeRad - CLICK_RAD_THRESHOLD : travelAngleChangeRad + CLICK_RAD_THRESHOLD;
                }
            }
            else
            {
                startAngleRad = 0;
                currentAngleRad = 0;
                travelAngleChangeRad = 0;
            }

            activeEvent = true;
        }

        public override void Event(Mapper mapper)
        {
            if (click)
            {
                //Trace.WriteLine($"OUTPUT EVENT {DateTime.UtcNow.ToString("fff")}");

                OutputActionData tempAction =
                    currentClickDir == ClickDirection.Clockwise ? clockwiseOutputAction : counterClockwiseOutputAction;

                int speed = (int)(Math.Abs(travelAngleChangeRad) / CLICK_RAD_THRESHOLD);

                mapper.RunEventFromAnalog(tempAction, true, speed);
                tempAction.activatedEvent = false;

                travelAngleChangeRad = travelAngleChangeRad > 0 ?
                    travelAngleChangeRad - (speed * CLICK_RAD_THRESHOLD) : travelAngleChangeRad + (speed * CLICK_RAD_THRESHOLD);

                click = false;
            }

            active = false;
            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
            startAngleRad = 0;
            currentAngleRad = 0;
            travelAngleChangeRad = 0;
            currentClickDir = ClickDirection.Clockwise;
            active = activeEvent = false;
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            // Do nothing. Keep current state data
        }

        public override void SoftCopyFromParent(TouchpadMapAction parentAction)
        {
            if (parentAction is TouchpadCircular tempCirleAction)
            {
                base.SoftCopyFromParent(parentAction);

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                foreach (string parentPropType in useParentProList)
                {
                    switch(parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempCirleAction.name;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
