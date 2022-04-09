using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.StickActions;
using SteamControllerTest.GyroActions;
using static SteamControllerTest.MapperUtil.OutputActionData;
using SteamControllerTest.StickModifiers;
using SteamControllerTest.TouchpadActions;
using SteamControllerTest.TriggerActions;

namespace SteamControllerTest
{
    [JsonConverter(typeof(MapActionTypeConverter))]
    public class MapActionSerializer
    {
        protected ActionLayer tempLayer;
        [JsonIgnore]
        public ActionLayer TempLayer => tempLayer;

        protected MapAction mapAction =
            new ButtonNoAction();
        [JsonIgnore]
        public MapAction MapAction { get => mapAction; set => mapAction = value; }

        [JsonProperty(Required = Required.Always)]
        public int Id
        {
            get => mapAction.Id;
            set => mapAction.Id = value;
        }

        public string Name
        {
            get => mapAction.Name;
            set
            {
                mapAction.Name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string ActionMode
        {
            get => mapAction.ActionTypeName;
            set => mapAction.ActionTypeName = value;
        }

        public event EventHandler NameChanged;

        public MapActionSerializer()
        {
        }

        public MapActionSerializer(ActionLayer tempLayer, MapAction mapAction)
        {
            this.mapAction = mapAction;
            this.tempLayer = tempLayer;
        }

        public virtual void PopulateMap()
        {
        }

        [OnDeserializing]
        internal void OnDeserializingMethod(StreamingContext context)
        {
            Trace.WriteLine("IN MapActionSerializer.OnDeserializingMethod");
            ActionLayerSerializer.CurrentActionIndex++;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Trace.WriteLine("IN MapActionSerializer.OnDeserializedMethod");
        }
    }

    public class ButtonActionSerializer : MapActionSerializer
    {
        private ButtonAction buttonAction = new ButtonAction();
        [JsonIgnore]
        internal ButtonAction ButtonAction
        {
            get => buttonAction;
            set => buttonAction = value;
        }

        private List<ActionFuncSerializer> actionFuncSerializers =
            new List<ActionFuncSerializer>();
        [JsonProperty("Functions", Required = Required.Always)]
        //[JsonConverter(typeof(ActionFuncsListConverter))]
        public List<ActionFuncSerializer> ActionFuncSerializers
        {
            get => actionFuncSerializers;
            set
            {
                actionFuncSerializers = value;
                ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ActionFuncSerializersChanged;

        public ButtonActionSerializer() : base()
        {
            mapAction = buttonAction;

            NameChanged += ButtonActionSerializer_NameChanged;
            ActionFuncSerializersChanged += ButtonActionSerializer_ActionFuncSerializersChanged;
        }

        private void ButtonActionSerializer_ActionFuncSerializersChanged(object sender, EventArgs e)
        {
            //if (buttonAction.ParentAction != null &&
            //    actionFuncSerializers?.Count > 0)
            if (actionFuncSerializers?.Count > 0)
            {
                buttonAction.ChangedProperties.Add(ButtonAction.PropertyKeyStrings.FUNCTIONS);
            }
        }

        private void ButtonActionSerializer_NameChanged(object sender, EventArgs e)
        {
            buttonAction.ChangedProperties.Add(ButtonAction.PropertyKeyStrings.NAME);
        }

        public ButtonActionSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is ButtonAction temp)
            {
                buttonAction = temp;
                this.mapAction = buttonAction;
                PopulateFuncs();
            }
        }

        public void RaiseActionFuncSerializersChanged()
        {
            ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateFuncs()
        {
            if (!buttonAction.UseParentActions)
            {
                foreach (ActionFunc tempFunc in buttonAction.ActionFuncs)
                {
                    actionFuncSerializers.Add(new ActionFuncSerializer(tempFunc));
                }
            }
        }

        public override void PopulateMap()
        {
            buttonAction.ActionFuncs.Clear();
            foreach (ActionFuncSerializer serializer in actionFuncSerializers)
            {
                serializer.PopulateFunc();
                buttonAction.ActionFuncs.Add(serializer.ActionFunc);
            }
        }

        [OnDeserializing]
        internal void OnButtonActionDeserializingMethod(StreamingContext context)
        {
            Trace.WriteLine("IN ButtonActionSerializer.OnDeserializingMethod");
            ActionLayerSerializer.CurrentActionIndex++;
        }

        [OnDeserialized]
        internal void OnButtionActionDeserializedMethod(StreamingContext context)
        {
            Trace.WriteLine("IN ButtonActionSerializer.OnDeserializedMethod");
        }
    }

    public class DpadActionSerializer : MapActionSerializer
    {
        public class DPadPadDirBinding
        {
            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        public class DpadPadActionSettings
        {
            private DPadActions.DPadAction padAction;

            public DPadActions.DPadAction.DPadMode PadMode
            {
                get => padAction.CurrentMode;
                set
                {
                    padAction.CurrentMode = value;
                    PadModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public double DelayTime
            {
                get => padAction.DelayTime;
                set
                {
                    padAction.DelayTime = Math.Clamp(value, 0.0, 3600.0);
                    DelayTimeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DelayTimeChanged;

            public DpadPadActionSettings(DPadActions.DPadAction padAction)
            {
                this.padAction = padAction;
            }

            public event EventHandler PadModeChanged;
        }

        private DPadActions.DPadAction dPadAction = new DPadActions.DPadAction();

        private Dictionary<DPadActions.DpadDirections, DPadPadDirBinding> dictPadBindings =
            new Dictionary<DPadActions.DpadDirections, DPadPadDirBinding>();

        [JsonProperty("Bindings", Required = Required.Always)]
        public Dictionary<DPadActions.DpadDirections, DPadPadDirBinding> DictPadBindings
        {
            get => dictPadBindings;
            set => dictPadBindings = value;
        }

        private DpadPadActionSettings settings;
        public DpadPadActionSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public DpadActionSerializer() : base()
        {
            mapAction = dPadAction;
            settings = new DpadPadActionSettings(dPadAction);

            settings.PadModeChanged += Settings_PadModeChanged;
            settings.DelayTimeChanged += Settings_DelayTimeChanged;
        }

        private void Settings_DelayTimeChanged(object sender, EventArgs e)
        {
            dPadAction.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.DELAY_TIME);
        }

        private void Settings_PadModeChanged(object sender, EventArgs e)
        {
            //if (mapAction.ParentAction != null)
            {
                dPadAction.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_MODE);
                //mapAction.ChangedProperties.Add("PadMode");
            }
        }

        public DpadActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is DPadActions.DPadAction temp)
            {
                dPadAction = temp;
                mapAction = dPadAction;
                settings = new DpadPadActionSettings(dPadAction);
                PopulateFuncs();
            }
        }

        // Post-deserialize
        public override void PopulateMap()
        {
            foreach (ButtonAction dirButton in dPadAction.EventCodes4)
            {
                dirButton?.ActionFuncs.Clear();
            }

            //foreach (KeyValuePair<DPadActions.DpadDirections, List<ActionFuncSerializer>> tempKeyPair in dictPadBindings)
            foreach (KeyValuePair<DPadActions.DpadDirections, DPadPadDirBinding> tempKeyPair in dictPadBindings)
            {
                DPadActions.DpadDirections dir = tempKeyPair.Key;
                List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value.ActionFuncSerializers;
                //List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value;

                //ButtonAction tempDirButton = dPadAction.EventCodes4[(int)dir];
                //if (tempDirButton != null)
                {
                    //tempDirButton.Name = tempKeyPair.Value.ActionDirName;
                    ButtonAction dirButton = new ButtonAction();
                    dirButton.Name = tempKeyPair.Value.ActionDirName;
                    foreach (ActionFuncSerializer serializer in tempSerializers)
                    {
                        //ButtonAction dirButton = dPadAction.EventCodes4[(int)dir];
                        serializer.PopulateFunc();
                        dirButton.ActionFuncs.Add(serializer.ActionFunc);
                        //dPadAction.EventCodes4[(int)dir] = dirButton;
                    }

                    dPadAction.EventCodes4[(int)dir] = dirButton;
                    FlagBtnChangedDirection(dir, dPadAction);
                }
            }
        }

        // Pre-serialize
        private void PopulateFuncs()
        {
            List<ActionFuncSerializer> tempFuncs = new List<ActionFuncSerializer>();
            DPadActions.DpadDirections[] tempDirs = null;

            if (dPadAction.CurrentMode == DPadActions.DPadAction.DPadMode.Standard ||
                dPadAction.CurrentMode == DPadActions.DPadAction.DPadMode.FourWayCardinal)
            {
                tempDirs = new DPadActions.DpadDirections[4]
                {
                    DPadActions.DpadDirections.Up, DPadActions.DpadDirections.Down,
                    DPadActions.DpadDirections.Left, DPadActions.DpadDirections.Right
                };
            }
            else if (dPadAction.CurrentMode == DPadActions.DPadAction.DPadMode.EightWay)
            {
                tempDirs = new DPadActions.DpadDirections[8]
                {
                    DPadActions.DpadDirections.Up, DPadActions.DpadDirections.Down,
                    DPadActions.DpadDirections.Left, DPadActions.DpadDirections.Right,
                    DPadActions.DpadDirections.UpLeft, DPadActions.DpadDirections.UpRight,
                    DPadActions.DpadDirections.DownLeft, DPadActions.DpadDirections.DownRight
                };
            }
            else if (dPadAction.CurrentMode == DPadActions.DPadAction.DPadMode.FourWayDiagonal)
            {
                tempDirs = new DPadActions.DpadDirections[4]
                {
                    DPadActions.DpadDirections.UpLeft, DPadActions.DpadDirections.UpRight,
                    DPadActions.DpadDirections.DownLeft, DPadActions.DpadDirections.DownRight
                };
            }

            for (int i = 0; i < tempDirs.Length; i++)
            {
                DPadActions.DpadDirections tempDir = tempDirs[i];
                ButtonAction dirButton = dPadAction.EventCodes4[(int)tempDir];

                tempFuncs.Clear();
                foreach (ActionFunc tempFunc in dirButton.ActionFuncs)
                {
                    tempFuncs.Add(new ActionFuncSerializer(tempFunc));
                }

                //dictPadBindings.Add(tempDir, tempFuncs);
                dictPadBindings.Add(tempDir, new DPadPadDirBinding()
                {
                    ActionDirName = dirButton.Name,
                    ActionFuncSerializers = tempFuncs,
                });
            }
        }

        public void FlagBtnChangedDirection(DPadActions.DpadDirections dir,
            DPadActions.DPadAction action)
        {
            switch (dir)
            {
                case DPadActions.DpadDirections.Up:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_UP);
                    break;
                case DPadActions.DpadDirections.Down:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_DOWN);
                    break;
                case DPadActions.DpadDirections.Left:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_LEFT);
                    break;
                case DPadActions.DpadDirections.Right:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_RIGHT);
                    break;
                case DPadActions.DpadDirections.UpLeft:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_UPLEFT);
                    break;
                case DPadActions.DpadDirections.UpRight:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_UPRIGHT);
                    break;
                case DPadActions.DpadDirections.DownLeft:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_DOWNLEFT);
                    break;
                case DPadActions.DpadDirections.DownRight:
                    action.ChangedProperties.Add(DPadActions.DPadAction.PropertyKeyStrings.PAD_DIR_DOWNRIGHT);
                    break;
                default:
                    break;
            }
        }
    }

    public class ButtonNoActionSerializer : MapActionSerializer
    {
        private ButtonNoAction buttonNoAction = new ButtonNoAction();

        public ButtonNoActionSerializer() : base()
        {
            mapAction = buttonNoAction;
        }

        public ButtonNoActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is ButtonNoAction temp)
            {
                buttonNoAction = temp;
                mapAction = buttonNoAction;
            }
        }
    }

    public class DpadNoActionSerializer : MapActionSerializer
    {
        private DPadActions.DPadNoAction dpadNoAction = new DPadActions.DPadNoAction();

        public DpadNoActionSerializer() : base()
        {
            mapAction = dpadNoAction;
        }

        public DpadNoActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is DPadActions.DPadNoAction temp)
            {
                dpadNoAction = temp;
                mapAction = dpadNoAction;
            }
        }
    }

    public class DpadTranslateSerializer : MapActionSerializer
    {
        public class DpadTranslateSettings
        {
            private DPadActions.DPadTranslate dpadTransAct;

            public DPadActionCodes OutputDPad
            {
                get => dpadTransAct.OutputAction.DpadCode;
                set => dpadTransAct.OutputAction.DpadCode = value;
            }

            public DpadTranslateSettings(DPadActions.DPadTranslate action)
            {
                this.dpadTransAct = action;
            }
        }

        private DPadActions.DPadTranslate dpadTransAct =
            new DPadActions.DPadTranslate();

        private DpadTranslateSettings settings;
        public DpadTranslateSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public DpadTranslateSerializer() : base()
        {
            mapAction = dpadTransAct;
            settings = new DpadTranslateSettings(dpadTransAct);
        }

        // Serialize ctor
        public DpadTranslateSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is DPadActions.DPadTranslate temp)
            {
                dpadTransAct = temp;
                mapAction = dpadTransAct;
                settings = new DpadTranslateSettings(dpadTransAct);
            }
        }
    }

    public class TriggerTranslateActionSerializer : MapActionSerializer
    {
        public class TriggerTranslateSettings
        {
            private TriggerTranslate triggerAction;

            public JoypadActionCodes OutputTrigger
            {
                get => triggerAction.OutputData.JoypadCode;
                set
                {
                    triggerAction.OutputData.JoypadCode = value;
                    OutputTriggerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputTriggerChanged;

            public double DeadZone
            {
                get => triggerAction.DeadMod.DeadZone;
                set
                {
                    triggerAction.DeadMod.DeadZone = Math.Clamp(value, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public double MaxZone
            {
                get => triggerAction.DeadMod.MaxZone;
                set
                {
                    triggerAction.DeadMod.MaxZone = Math.Clamp(value, 0.0, 1.0);
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxZoneChanged;

            public double AntiDeadZone
            {
                get => triggerAction.DeadMod.AntiDeadZone;
                set
                {
                    triggerAction.DeadMod.AntiDeadZone = Math.Clamp(value, 0.0, 1.0);
                    AntiDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler AntiDeadZoneChanged;

            public TriggerTranslateSettings(TriggerTranslate action)
            {
                triggerAction = action;
            }
        }

        private TriggerTranslate triggerAction
            = new TriggerTranslate();

        private TriggerTranslateSettings settings;
        public TriggerTranslateSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public TriggerTranslateActionSerializer() : base()
        {
            mapAction = triggerAction;
            settings = new TriggerTranslateSettings(triggerAction);

            NameChanged += TriggerTranslateActionSerializer_NameChanged;
            settings.OutputTriggerChanged += Settings_OutputTriggerChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.MaxZoneChanged += Settings_MaxZoneChanged;
            settings.AntiDeadZoneChanged += Settings_AntiDeadZoneChanged;
        }

        private void TriggerTranslateActionSerializer_NameChanged(object sender, EventArgs e)
        {
            triggerAction.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.NAME);
        }

        private void Settings_AntiDeadZoneChanged(object sender, EventArgs e)
        {
            triggerAction.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.ANTIDEAD_ZONE);
        }

        private void Settings_MaxZoneChanged(object sender, EventArgs e)
        {
            triggerAction.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.MAX_ZONE);
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            triggerAction.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.DEAD_ZONE);
        }

        private void Settings_OutputTriggerChanged(object sender, EventArgs e)
        {
            triggerAction.ChangedProperties.Add(TriggerTranslate.PropertyKeyStrings.OUTPUT_TRIGGER);
        }

        // Serialize
        public TriggerTranslateActionSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is TriggerTranslate temp)
            {
                triggerAction = temp;
                this.mapAction = triggerAction;
                settings = new TriggerTranslateSettings(triggerAction);
            }
        }
    }

    public class TriggerDualStageActionSerializer : MapActionSerializer
    {
        public class TriggerDualStageSettings
        {
            private TriggerDualStageAction triggerDualAction;

            public double DeadZone
            {
                get => triggerDualAction.DeadMod.DeadZone;
                set
                {
                    triggerDualAction.DeadMod.DeadZone = Math.Clamp(value, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public double MaxZone
            {
                get => triggerDualAction.DeadMod.MaxZone;
                set
                {
                    triggerDualAction.DeadMod.MaxZone = Math.Clamp(value, 0.0, 1.0);
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxZoneChanged;

            public TriggerDualStageAction.DualStageMode DualStageMode
            {
                get => triggerDualAction.TriggerStateMode;
                set
                {
                    triggerDualAction.TriggerStateMode = value;
                    DualStageModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DualStageModeChanged;

            public int HipFireDelay
            {
                get => triggerDualAction.HipFireMS;
                set
                {
                    triggerDualAction.HipFireMS = value;
                    HipFireDelayChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler HipFireDelayChanged;

            public TriggerDualStageSettings(TriggerDualStageAction action)
            {
                triggerDualAction = action;
            }
        }

        public class StageButtonBinding
        {
            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        private TriggerDualStageAction triggerDualAction = new TriggerDualStageAction();


        private TriggerDualStageSettings settings;
        public TriggerDualStageSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        private StageButtonBinding softPullStageButton = new StageButtonBinding();
        public StageButtonBinding SoftPull
        {
            get => softPullStageButton;
            set
            {
                softPullStageButton = value;
                SoftPullChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler SoftPullChanged;

        private StageButtonBinding fullPullStageButton = new StageButtonBinding();
        public StageButtonBinding FullPull
        {
            get => fullPullStageButton;
            set
            {
                fullPullStageButton = value;
                FullPullChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler FullPullChanged;

        // Deserialize
        public TriggerDualStageActionSerializer() : base()
        {
            mapAction = triggerDualAction;
            settings = new TriggerDualStageSettings(triggerDualAction);

            NameChanged += TriggerDualStageActionSerializer_NameChanged;
            SoftPullChanged += TriggerDualStageActionSerializer_SoftPullChanged;
            FullPullChanged += TriggerDualStageActionSerializer_FullPullChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.MaxZoneChanged += Settings_MaxZoneChanged;
            settings.DualStageModeChanged += Settings_DualStageModeChanged;
            settings.HipFireDelayChanged += Settings_HipFireDelayChanged;
        }

        private void Settings_HipFireDelayChanged(object sender, EventArgs e)
        {
            triggerDualAction.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.HIPFIRE_DELAY);
        }

        private void Settings_DualStageModeChanged(object sender, EventArgs e)
        {
            triggerDualAction.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.DUALSTAGE_MODE);
        }

        private void TriggerDualStageActionSerializer_FullPullChanged(object sender, EventArgs e)
        {
            triggerDualAction.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.FULLPULL_BUTTON);
        }

        private void TriggerDualStageActionSerializer_SoftPullChanged(object sender, EventArgs e)
        {
            triggerDualAction.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.SOFTPULL_BUTTON);
        }

        private void Settings_MaxZoneChanged(object sender, EventArgs e)
        {
            triggerDualAction.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.MAX_ZONE);
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            triggerDualAction.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.DEAD_ZONE);
        }

        private void TriggerDualStageActionSerializer_NameChanged(object sender, EventArgs e)
        {
            triggerDualAction.ChangedProperties.Add(TriggerDualStageAction.PropertyKeyStrings.NAME);
        }

        // Pre-serialize ctor
        public TriggerDualStageActionSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is TriggerDualStageAction temp)
            {
                triggerDualAction = temp;
                //mapAction = triggerDualAction;
                settings = new TriggerDualStageSettings(triggerDualAction);

                softPullStageButton.ActionDirName = triggerDualAction.SoftPullActButton.Name;
                fullPullStageButton.ActionDirName = triggerDualAction.FullPullActButton.Name;

                PopulateFuncs();
            }
        }

        // Deserialize
        public override void PopulateMap()
        {
            triggerDualAction.SoftPullActButton.ActionFuncs.Clear();
            triggerDualAction.FullPullActButton.ActionFuncs.Clear();

            AxisDirButton tempButton = triggerDualAction.SoftPullActButton;
            foreach(ActionFuncSerializer serializer in softPullStageButton.ActionFuncSerializers)
            {
                serializer.PopulateFunc();
                tempButton.ActionFuncs.Add(serializer.ActionFunc);
            }
            tempButton.Name = softPullStageButton.ActionDirName;

            tempButton = triggerDualAction.FullPullActButton;
            foreach (ActionFuncSerializer serializer in fullPullStageButton.ActionFuncSerializers)
            {
                serializer.PopulateFunc();
                tempButton.ActionFuncs.Add(serializer.ActionFunc);
            }
            tempButton.Name = fullPullStageButton.ActionDirName;
        }

        public void PopulateFuncs()
        {
            List<ActionFuncSerializer> tempFuncs = new List<ActionFuncSerializer>();
            foreach(ActionFunc tempFunc in triggerDualAction.SoftPullActButton.ActionFuncs)
            {
                tempFuncs.Add(new ActionFuncSerializer(tempFunc));
            }
            softPullStageButton.ActionFuncSerializers.AddRange(tempFuncs);

            tempFuncs.Clear();

            foreach (ActionFunc tempFunc in triggerDualAction.FullPullActButton.ActionFuncs)
            {
                tempFuncs.Add(new ActionFuncSerializer(tempFunc));
            }

            fullPullStageButton.ActionFuncSerializers.AddRange(tempFuncs);
        }
    }

    public class TriggerButtonActionSerializer : MapActionSerializer
    {
        private TriggerButtonAction trigBtnAction = new TriggerButtonAction();

        private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
        [JsonProperty("Functions", Required = Required.Always)]
        public List<ActionFuncSerializer> ActionFuncSerializers
        {
            get => actionFuncSerializers;
            set
            {
                actionFuncSerializers = value;
                ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ActionFuncSerializersChanged;

        // Deserialize
        public TriggerButtonActionSerializer() : base()
        {
            mapAction = trigBtnAction;
        }

        // Serialize
        public TriggerButtonActionSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is TriggerButtonAction temp)
            {
                trigBtnAction = temp;
                this.mapAction = trigBtnAction;
                PopulateFuncs();
            }
        }

        public override void PopulateMap()
        {
            trigBtnAction.EventButton.ActionFuncs.Clear();
            foreach (ActionFuncSerializer serializer in actionFuncSerializers)
            {
                serializer.PopulateFunc();
                trigBtnAction.EventButton.ActionFuncs.Add(serializer.ActionFunc);
            }
        }

        private void PopulateFuncs()
        {
            //if (!buttonAction.UseParentActions)
            {
                foreach (ActionFunc tempFunc in trigBtnAction.EventButton.ActionFuncs)
                {
                    actionFuncSerializers.Add(new ActionFuncSerializer(tempFunc));
                }
            }
        }
    }

    public class TouchpadActionPadSerializer : MapActionSerializer
    {
        public class TouchPadDirBinding
        {
            //private StickPadAction.DpadDirections direction;
            //[JsonIgnore]
            //public StickPadAction.DpadDirections Direction
            //{
            //    get => direction;
            //}

            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        public class TouchpadActionPadSettings
        {
            private TouchpadActionPad touchActionPadAction;

            public double DeadZone
            {
                get => touchActionPadAction.DeadMod.DeadZone;
                set
                {
                    touchActionPadAction.DeadMod.DeadZone = value;
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public double MaxZone
            {
                get => touchActionPadAction.DeadMod.MaxZone;
                set
                {
                    touchActionPadAction.DeadMod.MaxZone = Math.Clamp(value, 0.0, 1.0);
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxZoneChanged;

            public TouchpadActionPad.DPadMode PadMode
            {
                get => touchActionPadAction.CurrentMode;
                set
                {
                    touchActionPadAction.CurrentMode = value;
                    PadModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler PadModeChanged;

            public int DiagonalRange
            {
                get => touchActionPadAction.DiagonalRange;
                set
                {
                    touchActionPadAction.DiagonalRange = Math.Clamp(value, -180, 180);
                    DiagonalRangeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DiagonalRangeChanged;

            public int Rotation
            {
                get => touchActionPadAction.Rotation;
                set
                {
                    touchActionPadAction.Rotation = value;
                    RotationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler RotationChanged;

            [JsonProperty("UseOuterRing")]
            public bool UseOuterRing
            {
                get => touchActionPadAction.UseRingButton;
                set
                {
                    touchActionPadAction.UseRingButton = value;
                    UseOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseOuterRingChanged;

            [JsonProperty("OuterRingDeadZone")]
            public double OuterRingDeadZone
            {
                get => touchActionPadAction.OuterRingDeadZone;
                set
                {
                    touchActionPadAction.OuterRingDeadZone = value;
                    OuterRingDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OuterRingDeadZoneChanged;

            [JsonProperty("UseAsOuterRing")]
            public bool UseAsOuterRing
            {
                get => touchActionPadAction.UseAsOuterRing;
                set
                {
                    touchActionPadAction.UseAsOuterRing = value;
                    UseAsOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseAsOuterRingChanged;

            public TouchpadActionPadSettings(TouchpadActionPad action)
            {
                touchActionPadAction = action;
            }
        }
        
        private Dictionary<TouchpadActionPad.DpadDirections, TouchPadDirBinding> dictPadBindings =
            new Dictionary<TouchpadActionPad.DpadDirections, TouchPadDirBinding>();

        [JsonProperty("Bindings", Required = Required.Always)]
        public Dictionary<TouchpadActionPad.DpadDirections, TouchPadDirBinding> DictPadBindings
        {
            get => dictPadBindings;
            set => dictPadBindings = value;
        }

        private TouchPadDirBinding ringBinding;

        [JsonProperty("OuterRingBinding")]
        public TouchPadDirBinding RingBinding
        {
            get => ringBinding;
            set
            {
                ringBinding = value;
                RingBindingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RingBindingChanged;

        private TouchpadActionPad touchActionPadAction = new TouchpadActionPad();

        private TouchpadActionPadSettings settings;
        public TouchpadActionPadSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public TouchpadActionPadSerializer() : base()
        {
            mapAction = touchActionPadAction;
            settings = new TouchpadActionPadSettings(touchActionPadAction);

            NameChanged += TouchpadActionPadSerializer_NameChanged;
            RingBindingChanged += TouchpadActionPadSerializer_RingBindingChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.MaxZoneChanged += Settings_MaxZoneChanged;
            settings.DiagonalRangeChanged += Settings_DiagonalRangeChanged;
            settings.PadModeChanged += Settings_PadModeChanged;
            settings.RotationChanged += Settings_RotationChanged;
            settings.UseOuterRingChanged += Settings_UseOuterRingChanged;
            settings.UseAsOuterRingChanged += Settings_UseAsOuterRingChanged;
            settings.OuterRingDeadZoneChanged += Settings_OuterRingDeadZoneChanged;
        }

        private void Settings_RotationChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.ROTATION);
        }

        private void Settings_MaxZoneChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.MAX_ZONE);
        }

        private void Settings_OuterRingDeadZoneChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.OUTER_RING_DEAD_ZONE);
        }

        private void Settings_UseAsOuterRingChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.USE_AS_OUTER_RING);
        }

        private void Settings_UseOuterRingChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.USE_OUTER_RING);
        }

        private void TouchpadActionPadSerializer_RingBindingChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.OUTER_RING_BUTTON);
        }

        private void Settings_PadModeChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_MODE);
        }

        private void Settings_DiagonalRangeChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.DIAGONAL_RANGE);
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.DEAD_ZONE);
        }

        private void TouchpadActionPadSerializer_NameChanged(object sender, EventArgs e)
        {
            touchActionPadAction.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.NAME);
        }

        // Pre-serialize
        public TouchpadActionPadSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is TouchpadActionPad temp)
            {
                touchActionPadAction = temp;
                this.mapAction = touchActionPadAction;
                settings = new TouchpadActionPadSettings(touchActionPadAction);
                PopulateFuncs();
            }
        }

        // Pre-serialize
        private void PopulateFuncs()
        {
            List<ActionFuncSerializer> tempFuncs = new List<ActionFuncSerializer>();
            TouchpadActionPad.DpadDirections[] tempDirs = null;

            if (touchActionPadAction.CurrentMode == TouchpadActionPad.DPadMode.Standard ||
                touchActionPadAction.CurrentMode == TouchpadActionPad.DPadMode.FourWayCardinal)
            {
                tempDirs = new TouchpadActionPad.DpadDirections[4]
                {
                    TouchpadActionPad.DpadDirections.Up, TouchpadActionPad.DpadDirections.Down,
                    TouchpadActionPad.DpadDirections.Left, TouchpadActionPad.DpadDirections.Right
                };
            }
            else if (touchActionPadAction.CurrentMode == TouchpadActionPad.DPadMode.EightWay)
            {
                tempDirs = new TouchpadActionPad.DpadDirections[8]
                {
                    TouchpadActionPad.DpadDirections.Up, TouchpadActionPad.DpadDirections.Down,
                    TouchpadActionPad.DpadDirections.Left, TouchpadActionPad.DpadDirections.Right,
                    TouchpadActionPad.DpadDirections.UpLeft, TouchpadActionPad.DpadDirections.UpRight,
                    TouchpadActionPad.DpadDirections.DownLeft, TouchpadActionPad.DpadDirections.DownRight
                };
            }
            else if (touchActionPadAction.CurrentMode == TouchpadActionPad.DPadMode.FourWayDiagonal)
            {
                tempDirs = new TouchpadActionPad.DpadDirections[4]
                {
                    TouchpadActionPad.DpadDirections.UpLeft, TouchpadActionPad.DpadDirections.UpRight,
                    TouchpadActionPad.DpadDirections.DownLeft, TouchpadActionPad.DpadDirections.DownRight
                };
            }

            for (int i = 0; i < tempDirs.Length; i++)
            {
                TouchpadActionPad.DpadDirections tempDir = tempDirs[i];
                ButtonAction dirButton = touchActionPadAction.EventCodes4[(int)tempDir];

                tempFuncs.Clear();
                foreach (ActionFunc tempFunc in dirButton.ActionFuncs)
                {
                    tempFuncs.Add(new ActionFuncSerializer(tempFunc));
                }

                //dictPadBindings.Add(tempDir, tempFuncs);
                dictPadBindings.Add(tempDir,
                    new TouchPadDirBinding()
                    {
                        ActionDirName = dirButton.Name,
                        ActionFuncSerializers = tempFuncs,
                    });
            }

            if (touchActionPadAction.RingButton != null)
            {
                ringBinding = new TouchPadDirBinding();
                ringBinding.ActionDirName = touchActionPadAction.RingButton.Name;
                foreach (ActionFunc tempFunc in touchActionPadAction.RingButton.ActionFuncs)
                {
                    ringBinding.ActionFuncSerializers.Add(new ActionFuncSerializer(tempFunc));
                }
            }
        }

        // Post-deserialize
        public override void PopulateMap()
        {
            foreach (ButtonAction dirButton in touchActionPadAction.EventCodes4)
            {
                dirButton.ActionFuncs.Clear();
            }

            foreach (KeyValuePair<TouchpadActionPad.DpadDirections, TouchPadDirBinding> tempKeyPair in dictPadBindings)
            //foreach(KeyValuePair<StickPadAction.DpadDirections, List<ActionFuncSerializer>> tempKeyPair in dictPadBindings)
            //foreach(DictionaryEntry entry in dictPadBindings)
            {
                TouchpadActionPad.DpadDirections dir = tempKeyPair.Key;
                List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value.ActionFuncSerializers;
                //List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value;
                //StickPadAction.DpadDirections dir = (StickPadAction.DpadDirections)entry.Key;
                //List<ActionFuncSerializer> tempSerializers = entry.Value as List<ActionFuncSerializer>;

                ButtonAction tempDirButton = null;
                //foreach (AxisDirButton dirButton in stickPadAct.EventCodes4)
                {
                    tempDirButton = touchActionPadAction.EventCodes4[(int)dir];
                    if (tempDirButton != null)
                    {
                        tempDirButton.Name = tempKeyPair.Value.ActionDirName;
                    }
                }

                if (tempDirButton != null)
                {
                    foreach (ActionFuncSerializer serializer in tempSerializers)
                    {
                        serializer.PopulateFunc();
                        tempDirButton.ActionFuncs.Add(serializer.ActionFunc);
                    }

                    FlagBtnChangedDirection(dir, touchActionPadAction);
                    tempDirButton = null;
                }
            }

            if (ringBinding != null)
            {
                touchActionPadAction.RingButton.Name = ringBinding.ActionDirName;
                List<ActionFuncSerializer> tempSerializers = ringBinding.ActionFuncSerializers;
                foreach (ActionFuncSerializer serializer in tempSerializers)
                {
                    serializer.PopulateFunc();
                    touchActionPadAction.RingButton.ActionFuncs.Add(serializer.ActionFunc);
                }

                touchActionPadAction.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.OUTER_RING_BUTTON);
                //stickPadAct.RingButton.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.OUTER_RING_BUTTON);
            }
        }

        public void FlagBtnChangedDirection(TouchpadActionPad.DpadDirections dir,
            TouchpadActionPad action)
        {
            switch (dir)
            {
                case TouchpadActionPad.DpadDirections.Up:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_UP);
                    break;
                case TouchpadActionPad.DpadDirections.Down:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_DOWN);
                    break;
                case TouchpadActionPad.DpadDirections.Left:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_LEFT);
                    break;
                case TouchpadActionPad.DpadDirections.Right:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_RIGHT);
                    break;
                case TouchpadActionPad.DpadDirections.UpLeft:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_UPLEFT);
                    break;
                case TouchpadActionPad.DpadDirections.UpRight:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_UPRIGHT);
                    break;
                case TouchpadActionPad.DpadDirections.DownLeft:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_DOWNLEFT);
                    break;
                case TouchpadActionPad.DpadDirections.DownRight:
                    action.ChangedProperties.Add(TouchpadActionPad.PropertyKeyStrings.PAD_DIR_DOWNRIGHT);
                    break;
                default:
                    break;
            }
        }
    }

    public class TouchpadMouseSerializer : MapActionSerializer
    {
        private TouchpadMouse touchMouseAction = new TouchpadMouse();

        // Deserialize
        public TouchpadMouseSerializer() : base()
        {
            mapAction = touchMouseAction;
            //settings = new TouchStickActionSettings(touchMouseAction);
        }

        // Pre-serialize
        public TouchpadMouseSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is TouchpadMouse temp)
            {
                touchMouseAction = temp;
                this.mapAction = touchMouseAction;
                //settings = new TouchStickActionSettings(touchMouseAction);
            }
        }
    }

    public class TouchpadMouseJoystickSerializer : MapActionSerializer
    {
        public class TouchpadMouseJoystickSettings
        {
            private TouchpadMouseJoystick touchMouseJoyAction;

            public StickActionCodes OutputStick
            {
                get => touchMouseJoyAction.OutputAction.StickCode;
                set
                {
                    touchMouseJoyAction.OutputAction.StickCode = value;
                    OutputStickChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputStickChanged;

            public TouchpadMouseJoystickSettings(TouchpadMouseJoystick action)
            {
                touchMouseJoyAction = action;
            }
        }

        private TouchpadMouseJoystick touchMouseJoyAction = new TouchpadMouseJoystick();

        private TouchpadMouseJoystickSettings settings;
        public TouchpadMouseJoystickSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public TouchpadMouseJoystickSerializer() : base()
        {
            mapAction = touchMouseJoyAction;
            settings = new TouchpadMouseJoystickSettings(touchMouseJoyAction);

            NameChanged += TouchpadMouseJoystickSerializer_NameChanged;
            settings.OutputStickChanged += Settings_OutputStickChanged;
        }

        private void TouchpadMouseJoystickSerializer_NameChanged(object sender, EventArgs e)
        {
            touchMouseJoyAction.ChangedProperties.Add(TouchpadMouseJoystick.PropertyKeyStrings.NAME);
        }

        private void Settings_OutputStickChanged(object sender, EventArgs e)
        {
            touchMouseJoyAction.ChangedProperties.Add(TouchpadMouseJoystick.PropertyKeyStrings.OUTPUT_STICK);
        }

        // Pre-serialize
        public TouchpadMouseJoystickSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is TouchpadMouseJoystick temp)
            {
                touchMouseJoyAction = temp;
                this.mapAction = touchMouseJoyAction;
                settings = new TouchpadMouseJoystickSettings(touchMouseJoyAction);
            }
        }
    }

    public class TouchpadStickActionSerializer : MapActionSerializer
    {
        public class TouchStickActionSettings
        {
            private TouchpadStickAction touchStickAction;

            public StickActionCodes OutputStick
            {
                get => touchStickAction.OutputAction.StickCode;
                set
                {
                    touchStickAction.OutputAction.StickCode = value;
                    OutputStickChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputStickChanged;

            public double DeadZone
            {
                get => touchStickAction.DeadMod.DeadZone;
                set
                {
                    touchStickAction.DeadMod.DeadZone = Math.Clamp(value, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public double MaxZone
            {
                get => touchStickAction.DeadMod.MaxZone;
                set
                {
                    touchStickAction.DeadMod.MaxZone = Math.Clamp(value, 0.0, 1.0);
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxZoneChanged;

            public double AntiDeadZone
            {
                get => touchStickAction.DeadMod.AntiDeadZone;
                set
                {
                    touchStickAction.DeadMod.AntiDeadZone = Math.Clamp(value, 0.0, 1.0);
                    AntiDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler AntiDeadZoneChanged;

            public TouchStickActionSettings(TouchpadStickAction action)
            {
                this.touchStickAction = action;
            }
        }

        private TouchpadStickAction touchStickAction = new TouchpadStickAction();

        private TouchStickActionSettings settings;
        public TouchStickActionSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public TouchpadStickActionSerializer() : base()
        {
            mapAction = touchStickAction;
            settings = new TouchStickActionSettings(touchStickAction);

            NameChanged += TouchpadStickActionSerializer_NameChanged;
            settings.OutputStickChanged += Settings_OutputStickChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.MaxZoneChanged += Settings_MaxZoneChanged;
            settings.AntiDeadZoneChanged += Settings_AntiDeadZoneChanged;
        }

        private void TouchpadStickActionSerializer_NameChanged(object sender, EventArgs e)
        {
            touchStickAction.ChangedProperties.Add(TouchpadStickAction.PropertyKeyStrings.NAME);
        }

        // Pre-serialize
        public TouchpadStickActionSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is TouchpadStickAction temp)
            {
                touchStickAction = temp;
                this.mapAction = touchStickAction;
                settings = new TouchStickActionSettings(touchStickAction);
            }
        }

        private void Settings_AntiDeadZoneChanged(object sender, EventArgs e)
        {
            touchStickAction.ChangedProperties.Add(TouchpadStickAction.PropertyKeyStrings.ANTIDEAD_ZONE);
        }

        private void Settings_MaxZoneChanged(object sender, EventArgs e)
        {
            touchStickAction.ChangedProperties.Add(TouchpadStickAction.PropertyKeyStrings.MAX_ZONE);
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            touchStickAction.ChangedProperties.Add(TouchpadStickAction.PropertyKeyStrings.DEAD_ZONE);
        }

        private void Settings_OutputStickChanged(object sender, EventArgs e)
        {
            touchStickAction.ChangedProperties.Add(TouchpadStickAction.PropertyKeyStrings.OUTPUT_STICK);
        }
    }

    public class TouchpadAbsActionSerializer : MapActionSerializer
    {
        public class OuterRingBinding
        {
            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        public class TouchpadAbsActionSettings
        {
            private TouchpadAbsAction touchAbsAct;

            public double DeadZone
            {
                get => touchAbsAct.DeadMod.DeadZone;
                set
                {
                    touchAbsAct.DeadMod.DeadZone = Math.Clamp(value, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            [JsonProperty("UseOuterRing")]
            public bool UseOuterRing
            {
                get => touchAbsAct.UseRingButton;
                set
                {
                    touchAbsAct.UseRingButton = value;
                    UseOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseOuterRingChanged;

            [JsonProperty("OuterRingDeadZone")]
            public double OuterRingDeadZone
            {
                get => touchAbsAct.OuterRingDeadZone;
                set
                {
                    touchAbsAct.OuterRingDeadZone = value;
                    OuterRingDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OuterRingDeadZoneChanged;

            [JsonProperty("UseAsOuterRing")]
            public bool UseAsOuterRing
            {
                get => touchAbsAct.UseAsOuterRing;
                set
                {
                    touchAbsAct.UseAsOuterRing = value;
                    UseAsOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseAsOuterRingChanged;

            public TouchpadAbsActionSettings(TouchpadAbsAction action)
            {
                touchAbsAct = action;
            }
        }

        private OuterRingBinding ringBinding;

        [JsonProperty("OuterRingBinding")]
        public OuterRingBinding RingBinding
        {
            get => ringBinding;
            set
            {
                ringBinding = value;
                RingBindingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RingBindingChanged;

        private TouchpadAbsActionSettings settings;
        public TouchpadAbsActionSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        private TouchpadAbsAction touchAbsAct = new TouchpadAbsAction();

        // Deserialize
        public TouchpadAbsActionSerializer() : base()
        {
            mapAction = touchAbsAct;
            settings = new TouchpadAbsActionSettings(touchAbsAct);

            NameChanged += TouchpadAbsActionSerializer_NameChanged;
            RingBindingChanged += TouchpadAbsActionSerializer_RingBindingChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.UseAsOuterRingChanged += Settings_UseAsOuterRingChanged;
            settings.UseOuterRingChanged += Settings_UseOuterRingChanged;
            settings.OuterRingDeadZoneChanged += Settings_OuterRingDeadZoneChanged;
        }

        // Serialize ctor
        public TouchpadAbsActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is TouchpadAbsAction temp)
            {
                touchAbsAct = temp;
                mapAction = touchAbsAct;
                settings = new TouchpadAbsActionSettings(touchAbsAct);
            }
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            touchAbsAct.ChangedProperties.Add(TouchpadAbsAction.PropertyKeyStrings.DEAD_ZONE);
        }

        private void TouchpadAbsActionSerializer_NameChanged(object sender, EventArgs e)
        {
            touchAbsAct.ChangedProperties.Add(TouchpadAbsAction.PropertyKeyStrings.NAME);
        }

        private void Settings_OuterRingDeadZoneChanged(object sender, EventArgs e)
        {
            touchAbsAct.ChangedProperties.Add(TouchpadAbsAction.PropertyKeyStrings.OUTER_RING_DEAD_ZONE);
        }

        private void Settings_UseOuterRingChanged(object sender, EventArgs e)
        {
            touchAbsAct.ChangedProperties.Add(TouchpadAbsAction.PropertyKeyStrings.USE_OUTER_RING);
        }

        private void Settings_UseAsOuterRingChanged(object sender, EventArgs e)
        {
            touchAbsAct.ChangedProperties.Add(TouchpadAbsAction.PropertyKeyStrings.USE_AS_OUTER_RING);
        }

        private void TouchpadAbsActionSerializer_RingBindingChanged(object sender, EventArgs e)
        {
            touchAbsAct.ChangedProperties.Add(TouchpadAbsAction.PropertyKeyStrings.OUTER_RING_BUTTON);
        }
    }

    public class AxisDirButtonSerializer : MapActionSerializer
    {
        private ButtonActions.AxisDirButton axisDirButton =
            new ButtonActions.AxisDirButton();

        private List<ActionFuncSerializer> actionFuncSerializers =
           new List<ActionFuncSerializer>();
        [JsonProperty("Functions", Required = Required.Always)]
        public List<ActionFuncSerializer> ActionFuncSerializers
        {
            get => actionFuncSerializers;
            set => actionFuncSerializers = value;
        }

        public AxisDirButtonSerializer() : base()
        {
            mapAction = axisDirButton;
        }

        public AxisDirButtonSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is ButtonActions.AxisDirButton temp)
            {
                axisDirButton = temp;
                this.mapAction = axisDirButton;
                PopulateFuncs();
            }
        }

        private void PopulateFuncs()
        {
            foreach (ActionFunc tempFunc in axisDirButton.ActionFuncs)
            {
                actionFuncSerializers.Add(new ActionFuncSerializer(tempFunc));
            }
        }

        public override void PopulateMap()
        {
            axisDirButton.ActionFuncs.Clear();
            foreach (ActionFuncSerializer serializer in actionFuncSerializers)
            {
                serializer.PopulateFunc();
                axisDirButton.ActionFuncs.Add(serializer.ActionFunc);
            }
        }
    }

    public class StickNoActionSerializer : MapActionSerializer
    {
        private StickNoAction stickNoAction = new StickNoAction();

        public StickNoActionSerializer() : base()
        {
            mapAction = stickNoAction;
        }

        public StickNoActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is StickNoAction temp)
            {
                stickNoAction = temp;
                mapAction = stickNoAction;
            }
        }
    }

    public class StickPadActionSerializer : MapActionSerializer
    {
        public class StickPadDirBinding
        {
            //private StickPadAction.DpadDirections direction;
            //[JsonIgnore]
            //public StickPadAction.DpadDirections Direction
            //{
            //    get => direction;
            //}

            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        public class StickPadActionSettings
        {
            private StickPadAction padAction;

            public double DeadZone
            {
                get => padAction.DeadMod.DeadZone;
                set
                {
                    padAction.DeadMod.DeadZone = Math.Clamp(value, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public StickPadAction.DPadMode PadMode
            {
                get => padAction.CurrentMode;
                set
                {
                    padAction.CurrentMode = value;
                    PadModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler PadModeChanged;

            public StickDeadZone.DeadZoneTypes DeadZoneType
            {
                get => padAction.DeadMod.DeadZoneType;
                set
                {
                    padAction.DeadMod.DeadZoneType = value;
                    DeadZoneTypeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneTypeChanged;

            [JsonProperty("UseOuterRing")]
            public bool UseOuterRing
            {
                get => padAction.UseRingButton;
                set
                {
                    padAction.UseRingButton = value;
                    UseOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseOuterRingChanged;

            [JsonProperty("OuterRingDeadZone")]
            public double OuterRingDeadZone
            {
                get => padAction.OuterRingDeadZone;
                set
                {
                    padAction.OuterRingDeadZone = value;
                    OuterRingDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OuterRingDeadZoneChanged;

            [JsonProperty("UseAsOuterRing")]
            public bool UseAsOuterRing
            {
                get => padAction.UseAsOuterRing;
                set
                {
                    padAction.UseAsOuterRing = value;
                    UseAsOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseAsOuterRingChanged;

            public int Rotation
            {
                get => padAction.Rotation;
                set
                {
                    padAction.Rotation = value;
                    RotationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler RotationChanged;

            public int DiagonalRange
            {
                get => padAction.DiagonalRange;
                set
                {
                    padAction.DiagonalRange = value;
                    DiagonalRangeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DiagonalRangeChanged;

            public StickPadActionSettings(StickPadAction padAction)
            {
                this.padAction = padAction;
            }
        }

        private StickPadAction stickPadAct =
            new StickPadAction();

        private Dictionary<StickPadAction.DpadDirections, StickPadDirBinding> dictPadBindings =
            new Dictionary<StickPadAction.DpadDirections, StickPadDirBinding>();

        [JsonProperty("Bindings", Required = Required.Always)]
        public Dictionary<StickPadAction.DpadDirections, StickPadDirBinding> DictPadBindings
        {
            get => dictPadBindings;
            set => dictPadBindings = value;
        }

        private StickPadDirBinding ringBinding;

        [JsonProperty("OuterRingBinding")]
        public StickPadDirBinding RingBinding
        {
            get => ringBinding;
            set
            {
                ringBinding = value;
                RingBindingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RingBindingChanged;

        //private List<StickPadBindings> padBindings =
        //    new List<StickPadBindings>()
        //    {
        //        new StickPadBindings(StickPadAction.DpadDirections.Centered),
        //        new StickPadBindings(StickPadAction.DpadDirections.Up),
        //        new StickPadBindings(StickPadAction.DpadDirections.Right),
        //        new StickPadBindings(StickPadAction.DpadDirections.UpRight),
        //        new StickPadBindings(StickPadAction.DpadDirections.Down),
        //        new StickPadBindings(StickPadAction.DpadDirections.Centered),
        //        new StickPadBindings(StickPadAction.DpadDirections.DownRight),
        //        new StickPadBindings(StickPadAction.DpadDirections.Centered),
        //        new StickPadBindings(StickPadAction.DpadDirections.Left),
        //        new StickPadBindings(StickPadAction.DpadDirections.UpLeft),
        //        new StickPadBindings(StickPadAction.DpadDirections.Centered),
        //        new StickPadBindings(StickPadAction.DpadDirections.Centered),
        //        new StickPadBindings(StickPadAction.DpadDirections.DownLeft),
        //    };
        //[JsonProperty("Bindings")]
        //public List<StickPadBindings> PadBindings
        //{
        //    get => padBindings;
        //    set => padBindings = value;
        //}

        //private List<AxisDirButtonSerializer> axisDirButtons =
        //    new List<AxisDirButtonSerializer>()
        //    {
        //        new AxisDirButtonSerializer(), new AxisDirButtonSerializer(),
        //        new AxisDirButtonSerializer(), new AxisDirButtonSerializer(),
        //        new AxisDirButtonSerializer(), new AxisDirButtonSerializer(),
        //        new AxisDirButtonSerializer(), new AxisDirButtonSerializer(),
        //        new AxisDirButtonSerializer(), new AxisDirButtonSerializer(),
        //        new AxisDirButtonSerializer(), new AxisDirButtonSerializer(),
        //        new AxisDirButtonSerializer()
        //    };

        //[JsonProperty("Up")]
        //public AxisDirButtonSerializer UpButton
        //{
        //    get => axisDirButtons[(int)StickPadAction.DpadDirections.Up];
        //}

        private StickPadActionSettings settings;
        public StickPadActionSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public StickPadActionSerializer() : base()
        {
            mapAction = stickPadAct;
            settings = new StickPadActionSettings(stickPadAct);

            NameChanged += StickPadActionSerializer_NameChanged;
            RingBindingChanged += StickPadActionSerializer_RingBindingChanged;
            settings.PadModeChanged += Settings_PadModeChanged;
            settings.DeadZoneTypeChanged += Settings_DeadZoneTypeChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.UseOuterRingChanged += Settings_UseOuterRingChanged;
            settings.OuterRingDeadZoneChanged += Settings_OuterRingDeadZoneChanged;
            settings.UseAsOuterRingChanged += Settings_UseAsOuterRingChanged;
            settings.RotationChanged += Settings_RotationChanged;
            settings.DiagonalRangeChanged += Settings_DiagonalRangeChanged;
        }

        private void Settings_DeadZoneTypeChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.DEAD_ZONE_TYPE);
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.DEAD_ZONE);
        }

        private void Settings_DiagonalRangeChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.DIAGONAL_RANGE);
        }

        private void Settings_RotationChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.ROTATION);
        }

        // Pre-serialize
        public StickPadActionSerializer(ActionLayer tempLayer, MapAction mapAction) :
            base(tempLayer, mapAction)
        {
            if (mapAction is StickPadAction temp)
            {
                stickPadAct = temp;
                this.mapAction = stickPadAct;
                settings = new StickPadActionSettings(stickPadAct);
                PopulateFuncs();
            }
        }

        private void StickPadActionSerializer_NameChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.NAME);
        }

        private void Settings_UseAsOuterRingChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.USE_AS_OUTER_RING);
        }

        private void Settings_OuterRingDeadZoneChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.OUTER_RING_DEAD_ZONE);
        }

        private void Settings_UseOuterRingChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.USE_OUTER_RING);
        }

        private void StickPadActionSerializer_RingBindingChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.OUTER_RING_BUTTON);
        }

        private void Settings_PadModeChanged(object sender, EventArgs e)
        {
            stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_MODE);
        }

        // Pre-serialize
        private void PopulateFuncs()
        {
            List<ActionFuncSerializer> tempFuncs = new List<ActionFuncSerializer>();
            StickPadAction.DpadDirections[] tempDirs = null;

            if (stickPadAct.CurrentMode == StickPadAction.DPadMode.Standard ||
                stickPadAct.CurrentMode == StickPadAction.DPadMode.FourWayCardinal)
            {
                tempDirs = new StickPadAction.DpadDirections[4]
                {
                    StickPadAction.DpadDirections.Up, StickPadAction.DpadDirections.Down,
                    StickPadAction.DpadDirections.Left, StickPadAction.DpadDirections.Right
                };
            }
            else if (stickPadAct.CurrentMode == StickPadAction.DPadMode.EightWay)
            {
                tempDirs = new StickPadAction.DpadDirections[8]
                {
                    StickPadAction.DpadDirections.Up, StickPadAction.DpadDirections.Down,
                    StickPadAction.DpadDirections.Left, StickPadAction.DpadDirections.Right,
                    StickPadAction.DpadDirections.UpLeft, StickPadAction.DpadDirections.UpRight,
                    StickPadAction.DpadDirections.DownLeft, StickPadAction.DpadDirections.DownRight
                };
            }
            else if (stickPadAct.CurrentMode == StickPadAction.DPadMode.FourWayDiagonal)
            {
                tempDirs = new StickPadAction.DpadDirections[4]
                {
                    StickPadAction.DpadDirections.UpLeft, StickPadAction.DpadDirections.UpRight,
                    StickPadAction.DpadDirections.DownLeft, StickPadAction.DpadDirections.DownRight
                };
            }

            for (int i = 0; i < tempDirs.Length; i++)
            {
                StickPadAction.DpadDirections tempDir = tempDirs[i];
                AxisDirButton dirButton = stickPadAct.EventCodes4[(int)tempDir];

                tempFuncs.Clear();
                foreach (ActionFunc tempFunc in dirButton.ActionFuncs)
                {
                    tempFuncs.Add(new ActionFuncSerializer(tempFunc));
                }

                //dictPadBindings.Add(tempDir, tempFuncs);
                dictPadBindings.Add(tempDir,
                    new StickPadDirBinding()
                    {
                        ActionDirName = dirButton.Name,
                        ActionFuncSerializers = tempFuncs,
                    });
            }

            if (stickPadAct.RingButton != null)
            {
                ringBinding = new StickPadDirBinding();
                ringBinding.ActionDirName = stickPadAct.RingButton.Name;
                foreach (ActionFunc tempFunc in stickPadAct.RingButton.ActionFuncs)
                {
                    ringBinding.ActionFuncSerializers.Add(new ActionFuncSerializer(tempFunc));
                }
            }
        }

        // Post-deserialize
        public override void PopulateMap()
        {
            foreach(AxisDirButton dirButton in stickPadAct.EventCodes4)
            {
                dirButton.ActionFuncs.Clear();
            }

            foreach (KeyValuePair<StickPadAction.DpadDirections, StickPadDirBinding> tempKeyPair in dictPadBindings)
            //foreach(KeyValuePair<StickPadAction.DpadDirections, List<ActionFuncSerializer>> tempKeyPair in dictPadBindings)
            //foreach(DictionaryEntry entry in dictPadBindings)
            {
                StickPadAction.DpadDirections dir = tempKeyPair.Key;
                List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value.ActionFuncSerializers;
                //List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value;
                //StickPadAction.DpadDirections dir = (StickPadAction.DpadDirections)entry.Key;
                //List<ActionFuncSerializer> tempSerializers = entry.Value as List<ActionFuncSerializer>;

                AxisDirButton tempDirButton = null;
                //foreach (AxisDirButton dirButton in stickPadAct.EventCodes4)
                {
                    tempDirButton = stickPadAct.EventCodes4[(int)dir];
                    if (tempDirButton != null)
                    {
                        tempDirButton.Name = tempKeyPair.Value.ActionDirName;
                    }
                }

                if (tempDirButton != null)
                {
                    foreach (ActionFuncSerializer serializer in tempSerializers)
                    {
                        serializer.PopulateFunc();
                        tempDirButton.ActionFuncs.Add(serializer.ActionFunc);
                    }

                    FlagBtnChangedDirection(dir, stickPadAct);
                    tempDirButton = null;
                }
            }

            if (ringBinding != null)
            {
                stickPadAct.RingButton.Name = ringBinding.ActionDirName;
                List<ActionFuncSerializer> tempSerializers = ringBinding.ActionFuncSerializers;
                foreach (ActionFuncSerializer serializer in tempSerializers)
                {
                    serializer.PopulateFunc();
                    stickPadAct.RingButton.ActionFuncs.Add(serializer.ActionFunc);
                }

                stickPadAct.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.OUTER_RING_BUTTON);
                //stickPadAct.RingButton.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.OUTER_RING_BUTTON);
            }
        }

        public void FlagBtnChangedDirection(StickPadAction.DpadDirections dir,
            StickPadAction action)
        {
            switch(dir)
            {
                case StickPadAction.DpadDirections.Up:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_UP);
                    break;
                case StickPadAction.DpadDirections.Down:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_DOWN);
                    break;
                case StickPadAction.DpadDirections.Left:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_LEFT);
                    break;
                case StickPadAction.DpadDirections.Right:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_RIGHT);
                    break;
                case StickPadAction.DpadDirections.UpLeft:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_UPLEFT);
                    break;
                case StickPadAction.DpadDirections.UpRight:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_UPRIGHT);
                    break;
                case StickPadAction.DpadDirections.DownLeft:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_DOWNLEFT);
                    break;
                case StickPadAction.DpadDirections.DownRight:
                    action.ChangedProperties.Add(StickPadAction.PropertyKeyStrings.PAD_DIR_DOWNRIGHT);
                    break;
                default:
                    break;
            }
        }
    }

    public class StickMouseSerializer : MapActionSerializer
    {
        public class StickMouseSettings
        {
            private StickMouse stickMouseAction;

            public int MouseSpeed
            {
                get => stickMouseAction.MouseSpeed;
                set
                {
                    stickMouseAction.MouseSpeed = value;
                    MouseSpeedChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public event EventHandler MouseSpeedChanged;

            public double DeadZone
            {
                get => stickMouseAction.DeadMod.DeadZone;
                set
                {
                    stickMouseAction.DeadMod.DeadZone = value;
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public double MaxZone
            {
                get => stickMouseAction.DeadMod.MaxZone;
                set
                {
                    stickMouseAction.DeadMod.MaxZone = value;
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxZoneChanged;

            [JsonConverter(typeof(SafeStringEnumConverter),
                StickOutCurve.Curve.Linear)]
            public StickOutCurve.Curve OutputCurve
            {
                get => stickMouseAction.OutputCurve;
                set
                {
                    stickMouseAction.OutputCurve = value;
                    OutputCurveChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputCurveChanged;

            public StickMouse.DeltaAccelSettings DeltaSettings
            {
                get => stickMouseAction.MouseDeltaSettings;
                set
                {
                    stickMouseAction.MouseDeltaSettings = value;
                    DeltaSettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeltaSettingsChanged;

            public StickMouseSettings(StickMouse mouseAction)
            {
                stickMouseAction = mouseAction;
            }
        }

        private StickMouse stickMouseAction =
            new StickMouse();

        private StickMouseSettings settings;
        public StickMouseSettings Settings { get => settings; set => settings = value; }

        public StickMouseSerializer() : base()
        {
            mapAction = stickMouseAction;
            settings = new StickMouseSettings(stickMouseAction);

            NameChanged += StickMouseSerializer_NameChanged;
            settings.MouseSpeedChanged += Settings_MouseSpeedChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.MaxZoneChanged += Settings_MaxZoneChanged;
            settings.OutputCurveChanged += Settings_OutputCurveChanged;
            settings.DeltaSettingsChanged += Settings_DeltaSettingsChanged;
        }

        public StickMouseSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is StickMouse temp)
            {
                stickMouseAction = temp;
                mapAction = stickMouseAction;
                settings = new StickMouseSettings(stickMouseAction);
            }
        }

        private void Settings_DeltaSettingsChanged(object sender, EventArgs e)
        {
            stickMouseAction.ChangedProperties.Add(StickMouse.PropertyKeyStrings.DELTA_SETTINGS);
        }

        private void Settings_OutputCurveChanged(object sender, EventArgs e)
        {
            stickMouseAction.ChangedProperties.Add(StickMouse.PropertyKeyStrings.OUTPUT_CURVE);
        }

        private void Settings_MaxZoneChanged(object sender, EventArgs e)
        {
            stickMouseAction.ChangedProperties.Add(StickMouse.PropertyKeyStrings.MAX_ZONE);
        }

        private void StickMouseSerializer_NameChanged(object sender, EventArgs e)
        {
            stickMouseAction.ChangedProperties.Add(StickMouse.PropertyKeyStrings.NAME);
        }

        private void Settings_MouseSpeedChanged(object sender, EventArgs e)
        {
            stickMouseAction.ChangedProperties.Add(StickMouse.PropertyKeyStrings.MOUSE_SPEED);
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            stickMouseAction.ChangedProperties.Add(StickMouse.PropertyKeyStrings.DEAD_ZONE);
        }
    }

    public class StickTranslateSerializer : MapActionSerializer
    {
        public class StickTranslateSettings
        {
            private StickTranslate stickTransAct;

            public StickActionCodes OutputStick
            {
                get => stickTransAct.OutputAction.StickCode;
                set
                {
                    stickTransAct.OutputAction.StickCode = value;
                    OutputStickChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputStickChanged;

            public double DeadZone
            {
                get => stickTransAct.DeadMod.DeadZone;
                set
                {
                    stickTransAct.DeadMod.DeadZone = value;
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public double MaxZone
            {
                get => stickTransAct.DeadMod.MaxZone;
                set
                {
                    stickTransAct.DeadMod.MaxZone = value;
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxZoneChanged;

            public double AntiDeadZone
            {
                get => stickTransAct.DeadMod.AntiDeadZone;
                set
                {
                    stickTransAct.DeadMod.AntiDeadZone = value;
                    AntiZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler AntiZoneChanged;

            public StickModifiers.StickOutCurve.Curve OutputCurve
            {
                get => stickTransAct.OutputCurve;
                set
                {
                    stickTransAct.OutputCurve = value;
                    OutputCurveChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputCurveChanged;

            public bool InvertX
            {
                get => stickTransAct.InvertX;
                set
                {
                    stickTransAct.InvertX = value;
                    InvertXChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler InvertXChanged;

            public bool InvertY
            {
                get => stickTransAct.InvertY;
                set
                {
                    stickTransAct.InvertY = value;
                    InvertYChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler InvertYChanged;

            public int Rotation
            {
                get => stickTransAct.Rotation;
                set
                {
                    stickTransAct.Rotation = Math.Clamp(value, -180, 180);
                    RotationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler RotationChanged;

            public double VerticalScale
            {
                get => stickTransAct.VerticalScale;
                set
                {
                    stickTransAct.VerticalScale = Math.Clamp(value, 0.01, 10.0);
                    VerticalScaleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler VerticalScaleChanged;

            public bool MaxOutputEnabled
            {
                get => stickTransAct.MaxOutputEnabled;
                set
                {
                    stickTransAct.MaxOutputEnabled = value;
                    MaxOutputEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxOutputEnabledChanged;

            public double MaxOutput
            {
                get => stickTransAct.MaxOutput;
                set
                {
                    stickTransAct.MaxOutput = Math.Clamp(value, 0.0, 1.0);
                    MaxOutputChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxOutputChanged;

            public bool SquareStickEnabled
            {
                get => stickTransAct.SquareStickEnabled;
                set
                {
                    stickTransAct.SquareStickEnabled = value;
                    SquareStickEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SquareStickEnabledChanged;

            public double SquareStickRoundness
            {
                get => stickTransAct.SquareStickRoundness;
                set
                {
                    stickTransAct.SquareStickRoundness = Math.Clamp(value, 1.0, 10.0);
                    SquareStickRoundnessChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SquareStickRoundnessChanged;

            public StickTranslateSettings(StickTranslate action)
            {
                this.stickTransAct = action;
            }
        }

        private StickTranslate stickTransAct =
            new StickTranslate();

        private StickTranslateSettings settings;
        public StickTranslateSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public StickTranslateSerializer() : base()
        {
            mapAction = stickTransAct;
            settings = new StickTranslateSettings(stickTransAct);

            NameChanged += StickTranslateSerializer_NameChanged;
            settings.OutputStickChanged += Settings_OutputStickChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.MaxZoneChanged += Settings_MaxZoneChanged;
            settings.AntiZoneChanged += Settings_AntiZoneChanged;
            settings.OutputCurveChanged += Settings_OutputCurveChanged;
            settings.InvertXChanged += Settings_InvertXChanged;
            settings.InvertYChanged += Settings_InvertYChanged;
            settings.RotationChanged += Settings_RotationChanged;
            settings.VerticalScaleChanged += Settings_VerticalScaleChanged;
            settings.MaxOutputEnabledChanged += Settings_MaxOutputEnabledChanged;
            settings.MaxOutputChanged += Settings_MaxOutputChanged;
            settings.SquareStickEnabledChanged += Settings_SquareStickEnabledChanged;
            settings.SquareStickRoundnessChanged += Settings_SquareStickRoundnessChanged;
        }

        private void Settings_SquareStickRoundnessChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.SQUARE_STICK_ROUNDNESS);
        }

        private void Settings_SquareStickEnabledChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.SQUARE_STICK_ENABLED);
        }

        private void Settings_MaxOutputChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.MAX_OUTPUT);
        }

        private void Settings_MaxOutputEnabledChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.MAX_OUTPUT_ENABLED);
        }

        private void Settings_VerticalScaleChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.VERTICAL_SCALE);
        }

        private void Settings_RotationChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.ROTATION);
        }

        private void Settings_InvertYChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.INVERT_Y);
        }

        private void Settings_InvertXChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.INVERT_X);
        }

        private void Settings_OutputCurveChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.OUTPUT_CURVE);
        }

        private void Settings_AntiZoneChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.ANTIDEAD_ZONE);
        }

        private void StickTranslateSerializer_NameChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.NAME);
        }

        private void Settings_MaxZoneChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.MAX_ZONE);
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.DEAD_ZONE);
        }

        private void Settings_OutputStickChanged(object sender, EventArgs e)
        {
            stickTransAct.ChangedProperties.Add(StickTranslate.PropertyKeyStrings.OUTPUT_STICK);
        }

        // Serialize ctor
        public StickTranslateSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is StickTranslate temp)
            {
                stickTransAct = temp;
                mapAction = stickTransAct;
                settings = new StickTranslateSettings(stickTransAct);
            }
        }
    }

    public class StickAbsMouseActionSerializer : MapActionSerializer
    {
        public class OuterRingBinding
        {
            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        public class StickAbsMouseSettings
        {
            private StickAbsMouse stickAbsMouseAction;

            public double DeadZone
            {
                get => stickAbsMouseAction.DeadMod.DeadZone;
                set
                {
                    stickAbsMouseAction.DeadMod.DeadZone = Math.Clamp(value, 0.0, 1.0);
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            [JsonProperty("UseOuterRing")]
            public bool UseOuterRing
            {
                get => stickAbsMouseAction.UseRingButton;
                set
                {
                    stickAbsMouseAction.UseRingButton = value;
                    UseOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseOuterRingChanged;

            [JsonProperty("OuterRingDeadZone")]
            public double OuterRingDeadZone
            {
                get => stickAbsMouseAction.OuterRingDeadZone;
                set
                {
                    stickAbsMouseAction.OuterRingDeadZone = value;
                    OuterRingDeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OuterRingDeadZoneChanged;

            [JsonProperty("UseAsOuterRing")]
            public bool UseAsOuterRing
            {
                get => stickAbsMouseAction.UseAsOuterRing;
                set
                {
                    stickAbsMouseAction.UseAsOuterRing = value;
                    UseAsOuterRingChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseAsOuterRingChanged;

            public StickAbsMouseSettings(StickAbsMouse absMouseAction)
            {
                stickAbsMouseAction = absMouseAction;
            }
        }

        private OuterRingBinding ringBinding;

        [JsonProperty("OuterRingBinding")]
        public OuterRingBinding RingBinding
        {
            get => ringBinding;
            set
            {
                ringBinding = value;
                RingBindingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler RingBindingChanged;

        private StickAbsMouse stickAbsMouseAction = new StickAbsMouse();
        private StickAbsMouseSettings settings;
        public StickAbsMouseSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public StickAbsMouseActionSerializer() : base()
        {
            mapAction = stickAbsMouseAction;
            settings = new StickAbsMouseSettings(stickAbsMouseAction);

            NameChanged += StickAbsMouseActionSerializer_NameChanged;
            RingBindingChanged += StickAbsMouseActionSerializer_RingBindingChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.UseAsOuterRingChanged += Settings_UseAsOuterRingChanged;
            settings.UseOuterRingChanged += Settings_UseOuterRingChanged;
            settings.OuterRingDeadZoneChanged += Settings_OuterRingDeadZoneChanged;
        }

        // Serialize
        public StickAbsMouseActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is StickAbsMouse temp)
            {
                stickAbsMouseAction = temp;
                mapAction = stickAbsMouseAction;
                settings = new StickAbsMouseSettings(stickAbsMouseAction);
            }
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            stickAbsMouseAction.ChangedProperties.Add(StickAbsMouse.PropertyKeyStrings.DEAD_ZONE);
        }

        private void StickAbsMouseActionSerializer_NameChanged(object sender, EventArgs e)
        {
            stickAbsMouseAction.ChangedProperties.Add(StickAbsMouse.PropertyKeyStrings.NAME);
        }

        private void Settings_OuterRingDeadZoneChanged(object sender, EventArgs e)
        {
            stickAbsMouseAction.ChangedProperties.Add(StickAbsMouse.PropertyKeyStrings.OUTER_RING_DEAD_ZONE);
        }

        private void Settings_UseOuterRingChanged(object sender, EventArgs e)
        {
            stickAbsMouseAction.ChangedProperties.Add(StickAbsMouse.PropertyKeyStrings.USE_OUTER_RING);
        }

        private void Settings_UseAsOuterRingChanged(object sender, EventArgs e)
        {
            stickAbsMouseAction.ChangedProperties.Add(StickAbsMouse.PropertyKeyStrings.USE_AS_OUTER_RING);
        }

        private void StickAbsMouseActionSerializer_RingBindingChanged(object sender, EventArgs e)
        {
            stickAbsMouseAction.ChangedProperties.Add(StickAbsMouse.PropertyKeyStrings.OUTER_RING_BUTTON);

            stickAbsMouseAction.RingButton.Name = ringBinding.ActionDirName;
            stickAbsMouseAction.RingButton.ActionFuncs.Clear();
            List<ActionFuncSerializer> tempSerializers = ringBinding.ActionFuncSerializers;
            foreach (ActionFuncSerializer serializer in tempSerializers)
            {
                serializer.PopulateFunc();
                stickAbsMouseAction.RingButton.ActionFuncs.Add(serializer.ActionFunc);
            }
        }
    }

    public static class GyroActionsUtils
    {
        public enum GyroTriggerEvalCond
        {
            And,
            Or,
        }
    }

    public class GyroMouseSerializer : MapActionSerializer
    {
        public class GyroMouseSettings
        {
            private GyroMouse gyroMouseAction;

            public int DeadZone
            {
                get => gyroMouseAction.mouseParams.deadzone;
                set
                {
                    gyroMouseAction.mouseParams.deadzone = value;
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public double Sensitivity
            {
                get => gyroMouseAction.mouseParams.sensitivity;
                set
                {
                    gyroMouseAction.mouseParams.sensitivity = value;
                    SensitivityChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SensitivityChanged;

            public double VerticalScale
            {
                get => gyroMouseAction.mouseParams.verticalScale;
                set
                {
                    gyroMouseAction.mouseParams.verticalScale = Math.Clamp(value, 0.0, 10.0);
                    VerticalScaleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler VerticalScaleChanged;

            public bool InvertX
            {
                get => gyroMouseAction.mouseParams.invertX;
                set
                {
                    gyroMouseAction.mouseParams.invertX = value;
                    InvertXChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler InvertXChanged;

            public bool InvertY
            {
                get => gyroMouseAction.mouseParams.invertY;
                set
                {
                    gyroMouseAction.mouseParams.invertY = value;
                    InvertYChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler InvertYChanged;

            [JsonConverter(typeof(TriggerButtonsConverter))]
            public JoypadActionCodes[] TriggerButtons
            {
                get => gyroMouseAction.mouseParams.gyroTriggerButtons;
                set
                {
                    gyroMouseAction.mouseParams.gyroTriggerButtons = value;
                    TriggersButtonChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggersButtonChanged;

            public bool TriggerActivates
            {
                get => gyroMouseAction.mouseParams.triggerActivates;
                set
                {
                    gyroMouseAction.mouseParams.triggerActivates = value;
                    TriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggerActivatesChanged;

            public GyroActionsUtils.GyroTriggerEvalCond EvalCond
            {
                get => gyroMouseAction.mouseParams.andCond ?
                    GyroActionsUtils.GyroTriggerEvalCond.And : GyroActionsUtils.GyroTriggerEvalCond.Or;
                set
                {
                    gyroMouseAction.mouseParams.andCond =
                        value == GyroActionsUtils.GyroTriggerEvalCond.And ? true : false;

                    EvalCondChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler EvalCondChanged;

            public GyroMouseXAxisChoice UseForXAxis
            {
                get => gyroMouseAction.mouseParams.useForXAxis;
                set
                {
                    gyroMouseAction.mouseParams.useForXAxis = value;
                    UseForXAxisChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseForXAxisChanged;

            public double MinThreshold
            {
                get => gyroMouseAction.mouseParams.minThreshold;
                set
                {
                    gyroMouseAction.mouseParams.minThreshold = value;
                    MinThresholdChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MinThresholdChanged;

            public bool Toggle
            {
                get => gyroMouseAction.mouseParams.toggleAction;
                set
                {
                    gyroMouseAction.mouseParams.toggleAction = value;
                    ToggleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ToggleChanged;

            public bool SmoothingEnabled
            {
                get => gyroMouseAction.mouseParams.smoothing;
                set
                {
                    gyroMouseAction.mouseParams.smoothing = value;
                    SmoothingEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SmoothingEnabledChanged;

            public double SmoothingMinCutoff
            {
                get => gyroMouseAction.mouseParams.oneEuroMinCutoff;
                set
                {
                    gyroMouseAction.mouseParams.oneEuroMinCutoff = Math.Clamp(value, 0.0, 10.0);
                    SmoothingMinCutoffChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SmoothingMinCutoffChanged;

            public double SmoothingMinBeta
            {
                get => gyroMouseAction.mouseParams.oneEuroMinBeta;
                set
                {
                    gyroMouseAction.mouseParams.oneEuroMinBeta = Math.Clamp(value, 0.0, 1.0);
                    SmoothingMinBetaChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SmoothingMinBetaChanged;

            public GyroMouseSettings(GyroMouse mouseAction)
            {
                gyroMouseAction = mouseAction;
            }
        }

        private GyroMouse gyroMouseAction = new GyroMouse();
        private GyroMouseSettings settings;
        public GyroMouseSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public GyroMouseSerializer(): base()
        {
            mapAction = gyroMouseAction;
            settings = new GyroMouseSettings(gyroMouseAction);

            NameChanged += GyroMouseSerializer_NameChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.SensitivityChanged += Settings_SensitivityChanged;
            settings.VerticalScaleChanged += Settings_VerticalScaleChanged;
            settings.InvertXChanged += Settings_InvertXChanged;
            settings.InvertYChanged += Settings_InvertYChanged;
            settings.TriggersButtonChanged += Settings_TriggerButtonsChanged;
            settings.TriggerActivatesChanged += Settings_TriggerActivatesChanged;
            settings.EvalCondChanged += Settings_EvalCondChanged;
            settings.UseForXAxisChanged += Settings_UseForXAxisChanged;
            settings.MinThresholdChanged += Settings_MinThresholdChanged;
            settings.ToggleChanged += Settings_ToggleChanged;
            settings.SmoothingEnabledChanged += Settings_SmoothingEnabledChanged;
            settings.SmoothingMinCutoffChanged += Settings_SmoothingMinCutoffChanged;
            settings.SmoothingMinBetaChanged += Settings_SmoothingMinBetaChanged;
        }

        private void Settings_EvalCondChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.TRIGGER_EVAL_COND);
        }

        private void Settings_SmoothingMinBetaChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.SMOOTHING_MINBETA);
        }

        private void Settings_SmoothingMinCutoffChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.SMOOTHING_MINCUTOFF);
        }

        private void Settings_SmoothingEnabledChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.SMOOTHING_ENABLED);
        }

        private void Settings_ToggleChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.TOGGLE_ACTION);
        }

        private void Settings_MinThresholdChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.MIN_THRESHOLD);
        }

        private void Settings_UseForXAxisChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.X_AXIS);
        }

        private void Settings_InvertXChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.INVERT_X);
        }

        private void Settings_InvertYChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.INVERT_Y);
        }

        private void Settings_VerticalScaleChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.VERTICAL_SCALE);
        }

        private void Settings_SensitivityChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.SENSITIVITY);
        }

        private void Settings_TriggerActivatesChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.TRIGGER_ACTIVATE);
        }

        private void Settings_TriggerButtonsChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.TRIGGER_BUTTONS);
        }

        // Serialize
        public GyroMouseSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is GyroMouse temp)
            {
                gyroMouseAction = temp;
                mapAction = gyroMouseAction;
                settings = new GyroMouseSettings(gyroMouseAction);
            }
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.DEAD_ZONE);
        }

        private void GyroMouseSerializer_NameChanged(object sender, EventArgs e)
        {
            gyroMouseAction.ChangedProperties.Add(GyroMouse.PropertyKeyStrings.NAME);
        }
    }

    public class GyroDirectionalSwipeSerializer : MapActionSerializer
    {
        public class SwipeDirBinding
        {
            public enum SwipeDir
            {
                Up,
                Down,
                Left,
                Right,
            }

            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        public class GyroDirSwipeSettings
        {
            private GyroDirectionalSwipe gyroDirSwipeAction;

            public int DeadZoneX
            {
                get => gyroDirSwipeAction.swipeParams.deadzoneX;
                set
                {
                    gyroDirSwipeAction.swipeParams.deadzoneX = value;
                    DeadZoneXChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneXChanged;

            public int DeadZoneY
            {
                get => gyroDirSwipeAction.swipeParams.deadzoneY;
                set
                {
                    gyroDirSwipeAction.swipeParams.deadzoneY = value;
                    DeadZoneYChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneYChanged;

            public int DelayTime
            {
                get => gyroDirSwipeAction.swipeParams.delayTime;
                set
                {
                    gyroDirSwipeAction.swipeParams.delayTime = value;
                    DelayTimeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DelayTimeChanged;

            [JsonConverter(typeof(TriggerButtonsConverter))]
            public JoypadActionCodes[] TriggerButtons
            {
                get => gyroDirSwipeAction.swipeParams.gyroTriggerButtons;
                set
                {
                    gyroDirSwipeAction.swipeParams.gyroTriggerButtons = value;
                    TriggerButtonsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggerButtonsChanged;

            public bool TriggerActivates
            {
                get => gyroDirSwipeAction.swipeParams.triggerActivates;
                set
                {
                    gyroDirSwipeAction.swipeParams.triggerActivates = value;
                    TriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggerActivatesChanged;

            public GyroActionsUtils.GyroTriggerEvalCond EvalCond
            {
                get => gyroDirSwipeAction.swipeParams.andCond ?
                    GyroActionsUtils.GyroTriggerEvalCond.And : GyroActionsUtils.GyroTriggerEvalCond.Or;
                set
                {
                    gyroDirSwipeAction.swipeParams.andCond =
                        value == GyroActionsUtils.GyroTriggerEvalCond.And ? true : false;

                    EvalCondChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler EvalCondChanged;

            public GyroDirSwipeSettings(GyroDirectionalSwipe swipeAction)
            {
                gyroDirSwipeAction = swipeAction;
            }
        }

        private Dictionary<SwipeDirBinding.SwipeDir, SwipeDirBinding> dictDirBindings =
            new Dictionary<SwipeDirBinding.SwipeDir, SwipeDirBinding>();

        //private Dictionary<GyroDirectionalSwipe.SwipeAxisYDir, SwipeDirBinding> dictDirYBindings =
        //    new Dictionary<GyroDirectionalSwipe.SwipeAxisYDir, SwipeDirBinding>();

        [JsonProperty("Bindings", Required = Required.Always)]
        public Dictionary<SwipeDirBinding.SwipeDir, SwipeDirBinding> DictDirBindings
        {
            get => dictDirBindings;
            set => dictDirBindings = value;
        }

        //[JsonProperty("YBindings", Required = Required.Always)]
        //public Dictionary<GyroDirectionalSwipe.SwipeAxisYDir, SwipeDirBinding> DictDirYBindings
        //{
        //    get => dictDirYBindings;
        //    set => dictDirYBindings = value;
        //}

        private GyroDirectionalSwipe gyroDirSwipeAction = new GyroDirectionalSwipe();
        private GyroDirSwipeSettings settings;
        public GyroDirSwipeSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public GyroDirectionalSwipeSerializer(): base()
        {
            mapAction = gyroDirSwipeAction;
            settings = new GyroDirSwipeSettings(gyroDirSwipeAction);

            NameChanged += GyroDirectionalSwipeSerializer_NameChanged;
            settings.DeadZoneXChanged += Settings_DeadZoneXChanged;
            settings.DeadZoneYChanged += Settings_DeadZoneYChanged;
            settings.DelayTimeChanged += Settings_DelayTimeChanged;
            settings.TriggerActivatesChanged += Settings_TriggerActivatesChanged;
            settings.TriggerButtonsChanged += Settings_TriggerButtonsChanged;
            settings.EvalCondChanged += Settings_EvalCondChanged;
        }

        private void Settings_EvalCondChanged(object sender, EventArgs e)
        {
            gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_EVAL_COND);
        }

        // Serialize
        public GyroDirectionalSwipeSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is GyroDirectionalSwipe temp)
            {
                gyroDirSwipeAction = temp;
                MapAction = gyroDirSwipeAction;
                settings = new GyroDirSwipeSettings(gyroDirSwipeAction);
                PopulateFuncs();
            }
        }

        // Post-deserialize
        public override void PopulateMap()
        {
            foreach (ButtonAction dirButton in gyroDirSwipeAction.UsedEventsButtonsX)
            {
                dirButton?.ActionFuncs.Clear();
            }

            foreach (ButtonAction dirButton in gyroDirSwipeAction.UsedEventsButtonsY)
            {
                dirButton?.ActionFuncs.Clear();
            }

            foreach (KeyValuePair<SwipeDirBinding.SwipeDir, SwipeDirBinding> pair in dictDirBindings)
            {
                SwipeDirBinding.SwipeDir dir = pair.Key;
                List<ActionFuncSerializer> tempSerializers = pair.Value.ActionFuncSerializers;
                ButtonAction dirButton = new ButtonAction();
                dirButton.Name = pair.Value.ActionDirName;
                foreach (ActionFuncSerializer serializer in tempSerializers)
                {
                    serializer.PopulateFunc();
                    dirButton.ActionFuncs.Add(serializer.ActionFunc);
                }

                switch(dir)
                {
                    case SwipeDirBinding.SwipeDir.Left:
                        gyroDirSwipeAction.UsedEventsButtonsX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Left] = dirButton;
                        break;
                    case SwipeDirBinding.SwipeDir.Right:
                        gyroDirSwipeAction.UsedEventsButtonsX[(int)GyroDirectionalSwipe.SwipeAxisXDir.Right] = dirButton;
                        break;
                    case SwipeDirBinding.SwipeDir.Up:
                        gyroDirSwipeAction.UsedEventsButtonsY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Up] = dirButton;
                        break;
                    case SwipeDirBinding.SwipeDir.Down:
                        gyroDirSwipeAction.UsedEventsButtonsY[(int)GyroDirectionalSwipe.SwipeAxisYDir.Down] = dirButton;
                        break;
                    default:
                        break;
                }

                FlagBtnChangedDirection(dir);
            }

            //foreach (KeyValuePair<GyroDirectionalSwipe.SwipeAxisYDir, SwipeDirBinding> pair in dictDirYBindings)
            //{
            //    GyroDirectionalSwipe.SwipeAxisYDir dir = pair.Key;
            //    List<ActionFuncSerializer> tempSerializers = pair.Value.ActionFuncSerializers;
            //    ButtonAction dirButton = new ButtonAction();
            //    dirButton.Name = pair.Value.ActionDirName;
            //    foreach (ActionFuncSerializer serializer in tempSerializers)
            //    {
            //        serializer.PopulateFunc();
            //        dirButton.ActionFuncs.Add(serializer.ActionFunc);
            //    }

            //    gyroDirSwipeAction.UsedEventsButtonsY[(int)dir] = dirButton;
            //    FlagBtnChangedYDirection(dir);
            //}
        }

        private void FlagBtnChangedDirection(SwipeDirBinding.SwipeDir dir)
        {
            switch(dir)
            {
                case SwipeDirBinding.SwipeDir.Left:
                    gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_LEFT);
                    break;
                case SwipeDirBinding.SwipeDir.Right:
                    gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_RIGHT);
                    break;
                case SwipeDirBinding.SwipeDir.Up:
                    gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_UP);
                    break;
                case SwipeDirBinding.SwipeDir.Down:
                    gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.PAD_DIR_DOWN);
                    break;
                default:
                    break;
            }
        }

        //private void FlagBtnChangedYDirection(GyroDirectionalSwipe.SwipeAxisYDir dir)
        //{
        //    switch(dir)
        //    {
        //        case GyroDirectionalSwipe.SwipeAxisYDir.Up:
        //            //gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.)
        //            break;
        //        case GyroDirectionalSwipe.SwipeAxisYDir.Down:
        //            //gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.)
        //            break;
        //        default:
        //            break;
        //    }
        //}

        // Pre-serialize
        private void PopulateFuncs()
        {
            List<ActionFuncSerializer> tempFuncs = new List<ActionFuncSerializer>();
            GyroDirectionalSwipe.SwipeAxisXDir[] tempDirsX = new GyroDirectionalSwipe.SwipeAxisXDir[]
            {
                GyroDirectionalSwipe.SwipeAxisXDir.Left,
                GyroDirectionalSwipe.SwipeAxisXDir.Right,
            };

            GyroDirectionalSwipe.SwipeAxisYDir[] tempDirsY = new GyroDirectionalSwipe.SwipeAxisYDir[]
            {
                GyroDirectionalSwipe.SwipeAxisYDir.Up,
                GyroDirectionalSwipe.SwipeAxisYDir.Down,
            };

            for (int i = 0; i < tempDirsX.Length; i++)
            {
                GyroDirectionalSwipe.SwipeAxisXDir tempDir = tempDirsX[i];
                ButtonAction dirButton = gyroDirSwipeAction.UsedEventsButtonsX[(int)tempDir];

                tempFuncs.Clear();
                foreach (ActionFunc tempFunc in dirButton.ActionFuncs)
                {
                    tempFuncs.Add(new ActionFuncSerializer(tempFunc));
                }

                SwipeDirBinding.SwipeDir swipeDir = SwipeDirBinding.SwipeDir.Left;
                if (tempDir == GyroDirectionalSwipe.SwipeAxisXDir.Left)
                {
                    swipeDir = SwipeDirBinding.SwipeDir.Left;
                }
                else if (tempDir == GyroDirectionalSwipe.SwipeAxisXDir.Right)
                {
                    swipeDir = SwipeDirBinding.SwipeDir.Right;
                }

                dictDirBindings.Add(swipeDir, new SwipeDirBinding()
                {
                    ActionDirName = dirButton.Name,
                    ActionFuncSerializers = tempFuncs,
                });
            }

            for (int i = 0; i < tempDirsY.Length; i++)
            {
                GyroDirectionalSwipe.SwipeAxisYDir tempDir = tempDirsY[i];
                ButtonAction dirButton = gyroDirSwipeAction.UsedEventsButtonsY[(int)tempDir];

                tempFuncs.Clear();
                foreach (ActionFunc tempFunc in dirButton.ActionFuncs)
                {
                    tempFuncs.Add(new ActionFuncSerializer(tempFunc));
                }

                SwipeDirBinding.SwipeDir swipeDir = SwipeDirBinding.SwipeDir.Left;
                if (tempDir == GyroDirectionalSwipe.SwipeAxisYDir.Up)
                {
                    swipeDir = SwipeDirBinding.SwipeDir.Up;
                }
                else if (tempDir == GyroDirectionalSwipe.SwipeAxisYDir.Down)
                {
                    swipeDir = SwipeDirBinding.SwipeDir.Down;
                }

                dictDirBindings.Add(swipeDir, new SwipeDirBinding()
                {
                    ActionDirName = dirButton.Name,
                    ActionFuncSerializers = tempFuncs,
                });
            }
        }

        private void Settings_TriggerButtonsChanged(object sender, EventArgs e)
        {
            gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_BUTTONS);
        }

        private void Settings_TriggerActivatesChanged(object sender, EventArgs e)
        {
            gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.TRIGGER_ACTIVATE);
        }

        private void Settings_DelayTimeChanged(object sender, EventArgs e)
        {
            gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.DELAY_TIME);
        }

        private void Settings_DeadZoneYChanged(object sender, EventArgs e)
        {
            gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_Y);
        }

        private void Settings_DeadZoneXChanged(object sender, EventArgs e)
        {
            gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.DEAD_ZONE_X);
        }

        private void GyroDirectionalSwipeSerializer_NameChanged(object sender, EventArgs e)
        {
            gyroDirSwipeAction.ChangedProperties.Add(GyroDirectionalSwipe.PropertyKeyStrings.NAME);
        }
    }

    public class GyroMouseJoystickSerializer : MapActionSerializer
    {
        public class GyroMouseJoystickSettings
        {
            private GyroMouseJoystick gyroMouseStickAction;

            public int DeadZone
            {
                get => gyroMouseStickAction.mStickParms.deadZone;
                set
                {
                    gyroMouseStickAction.mStickParms.deadZone = value;
                    DeadZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler DeadZoneChanged;

            public int MaxZone
            {
                get => gyroMouseStickAction.mStickParms.maxZone;
                set
                {
                    gyroMouseStickAction.mStickParms.maxZone = value;
                    MaxZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxZoneChanged;

            [JsonConverter(typeof(TriggerButtonsConverter))]
            public JoypadActionCodes[] TriggerButtons
            {
                get => gyroMouseStickAction.mStickParms.gyroTriggerButtons;
                set
                {
                    gyroMouseStickAction.mStickParms.gyroTriggerButtons = value;
                    TriggerButtonsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggerButtonsChanged;

            public bool TriggerActivates
            {
                get => gyroMouseStickAction.mStickParms.triggerActivates;
                set
                {
                    gyroMouseStickAction.mStickParms.triggerActivates = value;
                    TriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggerActivatesChanged;

            public GyroActionsUtils.GyroTriggerEvalCond EvalCond
            {
                get => gyroMouseStickAction.mStickParms.andCond ?
                    GyroActionsUtils.GyroTriggerEvalCond.And : GyroActionsUtils.GyroTriggerEvalCond.Or;
                set
                {
                    gyroMouseStickAction.mStickParms.andCond =
                        value == GyroActionsUtils.GyroTriggerEvalCond.And ? true : false;

                    EvalCondChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler EvalCondChanged;

            public GyroMouseXAxisChoice UseForXAxis
            {
                get => gyroMouseStickAction.mStickParms.useForXAxis;
                set
                {
                    gyroMouseStickAction.mStickParms.useForXAxis = value;
                    UseForXAxisChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler UseForXAxisChanged;

            public double AntiDeadZoneX
            {
                get => gyroMouseStickAction.mStickParms.antiDeadzoneX;
                set
                {
                    gyroMouseStickAction.mStickParms.antiDeadzoneX = value;
                    AntiDeadZoneXChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler AntiDeadZoneXChanged;

            public double AntiDeadZoneY
            {
                get => gyroMouseStickAction.mStickParms.antiDeadzoneY;
                set
                {
                    gyroMouseStickAction.mStickParms.antiDeadzoneY = value;
                    AntiDeadZoneYChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler AntiDeadZoneYChanged;

            public bool InvertX
            {
                get => gyroMouseStickAction.mStickParms.invertX;
                set
                {
                    gyroMouseStickAction.mStickParms.invertX = value;
                    InvertXChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler InvertXChanged;

            public bool InvertY
            {
                get => gyroMouseStickAction.mStickParms.invertY;
                set
                {
                    gyroMouseStickAction.mStickParms.invertY = value;
                    InvertYChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler InvertYChanged;

            public double VerticalScale
            {
                get => gyroMouseStickAction.mStickParms.verticalScale;
                set
                {
                    gyroMouseStickAction.mStickParms.verticalScale = Math.Clamp(value, 0.0, 10.0);
                    VerticalScaleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler VerticalScaleChanged;

            public GyroMouseJoystickOuputAxes OutputAxes
            {
                get => gyroMouseStickAction.mStickParms.outputAxes;
                set
                {
                    gyroMouseStickAction.mStickParms.outputAxes = value;
                    OutputAxesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputAxesChanged;

            public StickActionCodes OutputStick
            {
                get => gyroMouseStickAction.mStickParms.outputStick;
                set
                {
                    gyroMouseStickAction.mStickParms.OutputStick = value;
                    OutputStickChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler OutputStickChanged;

            public bool MaxOutputEnabled
            {
                get => gyroMouseStickAction.mStickParms.maxOutputEnabled;
                set
                {
                    gyroMouseStickAction.mStickParms.maxOutputEnabled = value;
                    MaxOutputEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxOutputEnabledChanged;

            public double MaxOutput
            {
                get => gyroMouseStickAction.mStickParms.maxOutput;
                set
                {
                    gyroMouseStickAction.mStickParms.maxOutput = value;
                    MaxOutputChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler MaxOutputChanged;

            public bool Toggle
            {
                get => gyroMouseStickAction.mStickParms.toggleAction;
                set
                {
                    gyroMouseStickAction.mStickParms.toggleAction = value;
                    ToggleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ToggleChanged;

            public bool SmoothingEnabled
            {
                get => gyroMouseStickAction.mStickParms.smoothing;
                set
                {
                    gyroMouseStickAction.mStickParms.smoothing = value;
                    SmoothingEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SmoothingEnabledChanged;

            public double SmoothingMinCutoff
            {
                get => gyroMouseStickAction.mStickParms.oneEuroMinCutoff;
                set
                {
                    gyroMouseStickAction.mStickParms.oneEuroMinCutoff = Math.Clamp(value, 0.0, 10.0);
                    SmoothingMinCutoffChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SmoothingMinCutoffChanged;

            public double SmoothingMinBeta
            {
                get => gyroMouseStickAction.mStickParms.oneEuroMinBeta;
                set
                {
                    gyroMouseStickAction.mStickParms.oneEuroMinBeta = Math.Clamp(value, 0.0, 1.0);
                    SmoothingMinBetaChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler SmoothingMinBetaChanged;

            public GyroMouseJoystickSettings(GyroMouseJoystick mouseStickAction)
            {
                gyroMouseStickAction = mouseStickAction;
            }
        }

        private GyroMouseJoystick gyroMouseJoystickAction = new GyroMouseJoystick();
        private GyroMouseJoystickSettings settings;
        public GyroMouseJoystickSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        // Deserialize
        public GyroMouseJoystickSerializer() : base()
        {
            mapAction = gyroMouseJoystickAction;
            settings = new GyroMouseJoystickSettings(gyroMouseJoystickAction);

            NameChanged += GyroMouseJoystickSerializer_NameChanged;
            settings.DeadZoneChanged += Settings_DeadZoneChanged;
            settings.MaxZoneChanged += Settings_MaxZoneChanged;
            settings.TriggerButtonsChanged += Settings_TriggerButtonsChanged;
            settings.TriggerActivatesChanged += Settings_TriggerActivatesChanged;
            settings.EvalCondChanged += Settings_EvalCondChanged;
            settings.UseForXAxisChanged += Settings_UseForXAxisChanged;
            settings.AntiDeadZoneXChanged += Settings_AntiDeadZoneXChanged;
            settings.AntiDeadZoneYChanged += Settings_AntiDeadZoneYChanged;
            settings.InvertXChanged += Settings_InvertXChanged;
            settings.InvertYChanged += Settings_InvertYChanged;
            settings.VerticalScaleChanged += Settings_VerticalScaleChanged;
            settings.OutputAxesChanged += Settings_OutputAxesChanged;
            settings.OutputStickChanged += Settings_OutputStickChanged;
            settings.MaxOutputEnabledChanged += Settings_MaxOutputEnabledChanged;
            settings.MaxOutputChanged += Settings_MaxOutputChanged;
            settings.ToggleChanged += Settings_ToggleChanged;
            settings.SmoothingEnabledChanged += Settings_SmoothingEnabledChanged;
            settings.SmoothingMinCutoffChanged += Settings_SmoothingMinCutoffChanged;
            settings.SmoothingMinBetaChanged += Settings_SmoothingMinBetaChanged;
        }

        private void Settings_EvalCondChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_EVAL_COND);
        }

        private void Settings_SmoothingMinBetaChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.SMOOTHING_MINBETA);
        }

        private void Settings_SmoothingMinCutoffChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.SMOOTHING_MINCUTOFF);
        }

        private void Settings_SmoothingEnabledChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.SMOOTHING_ENABLED);
        }

        private void Settings_ToggleChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.TOGGLE_ACTION);
        }

        private void Settings_MaxOutputChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.MAX_OUTPUT);
        }

        private void Settings_MaxOutputEnabledChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.MAX_OUTPUT_ENABLED);
        }

        private void Settings_OutputStickChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.OUTPUT_STICK);
        }

        private void Settings_OutputAxesChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.OUTPUT_AXES);
        }

        private void Settings_VerticalScaleChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.VERTICAL_SCALE);
        }

        private void Settings_InvertYChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.INVERT_Y);
        }

        private void Settings_InvertXChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.INVERT_X);
        }

        private void Settings_MaxZoneChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.MAX_ZONE);
        }

        private void Settings_AntiDeadZoneYChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_Y);
        }

        private void Settings_AntiDeadZoneXChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.ANTIDEAD_ZONE_X);
        }

        private void Settings_UseForXAxisChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.X_AXIS);
        }

        private void Settings_TriggerActivatesChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_ACTIVATE);
        }

        private void Settings_TriggerButtonsChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.TRIGGER_BUTTONS);
        }

        public GyroMouseJoystickSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is GyroMouseJoystick temp)
            {
                gyroMouseJoystickAction = temp;
                mapAction = gyroMouseJoystickAction;
                settings = new GyroMouseJoystickSettings(gyroMouseJoystickAction);
            }
        }

        private void Settings_DeadZoneChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.DEAD_ZONE);
        }

        private void GyroMouseJoystickSerializer_NameChanged(object sender, EventArgs e)
        {
            gyroMouseJoystickAction.ChangedProperties.Add(GyroMouseJoystick.PropertyKeyStrings.NAME);
        }
    }

    public class GyroNoMapActionSerializer : MapActionSerializer
    {
        private GyroNoMapAction gyroNoAction = new GyroNoMapAction();

        public GyroNoMapActionSerializer() : base()
        {
            mapAction = gyroNoAction;
        }

        public GyroNoMapActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is GyroNoMapAction temp)
            {
                gyroNoAction = temp;
                mapAction = gyroNoAction;
            }
        }
    }

    public class GyroPadActionSerializer : MapActionSerializer
    {
        public class GyroPadDirBinding
        {
            private string actionDirName;
            [JsonProperty("Name", Required = Required.Default)]
            public string ActionDirName
            {
                get => actionDirName;
                set => actionDirName = value;
            }

            private List<ActionFuncSerializer> actionFuncSerializers =
                new List<ActionFuncSerializer>();
            [JsonProperty("Functions", Required = Required.Always)]
            public List<ActionFuncSerializer> ActionFuncSerializers
            {
                get => actionFuncSerializers;
                set
                {
                    actionFuncSerializers = value;
                    ActionFuncSerializersChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler ActionFuncSerializersChanged;
        }

        public class GyroPadActionSettings
        {
            private GyroPadAction padAction;

            public GyroPadAction.DPadMode PadMode
            {
                get => padAction.CurrentMode;
                set
                {
                    padAction.CurrentMode = value;
                    PadModeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler PadModeChanged;

            [JsonConverter(typeof(TriggerButtonsConverter))]
            public JoypadActionCodes[] TriggerButtons
            {
                get => padAction.padParams.gyroTriggerButtons;
                set
                {
                    padAction.padParams.gyroTriggerButtons = value;
                    TriggersButtonChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggersButtonChanged;

            public bool TriggerActivates
            {
                get => padAction.padParams.triggerActivates;
                set
                {
                    padAction.padParams.triggerActivates = value;
                    TriggerActivatesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler TriggerActivatesChanged;

            public GyroActionsUtils.GyroTriggerEvalCond EvalCond
            {
                get => padAction.padParams.andCond ?
                    GyroActionsUtils.GyroTriggerEvalCond.And : GyroActionsUtils.GyroTriggerEvalCond.Or;
                set
                {
                    padAction.padParams.andCond =
                        value == GyroActionsUtils.GyroTriggerEvalCond.And ? true : false;

                    EvalCondChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            public event EventHandler EvalCondChanged;

            public GyroPadActionSettings(GyroPadAction padAction)
            {
                this.padAction = padAction;
            }
        }

        private GyroPadAction gyroPadAction = new GyroPadAction();

        private Dictionary<GyroPadAction.DpadDirections, GyroPadDirBinding> dictPadBindings =
            new Dictionary<GyroPadAction.DpadDirections, GyroPadDirBinding>();

        [JsonProperty("Bindings", Required = Required.Always)]
        public Dictionary<GyroPadAction.DpadDirections, GyroPadDirBinding> DictPadBindings
        {
            get => dictPadBindings;
            set => dictPadBindings = value;
        }

        private GyroPadActionSettings settings;
        public GyroPadActionSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public GyroPadActionSerializer() : base()
        {
            mapAction = gyroPadAction;
            settings = new GyroPadActionSettings(gyroPadAction);

            settings.PadModeChanged += Settings_PadModeChanged;
            settings.TriggersButtonChanged += Settings_TriggersButtonChanged;
            settings.TriggerActivatesChanged += Settings_TriggerActivatesChanged;
            settings.EvalCondChanged += Settings_EvalCondChanged;
        }

        private void Settings_EvalCondChanged(object sender, EventArgs e)
        {
            gyroPadAction.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.TRIGGER_EVAL_COND);
        }

        private void Settings_TriggerActivatesChanged(object sender, EventArgs e)
        {
            gyroPadAction.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.TRIGGER_ACTIVATE);
        }

        private void Settings_TriggersButtonChanged(object sender, EventArgs e)
        {
            gyroPadAction.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.TRIGGER_BUTTONS);
        }

        private void Settings_PadModeChanged(object sender, EventArgs e)
        {
            //if (mapAction.ParentAction != null)
            {
                gyroPadAction.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_MODE);
            }
        }

        public GyroPadActionSerializer(ActionLayer tempLayer, MapAction action) :
            base(tempLayer, action)
        {
            if (action is GyroPadAction temp)
            {
                gyroPadAction = temp;
                mapAction = gyroPadAction;
                settings = new GyroPadActionSettings(gyroPadAction);
                PopulateFuncs();
            }
        }

        // Post-deserialize
        public override void PopulateMap()
        {
            foreach (AxisDirButton dirButton in gyroPadAction.EventCodes4)
            {
                dirButton?.ActionFuncs.Clear();
            }

            //foreach (KeyValuePair<GyroPadAction.DpadDirections, List<ActionFuncSerializer>> tempKeyPair in dictPadBindings)
            foreach (KeyValuePair<GyroPadAction.DpadDirections, GyroPadDirBinding> tempKeyPair in dictPadBindings)
            {
                GyroPadAction.DpadDirections dir = tempKeyPair.Key;
                List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value.ActionFuncSerializers;
                //List<ActionFuncSerializer> tempSerializers = tempKeyPair.Value;

                //ButtonAction tempDirButton = dPadAction.EventCodes4[(int)dir];
                //if (tempDirButton != null)
                {
                    //tempDirButton.Name = tempKeyPair.Value.ActionDirName;
                    AxisDirButton dirButton = new AxisDirButton();
                    dirButton.Name = tempKeyPair.Value.ActionDirName;
                    foreach (ActionFuncSerializer serializer in tempSerializers)
                    {
                        //ButtonAction dirButton = dPadAction.EventCodes4[(int)dir];
                        serializer.PopulateFunc();
                        dirButton.ActionFuncs.Add(serializer.ActionFunc);
                        //dPadAction.EventCodes4[(int)dir] = dirButton;
                    }

                    gyroPadAction.EventCodes4[(int)dir] = dirButton;
                    FlagBtnChangedDirection(dir, gyroPadAction);
                }
            }
        }

        // Pre-serialize
        private void PopulateFuncs()
        {
            List<ActionFuncSerializer> tempFuncs = new List<ActionFuncSerializer>();
            GyroPadAction.DpadDirections[] tempDirs = null;

            if (gyroPadAction.CurrentMode == GyroPadAction.DPadMode.Standard ||
                gyroPadAction.CurrentMode == GyroPadAction.DPadMode.FourWayCardinal)
            {
                tempDirs = new GyroPadAction.DpadDirections[4]
                {
                    GyroPadAction.DpadDirections.Up, GyroPadAction.DpadDirections.Down,
                    GyroPadAction.DpadDirections.Left, GyroPadAction.DpadDirections.Right
                };
            }
            else if (gyroPadAction.CurrentMode == GyroPadAction.DPadMode.EightWay)
            {
                tempDirs = new GyroPadAction.DpadDirections[8]
                {
                    GyroPadAction.DpadDirections.Up, GyroPadAction.DpadDirections.Down,
                    GyroPadAction.DpadDirections.Left, GyroPadAction.DpadDirections.Right,
                    GyroPadAction.DpadDirections.UpLeft, GyroPadAction.DpadDirections.UpRight,
                    GyroPadAction.DpadDirections.DownLeft, GyroPadAction.DpadDirections.DownRight
                };
            }
            else if (gyroPadAction.CurrentMode == GyroPadAction.DPadMode.FourWayDiagonal)
            {
                tempDirs = new GyroPadAction.DpadDirections[4]
                {
                    GyroPadAction.DpadDirections.Up, GyroPadAction.DpadDirections.Down,
                    GyroPadAction.DpadDirections.Left, GyroPadAction.DpadDirections.Right
                };
            }

            for (int i = 0; i < tempDirs.Length; i++)
            {
                GyroPadAction.DpadDirections tempDir = tempDirs[i];
                AxisDirButton dirButton = gyroPadAction.EventCodes4[(int)tempDir];

                tempFuncs.Clear();
                foreach (ActionFunc tempFunc in dirButton.ActionFuncs)
                {
                    tempFuncs.Add(new ActionFuncSerializer(tempFunc));
                }

                //dictPadBindings.Add(tempDir, tempFuncs);
                dictPadBindings.Add(tempDir, new GyroPadDirBinding()
                {
                    ActionDirName = dirButton.Name,
                    ActionFuncSerializers = tempFuncs,
                });
            }
        }

        public void FlagBtnChangedDirection(GyroPadAction.DpadDirections dir,
            GyroPadAction action)
        {
            switch (dir)
            {
                case GyroPadAction.DpadDirections.Up:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_UP);
                    break;
                case GyroPadAction.DpadDirections.Down:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_DOWN);
                    break;
                case GyroPadAction.DpadDirections.Left:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_LEFT);
                    break;
                case GyroPadAction.DpadDirections.Right:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_RIGHT);
                    break;
                case GyroPadAction.DpadDirections.UpLeft:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_UPLEFT);
                    break;
                case GyroPadAction.DpadDirections.UpRight:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_UPRIGHT);
                    break;
                case GyroPadAction.DpadDirections.DownLeft:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_DOWNLEFT);
                    break;
                case GyroPadAction.DpadDirections.DownRight:
                    action.ChangedProperties.Add(GyroPadAction.PropertyKeyStrings.PAD_DIR_DOWNRIGHT);
                    break;
                default:
                    break;
            }
        }
    }

    public class MapActionTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MapActionSerializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject j = JObject.Load(reader);
            Trace.WriteLine("HUP RIDE");
            MapActionSerializer current = existingValue as MapActionSerializer;

            string actionOutput = j["ActionMode"]?.ToString();
            //bool status = int.TryParse(j["Index"]?.ToString(), out int ind);
            object resultInstance = null;
            switch (actionOutput)
            {
                case "ButtonAction":
                    ButtonActionSerializer instance = new ButtonActionSerializer();
                    ButtonAction tempAction = instance.ButtonAction;
                    /*if (ActionSetSerializer.TopActionLayer != null)
                    {
                        //int ind = ActionLayerSerializer.CurrentActionIndex;
                        bool status = int.TryParse(j["Id"]?.ToString(), out int parentId);
                        int ind = status ? ActionSetSerializer.TopActionLayer.LayerActions.FindIndex((item) => item.Id == parentId) : -1;
                        if (status && ind >= 0)
                        //if (ind > 0 &&
                        //    ind < ActionSetSerializer.TopActionLayer.LayerActions.Count)
                        {
                            MapAction tempMap =
                                ActionSetSerializer.TopActionLayer.LayerActions[ind];
                            if (tempMap.IsSameType(tempMap, tempAction))
                            {
                                instance.ButtonAction =
                                    ((tempMap as ButtonAction).DuplicateAction() as ButtonAction);
                            }
                        }
                    }
                    */

                    JsonConvert.PopulateObject(j.ToString(), instance);
                    instance.ActionFuncSerializers.RemoveAll((item) => item == null);
                    instance.RaiseActionFuncSerializersChanged();
                    resultInstance = instance;
                    break;
                case "ButtonNoAction":
                    ButtonNoActionSerializer btnNoActinstance = new ButtonNoActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), btnNoActinstance);
                    resultInstance = btnNoActinstance;
                    break;
                case "StickPadAction":
                    StickPadActionSerializer stickPadInstance = new StickPadActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), stickPadInstance);
                    foreach (StickPadAction.DpadDirections dir in stickPadInstance.DictPadBindings.Keys)
                    {
                        //stickPadInstance.DictPadBindings[dir].RemoveAll((item) => item == null);
                        //stickPadInstance.DictPadBindings[dir].RemoveAll((item) => item == null);
                        stickPadInstance.DictPadBindings[dir].ActionFuncSerializers.RemoveAll((item) => item == null);
                    }

                    resultInstance = stickPadInstance;
                    break;
                case "StickMouseAction":
                    StickMouseSerializer stickMouseInstance = new StickMouseSerializer();
                    JsonConvert.PopulateObject(j.ToString(), stickMouseInstance);
                    resultInstance = stickMouseInstance;
                    break;
                case "StickTranslateAction":
                    StickTranslateSerializer stickTransActInstance = new StickTranslateSerializer();
                    JsonConvert.PopulateObject(j.ToString(), stickTransActInstance);
                    resultInstance = stickTransActInstance;
                    break;
                case "StickAbsMouseAction":
                    StickAbsMouseActionSerializer stickAbsMouseInstance = new StickAbsMouseActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), stickAbsMouseInstance);
                    resultInstance = stickAbsMouseInstance;
                    break;
                case "StickNoAction":
                    StickNoActionSerializer stickNoActinstance = new StickNoActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), stickNoActinstance);
                    resultInstance = stickNoActinstance;
                    break;
                case "TriggerTranslateAction":
                    TriggerTranslateActionSerializer triggerActInstance = new TriggerTranslateActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), triggerActInstance);
                    resultInstance = triggerActInstance;
                    break;
                case "TriggerButtonAction":
                    TriggerButtonActionSerializer triggerButtonActInstance = new TriggerButtonActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), triggerButtonActInstance);
                    resultInstance = triggerButtonActInstance;
                    break;
                case "TriggerDualStageAction":
                    TriggerDualStageActionSerializer triggerDualActInstance = new TriggerDualStageActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), triggerDualActInstance);
                    resultInstance = triggerDualActInstance;
                    break;
                case "TouchStickTranslateAction":
                    TouchpadStickActionSerializer touchStickActInstance = new TouchpadStickActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), touchStickActInstance);
                    resultInstance = touchStickActInstance;
                    break;
                case "TouchMouseAction":
                    TouchpadMouseSerializer touchMouseActInstance = new TouchpadMouseSerializer();
                    JsonConvert.PopulateObject(j.ToString(), touchMouseActInstance);
                    resultInstance = touchMouseActInstance;
                    break;
                case "TouchMouseJoystickAction":
                    TouchpadMouseJoystickSerializer touchMouseJoyActInstance = new TouchpadMouseJoystickSerializer();
                    JsonConvert.PopulateObject(j.ToString(), touchMouseJoyActInstance);
                    resultInstance = touchMouseJoyActInstance;
                    break;
                case "TouchActionPadAction":
                    TouchpadActionPadSerializer touchActionPadInstance = new TouchpadActionPadSerializer();
                    JsonConvert.PopulateObject(j.ToString(), touchActionPadInstance);
                    resultInstance = touchActionPadInstance;
                    break;
                case "TouchAbsPadAction":
                    TouchpadAbsActionSerializer touchAbsActionInstance = new TouchpadAbsActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), touchAbsActionInstance);
                    resultInstance = touchAbsActionInstance;
                    break;
                case "DPadAction":
                    DpadActionSerializer dpadActSerializer = new DpadActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), dpadActSerializer);
                    foreach (DPadActions.DpadDirections dir in dpadActSerializer.DictPadBindings.Keys)
                    {
                        //dpadActSerializer.DictPadBindings[dir].RemoveAll((item) => item == null);
                        dpadActSerializer.DictPadBindings[dir].ActionFuncSerializers.RemoveAll((item) => item == null);
                    }
                    resultInstance = dpadActSerializer;
                    break;
                case "DPadNoAction":
                    DpadNoActionSerializer dpadNoActSerializer = new DpadNoActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), dpadNoActSerializer);
                    resultInstance = dpadNoActSerializer;
                    break;
                case "DPadTranslateAction":
                    DpadTranslateSerializer dpadTransActSerializer = new DpadTranslateSerializer();
                    JsonConvert.PopulateObject(j.ToString(), dpadTransActSerializer);
                    resultInstance = dpadTransActSerializer;
                    break;
                case "GyroMouseAction":
                    GyroMouseSerializer gyroMouseInstance = new GyroMouseSerializer();
                    JsonConvert.PopulateObject(j.ToString(), gyroMouseInstance);
                    resultInstance = gyroMouseInstance;
                    break;
                case "GyroMouseJoystickAction":
                    GyroMouseJoystickSerializer gyroMouseStickInstance = new GyroMouseJoystickSerializer();
                    JsonConvert.PopulateObject(j.ToString(), gyroMouseStickInstance);
                    resultInstance = gyroMouseStickInstance;
                    break;
                case "GyroDirSwipeAction":
                    GyroDirectionalSwipeSerializer gyroDirSwipeInstance = new GyroDirectionalSwipeSerializer();
                    JsonConvert.PopulateObject(j.ToString(), gyroDirSwipeInstance);
                    resultInstance = gyroDirSwipeInstance;
                    break;
                case "GyroNoAction":
                    GyroNoMapActionSerializer gyroNoActinstance = new GyroNoMapActionSerializer();
                    JsonConvert.PopulateObject(j.ToString(), gyroNoActinstance);
                    resultInstance = gyroNoActinstance;
                    break;
                default:
                    break;
            }

            return resultInstance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //JObject tempJ = new JObject();
            MapActionSerializer current = value as MapActionSerializer;
            MapAction tempMapAction = current.MapAction;
            switch (current.MapAction.ActionTypeName)
            {
                case "ButtonAction":
                    ButtonAction tempAction = tempMapAction as ButtonAction;
                    ButtonActionSerializer actionSerializer = new ButtonActionSerializer(current.TempLayer, tempMapAction);
                    //JsonConvert.SerializeObject(actionSerializer);
                    serializer.Serialize(writer, actionSerializer);
                    break;
                case "ButtonNoAction":
                    ButtonNoActionSerializer btnNoActSerializer = new ButtonNoActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, btnNoActSerializer);
                    break;
                case "StickPadAction":
                    //StickActions.StickPadAction tempStickPadAction = tempMapAction as StickActions.StickPadAction;
                    StickPadActionSerializer stickPadSerializer = new StickPadActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, stickPadSerializer);
                    break;
                case "StickMouseAction":
                    StickMouseSerializer stickMouseSerializer = new StickMouseSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, stickMouseSerializer);
                    break;
                case "StickTranslateAction":
                    StickTranslateSerializer stickTransSerializer = new StickTranslateSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, stickTransSerializer);
                    break;
                case "StickAbsMouseAction":
                    StickAbsMouseActionSerializer stickAbsMouseSerializer = new StickAbsMouseActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, stickAbsMouseSerializer);
                    break;
                case "StickNoAction":
                    StickNoActionSerializer stickNoActSerializer = new StickNoActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, stickNoActSerializer);
                    break;
                case "TriggerTranslateAction":
                    TriggerTranslateActionSerializer triggerActSerializer = new TriggerTranslateActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, triggerActSerializer);
                    break;
                case "TriggerButtonAction":
                    TriggerButtonActionSerializer triggerBtnActSerializer = new TriggerButtonActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, triggerBtnActSerializer);
                    break;
                case "TriggerDualStageAction":
                    TriggerDualStageActionSerializer triggerDualActSerializer = new TriggerDualStageActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, triggerDualActSerializer);
                    break;
                case "TouchStickTranslateAction":
                    TouchpadStickActionSerializer touchStickActSerializer = new TouchpadStickActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, touchStickActSerializer);
                    break;
                case "TouchMouseAction":
                    TouchpadMouseSerializer touchMouseActSerializer = new TouchpadMouseSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, touchMouseActSerializer);
                    break;
                case "TouchMouseJoystickAction":
                    TouchpadMouseJoystickSerializer touchMouseJoyActSerializer = new TouchpadMouseJoystickSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, touchMouseJoyActSerializer);
                    break;
                case "TouchActionPadAction":
                    TouchpadActionPadSerializer touchActionPadSerializer = new TouchpadActionPadSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, touchActionPadSerializer);
                    break;
                case "TouchAbsPadAction":
                    TouchpadAbsActionSerializer touchAbsActSerializer = new TouchpadAbsActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, touchAbsActSerializer);
                    break;
                case "DPadAction":
                    DpadActionSerializer dpadActSerializer = new DpadActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, dpadActSerializer);
                    break;
                case "DPadNoAction":
                    DpadNoActionSerializer dpadNoActSerializer = new DpadNoActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, dpadNoActSerializer);
                    break;
                case "DPadTranslateAction":
                    DpadTranslateSerializer dpadTransActSerializer = new DpadTranslateSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, dpadTransActSerializer);
                    break;
                case "GyroMouseAction":
                    GyroMouseSerializer gyroMouseSerializer = new GyroMouseSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, gyroMouseSerializer);
                    break;
                case "GyroMouseJoystickAction":
                    GyroMouseJoystickSerializer gyroMouseStickSerializer = new GyroMouseJoystickSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, gyroMouseStickSerializer);
                    break;
                case "GyroDirSwipeAction":
                    GyroDirectionalSwipeSerializer gyroDirSwipeSerializer = new GyroDirectionalSwipeSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, gyroDirSwipeSerializer);
                    break;
                case "GyroNoAction":
                    GyroNoMapActionSerializer gyroNoActSerializer = new GyroNoMapActionSerializer(current.TempLayer, tempMapAction);
                    serializer.Serialize(writer, gyroNoActSerializer);
                    break;
                default:
                    break;
            }

            //serializer.Serialize(new JTokenWriter(tempJ), value);
            //serializer.Serialize(writer, value);
        }
    }

    [JsonConverter(typeof(OutputActionDataTypeConverter))]
    public class OutputActionDataSerializer
    {
        private OutputActionData outputData;
        [JsonIgnore]
        public OutputActionData OutputData { get => outputData; }

        [JsonProperty(PropertyName = "Type", Required = Required.Always)]
        public ActionType ActionType
        {
            get => outputData.OutputType;
            set => outputData.OutputType = value;
        }

        public int Code
        {
            get => outputData.OutputCode;
            set => outputData.OutputCode = value;
        }

        public JoypadActionCodes PadCode
        {
            get => outputData.JoypadCode;
            set => outputData.JoypadCode = value;
        }

        [JsonConstructor]
        public OutputActionDataSerializer()
        {
            this.outputData = new OutputActionData(ActionType.Empty, 0);
        }

        public OutputActionDataSerializer(OutputActionData data)
        {
            this.outputData = data;
        }

        public static uint ParseKeyboardCodeString(string key)
        {
            uint result = 0;
            switch (key)
            {
                case "A":
                    result = (uint)VirtualKeys.A;
                    break;
                case "B":
                    result = (uint)VirtualKeys.B;
                    break;
                case "C":
                    result = (uint)VirtualKeys.C;
                    break;
                case "D":
                    result = (uint)VirtualKeys.D;
                    break;
                case "E":
                    result = (uint)VirtualKeys.E;
                    break;
                case "F":
                    result = (uint)VirtualKeys.F;
                    break;
                case "G":
                    result = (uint)VirtualKeys.G;
                    break;
                case "H":
                    result = (uint)VirtualKeys.H;
                    break;
                case "I":
                    result = (uint)VirtualKeys.I;
                    break;
                case "J":
                    result = (uint)VirtualKeys.J;
                    break;
                case "K":
                    result = (uint)VirtualKeys.K;
                    break;
                case "L":
                    result = (uint)VirtualKeys.L;
                    break;
                case "M":
                    result = (uint)VirtualKeys.M;
                    break;
                case "N":
                    result = (uint)VirtualKeys.N;
                    break;
                case "O":
                    result = (uint)VirtualKeys.O;
                    break;
                case "P":
                    result = (uint)VirtualKeys.P;
                    break;
                case "Q":
                    result = (uint)VirtualKeys.Q;
                    break;
                case "R":
                    result = (uint)VirtualKeys.R;
                    break;
                case "S":
                    result = (uint)VirtualKeys.S;
                    break;
                case "T":
                    result = (uint)VirtualKeys.T;
                    break;
                case "U":
                    result = (uint)VirtualKeys.U;
                    break;
                case "V":
                    result = (uint)VirtualKeys.V;
                    break;
                case "W":
                    result = (uint)VirtualKeys.W;
                    break;
                case "X":
                    result = (uint)VirtualKeys.X;
                    break;
                case "Y":
                    result = (uint)VirtualKeys.Y;
                    break;
                case "Z":
                    result = (uint)VirtualKeys.Z;
                    break;
                case "N1":
                    result = (uint)VirtualKeys.N1;
                    break;
                case "N2":
                    result = (uint)VirtualKeys.N2;
                    break;
                case "N3":
                    result = (uint)VirtualKeys.N3;
                    break;
                case "N4":
                    result = (uint)VirtualKeys.N4;
                    break;
                case "N5":
                    result = (uint)VirtualKeys.N5;
                    break;
                case "N6":
                    result = (uint)VirtualKeys.N6;
                    break;
                case "N7":
                    result = (uint)VirtualKeys.N7;
                    break;
                case "N8":
                    result = (uint)VirtualKeys.N8;
                    break;
                case "N9":
                    result = (uint)VirtualKeys.N9;
                    break;
                case "N0":
                    result = (uint)VirtualKeys.N0;
                    break;
                case "Minus":
                    result = (uint)VirtualKeys.OEMMinus;
                    break;
                case "Equal":
                    result = (uint)VirtualKeys.NEC_Equal;
                    break;
                case "LeftBracket":
                    result = (uint)VirtualKeys.OEM4;
                    break;
                case "RightBracket":
                    result = (uint)VirtualKeys.OEM6;
                    break;
                case "Backslash":
                    result = (uint)VirtualKeys.OEM5;
                    break;
                case "Semicolor":
                    result = (uint)VirtualKeys.OEM1;
                    break;
                case "Quote":
                    result = (uint)VirtualKeys.OEM7;
                    break;
                case "Comma":
                    result = (uint)VirtualKeys.OEMComma;
                    break;
                case "Period":
                    result = (uint)VirtualKeys.OEMPeriod;
                    break;
                case "Slash":
                    result = (uint)VirtualKeys.OEM2;
                    break;
                case "Space":
                case "Spacebar":
                    result = (uint)VirtualKeys.Space;
                    break;
                case "Backspace":
                    result = (uint)VirtualKeys.Back;
                    break;
                case "CapsLock":
                    result = (uint)VirtualKeys.CapsLock;
                    break;
                case "LeftAlt":
                    result = (uint)VirtualKeys.LeftMenu;
                    break;
                case "RightAlt":
                    result = (uint)VirtualKeys.RightMenu;
                    break;
                case "Windows":
                case "LeftWindows":
                    result = (uint)VirtualKeys.LeftWindows;
                    break;
                case "RightWindows":
                    result = (uint)VirtualKeys.RightWindows;
                    break;
                case "LeftControl":
                    result = (uint)VirtualKeys.LeftControl;
                    break;
                case "RightControl":
                    result = (uint)VirtualKeys.RightControl;
                    break;
                case "Esc":
                case "Escape":
                    result = (uint)VirtualKeys.Escape;
                    break;
                case "LeftShift":
                    result = (uint)VirtualKeys.LeftShift;
                    break;
                case "RightShift":
                    result = (uint)VirtualKeys.RightShift;
                    break;
                case "Enter":
                case "Return":
                    result = (uint)VirtualKeys.Return;
                    break;
                case "Tab":
                    result = (uint)VirtualKeys.Tab;
                    break;
                case "F1":
                    result = (uint)VirtualKeys.F1;
                    break;
                case "F2":
                    result = (uint)VirtualKeys.F2;
                    break;
                case "F3":
                    result = (uint)VirtualKeys.F3;
                    break;
                case "F4":
                    result = (uint)VirtualKeys.F4;
                    break;
                case "F5":
                    result = (uint)VirtualKeys.F5;
                    break;
                case "F6":
                    result = (uint)VirtualKeys.F6;
                    break;
                case "F7":
                    result = (uint)VirtualKeys.F7;
                    break;
                case "F8":
                    result = (uint)VirtualKeys.F8;
                    break;
                case "F9":
                    result = (uint)VirtualKeys.F9;
                    break;
                case "F10":
                    result = (uint)VirtualKeys.F10;
                    break;
                case "F11":
                    result = (uint)VirtualKeys.F11;
                    break;
                case "F12":
                    result = (uint)VirtualKeys.F12;
                    break;
                case "Up":
                    result = (uint)VirtualKeys.Up;
                    break;
                case "Down":
                    result = (uint)VirtualKeys.Down;
                    break;
                case "Left":
                    result = (uint)VirtualKeys.Left;
                    break;
                case "Right":
                    result = (uint)VirtualKeys.Right;
                    break;
                case "Insert":
                    result = (uint)VirtualKeys.Insert;
                    break;
                case "Delete":
                    result = (uint)VirtualKeys.Delete;
                    break;
                case "Home":
                    result = (uint)VirtualKeys.Home;
                    break;
                case "End":
                    result = (uint)VirtualKeys.End;
                    break;
                case "PageUp":
                    result = (uint)VirtualKeys.Prior;
                    break;
                case "PageDown":
                    result = (uint)VirtualKeys.Next;
                    break;
                case "PrintScreen":
                    result = (uint)VirtualKeys.Print;
                    break;
                case "ScrollLock":
                    result = (uint)VirtualKeys.ScrollLock;
                    break;
                case "Pause":
                case "Break":
                    result = (uint)VirtualKeys.Pause;
                    break;
                case "VolumeUp":
                    result = (uint)VirtualKeys.VolumeUp;
                    break;
                case "VolumeDown":
                    result = (uint)VirtualKeys.VolumeDown;
                    break;
                case "VolumeMute":
                    result = (uint)VirtualKeys.VolumeMute;
                    break;
                case "MediaPlayPause":
                    result = (uint)VirtualKeys.MediaPlayPause;
                    break;
                case "Grave":
                case "Tilde":
                    result = (uint)VirtualKeys.OEM3;
                    break;
                default:
                    break;
            }
            return result;
        }

        //public static int ParseMouseButtonCodeString(string key)
        //{
        //    int result = 0;
        //    switch(key)
        //    {
        //        case "LeftButton":
        //        case "Left":
        //            result = MouseButtonCodes.MOUSE_LEFT_BUTTON;
        //            break;
        //        case "RightButton":
        //        case "Right":
        //            result = MouseButtonCodes.MOUSE_RIGHT_BUTTON;
        //            break;
        //        case "MiddleButton":
        //        case "Middle":
        //            result = MouseButtonCodes.MOUSE_MIDDLE_BUTTON;
        //            break;
        //        case "XButton1":
        //            result = MouseButtonCodes.MOUSE_XBUTTON1;
        //            break;
        //        case "XButton2":
        //            result = MouseButtonCodes.MOUSE_XBUTTON2;
        //            break;
        //        default:
        //            break;
        //    }
        //    return result;
        //}

        public enum MouseButtonAliases : int
        {
            None,
            LeftButton = MouseButtonCodes.MOUSE_LEFT_BUTTON,
            Left = LeftButton,
            RightButton = MouseButtonCodes.MOUSE_RIGHT_BUTTON,
            Right = RightButton,
            MiddleButton = MouseButtonCodes.MOUSE_MIDDLE_BUTTON,
            Middle = MiddleButton,
            XButton1 = MouseButtonCodes.MOUSE_XBUTTON1,
            XButton2 = MouseButtonCodes.MOUSE_XBUTTON2,
        }

        public enum MouseWheelAliases : uint
        {
            None = 0,
            WheelUp,
            WheelDown,
            WheelLeft,
            WheelRight,
        }
    }

    public class OutputActionDataTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OutputActionDataSerializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject j = JObject.Load(reader);

            string outputType = j["Type"]?.ToString();
            if (!Enum.TryParse(outputType, out ActionType checkType))
            {
                return null;
            }

            OutputActionDataSerializer resultInstance = null;
            OutputActionData tempInstance = new OutputActionData(ActionType.Empty, 0);
            switch (checkType)
            {
                case ActionType.Keyboard:
                    {
                        tempInstance.OutputType = checkType;
                        string tempKeyAlias = j["Code"]?.ToString() ?? string.Empty;
                        if (uint.TryParse(tempKeyAlias, out uint temp))
                        {
                            // String parses to a uint. Assume native code
                            tempInstance.OutputCode = (int)ProfileSerializer.FakerInputMapper.GetRealEventKey(temp);
                            tempInstance.OutputCodeAlias = (int)temp;
                            tempInstance.OutputCodeStr = tempKeyAlias;
                            //tempInstance.OutputCode = temp;
                        }
                        else
                        {
                            // Check mapping aliases
                            if (!string.IsNullOrEmpty(tempKeyAlias))
                            {
                                if (tempKeyAlias.StartsWith("0x") &&
                                    uint.TryParse(tempKeyAlias.Remove(0, 2), System.Globalization.NumberStyles.HexNumber, null, out temp))
                                {
                                    // alias is a hex number (copied from MS docs?)
                                    tempInstance.OutputCode = (int)ProfileSerializer.FakerInputMapper.GetRealEventKey(temp);
                                    tempInstance.OutputCodeAlias = (int)temp;
                                    tempInstance.OutputCodeStr = tempKeyAlias;
                                }
                                else
                                {
                                    // Check alias for known mapping
                                    temp = OutputActionDataSerializer.ParseKeyboardCodeString(tempKeyAlias);
                                    temp = ProfileSerializer.FakerInputMapper.GetRealEventKey(temp);
                                    tempInstance.OutputCode = (int)temp;
                                    tempInstance.OutputCodeAlias = (int)temp;
                                    tempInstance.OutputCodeStr = tempKeyAlias;
                                }
                            }
                        }

                        DeserializeExtraJSONProperties(tempInstance, j);
                        resultInstance = new OutputActionDataSerializer(tempInstance);
                        break;
                    }
                case ActionType.RelativeMouse:
                    {
                        tempInstance.OutputType = checkType;
                        if (int.TryParse(j["Code"]?.ToString(), out int temp))
                        {
                            tempInstance.OutputCode = temp;
                            tempInstance.OutputCodeAlias = temp;
                        }

                        DeserializeExtraJSONProperties(tempInstance, j);
                        resultInstance = new OutputActionDataSerializer(tempInstance);
                        break;
                    }
                case ActionType.MouseButton:
                    {
                        tempInstance.OutputType = checkType;
                        string tempAlias = j["Code"]?.ToString() ?? string.Empty;
                        if (int.TryParse(tempAlias, out int temp))
                        {
                            // String parses to a int. Assume native code
                            tempInstance.OutputCode = temp;
                            tempInstance.OutputCodeStr = temp.ToString();
                        }
                        else
                        {
                            // Check mapping aliases
                            if (Enum.TryParse(tempAlias,
                                out OutputActionDataSerializer.MouseButtonAliases mButtonAlias))
                            {
                                //temp = OutputActionDataSerializer.ParseMouseButtonCodeString(tempAlias);
                                temp = (int)mButtonAlias;
                                tempInstance.OutputCode = (int)temp;
                                tempInstance.OutputCodeStr = tempAlias;
                            }
                        }

                        DeserializeExtraJSONProperties(tempInstance, j);
                        resultInstance = new OutputActionDataSerializer(tempInstance);
                        break;
                    }
                case ActionType.MouseWheel:
                    tempInstance.OutputType = checkType;
                    //tempInstance.OutputCode = 128;
                    string tempWheelAlias = j["Code"]?.ToString() ?? string.Empty;
                    int wheelTemp = 0;
                    if (int.TryParse(tempWheelAlias, out wheelTemp))
                    {
                        tempInstance.OutputCode = wheelTemp;
                        tempInstance.OutputCodeStr = tempWheelAlias;
                    }
                    else if (Enum.TryParse(tempWheelAlias,
                        out OutputActionDataSerializer.MouseWheelAliases buttonAlias))
                    {
                        wheelTemp = (int)buttonAlias;
                        tempInstance.OutputCode = wheelTemp;
                        tempInstance.OutputCodeStr = tempWheelAlias;
                    }

                    DeserializeExtraJSONProperties(tempInstance, j);
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
                case ActionType.GamepadControl:
                    {
                        tempInstance.OutputType = checkType;
                        if (Enum.TryParse(j["PadOutput"]?.ToString(), out JoypadActionCodes temp))
                        {
                            tempInstance.JoypadCode = temp;
                        }

                        DeserializeExtraJSONProperties(tempInstance, j);
                        resultInstance = new OutputActionDataSerializer(tempInstance);
                        break;
                    }
                case ActionType.SwitchSet:
                    tempInstance.OutputType = checkType;
                    if (int.TryParse(j["Set"]?.ToString(), out int setTemp))
                    {
                        tempInstance.ChangeToSet = setTemp;
                    }

                    DeserializeExtraJSONProperties(tempInstance, j);
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
                case ActionType.SwitchActionLayer:
                    tempInstance.OutputType = checkType;
                    if (int.TryParse(j["Layer"]?.ToString(), out int layerTemp))
                    {
                        tempInstance.ChangeToLayer = layerTemp;
                    }

                    DeserializeExtraJSONProperties(tempInstance, j);
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
                case ActionType.ApplyActionLayer:
                    tempInstance.OutputType = checkType;
                    if (int.TryParse(j["Layer"]?.ToString(), out int applyLayerNumTemp))
                    {
                        tempInstance.ChangeToLayer = applyLayerNumTemp;
                    }

                    DeserializeExtraJSONProperties(tempInstance, j);
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
                case ActionType.RemoveActionLayer:
                    tempInstance.OutputType = checkType;
                    if (int.TryParse(j["Layer"]?.ToString(), out int removeLayerNumTemp))
                    {
                        tempInstance.ChangeToLayer = removeLayerNumTemp;
                    }

                    DeserializeExtraJSONProperties(tempInstance, j);
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
                case ActionType.HoldActionLayer:
                    tempInstance.OutputType = checkType;
                    if (int.TryParse(j["Layer"]?.ToString(), out int holdLayerNumTemp))
                    {
                        tempInstance.ChangeToLayer = holdLayerNumTemp;
                    }

                    DeserializeExtraJSONProperties(tempInstance, j);
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
                case ActionType.Empty:
                    tempInstance.OutputType = ActionType.Empty;
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
                default:
                    //throw new JsonException();
                    tempInstance.OutputType = ActionType.Empty;
                    resultInstance = new OutputActionDataSerializer(tempInstance);
                    break;
            }

            return resultInstance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            OutputActionDataSerializer current = value as OutputActionDataSerializer;

            JObject tempJ = new JObject();

            //writer.WriteStartObject();
            //writer.WritePropertyName("type");
            //writer.WriteValue(current.ActionType.ToString());

            tempJ.Add("Type", current.ActionType.ToString());
            switch (current.ActionType)
            {
                case ActionType.Keyboard:
                    tempJ.Add("Code", current.OutputData.OutputCodeStr);
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.RelativeMouse:
                case ActionType.MouseButton:
                    tempJ.Add("Code", current.OutputData.OutputCode);
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.MouseWheel:
                    tempJ.Add("Code", current.OutputData.OutputCode);
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.GamepadControl:
                    tempJ.Add("PadOutput", current.OutputData.JoypadCode.ToString());
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.SwitchSet:
                    tempJ.Add("Set", current.OutputData.ChangeToSet);
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.SwitchActionLayer:
                    tempJ.Add("Layer", current.OutputData.ChangeToLayer);
                    //SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.ApplyActionLayer:
                    tempJ.Add("Layer", current.OutputData.ChangeToLayer);
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.RemoveActionLayer:
                    //tempJ.Add("Layer", current.OutputData.ChangeToLayer);
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                case ActionType.HoldActionLayer:
                    tempJ.Add("Layer", current.OutputData.ChangeToLayer);
                    SerializeExtraJSONProperties(current.OutputData, tempJ);
                    break;
                default:
                    break;
            }

            serializer.Serialize(new JTokenWriter(tempJ), value);

            //writer.WriteEndObject();
            //JObject j = JObject.FromObject(value);
            //j.WriteTo(writer);
            //serializer.Serialize(writer, value);
        }

        private void DeserializeExtraJSONProperties(OutputActionData actionData, JObject jsonObject)
        {
            switch (actionData.OutputType)
            {
                case ActionType.Keyboard:
                    break;
                case ActionType.RelativeMouse:
                    break;
                case ActionType.MouseWheel:
                    if (int.TryParse(jsonObject["Settings"]?["TickTime"]?.ToString(), out int temp) &&
                        temp > 0)
                    {
                        actionData.CheckTick = true;
                        actionData.DurationMs = temp;
                    }

                    break;
                case ActionType.GamepadControl:
                    if (bool.TryParse(jsonObject["Settings"]?["Negative"]?.ToString(), out bool tempNeg) &&
                        tempNeg)
                    {
                        actionData.Negative = tempNeg;
                    }

                    break;
                case ActionType.SwitchSet:
                    if (Enum.TryParse(jsonObject["Settings"]?["ChangeCondition"]?.ToString(), out SetChangeCondition tempCond))
                    {
                        actionData.ChangeCondition = tempCond;
                    }

                    break;
                case ActionType.SwitchActionLayer:
                    if (Enum.TryParse(jsonObject["Settings"]?["ChangeCondition"]?.ToString(), out ActionLayerChangeCondition tempSwitchLayerCond))
                    {
                        actionData.LayerChangeCondition = tempSwitchLayerCond;
                    }

                    break;
                case ActionType.ApplyActionLayer:
                    if (Enum.TryParse(jsonObject["Settings"]?["ChangeCondition"]?.ToString(), out ActionLayerChangeCondition applyLayerCond))
                    {
                        actionData.LayerChangeCondition = applyLayerCond;
                    }

                    break;
                case ActionType.RemoveActionLayer:
                    if (Enum.TryParse(jsonObject["Settings"]?["ChangeCondition"]?.ToString(), out ActionLayerChangeCondition removeLayerCond))
                    {
                        actionData.LayerChangeCondition = removeLayerCond;
                    }

                    break;
                case ActionType.HoldActionLayer:
                    //if (Enum.TryParse(jsonObject["Settings"]?["ChangeCondition"]?.ToString(), out ActionLayerChangeCondition removeLayerCond))
                    //{
                    //    actionData.LayerChangeCondition = removeLayerCond;
                    //}

                    break;
                default:
                    break;
            }
        }

        private void SerializeExtraJSONProperties(OutputActionData actionData, JObject jsonObject)
        {
            switch (actionData.OutputType)
            {
                case ActionType.Keyboard:
                    break;
                case ActionType.RelativeMouse:
                    break;
                case ActionType.MouseWheel:
                    JObject settingsJ = new JObject();
                    if (!actionData.checkTick && actionData.DurationMs != 0)
                    {
                        settingsJ.Add("TickTime", actionData.DurationMs);
                    }

                    if (settingsJ.Count > 0)
                    {
                        jsonObject.Add("Settings", settingsJ);
                    }

                    break;
                case ActionType.GamepadControl:
                    JObject settingsPadControlJ = new JObject();
                    if (actionData.Negative)
                    {
                        settingsPadControlJ.Add("Negative", actionData.Negative);
                    }

                    if (settingsPadControlJ.Count > 0)
                    {
                        jsonObject.Add("Settings", settingsPadControlJ);
                    }

                    break;
                case ActionType.SwitchSet:
                    JObject settingsSetJ = new JObject();
                    if (actionData.ChangeCondition != SetChangeCondition.None)
                    {
                        settingsSetJ.Add("ChangeCondition", actionData.ChangeCondition.ToString());
                    }

                    if (settingsSetJ.Count > 0)
                    {
                        jsonObject.Add("Settings", settingsSetJ);
                    }

                    break;
                default:
                    break;
            }
        }
    }

    public class TriggerButtonsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(JoypadActionCodes[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<JoypadActionCodes> tempList = new List<JoypadActionCodes>();

            if (reader.TokenType == JsonToken.String)
            {
                tempList.Add(Enum.Parse<JoypadActionCodes>(reader.Value.ToString()));
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                JArray array = JArray.Load(reader);
                JsonConvert.PopulateObject(array.ToString(), tempList);
            }

            return tempList.ToArray();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JoypadActionCodes[] joypadActionCodes = (JoypadActionCodes[])value;
            if (joypadActionCodes.Length == 1)
            {
                serializer.Serialize(writer, joypadActionCodes[0].ToString());
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }
    }

    public class ActionFuncsListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<ActionFuncSerializer>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray array = JArray.Load(reader);
            List<ActionFuncSerializer> funcsList = new List<ActionFuncSerializer>();
            foreach(JToken token in array.Children())
            {
                if (token.Type == JTokenType.Object)
                {
                    ActionFuncSerializer temp = token.ToObject<ActionFuncSerializer>();
                    if (temp != null)
                    {
                        funcsList.Add(temp);
                    }
                }
            }

            return funcsList;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public class SafeStringEnumConverter : StringEnumConverter
    {
        public Enum DefaultValue { get; }

        public SafeStringEnumConverter(Enum defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch
            {
                return DefaultValue;
            }
        }
    }
}
