using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ActionUtil
{
    public class StartPressFunc : ActionFunc
    {
        private const int DEFAULT_DURATION_MS = 30;

        private bool status;

        private int durationMs = DEFAULT_DURATION_MS;
        public int DurationMs
        {
            get => durationMs;
            set => durationMs = value;
        }

        //private Stopwatch elapsed = new Stopwatch();
        private bool waited;

        public StartPressFunc()
        {
        }

        public StartPressFunc(StartPressFunc srcFunc)
        {
            srcFunc.CopyTo(this);

            durationMs = srcFunc.durationMs;
        }

        public override void Prepare(Mapper mapper, bool state,
            ActionFuncStateData stateData)
        {
            if (this.status != state)
            {
                this.status = state;
                waited = false;
                activeEvent = true;

                if (status)
                {
                    active = false;
                    outputActive = false;
                    finished = false;
                    //elapsed.Restart();
                }
                else
                {
                    //elapsed.Reset();

                    if (!toggleEnabled)
                    {
                        active = false;
                    }
                }
            }

            if (status && !finished && !waited)
            {
                if (stateData.elapsed.ElapsedMilliseconds <= durationMs)
                //if (elapsed.ElapsedMilliseconds <= durationMs)
                //if (!state && stateData.wasActive)
                {
                    active = true;
                    outputActive = true;
                    activeEvent = true;
                    // Execute system event
                    //SendOutputEvent(mapper);
                }
                else
                {
                    waited = true;
                    //elapsed.Stop();

                    if (!toggleEnabled)
                    {
                        finished = true;
                        active = false;
                        outputActive = false;
                    }
                }
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
            if (status && !finished && !waited)
            {
                if (stateData.elapsed.ElapsedMilliseconds > durationMs)
                {
                    waited = true;
                    //elapsed.Stop();

                    if (!toggleEnabled)
                    {
                        finished = true;
                        active = false;
                        outputActive = false;
                    }
                }
            }
        }

        public override void Release(Mapper mapper)
        {
            status = false;
            active = false;
            outputActive = false;
            activeEvent = false;
            waited = false;
            finished = false;
            //elapsed.Reset();
        }
    }
}
