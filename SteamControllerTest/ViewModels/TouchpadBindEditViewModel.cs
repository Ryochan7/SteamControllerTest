using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.TouchpadActions;

namespace SteamControllerTest.ViewModels
{
    public class TouchpadBindEditViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private TouchpadMapAction action;
        public TouchpadMapAction Action
        {
            get => action;
        }

        private UserControl displayControl;
        public UserControl DisplayControl
        {
            get => displayControl;
            set
            {
                displayControl = value;
                DisplayControlChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DisplayControlChanged;

        public TouchpadBindEditViewModel(Mapper mapper, TouchpadMapAction action)
        {
            this.mapper = mapper;
            this.action = action;
        }

        public TouchpadMapAction PrepareNewAction(int ind)
        {
            TouchpadMapAction result = null;
            switch (ind)
            {
                case 0:
                    result = new TouchpadNoAction();
                    break;
                case 1:
                    result = new TouchpadStickAction();
                    break;
                case 2:
                    result = new TouchpadActionPad();
                    break;
                case 3:
                    result = new TouchpadMouseJoystick();
                    break;
                case 4:
                    result = new TouchpadMouse();
                    break;
                case 5:
                    result = new TouchpadCircular();
                    break;
                default:
                    break;
            }
            return result;
        }

        public void SwitchAction(TouchpadMapAction action)
        {
            TouchpadMapAction oldAction = this.action;
            TouchpadMapAction newAction = action;

            mapper.QueueEvent(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);
                //int tempInd = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.LayerActions.FindIndex((item) => item == tempAction);
                //if (tempInd >= 0)
                {
                    mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.ReplaceTouchpadAction(oldAction, newAction);
                    //mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.LayerActions.RemoveAt(tempInd);
                    //mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.LayerActions.Insert(tempInd, newAction);

                }
            });

            this.action = action;
        }

        public void MigrateActionId(TouchpadMapAction newAction)
        {
            if (action.Id == MapAction.DEFAULT_UNBOUND_ID)
            {
                // Need to create new ID for action
                newAction.Id = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.FindNextAvailableId();
            }
            else
            {
                // Can re-use existing ID
                newAction.Id = action.Id;
            }
        }
    }
}
