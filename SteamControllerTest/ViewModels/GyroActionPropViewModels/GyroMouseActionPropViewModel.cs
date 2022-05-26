using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamControllerTest.GyroActions;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ViewModels.GyroActionPropViewModels
{
    public class GyroMouseActionPropViewModel : GyroActionPropVMBase
    {
        protected GyroMouse action;
        public GyroMouse Action
        {
            get => action;
        }

        private int DeadZone
        {
            get => action.mouseParams.deadzone;
            set
            {
                action.mouseParams.deadzone = value;
                DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DeadZoneChanged;

        private List<GyroTriggerButtonItem> triggerButtonItems;
        public List<GyroTriggerButtonItem> TriggerButtonItems => triggerButtonItems;

        public bool HighlightName
        {
            get => baseAction.ParentAction == null ||
                baseAction.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public bool HighlightDeadZone
        {
            get => baseAction.ParentAction == null ||
                baseAction.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.DEAD_ZONE);
        }
        public event EventHandler HighlightDeadZoneChanged;

        public override event EventHandler ActionPropertyChanged;

        public GyroMouseActionPropViewModel(Mapper mapper, GyroMapAction action)
        {
            this.mapper = mapper;
            this.action = action as GyroMouse;
            this.baseAction = action;

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                GyroMouse baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as GyroMouse;
                GyroMouse tempAction = new GyroMouse();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;
                this.baseAction = tempAction;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            PopulateModel();

            NameChanged += GyroMouseActionPropViewModel_NameChanged;
            DeadZoneChanged += GyroMouseActionPropViewModel_DeadZoneChanged;
        }

        private void GyroMouseActionPropViewModel_DeadZoneChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.DEAD_ZONE))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.DEAD_ZONE);
            }

            HighlightDeadZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GyroMouseActionPropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.NAME);
            }

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
                new GyroTriggerButtonItem("Left Touchpad Touch", JoypadActionCodes.LPadTouch),
                new GyroTriggerButtonItem("Right Touchpad Touch", JoypadActionCodes.RPadTouch),
                new GyroTriggerButtonItem("Stick Click", JoypadActionCodes.BtnThumbL),
                new GyroTriggerButtonItem("Back", JoypadActionCodes.BtnSelect),
                new GyroTriggerButtonItem("Start", JoypadActionCodes.BtnStart),
                new GyroTriggerButtonItem("Steam", JoypadActionCodes.BtnMode),
            });

            foreach(JoypadActionCodes code in action.mouseParams.gyroTriggerButtons)
            {
                GyroTriggerButtonItem tempItem = triggerButtonItems.Find((item) => item.Code == code);
                if (tempItem != null)
                {
                    tempItem.Enabled = true;
                }
            }
        }
    }

    public class GyroTriggerButtonItem
    {
        private string displayString;
        public string DisplayString
        {
            get => displayString;
        }

        private JoypadActionCodes code;
        public JoypadActionCodes Code => code;

        private bool enabled;
        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                EnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler EnabledChanged;

        public GyroTriggerButtonItem(string displayString, JoypadActionCodes code,
            bool enabled=false)
        {
            this.displayString = displayString;
            this.code = code;
            this.enabled = enabled;
        }
    }
}
