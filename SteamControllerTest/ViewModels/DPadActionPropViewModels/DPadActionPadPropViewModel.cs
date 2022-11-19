using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.DPadActions;
using SteamControllerTest.StickActions;

namespace SteamControllerTest.ViewModels.DPadActionPropViewModels
{
    public class DPadActionPadPropViewModel
    {
        private Mapper mapper;
        public Mapper Mapper
        {
            get => mapper;
        }

        private DPadAction action;
        public DPadAction Action
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


        public string ActionUpBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.Up].DescribeActions(mapper);
        }

        public string ActionDownBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.Down].DescribeActions(mapper);
        }

        public string ActionLeftBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.Left].DescribeActions(mapper);
        }

        public string ActionRightBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.Right].DescribeActions(mapper);
        }

        public string ActionUpLeftBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.UpLeft].DescribeActions(mapper);
        }

        public string ActionUpRightBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.UpRight].DescribeActions(mapper);
        }

        public string ActionDownLeftBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.DownLeft].DescribeActions(mapper);
        }

        public string ActionDownRightBtnDisplayBind
        {
            get => action.EventCodes4[(int)DpadDirections.DownRight].DescribeActions(mapper);
        }


        public bool HighlightName
        {
            get => action.ParentAction == null ||
                action.ChangedProperties.Contains(DPadTranslate.PropertyKeyStrings.NAME);
        }
        public event EventHandler HighlightNameChanged;

        public event EventHandler ActionPropertyChanged;
        public event EventHandler<DPadMapAction> ActionChanged;

        private bool usingRealAction = false;

        public DPadActionPadPropViewModel(Mapper mapper, DPadMapAction action)
        {
            this.mapper = mapper;
            this.action = action as DPadAction;
            usingRealAction = true;

            // Check if base ActionLayer action from composite layer
            if (action.ParentAction == null &&
                mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer &&
                !mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.LayerActions.Contains(action) &&
                MapAction.IsSameType(mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId], action))
            {
                // Test with temporary object
                DPadAction baseLayerAction = mapper.ActionProfile.CurrentActionSet.DefaultActionLayer.normalActionDict[action.MappingId] as DPadAction;
                DPadAction tempAction = new DPadAction();
                tempAction.SoftCopyFromParent(baseLayerAction);
                //int tempLayerId = mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.Index;
                int tempId = mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.FindNextAvailableId();
                tempAction.Id = tempId;
                //tempAction.MappingId = this.action.MappingId;

                this.action = tempAction;
                usingRealAction = false;

                ActionPropertyChanged += ReplaceExistingLayerAction;
            }

            PrepareModel();

            NameChanged += DPadActionPropViewModel_NameChanged;
        }

        private void DPadActionPropViewModel_NameChanged(object sender, EventArgs e)
        {
            if (!action.ChangedProperties.Contains(DPadAction.PropertyKeyStrings.NAME))
            {
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.NAME);
            }

            action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.NAME);
            HighlightNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReplaceExistingLayerAction(object sender, EventArgs e)
        {
            if (!usingRealAction)
            {
                ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

                mapper.QueueEvent(() =>
                {
                    this.action.ParentAction.Release(mapper, ignoreReleaseActions: true);

                    mapper.ActionProfile.CurrentActionSet.RecentAppliedLayer.AddDPadAction(this.action);
                    if (mapper.ActionProfile.CurrentActionSet.UsingCompositeLayer)
                    {
                        mapper.ActionProfile.CurrentActionSet.RecompileCompositeLayer(mapper);
                    }
                    else
                    {
                        mapper.ActionProfile.CurrentActionSet.CurrentActionLayer.SyncActions();
                        mapper.ActionProfile.CurrentActionSet.ClearCompositeLayerActions();
                        mapper.ActionProfile.CurrentActionSet.PrepareCompositeLayer();
                    }

                    resetEvent.Set();
                });

                resetEvent.Wait();

                usingRealAction = true;

                ActionChanged?.Invoke(this, action);
            }
        }

        private void PrepareModel()
        {

        }

        public void UpdateUpDirAction(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);

                action.EventCodes4[(int)DpadDirections.Up] = newAction as ButtonAction;
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_UP);
                this.action.UsingParentActionButton[(int)DpadDirections.Up] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_UP);
            });
        }

        public void UpdateDownDirAction(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);

                action.EventCodes4[(int)DpadDirections.Down] = newAction as ButtonAction;
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_DOWN);
                this.action.UsingParentActionButton[(int)DpadDirections.Down] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_DOWN);
            });
        }

        public void UpdateLeftDirAction(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);

                action.EventCodes4[(int)DpadDirections.Left] = newAction as ButtonAction;
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_LEFT);
                this.action.UsingParentActionButton[(int)DpadDirections.Left] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_LEFT);
            });
        }

        public void UpdateRightDirAction(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);

                action.EventCodes4[(int)DpadDirections.Right] = newAction as ButtonAction;
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_RIGHT);
                this.action.UsingParentActionButton[(int)DpadDirections.Right] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_RIGHT);
            });
        }

        public void UpdateUpLeftDirAction(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);

                action.EventCodes4[(int)DpadDirections.UpLeft] = newAction as ButtonAction;
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_UPLEFT);
                this.action.UsingParentActionButton[(int)DpadDirections.UpLeft] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_UPLEFT);
            });
        }

        public void UpdateUpRightDirAction(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);

                action.EventCodes4[(int)DpadDirections.UpRight] = newAction as ButtonAction;
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_UPRIGHT);
                this.action.UsingParentActionButton[(int)DpadDirections.UpRight] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_UPRIGHT);
            });
        }

        public void UpdateDownLeftDirAction(ButtonAction oldAction, ButtonAction newAction)
        {
            if (!usingRealAction)
            {
                ReplaceExistingLayerAction(this, EventArgs.Empty);
            }

            ExecuteInMapperThread(() =>
            {
                oldAction.Release(mapper, ignoreReleaseActions: true);

                action.EventCodes4[(int)DpadDirections.DownLeft] = newAction as ButtonAction;
                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_DOWNLEFT);
                this.action.UsingParentActionButton[(int)DpadDirections.DownLeft] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_DOWNLEFT);
            });
        }

        public void UpdateDownRightDirAction(ButtonAction oldAction, ButtonAction newAction)
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
                    action.EventCodes4[(int)DpadDirections.DownRight] = newAction as ButtonAction;
                }

                action.ChangedProperties.Add(DPadAction.PropertyKeyStrings.PAD_DIR_DOWNRIGHT);
                this.action.UsingParentActionButton[(int)DpadDirections.DownRight] = false;
                action.RaiseNotifyPropertyChange(mapper, DPadAction.PropertyKeyStrings.PAD_DIR_DOWNRIGHT);
            });
        }

        protected void ExecuteInMapperThread(Action tempAction)
        {
            ManualResetEventSlim resetEvent = new ManualResetEventSlim(false);

            mapper.QueueEvent(() =>
            {
                tempAction?.Invoke();

                resetEvent.Set();
            });

            resetEvent.Wait();
        }
    }
}
