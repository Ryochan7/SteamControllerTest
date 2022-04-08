﻿using SteamControllerTest.StickModifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.TouchpadActions
{
    public class TouchpadAbsAction : TouchpadMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            public const string DEAD_ZONE = "DeadZone";

            //public const string OUTER_RING_BUTTON = "OuterRingButton";

            //public const string USE_OUTER_RING = "UseOuterRing";
            //public const string OUTER_RING_DEAD_ZONE = "OuterRingDeadZone";
            //public const string USE_AS_OUTER_RING = "UseAsOuterRing";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            PropertyKeyStrings.DEAD_ZONE,

            //PropertyKeyStrings.OUTER_RING_BUTTON,
            //PropertyKeyStrings.USE_OUTER_RING,
            //PropertyKeyStrings.OUTER_RING_DEAD_ZONE,
            //PropertyKeyStrings.USE_AS_OUTER_RING,
        };

        public struct AbsCoordRange
        {
            //public double top;
            //public double bottom;
            //public double left;
            //public double right;

            public double xcenter;
            public double ycenter;

            public double width;
            public double height;

            public void Init()
            {
                width = 1.0;
                height = 1.0;

                xcenter = 0.5;
                ycenter = 0.5;
            }
        }

        // Specify the input state of the button
        private bool inputStatus;

        private StickDeadZone deadMod;
        private AbsCoordRange absRange;

        private double xNorm = 0.0, yNorm = 0.0;
        private double xMotion;
        private double yMotion;
        private double fuzzXNorm;
        private double fuzzYNorm;

        public StickDeadZone DeadMod { get => deadMod; }
        public ref AbsCoordRange AbsMouseRange
        {
            get => ref absRange;
        }

        public TouchpadAbsAction()
        {
            deadMod = new StickDeadZone(0.0, 1.0, 0.0);
            absRange = new AbsCoordRange()
            {
                //top = 0.3, bottom = 0.7, left = 0.3, right = 0.7,
                width = 0.4,
                height = 0.4,
                xcenter = 0.5,
                ycenter = 0.5,
            };
        }

        public override void Prepare(Mapper mapper, ref TouchEventFrame touchFrame, bool alterState = true)
        {
            active = false;
            activeEvent = false;

            bool wasActive = xNorm != 0.0 || yNorm != 0.0;
            double prevXNorm = xNorm, prevYNorm = yNorm;

            xNorm = 0.0; yNorm = 0.0;
            xMotion = yMotion = 0.0;

            int axisXMid = touchpadDefinition.xAxis.mid, axisYMid = touchpadDefinition.yAxis.mid;
            int axisXVal = touchFrame.X; int axisYVal = touchFrame.Y;
            int axisXDir = axisXVal - axisXMid, axisYDir = axisYVal - axisYMid;
            bool xNegative = axisXDir < 0;
            bool yNegative = axisYDir < 0;
            int maxDirX = (!xNegative ? touchpadDefinition.xAxis.max : touchpadDefinition.xAxis.min) - axisXMid;
            int maxDirY = (!yNegative ? touchpadDefinition.yAxis.max : touchpadDefinition.yAxis.min) - axisYMid;
            deadMod.CalcOutValues(axisXDir, axisYDir, maxDirX,
                    maxDirY, out xNorm, out yNorm);

            bool isActive = xNorm != 0.0 || yNorm != 0.0;
            bool inSafeZone = isActive;
            if (inSafeZone || wasActive)
            {
                inputStatus = isActive;

                double usedXNorm = (!wasActive) ? xNorm : prevXNorm;
                double usedYNorm = (!wasActive) ? yNorm : prevYNorm;
                double xSign = usedXNorm >= 0.0 ? 1.0 : -1.0;
                double ySign = usedYNorm >= 0.0 ? 1.0 : -1.0;
                //double absXUnit = Math.Abs(usedXNorm);
                //double absYUnit = Math.Abs(usedYNorm);
                double angleRad = Math.Atan2(-axisYDir, axisXDir);
                double angCos = Math.Abs(Math.Cos(angleRad));
                double angSin = Math.Abs(Math.Sin(angleRad));

                // Implement fuzz logic to make output cursor less jittery when
                // trying to hold a position
                double fuzzSquared = 0.01 * 0.01;
                double dist = Math.Pow(fuzzXNorm - xNorm, 2) + Math.Pow(fuzzYNorm - yNorm, 2);
                if (dist <= fuzzSquared)
                {
                    active = true;
                    activeEvent = true;
                    return;
                }
                else
                {
                    fuzzXNorm = xNorm;
                    fuzzYNorm = yNorm;
                }

                double outXRatio = xNorm, outYRatio = yNorm;
                double antiDead = 0.2;

                // Find Release zone
                if (antiDead != 0.0)
                {
                    double antiDeadX = antiDead * angCos;
                    double antiDeadY = antiDead * angSin;
                    double absX = Math.Abs(outXRatio);
                    double absY = Math.Abs(outYRatio);
                    outXRatio = ((1.0 - antiDeadX) * absX + antiDeadX) * xSign;
                    outYRatio = ((1.0 - antiDeadY) * absY + antiDeadY) * ySign;
                    //Trace.WriteLine($"{antiDeadX} {outXRatio}");
                }

                //if (absRange.xcenter != 0.5 || absRange.ycenter != 0.5)
                {
                    //Trace.WriteLine($"{absRange.xcenter} {absRange.ycenter} {absRange.width / 2.0} {absRange.height / 2.0}");
                    outXRatio = (absRange.width / 2.0) * outXRatio + absRange.xcenter;
                    outYRatio = (absRange.height / 2.0) * (outYRatio * -1.0) + absRange.ycenter;

                    //Trace.WriteLine($"Test Abs Mouse: IN ({xNorm}, {yNorm}) OUT ({outXRatio}, {outYRatio})");
                }

                // Determine position relative to box size
                //if (absRange.width != 1.0 || absRange.height != 1.0)
                //{
                //    outXRatio = absRange.width * outXRatio;
                //    outYRatio = absRange.height * outYRatio;
                //}

                // Keep final coords within [-1.0, 1.0] range
                outXRatio = Math.Clamp(outXRatio, -1.0, 1.0);
                outYRatio = Math.Clamp(outYRatio, -1.0, 1.0);

                // [-1.0, 1.0] -> [0.0, 1.0]
                //xMotion = (1.0 - 0.5) * xNorm + 0.5;
                //yMotion = (1.0 - 0.5) * (yNorm * -1.0) + 0.5; // Invert Y
                //xMotion = (1.0 - 0.5) * outXRatio + 0.5;
                //yMotion = (1.0 - 0.5) * (outYRatio * -1.0) + 0.5; // Invert Y
                xMotion = outXRatio;
                yMotion = outYRatio;
                //xMotion = (absRange.right - 0.5) * xNorm + 0.5;
                //yMotion = (absRange.bottom - 0.5) * (yNorm * -1.0) + 0.5; // Invert Y

                //Trace.WriteLine($"Test Abs Mouse: IN ({xNorm}, {yNorm}) OUT ({xMotion}, {yMotion})");

                active = true;
                activeEvent = true;
            }
            else
            {
                inputStatus = false;
            }
        }

        public override void Event(Mapper mapper)
        {
            //if (xMotion != 0.0 || yMotion != 0.0)
            {
                mapper.AbsMouseX = xMotion; mapper.AbsMouseY = yMotion;
                mapper.AbsMouseSync = true;
            }

            active = xNorm != 0.0 || yNorm != 0.0;
            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
            xNorm = yNorm = 0.0;
            xMotion = yMotion = 0.0;
            fuzzXNorm = fuzzYNorm = 0.0;

            mapper.AbsMouseX = xMotion; mapper.AbsMouseY = yMotion;
            mapper.AbsMouseSync = true;

            active = false;
            activeEvent = false;
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            // Just call main Release method for now
            Release(mapper, resetState);
        }

        public override void SoftCopyFromParent(TouchpadMapAction parentAction)
        {
            if (parentAction is TouchpadAbsAction tempAbsAction)
            {
                base.SoftCopyFromParent(parentAction);

                //tempAbsAction.hasLayeredAction = true;
                mappingId = tempAbsAction.mappingId;

                this.touchpadDefinition = new TouchpadDefinition(tempAbsAction.touchpadDefinition);

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                foreach (string parentPropType in useParentProList)
                {
                    switch(parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempAbsAction.name;
                            break;
                        case PropertyKeyStrings.DEAD_ZONE:
                            deadMod.DeadZone = tempAbsAction.deadMod.DeadZone;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
