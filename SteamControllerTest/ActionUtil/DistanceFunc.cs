using System;
using System.Collections.Generic;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ActionUtil
{
    public class DistanceFunc : ActionFunc
    {
        private bool inputStatus;
        private bool outputActive;

        public DistanceFunc()
        {
        }

        public DistanceFunc(OutputActionData outputAction,
            double distance=0.0)
        {
            outputActions.Add(outputAction);
            outputActionEnumerator = new OutputActionDataEnumerator(outputActions);

            this.distance = distance;
        }

        public DistanceFunc(IEnumerable<OutputActionData> outputActions,
            double distance=0.0)
        {
            this.outputActions.AddRange(outputActions);
            outputActionEnumerator =
                new OutputActionDataEnumerator(this.outputActions);

            this.distance = distance;
        }

        public DistanceFunc(DistanceFunc secondFunc)
        {
            secondFunc.CopyTo(this);
            distance = secondFunc.distance;
        }

        public override void Prepare(Mapper mapper, bool state,
            ActionFuncStateData stateData)
        {
            if (inputStatus != state)
            {
                outputActive = stateData.axisNormValue >= distance;
                activeEvent = true;

                if (outputActive)
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
                outputActive = stateData.axisNormValue >= distance;
                if (outputActive)
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

            activeEvent = false;
        }

        public override void Release(Mapper mapper)
        {
            inputStatus = false;
            active = false;
            activeEvent = false;
            outputActive = false;
            finished = true;
        }
    }
}
