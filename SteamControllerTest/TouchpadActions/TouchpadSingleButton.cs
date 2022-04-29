using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ButtonActions;

namespace SteamControllerTest.TouchpadActions
{
    public class TouchpadSingleButton : TouchpadMapAction
    {
        public class PropertyKeyStrings
        {
            public const string NAME = "Name";
            public const string FUNCTIONS = "Functions";
        }

        private HashSet<string> fullPropertySet = new HashSet<string>()
        {
            PropertyKeyStrings.NAME,
            PropertyKeyStrings.FUNCTIONS,
        };

        private bool inputStatus;

        private ButtonAction usedEventButton = new ButtonAction();
        private bool useParentActions;
        public bool UseParentActions
        {
            get => useParentActions;
        }

        public ButtonAction EventButton
        {
            get => usedEventButton;
            set => usedEventButton = value;
        }

        public TouchpadSingleButton()
        {
        }

        public override void Prepare(Mapper mapper, ref TouchEventFrame touchFrame, bool alterState = true)
        {
            active = touchFrame.Touch;
            if (touchFrame.Touch != inputStatus)
            {
                inputStatus = touchFrame.Touch;
                active = true;
            }

            activeEvent = true;
        }

        public override void Event(Mapper mapper)
        {
            usedEventButton.Prepare(mapper, inputStatus);
            usedEventButton.Event(mapper);
        }

        public override void Release(Mapper mapper, bool resetState = true, bool ignoreReleaseActions = false)
        {
            if (active)
            {
                usedEventButton.Prepare(mapper, false);
                usedEventButton.Event(mapper);
            }
        }

        public override void SoftRelease(Mapper mapper, MapAction checkAction, bool resetState = true)
        {
            if (!useParentActions && active)
            {
                usedEventButton.Prepare(mapper, false);
                usedEventButton.Event(mapper);
            }

            if (resetState)
            {
                stateData.Reset();
            }
        }

        public override void SoftCopyFromParent(TouchpadMapAction parentAction)
        {
            if (parentAction is TouchpadSingleButton tempButtonAct)
            {
                base.SoftCopyFromParent(parentAction);

                // Determine the set with properties that should inherit
                // from the parent action
                IEnumerable<string> useParentProList =
                    fullPropertySet.Except(changedProperties);

                foreach (string parentPropType in useParentProList)
                {
                    switch(parentPropType)
                    {
                        case PropertyKeyStrings.NAME:
                            name = tempButtonAct.name;
                            break;
                        case PropertyKeyStrings.FUNCTIONS:
                            useParentActions = true;
                            usedEventButton = tempButtonAct.usedEventButton;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
