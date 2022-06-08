using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sensorit.Base;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.GyroActions
{
    public enum GyroMouseJoystickOuputAxes
    {
        All,
        XAxis,
        YAxis,
    }

    public struct GyroMouseJoystickParams
    {
        public int deadZone;
        public int maxZone;
        public double antiDeadzoneX;
        public double antiDeadzoneY;
        public JoypadActionCodes[] gyroTriggerButtons;
        public bool andCond;
        public bool triggerActivates;
        public double verticalScale;
        public GyroMouseXAxisChoice useForXAxis;
        public bool invertX;
        public bool invertY;
        public GyroMouseJoystickOuputAxes outputAxes;
        public StickActionCodes outputStick;
        public StickActionCodes OutputStick
        {
            get => outputStick;
            set
            {
                outputStick = value;
                OutputStickChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public bool maxOutputEnabled;
        public double maxOutput;
        public bool toggleAction;
        public bool smoothing;
        public double oneEuroMinCutoff;
        public double oneEuroMinBeta;

        public event EventHandler OutputStickChanged;
    }

    public class GyroMouseJoystick : GyroMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            public const string DEAD_ZONE = "DeadZone";
            public const string MAX_ZONE = "MaxZone";
            public const string ANTIDEAD_ZONE_X = "AntiDeadZoneX";
            public const string ANTIDEAD_ZONE_Y = "AntiDeadZoneY";
            public const string X_AXIS = "XAxis";
            public const string INVERT_X = "InvertX";
            public const string INVERT_Y = "InvertY";
            public const string VERTICAL_SCALE = "VerticalScale";
            public const string OUTPUT_AXES = "OutputAxes";
            public const string OUTPUT_STICK = "OuputStick";
            public const string MAX_OUTPUT = "MaxOutput";
            public const string MAX_OUTPUT_ENABLED = "MaxOutputEnabled";
            //public const string OUTPUT_CURVE = "OutputCurve";

            public const string TRIGGER_BUTTONS = "Triggers";
            public const string TRIGGER_ACTIVATE = "TriggersActivate";
            public const string TRIGGER_EVAL_COND = "TriggersEvalCond";
            public const string TOGGLE_ACTION = "ToggleAction";
            public const string SMOOTHING_ENABLED = "SmoothingEnabled";
            public const string SMOOTHING_MINCUTOFF = "SmoothingMinCutoff";
            public const string SMOOTHING_MINBETA = "SmoothingMinBeta";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            PropertyKeyStrings.DEAD_ZONE,
            PropertyKeyStrings.MAX_ZONE,
            PropertyKeyStrings.X_AXIS,
            PropertyKeyStrings.ANTIDEAD_ZONE_X,
            PropertyKeyStrings.ANTIDEAD_ZONE_Y,
            PropertyKeyStrings.INVERT_X,
            PropertyKeyStrings.INVERT_Y,
            PropertyKeyStrings.VERTICAL_SCALE,
            PropertyKeyStrings.OUTPUT_AXES,
            PropertyKeyStrings.OUTPUT_STICK,
            PropertyKeyStrings.MAX_OUTPUT,
            PropertyKeyStrings.MAX_OUTPUT_ENABLED,
            PropertyKeyStrings.TRIGGER_BUTTONS,
            PropertyKeyStrings.TRIGGER_ACTIVATE,
            PropertyKeyStrings.TRIGGER_EVAL_COND,
            PropertyKeyStrings.TOGGLE_ACTION,
            PropertyKeyStrings.SMOOTHING_ENABLED,
            PropertyKeyStrings.SMOOTHING_MINCUTOFF,
            PropertyKeyStrings.SMOOTHING_MINBETA,
        };

        public const string ACTION_TYPE_NAME = "GyroMouseJoystickAction";

        public GyroMouseJoystickParams mStickParms;
        private double xNorm, yNorm;
        private double prevXNorm, prevYNorm;
        private OutputActionData actionData = new OutputActionData(OutputActionData.ActionType.GamepadControl, StickActionCodes.RS);
        private bool previousTriggerActivated;
        private bool toggleActiveState;
        private OneEuroFilter smoothFilter = new OneEuroFilter(1.0, 1.0);

        public GyroMouseJoystick()
        {
            actionTypeName = ACTION_TYPE_NAME;
            mStickParms = new GyroMouseJoystickParams()
            {
                deadZone = 24,
                maxZone = 600,
                antiDeadzoneX = 0.45,
                antiDeadzoneY = 0.45,
                verticalScale = 1.0,
                outputAxes = GyroMouseJoystickOuputAxes.All,
                maxOutput = 1.0,
                andCond = true,
                gyroTriggerButtons = new JoypadActionCodes[0],
            };
            mStickParms.OutputStickChanged += MStickParms_OutputStickChanged;
        }

        private void MStickParms_OutputStickChanged(object sender, EventArgs e)
        {
            actionData.StickCode = mStickParms.outputStick;
        }

        public GyroMouseJoystick(GyroMouseJoystickParams mstickParams)
        {
            actionTypeName = ACTION_TYPE_NAME;
            this.mStickParms = mstickParams;
            mStickParms.OutputStickChanged += MStickParms_OutputStickChanged;
        }

        public GyroMouseJoystick(GyroMouseJoystick parentAction)
        {
            actionTypeName = ACTION_TYPE_NAME;
            this.parentAction = parentAction;
            this.mStickParms = parentAction.mStickParms;
            mStickParms.OutputStickChanged += MStickParms_OutputStickChanged;
        }

        public override void Prepare(Mapper mapper, ref GyroEventFrame joystickFrame, bool alterState = true)
        {
            //const int deadzone = 24;
            //const int deadzone = 24;
            //const int maxZone = 600;
            //const double antidead = 0.54;
            //const double antidead = 0.45;

            int deadzone = mStickParms.deadZone;
            int maxZone = mStickParms.maxZone;

            double timeElapsed = joystickFrame.timeElapsed;

            JoypadActionCodes[] tempTriggerButtons = mStickParms.gyroTriggerButtons;
            bool triggerButtonActive = mapper.IsButtonsActiveDraft(tempTriggerButtons, mStickParms.andCond);
            bool triggerActivated = true;
            //if (tempTriggerButton != JoypadActionCodes.Empty)
            {
                //bool triggerButtonActive = mapper.IsButtonActive(mStickParms.gyroTriggerButton);
                if (!mStickParms.triggerActivates && triggerButtonActive)
                {
                    triggerActivated = false;
                }
                else if (mStickParms.triggerActivates && !triggerButtonActive)
                {
                    triggerActivated = false;
                }
            }

            if (mStickParms.toggleAction)
            {
                if (triggerActivated && triggerActivated != previousTriggerActivated)
                {
                    toggleActiveState = !toggleActiveState;
                }

                previousTriggerActivated = triggerActivated;
                triggerActivated = toggleActiveState;
            }
            else
            {
                previousTriggerActivated = triggerActivated;
            }

            if (!triggerActivated)
            {
                prevXNorm = xNorm; prevYNorm = yNorm;
                xNorm = yNorm = 0.0;
                active = false;
                activeEvent = false;
                return;
            }

            // Base speed 15 ms
            //double tempDouble = timeElapsed * 66.67;
            double tempDouble = timeElapsed * joystickFrame.elapsedReference;
            //Console.WriteLine("Elasped: ({0}) DOUBLE {1}", current.timeElapsed, tempDouble);
            int deltaX = mStickParms.useForXAxis == GyroMouseXAxisChoice.Yaw ?
                joystickFrame.GyroYaw : joystickFrame.GyroRoll;
            int deltaY = -joystickFrame.GyroPitch;

            double tempAngle = Math.Atan2(-deltaY, deltaX);
            double normX = Math.Abs(Math.Cos(tempAngle));
            double normY = Math.Abs(Math.Sin(tempAngle));
            int signX = Math.Sign(deltaX);
            int signY = Math.Sign(deltaY);

            int deadzoneX = (int)Math.Abs(normX * deadzone);
            int deadzoneY = (int)Math.Abs(normY * deadzone);

            int maxValX = signX * maxZone;
            int maxValY = signY * maxZone;

            double xratio = 0.0, yratio = 0.0;
            double antiX = mStickParms.antiDeadzoneX * normX;
            double antiY = mStickParms.antiDeadzoneY * normY;

            if (Math.Abs(deltaX) > deadzoneX)
            {
                deltaX -= signX * deadzoneX;
                deltaX = (int)(deltaX * tempDouble);
                deltaX = (deltaX < 0 && deltaX < maxValX) ? maxValX :
                    (deltaX > 0 && deltaX > maxValX) ? maxValX : deltaX;
                //if (deltaX != maxValX) deltaX -= deltaX % (signX * GyroMouseFuzz);
            }
            else
            {
                deltaX = 0;
            }

            if (Math.Abs(deltaY) > deadzoneY)
            {
                deltaY -= signY * deadzoneY;
                deltaY = (int)(deltaY * tempDouble);
                deltaY = (deltaY < 0 && deltaY < maxValY) ? maxValY :
                    (deltaY > 0 && deltaY > maxValY) ? maxValY : deltaY;
                //if (deltaY != maxValY) deltaY -= deltaY % (signY * GyroMouseFuzz);
            }
            else
            {
                deltaY = 0;
            }

            deltaX = (int)mapper.MStickFilterX.Filter(deltaX, mapper.CurrentRate);
            deltaY = (int)mapper.MStickFilterY.Filter(deltaY, mapper.CurrentRate);

            if (deltaX != 0) xratio = deltaX / (double)maxValX;
            if (deltaY != 0) yratio = deltaY / (double)maxValY;

            if (mStickParms.verticalScale != 1.0)
            {
                yratio = Math.Clamp(yratio * mStickParms.verticalScale, 0.0, 1.0);
            }

            if (mStickParms.maxOutputEnabled)
            {
                double maxOutRatio = mStickParms.maxOutput;
                // Expand output a bit. Likely not going to get a straight line with Gyro
                double maxOutXRatio = Math.Min(normX / 0.98, 1.0) * maxOutRatio;
                double maxOutYRatio = Math.Min(normY / 0.98, 1.0) * maxOutRatio;

                xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
                yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);
            }

            xNorm = 0.0; yNorm = 0.0;
            if (xratio != 0.0)
            {
                xNorm = (1.0 - antiX) * xratio + antiX;
                xNorm = xNorm * signX; // Restore sign
            }

            if (yratio != 0.0)
            {
                yNorm = (1.0 - antiY) * yratio + antiY;
                yNorm = yNorm * signY; // Restore sign
            }

            if (xNorm != 0.0 || yNorm != 0.0)
            //if (xNorm != prevXNorm || yNorm != prevYNorm)
            {
                active = true;
                activeEvent = true;
            }
        }

        public override void Event(Mapper mapper)
        {
            /*double tempX = xMotion, tempY = yMotion;
            if (mouseParams.smoothing)
            {
                tempX = smoothFilter.Filter(xMotion, mapper.CurrentRate);
                tempY = smoothFilter.Filter(yMotion, mapper.CurrentRate);
            }
            */

            double outXNorm = xNorm;
            double outYNorm = yNorm;

            // Adjust sensitivity to work around rounding in filter method
            /*outXNorm *= 1.0005;
            outYNorm *= 1.0005;
            double tempXNorm = mapper.MStickFilterX.Filter(outXNorm, mapper.CurrentRate);
            double tempYNorm = mapper.MStickFilterY.Filter(outYNorm, mapper.CurrentRate);

            // Filter does not go back to absolute zero for reasons. Check
            // for low number and reset to zero
            if (Math.Abs(tempXNorm) < 0.0001) tempXNorm = 0.0;
            if (Math.Abs(tempYNorm) < 0.0001) tempYNorm = 0.0;

            // Need to check bounds again
            tempXNorm = Math.Clamp(tempXNorm, -1.0, 1.0);
            tempYNorm = Math.Clamp(tempYNorm, -1.0, 1.0);
            */
            double tempXNorm = outXNorm;
            double tempYNorm = outYNorm;

            if (mStickParms.invertX) tempXNorm = -1.0 * tempXNorm;
            if (mStickParms.invertY) tempYNorm = -1.0 * tempYNorm;

            if (mStickParms.outputAxes != GyroMouseJoystickOuputAxes.All)
            {
                if (mStickParms.outputAxes != GyroMouseJoystickOuputAxes.XAxis)
                {
                    tempXNorm = 0.0;
                }
                else if (mStickParms.outputAxes != GyroMouseJoystickOuputAxes.YAxis)
                {
                    tempYNorm = 0.0;
                }
            }

            //Trace.WriteLine($"HELP {tempXNorm} {tempYNorm}");

            mapper.GamepadFromStickInput(actionData, tempXNorm, tempYNorm);

            //if (xNorm != 0.0 || yNorm != 0.0)
            //{
            //    active = true;
            //}

            prevXNorm = xNorm;
            prevYNorm = yNorm;
            active = true;
            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true, bool ignoreReleaseActions = false)
        {
            //if (active)
            {
                mapper.GamepadFromStickInput(actionData, 0.0, 0.0);
            }

            xNorm = yNorm = 0.0;
            prevXNorm = prevYNorm = 0.0;
            active = false;
            activeEvent = false;
            ResetToggleActiveState();
            smoothFilter.Reset();
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            //if (active)
            {
                mapper.GamepadFromStickInput(actionData, 0.0, 0.0);
            }

            xNorm = yNorm = 0.0;
            prevXNorm = prevYNorm = 0.0;
            active = false;
            activeEvent = false;
            ResetToggleActiveState();
            smoothFilter.Reset();
        }

        public override void SoftCopyFromParent(GyroMapAction parentAction)
        {
            if (parentAction is GyroMouseJoystick tempGyroStickAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                tempGyroStickAction.hasLayeredAction = true;
                mappingId = tempGyroStickAction.mappingId;

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                bool updateSmoothing = false;
                foreach (string parentPropType in useParentProList)
                {
                    switch (parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempGyroStickAction.name;
                            break;
                        case PropertyKeyStrings.DEAD_ZONE:
                            mStickParms.deadZone = tempGyroStickAction.mStickParms.deadZone;
                            break;
                        case PropertyKeyStrings.MAX_ZONE:
                            mStickParms.maxZone = tempGyroStickAction.mStickParms.maxZone;
                            break;
                        case PropertyKeyStrings.ANTIDEAD_ZONE_X:
                            mStickParms.antiDeadzoneX = tempGyroStickAction.mStickParms.antiDeadzoneX;
                            break;
                        case PropertyKeyStrings.ANTIDEAD_ZONE_Y:
                            mStickParms.antiDeadzoneY = tempGyroStickAction.mStickParms.antiDeadzoneY;
                            break;
                        case PropertyKeyStrings.INVERT_X:
                            mStickParms.invertX = tempGyroStickAction.mStickParms.invertX;
                            break;
                        case PropertyKeyStrings.INVERT_Y:
                            mStickParms.invertY = tempGyroStickAction.mStickParms.invertY;
                            break;
                        case PropertyKeyStrings.VERTICAL_SCALE:
                            mStickParms.verticalScale = tempGyroStickAction.mStickParms.verticalScale;
                            break;
                        case PropertyKeyStrings.OUTPUT_AXES:
                            mStickParms.outputAxes = tempGyroStickAction.mStickParms.outputAxes;
                            break;
                        case PropertyKeyStrings.OUTPUT_STICK:
                            mStickParms.outputStick = tempGyroStickAction.mStickParms.outputStick;
                            actionData.StickCode = mStickParms.outputStick;
                            break;
                        case PropertyKeyStrings.MAX_OUTPUT_ENABLED:
                            mStickParms.maxOutputEnabled = tempGyroStickAction.mStickParms.maxOutputEnabled;
                            break;
                        case PropertyKeyStrings.MAX_OUTPUT:
                            mStickParms.maxOutput = tempGyroStickAction.mStickParms.maxOutput;
                            break;
                        case PropertyKeyStrings.TRIGGER_BUTTONS:
                            mStickParms.gyroTriggerButtons = tempGyroStickAction.mStickParms.gyroTriggerButtons;
                            break;
                        case PropertyKeyStrings.TRIGGER_ACTIVATE:
                            mStickParms.triggerActivates = tempGyroStickAction.mStickParms.triggerActivates;
                            break;
                        case PropertyKeyStrings.TRIGGER_EVAL_COND:
                            mStickParms.andCond = tempGyroStickAction.mStickParms.andCond;
                            break;
                        case PropertyKeyStrings.X_AXIS:
                            mStickParms.useForXAxis = tempGyroStickAction.mStickParms.useForXAxis;
                            break;
                        case PropertyKeyStrings.TOGGLE_ACTION:
                            mStickParms.toggleAction = tempGyroStickAction.mStickParms.toggleAction;
                            ResetToggleActiveState();
                            break;
                        case PropertyKeyStrings.SMOOTHING_ENABLED:
                            mStickParms.smoothing = tempGyroStickAction.mStickParms.smoothing;
                            updateSmoothing = true;
                            break;
                        case PropertyKeyStrings.SMOOTHING_MINCUTOFF:
                            mStickParms.oneEuroMinCutoff = tempGyroStickAction.mStickParms.oneEuroMinCutoff;
                            updateSmoothing = true;
                            break;
                        case PropertyKeyStrings.SMOOTHING_MINBETA:
                            mStickParms.oneEuroMinBeta = tempGyroStickAction.mStickParms.oneEuroMinBeta;
                            updateSmoothing = true;
                            break;
                        default:
                            break;
                    }
                }

                if (updateSmoothing)
                {
                    UpdateSmoothingFilter();
                }
            }
        }

        public override void BlankEvent(Mapper mapper)
        {
            mapper.GamepadFromStickInput(actionData, 0.0, 0.0);
            //mapper.ResetMouseJoystickData();
        }

        public override GyroMapAction DuplicateAction()
        {
            return new GyroMouseJoystick(this);
        }

        private void ResetToggleActiveState()
        {
            toggleActiveState = false;
            previousTriggerActivated = false;
        }

        public void UpdateSmoothingFilter()
        {
            smoothFilter = new OneEuroFilter(mStickParms.oneEuroMinCutoff,
                mStickParms.oneEuroMinBeta);
        }
    }
}
