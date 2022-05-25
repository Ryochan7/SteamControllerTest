using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamControllerTest.GyroActions;

namespace SteamControllerTest.ViewModels.GyroActionPropViewModels
{
    public class GyroMouseActionPropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private GyroMouse action;
        public GyroMouse Action
        {
            get => action;
        }

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

        public bool HighlightName
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(GyroMouse.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public event EventHandler ActionPropertyChanged;
        public event EventHandler<GyroMapAction> ActionChanged;

        private bool replacedAction = false;

        public GyroMouseActionPropViewModel(Mapper mapper, GyroMapAction action)
        {
            this.mapper = mapper;
            this.action = action as GyroMouse;

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

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }
        }

        private void ReplaceExistingLayerAction(object sender, EventArgs e)
        {
            if (!replacedAction)
            {
                ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

                mapper.QueueEvent(() =>
                {
                    this.action.ParentAction.Release(mapper, ignoreReleaseActions: true);

                    mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.AddGyroAction(this.action);
                    if (mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer)
                    {
                        mapper.ActionProfile.CurrentActionSet.RecompileCompositeLayer(mapper);
                    }

                    resetEvent.Set();
                });

                resetEvent.Wait();

                replacedAction = true;

                ActionChanged?.Invoke(this, action);
            }
        }
    }
}
