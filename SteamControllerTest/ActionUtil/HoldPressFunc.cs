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

        private bool turboEnabled;
        public bool TurboEnabled { get => turboEnabled; set => turboEnabled = value; }

        private int turboDurationMs;
        public int TurboDurationMs { get => turboDurationMs; set => turboDurationMs = value; }

        private Stopwatch turboStopwatch = new Stopwatch();

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
                        outputActive = active;
                    }
                }
                else
                {
                    if (!toggleEnabled)
                    {
                        active = false;
                        outputActive = active;
                        finished = true;
                    }
                    else if (!active)
                    {
                        finished = true;
                        outputActive = active;
                    }

                    if (!active && turboEnabled && turboStopwatch.IsRunning)
                    {
                        turboStopwatch.Reset();
                    }
                }
            }

            if (status && !finished && !waited)
            {
                if (stateData.elapsed.ElapsedMilliseconds >= durationMs)
                {
                    waited = true;
                    active = true;
                    outputActive = active;
                    activeEvent = true;
                    if (turboEnabled)
                    {
                        turboStopwatch.Restart();
                    }
                    // Execute system event
                    //SendOutputEvent(mapper);
                }
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
            if (!turboEnabled)
            {
                outputActive = active;
            }
            else
            {
                if (active)
                {
                    if (turboStopwatch.ElapsedMilliseconds >= turboDurationMs)
                    {
                        // Make state change occur
                        turboStopwatch.Restart();
                        outputActive = !outputActive;
                    }
                }
                else if (!active)
                {
                    turboStopwatch.Reset();
                    outputActive = false;
                }
            }

            activeEvent = false;
        }

        public override void Release(Mapper mapper)
        {
            status = false;
            active = false;
            outputActive = active;
            activeEvent = false;
            waited = false;
            finished = false;
            if (turboEnabled && turboStopwatch.IsRunning)
            {
                turboStopwatch.Reset();
            }
        }
    }
}
