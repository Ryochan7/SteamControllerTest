using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamControllerTest.ViewModels.Common;
using SteamControllerTest.GyroActions;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.StickModifiers;

namespace SteamControllerTest.ViewModels.GyroActionPropViewModels
{
    public class GyroMouseJoystickPropViewModel : GyroActionPropVMBase
    {
        private GyroMouseJoystick action;
        public GyroMouseJoystick Action
        {
            get => action;
        }

        private OutputStickSelectionItemList outputStickHolder;
        public OutputStickSelectionItemList OutputStickHolder => outputStickHolder;

        private int outputStickIndex = -1;
        public int OutputStickIndex
        {
            get => outputStickIndex;
            set
            {
                outputStickIndex = value;
                OutputStickIndexChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler OutputStickIndexChanged;

        public int DeadZone
        {
            get => action.mStickParms.deadZone;
            set
            {
                action.mStickParms.deadZone = value;
                DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DeadZoneChanged;

        private List<GyroTriggerButtonItem> triggerButtonItems;
        public List<GyroTriggerButtonItem> TriggerButtonItems => triggerButtonItems;

        public bool TriggerActivates
        {
            get => action.mStickParms.triggerActivates;
            set
            {
                if (action.mStickParms.triggerActivates == value) return;
                action.mStickParms.triggerActivates = value;
                TriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TriggerActivatesChanged;

        public string AntiDeadZoneX
        {
            get => action.mStickParms.antiDeadzoneX.ToString("N2");
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.mStickParms.antiDeadzoneX = Math.Clamp(temp, 0.0, 1.0);
                    AntiDeadZoneXChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler AntiDeadZoneXChanged;

        public string AntiDeadZoneY
        {
            get => action.mStickParms.antiDeadzoneY.ToString("N2");
            set
            {
                if (double.TryParse(value, out double temp))
                {
                    action.mStickParms.antiDeadzoneY = Math.Clamp(temp, 0.0, 1.0);
                    AntiDeadZoneYChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler AntiDeadZoneYChanged;

        //private List<GyroOutputCurveItem> outputCurveItems = new List<GyroOutputCurveItem>();
        //public List<GyroOutputCurveItem> OutputCurveItems => outputCurveItems;

        //private int outputCurveIndex = -1;
        //public int OutputCurveIndex
        //{
        //    get => outputCurveIndex;
        //    set
        //    {
        //        outputCurveIndex = value;
        //        OutputCurveIndexChanged?.Invoke(this, EventArgs.Empty);
        //        ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
        //    }
        //}
        //public event EventHandler OutputCurveIndexChanged;

        public bool HighlightName
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;


        public bool HighlightDeadZone
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.DEAD_ZONE);
        }
        public event EventHandler HighlightDeadZoneChanged;

        public bool HighlightOutputStick
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.OUTPUT_STICK);
        }
        public event EventHandler HighlightOutputStickChanged;

        public bool HighlightGyroTriggers
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_BUTTONS);
        }
        public event EventHandler HighlightGyroTriggersChanged;

        public bool HighlightGyroTriggerActivates
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_ACTIVATE);
        }
        public event EventHandler HighlightGyroTriggerActivatesChanged;

        public bool HighlightAntiDeadZoneX
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_X);
        }
        public event EventHandler HighlightAntiDeadZoneXChanged;

        public bool HighlightAntiDeadZoneY
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_Y);
        }
        public event EventHandler HighlightAntiDeadZoneYChanged;

        //public bool HighlightOutputCurve
        //{
        //    get => action.ParentAction == null ||
        //        action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_Y);
        //}
        //public event EventHandler HighlightOutputCurveChanged;

        public override event EventHandler ActionPropertyChanged;

        public GyroMouseJoystickPropViewModel(Mapper mapper, GyroMapAction action)
        {
            this.mapper = mapper;
            this.action = action as GyroMouseJoystick;
            this.baseAction = action;

            triggerButtonItems = new List<GyroTriggerButtonItem>();
            outputStickHolder = new OutputStickSelectionItemList();

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                GyroMouseJoystick baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as GyroMouseJoystick;
                GyroMouseJoystick tempAction = new GyroMouseJoystick();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;
                this.baseAction = this.action;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            PopulateModel();

            NameChanged += GyroMouseJoystickPropViewModel_NameChanged;
            OutputStickIndexChanged += GyroMouseJoystickPropViewModel_OutputStickIndexChanged;
            DeadZoneChanged += GyroMouseJoystickPropViewModel_DeadZoneChanged;
            TriggerActivatesChanged += GyroMouseJoystickPropViewModel_TriggerActivatesChanged;
            AntiDeadZoneXChanged += GyroMouseJoystickPropViewModel_AntiDeadZoneXChanged;
            AntiDeadZoneYChanged += GyroMouseJoystickPropViewModel_AntiDeadZoneYChanged;
        }

        private void GyroMouseJoystickPropViewModel_OutputStickIndexChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.OUTPUT_STICK))
            {
                action.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.OUTPUT_STICK);
            }

            HighlightOutputStickChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseJoystickPropViewModel_AntiDeadZoneYChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_Y))
            {
                action.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_Y);
            }

            HighlightAntiDeadZoneYChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseJoystickPropViewModel_AntiDeadZoneXChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_X))
            {
                action.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_X);
            }

            HighlightAntiDeadZoneXChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseJoystickPropViewModel_TriggerActivatesChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_ACTIVATE))
            {
                action.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_ACTIVATE);
            }

            HighlightGyroTriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
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

            foreach (JoypadActionCodes code in action.mStickParms.gyroTriggerButtons)
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

            outputStickIndex =
                outputStickHolder.StickAliasIndex(action.mStickParms.outputStick);

            //outputCurveItems.AddRange(new GyroOutputCurveItem[]
            //{
            //    new GyroOutputCurveItem("Linear", StickOutCurve.Curve.Linear),
            //    new GyroOutputCurveItem("Enhanced Precision", StickOutCurve.Curve.EnhancedPrecision),
            //    new GyroOutputCurveItem("Quadratic", StickOutCurve.Curve.Quadratic),
            //    new GyroOutputCurveItem("Cubic", StickOutCurve.Curve.Cubic),
            //    new GyroOutputCurveItem("Easeout Quadratic", StickOutCurve.Curve.EaseoutQuad),
            //    new GyroOutputCurveItem("Easeout Cubic", StickOutCurve.Curve.EaseoutCubic),
            //});
        }

        private void GyroTriggerItem_EnabledChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_BUTTONS))
            {
                action.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_BUTTONS);
            }

            HighlightGyroTriggersChanged?.Invoke(this, EventArgs.Empty);
            ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseJoystickPropViewModel_DeadZoneChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.DEAD_ZONE))
            {
                action.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.DEAD_ZONE);
            }

            HighlightDeadZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseJoystickPropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouseJoystick.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.NAME);
            }

            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class GyroOutputCurveItem
    {
        private string displayName;
        public string DisplayName
        {
            get => displayName;
        }

        private StickOutCurve.Curve outputCurve = StickOutCurve.Curve.Linear;
        public StickOutCurve.Curve OutputCurve => outputCurve;

        public GyroOutputCurveItem(string displayName, StickOutCurve.Curve outputCurve)
        {
            this.displayName = displayName;
            this.outputCurve = outputCurve;
        }
    }
}
