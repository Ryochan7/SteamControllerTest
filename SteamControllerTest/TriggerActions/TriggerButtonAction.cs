using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.AxisModifiers;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest.TriggerActions
{
    public class TriggerButtonAction : TriggerMapAction
    {
        private bool inputStatus;
        private AxisDirButton eventButton = new AxisDirButton();
        public AxisDirButton EventButton
        {
            get => eventButton;
        }

        private double axisNorm = 0.0;
        private AxisDeadZone deadZone;

        public AxisDeadZone DeadZone
        {
            get => deadZone;
        }

        public TriggerButtonAction()
        {
            deadZone = new AxisDeadZone(30 / 255.0, 1.0, 0.0);
        }

        public override void Prepare(Mapper mapper, double axisValue, bool alterState = true)
        {
            //bool inSafeZone = axisValue > 30;
            deadZone.CalcOutValues((int)axisValue, 255, out axisNorm);
            //bool inSafeZone = axisNorm != 0.0;
            //if (inSafeZone)
            //{
            //    axisNorm = (axisValue - 30.0) / (255.0 - 30.0);
            //}
            //else
            //{
            //    axisNorm = 0.0;
            //}

            eventButton.PrepareAnalog(mapper, axisNorm, 1.0);

            inputStatus = axisNorm > 0.0;
            active = eventButton.active;
            activeEvent = true;
        }

        public override void Event(Mapper mapper)
        {
            if (eventButton.active) eventButton.Event(mapper);

            active = axisNorm > 0.0;
            activeEvent = false;
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
            eventButton.Release(mapper);

            axisNorm = 0.0;
            inputStatus = false;
            active = activeEvent = false;
        }

        public override void SoftCopyFromParent(TriggerMapAction parentAction)
        {
            if (parentAction is TriggerButtonAction tempBtnAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                mappingId = tempBtnAction.mappingId;
            }
        }
    }
}
