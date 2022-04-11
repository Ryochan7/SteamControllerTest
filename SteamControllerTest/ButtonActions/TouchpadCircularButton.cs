using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ButtonActions
{
    public class TouchpadCircularButton : ButtonAction
    {
        protected double notches;

        public void PrepareCircular(Mapper mapper, double notches, bool alterState = true)
        {
            this.notches = notches;
            bool tempStatus = notches != 0.0;
            //if (this.status != tempStatus)
            {
                status = tempStatus;

                if (useParentActions)
                {
                    actionFuncCandidates.AddRange(parentButtonAct.ActionFuncs);
                }
                else
                {
                    actionFuncCandidates.AddRange(actionFuncs);
                }

                if (alterState)
                {
                    stateData.wasActive = stateData.state;
                    stateData.state = status;
                }
            }

            active = true;
            activeEvent = true;
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
            notches = 0.0;

            base.Release(mapper, resetState);
        }

        public override void WrapNotchesProcess(OutputActionData action)
        {
            //base.WrapTickProcess(action);

            switch (action.OutputType)
            {
                case OutputActionData.ActionType.Keyboard:
                case OutputActionData.ActionType.MouseButton:
                case OutputActionData.ActionType.MouseWheel:
                    {
                        action.currentNotches += this.notches;
                    }

                    break;
                default:
                    break;
            }
        }

        public void PrepareActions()
        {
            foreach(ActionFunc func in actionFuncs)
            {
                foreach(OutputActionData actionData in func.OutputActions)
                {
                    actionData.useNotches = true;
                    actionData.currentNotches = 0.0;
                }
            }
        }
    }
}
