using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.GyroActions;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ViewModels.GyroActionPropViewModels
{
    public class GyroDirSwipeActionPropViewModel : GyroActionPropVMBase
    {
        protected GyroDirectionalSwipe action;
        public GyroDirectionalSwipe Action
        {
            get => action;
        }

        private List<GyroTriggerButtonItem> triggerButtonItems;
        public List<GyroTriggerButtonItem> TriggerButtonItems => triggerButtonItems;

        public bool TriggerActivates
        {
            get => action.swipeParams.triggerActivates;
            set
            {
                if (action.swipeParams.triggerActivates == value) return;
                action.swipeParams.triggerActivates = value;
                TriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TriggerActivatesChanged;

        public int DeadZoneX
        {
            get => action.swipeParams.deadzoneX;
            set
            {
                {
                    action.swipeParams.deadzoneX = Math.Clamp(value, 0, 10000);
                    DeadZoneXChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler DeadZoneXChanged;

        public int DeadZoneY
        {
            get => action.swipeParams.deadzoneY;
            set
            {
                {
                    action.swipeParams.deadzoneY = Math.Clamp(value, 0, 10000);
                    DeadZoneYChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler DeadZoneYChanged;

        public int DelayTime
        {
            get => action.swipeParams.delayTime;
            set
            {
                action.swipeParams.delayTime = Math.Clamp(value, 0, 10000);
                DelayTimeChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DelayTimeChanged;

        public string ActionUpBtnDisplayBind
        {
            get => action.UsedEventsButtonsY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Up].DescribeActions(mapper);
        }

        public string ActionDownBtnDisplayBind
        {
            get => action.UsedEventsButtonsY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Down].DescribeActions(mapper);
        }

        public string ActionLeftBtnDisplayBind
        {
            get => action.UsedEventsButtonsX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Left].DescribeActions(mapper);
        }

        public string ActionRightBtnDisplayBind
        {
            get => action.UsedEventsButtonsX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Right].DescribeActions(mapper);
        }

        public bool HighlightName
        {
            get => baseAction.ParentAction == null ||
                baseAction.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public bool HighlightDeadZoneX
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_X);
        }
        public event EventHandler HighlightDeadZoneXChanged;

        public bool HighlightDeadZoneY
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_Y);
        }
        public event EventHandler HighlightDeadZoneYChanged;

        public bool HighlightDelayTime
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.DELAY_TIME);
        }
        public event EventHandler HighlightDelayTimeChanged;

        public bool HighlightTriggerActivates
        {
            get => action.ParentAction == null ||
                baseAction.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_ACTIVATE);
        }
        public event EventHandler HighlightTriggerActivatesChanged;

        public bool HighlightGyroTriggers
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_BUTTONS);
        }
        public event EventHandler HighlightGyroTriggersChanged;

        public override event EventHandler ActionPropertyChanged;

        public GyroDirSwipeActionPropViewModel(Mapper mapper, GyroMapAction action)
        {
            this.mapper = mapper;
            this.action = action as GyroDirectionalSwipe;
            this.baseAction = action;
            triggerButtonItems = new List<GyroTriggerButtonItem>();

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                GyroDirectionalSwipe baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as GyroDirectionalSwipe;
                GyroDirectionalSwipe tempAction = new GyroDirectionalSwipe();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;
                this.baseAction = tempAction;
                usingRealAction = false;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            PopulateModel();

            NameChanged += GyroDirSwipeActionPropViewModel_NameChanged;
            DeadZoneXChanged += GyroDirSwipeActionPropViewModel_DeadZoneXChanged;
            DeadZoneYChanged += GyroDirSwipeActionPropViewModel_DeadZoneYChanged;
            DelayTimeChanged += GyroDirSwipeActionPropViewModel_DelayTimeChanged;
            TriggerActivatesChanged += GyroDirSwipeActionPropViewModel_TriggerActivatesChanged;
        }

        private void GyroDirSwipeActionPropViewModel_TriggerActivatesChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_ACTIVATE))
            {
                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_ACTIVATE);
            }

            action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_ACTIVATE);
            HighlightTriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroDirSwipeActionPropViewModel_DelayTimeChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.DELAY_TIME))
            {
                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.DELAY_TIME);
            }

            action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.DELAY_TIME);
            HighlightDelayTimeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroDirSwipeActionPropViewModel_DeadZoneYChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_Y))
            {
                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_Y);
            }

            action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_Y);
            HighlightDeadZoneYChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroDirSwipeActionPropViewModel_DeadZoneXChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_X))
            {
                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_X);
            }

            action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_X);
            HighlightDeadZoneXChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroDirSwipeActionPropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.NAME);
            }

            action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.NAME);
            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateModel()
        {
            triggerButtonItems.AddRange(new GyroTriggerButtonItem[]
            {
                new GyroTriggerButtonItem("A", JoypadActionCodes.BtnSouth),
                new GyroTriggerButtonItem("B", JoypadActionCodes.BtnEast),
                new GyroTriggerButtonItem("X", JoypadActionCodes.BtnWest),
                new GyroTriggerButtonItem("Y", JoypadActionCodes.BtnNorth),
                new GyroTriggerButtonItem("Left Bumper", JoypadActionCodes.BtnLShoulder),
                new GyroTriggerButtonItem("Right Bumper", JoypadActionCodes.BtnRShoulder),
                new GyroTriggerButtonItem("Left Trigger", JoypadActionCodes.AxisLTrigger),
                new GyroTriggerButtonItem("Right Trigger", JoypadActionCodes.AxisRTrigger),
                new GyroTriggerButtonItem("Left Grip", JoypadActionCodes.BtnLGrip),
                new GyroTriggerButtonItem("Right Grip", JoypadActionCodes.BtnRGrip),
                new GyroTriggerButtonItem("Stick Click", JoypadActionCodes.BtnThumbL),
                new GyroTriggerButtonItem("Left Touchpad Touch", JoypadActionCodes.LPadTouch),
                new GyroTriggerButtonItem("Right Touchpad Touch", JoypadActionCodes.RPadTouch),
                new GyroTriggerButtonItem("Left Touchpad Click", JoypadActionCodes.LPadClick),
                new GyroTriggerButtonItem("Right Touchpad Click", JoypadActionCodes.RPadClick),
                new GyroTriggerButtonItem("Back", JoypadActionCodes.BtnSelect),
                new GyroTriggerButtonItem("Start", JoypadActionCodes.BtnStart),
                new GyroTriggerButtonItem("Steam", JoypadActionCodes.BtnMode),
            });

            foreach (JoypadActionCodes code in action.swipeParams.gyroTriggerButtons)
            {
                GyroTriggerButtonItem tempItem = triggerButtonItems.Find((item) => item.Code == code);
                if (tempItem != null)
                {
                    tempItem.Enabled = true;
                }
            }

            triggerButtonItems.ForEach((item) =>
            {
                item.EnabledChanged += GyroTriggerItem_EnabledChanged;
            });
        }

        private void GyroTriggerItem_EnabledChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_BUTTONS))
            {
                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_BUTTONS);
            }

            ExecuteInMapperThread(() =>
            {
                action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_BUTTONS);
            });

            HighlightGyroTriggersChanged?.Invoke(this, EventArgs.Empty);
            ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateUpDirButton(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                if (oldAction != null)
                {
                    oldAction.Release(mapper, ignoreReleaseActions: true);
                    action.UsedEventsButtonsY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Up] = newAction;
                }

                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_UP);
                action.UseParentDataY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Up] = false;
                action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_UP);
            });
        }

        public void UpdateDownDirButton(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                if (oldAction != null)
                {
                    oldAction.Release(mapper, ignoreReleaseActions: true);
                    action.UsedEventsButtonsY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Down] = newAction;
                }

                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_DOWN);
                action.UseParentDataY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Down] = false;
                action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_DOWN);
            });
        }

        public void UpdateLeftDirButton(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                if (oldAction != null)
                {
                    oldAction.Release(mapper, ignoreReleaseActions: true);
                    action.UsedEventsButtonsX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Left] = newAction;
                }

                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_LEFT);
                action.UseParentDataX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Left] = false;
                action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_LEFT);
            });
        }

        public void UpdateRightDirButton(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                if (oldAction != null)
                {
                    oldAction.Release(mapper, ignoreReleaseActions: true);
                    action.UsedEventsButtonsX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Right] = newAction;
                }

                action.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_RIGHT);
                action.UseParentDataX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Right] = false;
                action.RaiseNotifyPropertyChange(mapper, GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_RIGHT);
            });
        }
    }
}
