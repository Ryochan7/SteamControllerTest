using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                    {
                        TouchpadStickAction tempAction = new TouchpadStickAction();
                        var joyDefaults = mapper.DeviceActionDefaults.GrabTouchJoystickDefaults();
                        joyDefaults.Process(tempAction);
                        result = tempAction;
                    }

                    break;
                case 2:
                    {
                        TouchpadActionPad tempAction = new TouchpadActionPad();
                        var joyDefaults = mapper.DeviceActionDefaults.GrabTouchActionPadDefaults();
                        joyDefaults.Process(tempAction);
                        result = tempAction;
                    }

                    break;
                case 3:
                    {
                        TouchpadMouseJoystick tempAction = new TouchpadMouseJoystick();
                        var joyDefaults = mapper.DeviceActionDefaults.GrabTouchMouseJoystickDefaults();
                        joyDefaults.Process(tempAction);
                        result = tempAction;
                    }

                    break;
                case 4:
                    {
                        TouchpadMouse tempAction = new TouchpadMouse();
                        var joyDefaults = mapper.DeviceActionDefaults.GrabTouchMouseDefaults();
                        joyDefaults.Process(tempAction);
                        result = tempAction;
                    }

                    break;
                case 5:
                    {
                        TouchpadCircular tempAction = new TouchpadCircular();
                        var joyDefaults = mapper.DeviceActionDefaults.GrabTouchCircularActionDefaults();
                        joyDefaults.Process(tempAction);
                        result = tempAction;
                    }

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

            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

            mapper.QueueEvent(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);
                //int tempInd = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.LayerActions.FindIndex((item) => item == tempAction);
                //if (tempInd >= 0)
                {
                    //mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.LayerActions.RemoveAt(tempInd);
                    //mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.LayerActions.Insert(tempInd, newAction);

                    //oldAction.Release(mapper, ignoreReleaseActions: true);

                    //mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.AddTouchpadAction(this.action);
                    if (oldAction.Id != MapAction.DEFAULT_UNBOUND_ID)
                    {
                        mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.ReplaceTouchpadAction(oldAction, newAction);
                    }
                    else
                    {
                        mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.AddTouchpadAction(newAction);
                    }

                    if (mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer)
                    {
                        MapAction baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[oldAction.MappingId];
                        if (MapAction.IsSameType(baseLayerAction, newAction))
                        {
                            newAction.SoftCopyFromParent(baseLayerAction as TouchpadMapAction);
                        }

                        mapper.ActionProfile.CurrentActionSet.RecompileCompositeLayer(mapper);
                    }
                }

                resetEvent.Set();
            });

            resetEvent.Wait();

            this.action = action;
        }

        public void MigrateActionId(TouchpadMapAction newAction)
        {
            if (action.Id == MapAction.DEFAULT_UNBOUND_ID)
            {
                // Need to create new ID for action
                newAction.Id = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
            }
            else
            {
                // Can re-use existing ID
                newAction.Id = action.Id;
            }
        }

        public void UpdateAction(TouchpadMapAction currentAction)
        {
            this.action = currentAction;
        }
    }
}
