using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using SteamControllerTest.GyroActions;

namespace SteamControllerTest.ViewModels
{
    public class GyroBindEditViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private GyroMapAction action;
        public GyroMapAction Action
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

        public GyroBindEditViewModel(Mapper mapper, GyroMapAction action)
        {
            this.mapper = mapper;
            this.action = action;
        }

        public GyroMapAction PrepareNewAction(int ind)
        {
            GyroMapAction result = null;
            switch (ind)
            {
                case 0:
                    result = new GyroNoMapAction();
                    break;
                case 1:
                    result = new GyroMouse();
                    break;
                case 2:
                    result = new GyroMouseJoystick();
                    break;
                default:
                    break;
            }

            return result;
        }

        public void SwitchAction(GyroMapAction action)
        {
            GyroMapAction oldAction = this.action;
            GyroMapAction newAction = action;

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
                        mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.ReplaceGyroAction(oldAction, newAction);
                    }
                    else
                    {
                        mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.AddGyroAction(newAction);
                    }

                    if (mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer)
                    {
                        MapAction baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[oldAction.MappingId];
                        if (MapAction.IsSameType(baseLayerAction, newAction))
                        {
                            newAction.SoftCopyFromParent(baseLayerAction as GyroMapAction);
                        }

                        mapper.ActionProfile.CurrentActionSet.RecompileCompositeLayer(mapper);
                    }
                }

                resetEvent.Set();
            });

            resetEvent.Wait();

            this.action = action;
        }

        public void MigrateActionId(GyroMapAction newAction)
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

        public void UpdateAction(GyroMapAction newAction)
        {
            this.action = newAction;
        }
    }
}
