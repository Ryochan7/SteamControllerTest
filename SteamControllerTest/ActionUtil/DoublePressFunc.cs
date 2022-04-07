using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ActionUtil
{
    public class DoublePressFunc : ActionFunc
    {
        private enum TapStatus : uint
        {
            Inactive,
            FirstTap,
            SecondTap,
            Failed,
        }

        private bool status;

        private int durationMs;
        public int DurationMs { get => durationMs; set => durationMs = value; }

        private Stopwatch elapsed = new Stopwatch();
        //private bool firstPress;
        //private bool secondPress;
        private TapStatus currentTapStatus;

        public DoublePressFunc()
        {
        }

        public DoublePressFunc(DoublePressFunc srcFunc)
        {
            srcFunc.CopyTo(this);
            durationMs = srcFunc.durationMs;
        }

        public override void PrepareState(Mapper mapper, ActionFunc secondFunc)
        {
            base.PrepareState(mapper, secondFunc);

            if (secondFunc is DoublePressFunc tempFunc)
            {
                currentTapStatus = tempFunc.currentTapStatus;
            }
        }

        public override void Prepare(Mapper mapper, bool state, ActionFuncStateData stateData)
        {
            if (this.status != state)
            {
                this.status = state;
                activeEvent = true;

                if (status)
                {
                    if (toggleEnabled && active)
                    {
                        active = false;
                        currentTapStatus = TapStatus.Inactive;
                    }

                    if (currentTapStatus == TapStatus.Inactive)
                    {
                        active = false;
                        //finished = false;
                        elapsed.Restart();
                    }
                    else if (currentTapStatus == TapStatus.FirstTap)
                    {
                        if (elapsed.ElapsedMilliseconds <= durationMs)
                        {
                            //Console.WriteLine("DOUBLE TAP");
                            currentTapStatus = TapStatus.SecondTap;
                            active = true;
                            elapsed.Stop();
                            //finished = true;
                        }
                        else
                        {
                            //Console.WriteLine("DOUBLE TAP FAIL");
                            active = false;
                            //finished = true;
                            elapsed.Stop();
                            currentTapStatus = TapStatus.Failed;
                        }
                    }
                    else
                    {
                        //Console.WriteLine("MADE IT HERE: {0}", currentTapStatus.ToString());
                        active = false;
                        //finished = false;
                        elapsed.Stop();
                        currentTapStatus = TapStatus.Failed;
                    }
                }
                else
                {
                    if (currentTapStatus == TapStatus.Inactive)
                    {
                        if (elapsed.ElapsedMilliseconds <= durationMs)
                        {
                            //Console.WriteLine("SINGLE TAP");
                            currentTapStatus = TapStatus.FirstTap;
                            active = false;
                            //finished = false;
                        }
                        else
                        {
                            //Console.WriteLine("SINGLE TAP FAIL");
                            elapsed.Stop();
                            active = false;
                            //finished = true;
                            currentTapStatus = TapStatus.Inactive;
                        }
                    }
                    else if (currentTapStatus == TapStatus.SecondTap)
                    {
                        //Console.WriteLine("SECOND TAP PASS");
                        if (!toggleEnabled)
                        {
                            active = false;
                            currentTapStatus = TapStatus.Inactive;
                        }

                        //finished = true;
                    }
                    else if (currentTapStatus == TapStatus.Failed)
                    {
                        currentTapStatus = TapStatus.Inactive;
                        active = false;
                        //finished = true;
                    }
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
            finished = false;
            if (currentTapStatus != TapStatus.FirstTap)
            {
                elapsed.Stop();
                currentTapStatus = TapStatus.Inactive;
            }
            //currentTapStatus = TapStatus.Inactive;
            //elapsed.Reset();
        }

        public void Reset()
        {
            currentTapStatus = TapStatus.Inactive;
            elapsed.Reset();
        }
    }
}
