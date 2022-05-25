using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.TouchpadActions;

namespace SteamControllerTest.ViewModels.TouchpadActionPropViewModels
{
    public class TouchpadMousePropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper => mapper;

        private TouchpadMouse action;
        public TouchpadMouse Action => action;

        public string Name
        {
            get => action.Name;
            set
            {
                if (action.Name == value) return;
                action.Name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler NameChanged;

        public string DeadZone
        {
            get => action.DeadZone.ToString();
            set
            {
                if (int.TryParse(value, out int temp))
                {
                    action.DeadZone = Math.Clamp(temp, 0, 10000);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler DeadZoneChanged;

        public bool TrackballEnabled
        {
            get => action.TrackballEnabled;
            set
            {
                action.TrackballEnabled = value;
                TrackballEnabledChanged?.Invoke(this, EventArgs.Empty);
                ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TrackballEnabledChanged;

        public string TrackballFriction
        {
            get => action.TrackballFriction.ToString();
            set
            {
                if (int.TryParse(value, out int temp))
                {
                    action.TrackballFriction = Math.Clamp(temp, 0, 100);
                    TrackballFrictionChanged?.Invoke(this, EventArgs.Empty);
                    ActionPropertyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public event EventHandler TrackballFrictionChanged;

        public bool HighlightName
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public bool HighlightDeadZone
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.DEAD_ZONE);
        }
        public event EventHandler HighlightDeadZoneChanged;

        public bool HighlightTrackballEnabled
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.TRACKBALL_MODE);
        }
        public event EventHandler HighlightTrackballEnabledChanged;

        public bool HighlightTrackballFriction
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.TRACKBALL_FRICTION);
        }
        public event EventHandler HighlightTrackballFrictionChanged;

        public event EventHandler ActionPropertyChanged;

        private bool replacedAction = false;

        public TouchpadMousePropViewModel(Mapper mapper, TouchpadMapAction action)
        {
            this.mapper = mapper;
            this.action = action as TouchpadMouse;

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                TouchpadMouse baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as TouchpadMouse;
                TouchpadMouse tempAction = new TouchpadMouse();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            NameChanged += TouchpadMousePropViewModel_NameChanged;
            DeadZoneChanged += TouchpadMousePropViewModel_DeadZoneChanged;
            TrackballEnabledChanged += TouchpadMousePropViewModel_TrackballEnabledChanged;
            TrackballFrictionChanged += TouchpadMousePropViewModel_TrackballFrictionChanged;
            ActionPropertyChanged += SetProfileDirty;
        }

        private void TouchpadMousePropViewModel_TrackballFrictionChanged(object sender, EventArgs e)
        {
            if (!this.action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.TRACKBALL_FRICTION))
            {
                this.action.ChangedProperties.Add(TouchpadMouse.PropertyKeyStrings.TRACKBALL_FRICTION);
            }

            HighlightTrackballFrictionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TouchpadMousePropViewModel_TrackballEnabledChanged(object sender, EventArgs e)
        {
            if (!this.action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.TRACKBALL_MODE))
            {
                this.action.ChangedProperties.Add(TouchpadMouse.PropertyKeyStrings.TRACKBALL_MODE);
            }

            HighlightTrackballEnabledChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetProfileDirty(object sender, EventArgs e)
        {
            mapper.ActionProfile.Dirty = true;
        }

        private void TouchpadMousePropViewModel_DeadZoneChanged(object sender, EventArgs e)
        {
            if (!this.action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.NAME))
            {
                this.action.ChangedProperties.Add(TouchpadMouse.PropertyKeyStrings.NAME);
            }

            HighlightDeadZoneChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TouchpadMousePropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!this.action.ChangedProperties.Contains(TouchpadMouse.PropertyKeyStrings.NAME))
            {
                this.action.ChangedProperties.Add(TouchpadMouse.PropertyKeyStrings.NAME);
            }

            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReplaceExistingLayerAction(object sender, EventArgs e)
        {
            if (!replacedAction)
            {
                ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);
                mapper.QueueEvent(() =>
                {
                    this.action.ParentAction.Release(mapper, ignoreReleaseActions: true);

                    mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.AddTouchpadAction(this.action);
                    if (mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer)
                    {
                        mapper.ActionProfile.CurrentActionSet.RecompileCompositeLayer(mapper);
                    }

                    resetEvent.Set();
                });

                resetEvent.Wait();

                replacedAction = true;
            }
        }
    }
}
