using System;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ActionUtil
{
    public class AnalogFunc : ActionFunc
    {
        private const double DEFAULT_MIN_OUTPUT = 0.2;
        private const double DEFAULT_MAX_OUTPUT = 1.0;

        private bool inputStatus;
        //private JoypadActionCodes outputAxis;
        private double minOutput = DEFAULT_MIN_OUTPUT;
        private double maxOutput = DEFAULT_MAX_OUTPUT;
        private OutputActionData analogActionData =
            new OutputActionData(OutputActionData.ActionType.GamepadControl,
                JoypadActionCodes.Empty);

        public JoypadActionCodes OutputAxis
        {
            get => analogActionData.JoypadCode;
            set
            {
                if (value >= JoypadActionCodes.Axis1 &&
                    value <= JoypadActionCodes.AxisMax)
                {
                    analogActionData.JoypadCode = value;
                }
            }
        }

        public double MinOutput
        {
            get => minOutput;
            set
            {
                minOutput = Math.Clamp(value, 0.0, 1.0);
                CheckOutputBounds();
            }
        }

        public double MaxOutput
        {
            get => maxOutput;
            set
            {
                maxOutput = Math.Clamp(value, 0.0, 1.0);
                CheckOutputBounds();
            }
        }

        private void CheckOutputBounds()
        {
            if (minOutput > maxOutput)
            {
                minOutput = maxOutput;
            }
        }

        public override void Prepare(Mapper mapper, bool state,
            ActionFuncStateData stateData)
        {
            if (inputStatus != state)
            {
                inputStatus = state;
                activeEvent = true;

                if (inputStatus)
                {
                    active = true;
                    finished = false;
                }
                else
                {
                    active = false;
                    finished = true;
                }
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
            if (inputStatus)
            {
                double axisNorm = stateData.axisNormValue;
                double outputNorm = (maxOutput - minOutput) * axisNorm + minOutput;
                mapper.GamepadFromAxisInput(analogActionData, outputNorm);
            }

            activeEvent = false;
        }

        public override void Release(Mapper mapper)
        {
            if (active)
            {
                mapper.GamepadFromAxisInput(analogActionData, 0.0);
            }

            inputStatus = false;
            active = false;
            activeEvent = false;
            finished = false;
        }
    }
}
