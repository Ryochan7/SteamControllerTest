using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.TouchpadActions;

namespace SteamControllerTest.ViewModels
{
    public class ProfileEditorTestViewModel
    {
        private ManualResetEventSlim actionResetEvent = new ManualResetEventSlim(false);
        public ManualResetEventSlim ActionResetEvent => actionResetEvent;

        private Mapper mapper;
        public Mapper DeviceMapper
        {
            get => mapper;
        }

        private ProfileEntity profileEnt;
        public ProfileEntity ProfileEnt
        {
            get => profileEnt;
        }

        private Profile tempProfile;
        public Profile CurrentProfile
        {
            get => tempProfile;
        }

        private List<BindingItemsTest> buttonBindings = new List<BindingItemsTest>();
        public List<BindingItemsTest> ButtonBindings
        {
            get => buttonBindings;
        }

        private Dictionary<string, int> buttonBindingsIndexDict =
            new Dictionary<string, int>();
        public Dictionary<string, int> ButtonBindingsIndexDict
        {
            get => buttonBindingsIndexDict;
        }

        private List<TouchBindingItemsTest> touchpadBindings = new List<TouchBindingItemsTest>();
        public List<TouchBindingItemsTest> TouchpadBindings
        {
            get => touchpadBindings;
        }

        private int selectedTouchBindIndex = -1;
        public int SelectTouchBindIndex
        {
            get => selectedTouchBindIndex;
            set
            {
                if (selectedTouchBindIndex == value) return;
                selectedTouchBindIndex = value;
                SelectTouchBindIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SelectTouchBindIndexChanged;

        private ObservableCollection<ActionSetItemsTest> actionSetItems = new ObservableCollection<ActionSetItemsTest>();
        public ObservableCollection<ActionSetItemsTest> ActionSetItems => actionSetItems;

        private int selectedActionSetIndex = 0;
        public int SelectedActionSetIndex
        {
            get => selectedActionSetIndex;
            set => selectedActionSetIndex = value;
        }

        private List<ActionLayerItemsTest> layerItems = new List<ActionLayerItemsTest>();
        public List<ActionLayerItemsTest> LayerItems => layerItems;

        private int selectedActionLayerIndex = 0;
        public int SelectedActionLayerIndex
        {
            get => selectedActionLayerIndex;
            set => selectedActionLayerIndex = value;
        }

        public ProfileEditorTestViewModel(Mapper mapper, ProfileEntity profileEnt, Profile currentProfile)
        {
            this.mapper = mapper;
            this.profileEnt = profileEnt;
            this.tempProfile = currentProfile;
        }

        // Put List(s) in Mapper class to allow a more dynamic view later?
        public void Test()
        {
            foreach(ActionSet set in tempProfile.ActionSets)
            {
                ActionSetItemsTest tempItem = new ActionSetItemsTest(set);
                actionSetItems.Add(tempItem);
            }

            selectedActionLayerIndex = 0;
            selectedActionSetIndex = 0;
            PopulateLayerItems();
            PopulateCurrentLayerBindings();

            layerItems[0].ItemActive = true;
            actionSetItems[0].ItemActive = true;
        }

        public void RefreshSetBindings()
        {
            buttonBindings.Clear();
            buttonBindingsIndexDict.Clear();
            touchpadBindings.Clear();

            PopulateLayerItems();
            PopulateCurrentLayerBindings();
        }

        public void RefreshLayerBindings()
        {
            buttonBindings.Clear();
            buttonBindingsIndexDict.Clear();
            touchpadBindings.Clear();

            PopulateCurrentLayerBindings();
        }

        private void PopulateLayerItems()
        {
            ActionSetItemsTest setItem = actionSetItems[selectedActionSetIndex];
            ActionSet set = setItem.Set;

            layerItems.Clear();
            int tempInd = 0;
            foreach (ActionLayer layer in set.ActionLayers)
            {
                ActionLayerItemsTest tempLayerItem = new ActionLayerItemsTest(set, layer, tempInd++);
                layerItems.Add(tempLayerItem);
            }
        }

        private void PopulateCurrentLayerBindings()
        {
            int tempBtnInd = 0;
            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("A", out ButtonMapAction tempAct))
            {
                Trace.WriteLine($"{tempAct}");
                BindingItemsTest tempItem = new BindingItemsTest("A", "A", tempAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("A", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("B", out ButtonMapAction tempBAct))
            {
                Trace.WriteLine($"{tempBAct}");
                BindingItemsTest tempItem = new BindingItemsTest("B", "B", tempBAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("B", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("X", out ButtonMapAction tempXAct))
            {
                Trace.WriteLine($"{tempXAct}");
                BindingItemsTest tempItem = new BindingItemsTest("X", "X", tempXAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("X", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("Y", out ButtonMapAction tempYAct))
            {
                Trace.WriteLine($"{tempYAct}");
                BindingItemsTest tempItem = new BindingItemsTest("Y", "Y", tempYAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("Y", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("LShoulder", out ButtonMapAction tempLBAct))
            {
                Trace.WriteLine($"{tempLBAct}");
                BindingItemsTest tempItem = new BindingItemsTest("LShoulder", "Left Bumper", tempLBAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("LShoulder", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("RShoulder", out ButtonMapAction tempRBAct))
            {
                Trace.WriteLine($"{tempRBAct}");
                BindingItemsTest tempItem = new BindingItemsTest("RShoulder", "Right Bumper", tempRBAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("RShoulder", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("Back", out ButtonMapAction tempBackAct))
            {
                Trace.WriteLine($"{tempBackAct}");
                BindingItemsTest tempItem = new BindingItemsTest("Back", "Back", tempBackAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("Back", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("Start", out ButtonMapAction tempStartAct))
            {
                Trace.WriteLine($"{tempStartAct}");
                BindingItemsTest tempItem = new BindingItemsTest("Start", "Start", tempStartAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("Start", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("Steam", out ButtonMapAction tempSteamAct))
            {
                Trace.WriteLine($"{tempSteamAct}");
                BindingItemsTest tempItem = new BindingItemsTest("Steam", "Steam", tempSteamAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("Steam", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("LeftGrip", out ButtonMapAction tempLGripAct))
            {
                Trace.WriteLine($"{tempLGripAct}");
                BindingItemsTest tempItem = new BindingItemsTest("LeftGrip", "Left Grip", tempLGripAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("LeftGrip", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("RightGrip", out ButtonMapAction tempRGripAct))
            {
                Trace.WriteLine($"{tempRGripAct}");
                BindingItemsTest tempItem = new BindingItemsTest("RightGrip", "Right Grip", tempRGripAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("RightGrip", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("LeftPadClick", out ButtonMapAction tempLPadClickAct))
            {
                Trace.WriteLine($"{tempLPadClickAct}");
                BindingItemsTest tempItem = new BindingItemsTest("LeftPadClick", "Left Pad Click", tempLPadClickAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("LeftPadClick", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("RightPadClick", out ButtonMapAction tempRPadClickAct))
            {
                Trace.WriteLine($"{tempRPadClickAct}");
                BindingItemsTest tempItem = new BindingItemsTest("RightPadClick", "Right Pad Click", tempRPadClickAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("RightPadClick", tempBtnInd++);
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.buttonActionDict.
                TryGetValue("Stick", out ButtonMapAction tempStickAct))
            {
                Trace.WriteLine($"{tempStickAct}");
                BindingItemsTest tempItem = new BindingItemsTest("Stick", "Stick", tempStickAct);
                buttonBindings.Add(tempItem);
                buttonBindingsIndexDict.Add("Stick", tempBtnInd++);
            }


            if (tempProfile.CurrentActionSet.CurrentActionLayer.touchpadActionDict.
                TryGetValue("LeftTouchpad", out TouchpadMapAction tempTouchAct))
            {
                Trace.WriteLine($"{tempTouchAct}");
                touchpadBindings.Add(new TouchBindingItemsTest("LeftTouchpad", "Left Touchpad", tempTouchAct));
            }

            if (tempProfile.CurrentActionSet.CurrentActionLayer.touchpadActionDict.
                TryGetValue("RightTouchpad", out TouchpadMapAction tempRightTouchAct))
            {
                Trace.WriteLine($"{tempRightTouchAct}");
                touchpadBindings.Add(new TouchBindingItemsTest("RightTouchpad", "Right Touchpad", tempRightTouchAct));
            }
        }

        public void SwitchActionSets(int ind)
        {
            actionSetItems[selectedActionSetIndex].ItemActive = false;

            selectedActionSetIndex = ind;
            actionSetItems[ind].ItemActive = true;

            actionResetEvent.Reset();
            mapper.QueueEvent(() =>
            {
                mapper.ActionProfile.SwitchSets(ind, mapper);
                actionResetEvent.Set();
            });
        }

        public void SwitchActionLayer(int layerInd)
        {
            layerItems[selectedActionLayerIndex].ItemActive = false;

            selectedActionLayerIndex = layerInd;
            layerItems[layerInd].ItemActive = true;

            actionResetEvent.Reset();
            mapper.QueueEvent(() =>
            {
                mapper.ActionProfile.CurrentActionSet.SwitchActionLayer(mapper, layerInd);
                actionResetEvent.Set();
            });
        }

        public void TestFakeSave(ProfileEntity entity, Profile profile)
        {
            ProfileEntity tempEntity = entity;
            Profile tempProfile = profile;
            mapper.QueueEvent(() =>
            {
                ProfileSerializer profileSerializer = new ProfileSerializer(tempProfile);
                string tempOutJson = JsonConvert.SerializeObject(profileSerializer, Formatting.Indented,
                    new JsonSerializerSettings()
                    {
                        //Converters = new List<JsonConverter>()
                        //{
                        //    new MapActionSubTypeConverter(),
                        //}
                        //TypeNameHandling = TypeNameHandling.Objects
                        //ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                Trace.WriteLine(tempOutJson);

                actionResetEvent.Set();
            });
        }
    }

    public class ActionLayerItemsTest
    {
        private ActionSet set;
        public ActionSet Set => set;

        private ActionLayer layer;
        public ActionLayer Layer => layer;

        public string DisplayName
        {
            get
            {
                string result = $"Layer {layer.Index}";
                if (!string.IsNullOrEmpty(layer.Name))
                {
                    result = layer.Name;
                }

                return result;
            }
        }

        private int index;
        public int LayerIndex
        {
            get => index;
        }

        private bool itemActive;
        public bool ItemActive
        {
            get => itemActive;
            set
            {
                if (itemActive == value) return;
                itemActive = value;
                ItemActiveChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ItemActiveChanged;

        public ActionLayerItemsTest(ActionSet set, ActionLayer layer, int index)
        {
            this.set = set;
            this.layer = layer;
            this.index = index;
        }
    }

    public class ActionSetItemsTest
    {
        private ActionSet set;
        public ActionSet Set => set;

        public string DisplayName
        {
            get
            {
                string result = $"Set {set.Index}";
                if (!string.IsNullOrEmpty(set.Name))
                {
                    result = set.Name;
                }

                return result;
            }
        }

        public int SetIndex
        {
            get => set.Index;
        }

        private bool itemActive;
        public bool ItemActive
        {
            get => itemActive;
            set
            {
                if (itemActive == value) return;
                itemActive = value;
                ItemActiveChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ItemActiveChanged;

        public ActionSetItemsTest(ActionSet set)
        {
            this.set = set;
        }
    }

    public class TouchBindingItemsTest
    {
        private string displayInputMapString;
        public string DisplayInputMapString
        {
            get => displayInputMapString;
        }

        public string bindingName;
        public string BindingName
        {
            get => bindingName;
            //set => bindingName = value;
        }
        //public event EventHandler BindingNameChanged;

        private TouchpadMapAction mappedAction;
        public TouchpadMapAction MappedAction
        {
            get => mappedAction;
        }

        public string MappedActionType
        {
            get => mappedAction.ActionTypeName;
        }
        public event EventHandler MappedActionTypeChanged;

        public TouchBindingItemsTest(string bindingName, string displayInputMap,
            MapAction mappedAction)
        {
            this.bindingName = bindingName;
            this.displayInputMapString = displayInputMap;
            this.mappedAction = mappedAction as TouchpadMapAction;
        }

        public void UpdateAction(TouchpadMapAction action)
        {
            this.mappedAction = action;
            RaiseUIUpdate();
        }

        private void RaiseUIUpdate()
        {
            MappedActionTypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class BindingItemsTest
    {
        private string displayInputMapString;
        public string DisplayInputMapString
        {
            get => displayInputMapString;
        }

        public string bindingName;
        public string BindingName
        {
            get => bindingName;
            //set => bindingName = value;
        }
        //public event EventHandler BindingNameChanged;

        private ButtonMapAction mappedAction;
        public ButtonMapAction MappedAction
        {
            get => mappedAction;
        }

        public string MappedActionType
        {
            get => mappedAction.ActionTypeName;
        }
        public event EventHandler MappedActionTypeChanged;

        public BindingItemsTest(string bindingName, string displayInputMap, MapAction mappedAction)
        {
            this.bindingName = bindingName;
            this.displayInputMapString = displayInputMap;
            this.mappedAction = mappedAction as ButtonMapAction;
        }

        public void UpdateAction(MapAction action)
        {
            this.mappedAction = action as ButtonMapAction;
            RaiseUIUpdate();
        }

        private void RaiseUIUpdate()
        {
            MappedActionTypeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
