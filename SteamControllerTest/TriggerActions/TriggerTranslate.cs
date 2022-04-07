using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.AxisModifiers;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.TriggerActions
{
    public class TriggerTranslate : TriggerMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            public const string DEAD_ZONE = "DeadZone";
            public const string MAX_ZONE = "MaxZone";
            public const string ANTIDEAD_ZONE = "AntiDeadZone";
            public const string OUTPUT_TRIGGER = "OutputTrigger";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            PropertyKeyStrings.DEAD_ZONE,
            PropertyKeyStrings.MAX_ZONE,
            PropertyKeyStrings.ANTIDEAD_ZONE,
            PropertyKeyStrings.OUTPUT_TRIGGER,
        };

        private double axisNorm;
        private OutputActionData outputData;
        private AxisDeadZone deadMod;

        public OutputActionData OutputData
        {
            get => outputData;
        }

        public AxisDeadZone DeadMod
        {
            get => deadMod;
        }

        public TriggerTranslate()
        {
            outputData = new OutputActionData(OutputActionData.ActionType.GamepadControl,
                JoypadActionCodes.AxisLTrigger);

            //deadMod = new AxisDeadZone(30 / 255.0, 1.0, 0.0);
            deadMod = new AxisDeadZone(0.0, 1.0, 0.0);
        }

        public override void Prepare(Mapper mapper, double axisValue, bool alterState = true)
        {
            //axisNorm = axisValue / 255.0;
            deadMod.CalcOutValues((int)axisValue, 255, out axisNorm);
            stateData.state = axisNorm != 0.0;
            stateData.axisNormValue = axisNorm;

            active = true;
            activeEvent = true;
        }

        public override void Event(Mapper mapper)
        {
            mapper.GamepadFromAxisInput(outputData, axisNorm);

            active = false;
            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
            axisNorm = 0.0;
            mapper.GamepadFromAxisInput(outputData, axisNorm);

            active = false;
            activeEvent = false;

            if (resetState)
            {
                stateData.Reset();
            }
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            axisNorm = 0.0;
            mapper.GamepadFromAxisInput(outputData, axisNorm);

            active = false;
            activeEvent = false;

            if (resetState)
            {
                stateData.Reset();
            }
        }

        public override void SoftCopyFromParent(TriggerMapAction parentAction)
        {
            if (parentAction is TriggerTranslate tempTrigTranslateAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                mappingId = tempTrigTranslateAction.mappingId;

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                foreach (string parentPropType in useParentProList)
                {
                    switch(parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempTrigTranslateAction.name;
                            break;
                        case PropertyKeyStrings.DEAD_ZONE:
                            deadMod.DeadZone = tempTrigTranslateAction.deadMod.DeadZone;
                            break;
                        case PropertyKeyStrings.MAX_ZONE:
                            deadMod.MaxZone = tempTrigTranslateAction.deadMod.MaxZone;
                            break;
                        case PropertyKeyStrings.ANTIDEAD_ZONE:
                            deadMod.AntiDeadZone = tempTrigTranslateAction.deadMod.AntiDeadZone;
                            break;
                        case PropertyKeyStrings.OUTPUT_TRIGGER:
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
