using System;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ActionUtil
{
    public class ChordedPressFunc : ActionFunc
    {
        private bool inputStatus;
        private JoypadActionCodes triggerButton;
        private bool chordActive;

        public JoypadActionCodes TriggerButton
        {
            get => triggerButton;
            set => triggerButton = value;
        }

        public ChordedPressFunc()
        {
            onChorded = true;
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
                    bool triggerActive = mapper.IsButtonActive(triggerButton);
                    chordActive = triggerActive;
                    active = chordActive;
                    finished = false;
                }
                else
                {
                    chordActive = false;
                    active = false;
                    finished = true;
                }
            }
        }

        public override void Event(Mapper mapper, ActionFuncStateData stateData)
        {
            if (inputStatus)
            {
                bool chordActive = mapper.IsButtonActive(triggerButton);
                if (chordActive)
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
            else
            {
                active = false;
                finished = true;
                chordActive = false;
            }

            activeEvent = false;
        }

        public override void Release(Mapper mapper)
        {
            inputStatus = false;
            active = false;
            activeEvent = false;
            finished = true;
            chordActive = false;
        }
    }
}
