using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ActionUtil
{
    public class HoldPressFunc : ActionFunc
    {
        private bool status;

        private int durationMs;
        public int DurationMs { get => durationMs; set => durationMs = value; }

        private bool waited;

        public HoldPressFunc()
        {
            canPressInterrupt = true;
        }

        public HoldPressFunc(HoldPressFunc srcFunc)
        {
            srcFunc.CopyTo(this);

            canPressInterrupt = srcFunc.canPressInterrupt;
            durationMs = srcFunc.durationMs;
        }

        public override void Prepare(Mapper mapper, bool state, ActionFuncStateData stateData)
        {
            if (this.status != state)
            {
                this.status = state;
                waited = false;
                activeEvent = true;

                if (status)
                {
                    if (!toggleEnabled)
                    {
                        finished = false;
                    }
                    else
                    {
                        active = !active;
                        finished = !active;
                    }
                }
                else
                {
                    if (!toggleEnabled)
                    {
                        active = false;
                        finished = true;
                    }
                    else if (!active)
                    {
                        finished = true;
                    }
                }
            }

            if (status && !finished && !waited)
            {
                if (stateData.elapsed.ElapsedMilliseconds >= durationMs)
                {
                    waited = true;
                    active = true;
                    activeEvent = true;
                    // Execute system event
                    //SendOutputEvent(mapper);
                }
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
        }

        public override void Release(Mapper mapper)
        {
            status = false;
            active = false;
            activeEvent = false;
            waited = false;
            finished = false;
        }
    }
}
