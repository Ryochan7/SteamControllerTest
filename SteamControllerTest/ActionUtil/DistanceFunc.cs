using System;
using System.Collections.Generic;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ActionUtil
{
    public class DistanceFunc : ActionFunc
    {
        private bool inputStatus;
        private bool distanceOutputActive;

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
                distanceOutputActive = stateData.axisNormValue >= distance;
                activeEvent = true;

                if (distanceOutputActive)
                {
                    active = true;
                    distanceOutputActive = active;
                    finished = false;
                }
                else
                {
                    active = false;
                    distanceOutputActive = active;
                    finished = true;
                }

                outputActive = active;
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
            if (inputStatus)
            {
                distanceOutputActive = stateData.axisNormValue >= distance;
                if (distanceOutputActive)
                {
                    active = true;
                    distanceOutputActive = active;
                    finished = false;
                }
                else
                {
                    active = false;
                    distanceOutputActive = active;
                    finished = true;
                }

                outputActive = active;
            }
            else
            {
                outputActive = false;
            }

            activeEvent = false;
        }

        public override void Release(Mapper mapper)
        {
            inputStatus = false;
            active = false;
            outputActive = active;
            distanceOutputActive = active;
            activeEvent = false;
            finished = true;
        }
    }
}
