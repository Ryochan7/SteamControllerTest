using SteamControllerTest.MapperUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ActionUtil
{
    public abstract class ActionFunc
    {
        protected List<OutputActionData> outputActions =
            new List<OutputActionData>();
        protected OutputActionDataEnumerator outputActionEnumerator;

        public List<OutputActionData> OutputActions
        {
            get => outputActions; set => outputActions = value;
        }

        public bool activeEvent;
        public bool active;
        public bool analog;
        public bool finished;
        public bool toggleEnabled;
        public bool delayEnd;
        public bool interruptable;
        public bool canPressInterrupt;
        public double distance;

        // Special flags used to denote special function type
        public bool onRelease;
        public bool onDistance;
        public bool onChorded;

        public abstract void Prepare(Mapper mapper, bool state, ActionFuncStateData stateData);
        /*public virtual void PrepareAnalog(Mapper mapper, double state)
        {
            throw new NotImplementedException();
        }
        */

        public abstract void Event(Mapper mapper, ActionFuncStateData stateData);

        public virtual void PrepareState(Mapper mapper, ActionFunc secondFunc)
        {
        }

        public abstract void Release(Mapper mapper);
        protected virtual void ReleaseEvents(Mapper mapper)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ActionFunc secondFunc)
        {
            foreach (OutputActionData actionData in outputActions)
            {
                OutputActionData tempData = new OutputActionData(actionData);
                secondFunc.outputActions.Add(tempData);
            }

            secondFunc.outputActionEnumerator =
                new OutputActionDataEnumerator(secondFunc.outputActions);
        }
    }
}
