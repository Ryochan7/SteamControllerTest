using SteamControllerTest.MapperUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ActionUtil
{
    public class NormalPressFunc : ActionFunc
    {
        private bool inputStatus;
        private bool inToggleState;

        public NormalPressFunc()
        {
        }

        public NormalPressFunc(OutputActionData outputAction)
        {
            outputActions.Add(outputAction);
            outputActionEnumerator = new OutputActionDataEnumerator(outputActions);
        }

        public NormalPressFunc(IEnumerable<OutputActionData> outputActions)
        {
            this.outputActions.AddRange(outputActions);
            outputActionEnumerator =
                new OutputActionDataEnumerator(this.outputActions);
        }

        public NormalPressFunc(NormalPressFunc secondFunc)
        {
            foreach(OutputActionData actionData in secondFunc.outputActions)
            {
                OutputActionData tempData = new OutputActionData(actionData);
                outputActions.Add(tempData);
            }

            outputActionEnumerator =
                new OutputActionDataEnumerator(this.outputActions);
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
                    if (!toggleEnabled)
                    {
                        active = true;
                        finished = false;
                    }
                    else
                    {
                        active = true;
                        finished = false;
                    }
                }
                else
                {
                    if (!toggleEnabled)
                    {
                        active = false;
                        finished = true;
                    }
                    else if (inToggleState)
                    {
                        active = false;
                        finished = true;
                        inToggleState = false;
                    }
                    else
                    {
                        active = true;
                        finished = false;
                        inToggleState = true;
                    }
                }
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
            //if (inputStatus)
            //{
            //    if (!toggleEnabled)
            //    {
            //        finished = false;
            //        active = true;
            //    }
            //    else if (!inToggleState)
            //    {
            //        active = true;
            //        finished = false;
            //    }
            //}
            //else
            //{
            //    if (!toggleEnabled)
            //    {
            //        active = false;
            //        finished = true;
            //    }
            //    else if (inToggleState)
            //    {
            //        active = false;
            //        finished = true;
            //        inToggleState = false;
            //    }
            //    else
            //    {
            //        inToggleState = true;
            //    }
            //}
        }

        public override void Release(Mapper mapper)
        {
            inputStatus = false;
            active = false;
            activeEvent = false;
            finished = true;
            inToggleState = false;
        }
    }
}
