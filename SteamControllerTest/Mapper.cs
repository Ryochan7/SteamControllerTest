﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Sensorit.Base;

using SteamControllerTest.SteamControllerLibrary;
using FakerInputWrapper;
using System.Runtime.CompilerServices;
using SteamControllerTest.MapperUtil;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.StickActions;
using SteamControllerTest.DPadActions;
using SteamControllerTest.ButtonActions;
using SteamControllerTest.TriggerActions;
using SteamControllerTest.TouchpadActions;
using System.IO;
using Newtonsoft.Json;
using SteamControllerTest.GyroActions;
using System.Windows;

namespace SteamControllerTest
{
    public class Mapper
    {
        //public enum DpadDirections : uint
        //{
        //    Centered,
        //    Up = 1,
        //    Right = 2,
        //    UpRight = 3,
        //    Down = 4,
        //    DownRight = 6,
        //    Left = 8,
        //    UpLeft = 9,
        //    DownLeft = 12,
        //}

        public enum OutputContType : ushort
        {
            None,
            Xbox360,
            DualShock4,
        }

        public struct ButtonKeyAssociation
        {
            public ushort A;
            public ushort B;
            public ushort X;
            public ushort Y;
            public ushort LB;
            public ushort RB;
            public ushort Back;
            public ushort Guide;
            public ushort Start;
            public ushort LSClick;
            public ushort LGrip;
            public ushort RGrip;
            public ushort LeftTouchClick;
            public ushort RightTouchClick;
        }

        public struct TouchpadKeyAssociation
        {
            public ushort Up;
            public ushort Down;
            public ushort Left;
            public ushort Right;
            public ushort Click;
        }

        private const short STICK_MAX = 30000;
        private const short STICK_MIN = -30000;

        private const int inputResolution = STICK_MAX - STICK_MIN;
        private const float reciprocalInputResolution = 1 / (float)inputResolution;

        private const ushort STICK_MIDRANGE = inputResolution / 2;
        private const ushort STICK_NEUTRAL = STICK_MAX - STICK_MIDRANGE;

        private const int X360_STICK_MAX = 32767;
        private const int X360_STICK_MIN = -32768;
        private const int OUTPUT_X360_RESOLUTION = X360_STICK_MAX - X360_STICK_MIN;

        private const int DS4_STICK_MAX = 255;
        private const int DS4_STICK_MIN = 0;
        private const int DS4_STICK_MID = 128;
        private const int OUTPUT_DS4_RESOLUTION = DS4_STICK_MAX - DS4_STICK_MIN;

        private const int LT_DEADZONE = 80;
        private const int RT_DEADZONE = 80;

        private double absMouseX = 0.0;
        private double absMouseY = 0.0;
        private bool absMouseSync;

        public double AbsMouseX
        {
            get => absMouseX; set => absMouseX = value;
        }

        public double AbsMouseY
        {
            get => absMouseY; set => absMouseY = value;
        }

        public bool AbsMouseSync
        {
            get => absMouseSync;
            set => absMouseSync = value;
        }

        private double mouseX = 0.0;
        private double mouseY = 0.0;
        private bool mouseSync;
        public bool MouseSync { get => mouseSync; set => mouseSync = value; }

        /// <summary>
        /// Used to tell the Mapper that a MapAction fired a mouse event. Mainly so
        /// Mapper skips resetting mouse remainders
        /// </summary>
        private bool mouseEventFired;
        public bool MouseEventFired { get => mouseEventFired; set => mouseEventFired = value; }

        public double MouseX { get => mouseX; set => mouseX = value; }
        public double MouseY { get => mouseY; set => mouseY = value; }
        private double mouseXRemainder = 0.0;
        private double mouseYRemainder = 0.0;
        public double MouseXRemainder { get => mouseXRemainder; set => mouseXRemainder = value; }
        public double MouseYRemainder { get => mouseYRemainder; set => mouseYRemainder = value; }

        private int mouseWheelX;
        private int mouseWheelY;
        private bool mouseWheelSync;
        public int MouseWheelX
        {
            get => mouseWheelX; set => mouseWheelX = value;
        }

        public int MouseWheelY
        {
            get => mouseWheelY; set => mouseWheelY = value;
        }

        public bool MouseWheelSync
        {
            get => mouseWheelSync; set => mouseWheelSync = value;
        }

        //private OneEuroFilter mStickFilterX = new OneEuroFilter(minCutoff: 0.6, beta: 0.7);
        //private OneEuroFilter mStickFilterY = new OneEuroFilter(minCutoff: 0.6, beta: 0.7);
        private OneEuroFilter filterX = new OneEuroFilter(minCutoff: 0.4, beta: 0.6);
        private OneEuroFilter filterY = new OneEuroFilter(minCutoff: 0.4, beta: 0.6);

        private bool gamepadSync;

        private double currentRate = 1.0; // Expressed in Hz
        private double currentLatency = 1.0; // Expressed in sec
        private TouchEventFrame previousTouchFrameLeftPad;
        private TouchEventFrame previousTouchFrameRightPad;

        private const int EMPTY_QUEUED_ACTION_SET = -1;
        private int queuedActionSet = EMPTY_QUEUED_ACTION_SET;
        public int QueuedActionSet { get => queuedActionSet; set => queuedActionSet = value; }

        private const int EMPTY_QUEUED_ACTION_LAYER = -1;
        private int queuedActionLayer = EMPTY_QUEUED_ACTION_LAYER;
        private bool applyQueuedActionLayer;
        private bool switchQueuedActionLayer;
        public int QueuedActionLayer { get => queuedActionLayer; set => queuedActionLayer = value; }

        public double CurrentLatency { get => currentLatency; }
        public List<OutputActionData> PendingReleaseActions { get => pendingReleaseActions; set => pendingReleaseActions = value; }
        private List<ActionFunc> pendingReleaseFuns = new List<ActionFunc>();
        public List<ActionFunc> PendingReleaseFuns { get => pendingReleaseFuns; }

        private bool quit = false;
        public bool Quit { get => quit; set => quit = value; }

        public event EventHandler<string> ProfileChanged;

        //public OneEuroFilter FilterX { get => filterX; set => filterX = value; }
        //public OneEuroFilter FilterY { get => filterY; set => filterY = value; }
        public double CurrentRate { get => currentRate; set => currentRate = value; }
        //public OneEuroFilter MStickFilterX { get => mStickFilterX; set => mStickFilterX = value; }
        //public OneEuroFilter MStickFilterY { get => mStickFilterY; set => mStickFilterY = value; }


        private StickDefinition lsDefintion;
        private TouchpadDefinition leftPadDefiniton;
        private TouchpadDefinition rightPadDefinition;
        private TriggerDefinition leftTriggerDefinition;
        private TriggerDefinition rightTriggerDefinition;
        private GyroSensDefinition gyroSensDefinition;

        private Profile actionProfile = new Profile();
        public Profile ActionProfile => actionProfile;
        private IntermediateState intermediateState = new IntermediateState();
        public ref IntermediateState IntermediateStateRef => ref intermediateState;

        // Have Mapper make copies of state structs for manipulation. Allow
        // device reader state structs to remain untouched
        private SteamControllerState currentMapperState;
        private SteamControllerState previousMapperState;

        private List<OutputActionData> pendingReleaseActions =
            new List<OutputActionData>();

        // Keep reference to current editing action set from GUI
        // Allows different profile usage from mapper
        private ActionSet editActionSet;
        public ActionSet EditActionSet
        {
            get => editActionSet; set => editActionSet = value;
        }

        // Keep reference to current editing action layer from GUI
        // Allows different profile usage from mapper
        private ActionLayer editLayer;
        public ActionLayer EditLayer
        {
            get => editLayer; set => editLayer = value;
        }

        // VK, Count
        private static Dictionary<uint, int> keyReferenceCountDict = new Dictionary<uint, int>();
        // VK
        private static HashSet<uint> activeKeys = new HashSet<uint>();
        // VK
        private static HashSet<uint> releasedKeys = new HashSet<uint>();

        private static HashSet<int> currentMouseButtons = new HashSet<int>();
        private static HashSet<int> activeMouseButtons = new HashSet<int>();
        private static HashSet<int> releasedMouseButtons = new HashSet<int>();

        private FakerInputHandler fakerInputHandler;

        public class RequestOSDArgs : EventArgs
        {
            private string displayMessage;
            public string Message
            {
                get => displayMessage;
            }

            public RequestOSDArgs(string message)
            {
                displayMessage = message;
            }
        }

        public event EventHandler<RequestOSDArgs> RequestOSD;

        protected List<InputBindingMeta> bindingList = new List<InputBindingMeta>();
        public List<InputBindingMeta> BindingList
        {
            get => bindingList;
        }

        private Dictionary<string, InputBindingMeta> bindingDict = new Dictionary<string, InputBindingMeta>();
        public Dictionary<string, InputBindingMeta> BindingDict
        {
            get => bindingDict;
        }

        protected Dictionary<string, StickDefinition> knownStickDefinitions =
            new Dictionary<string, StickDefinition>();
        protected Dictionary<string, TriggerDefinition> knownTriggerDefinitions =
            new Dictionary<string, TriggerDefinition>();
        protected Dictionary<string, TouchpadDefinition> knownTouchpadDefinitions =
            new Dictionary<string, TouchpadDefinition>();
        protected Dictionary<string, GyroSensDefinition> knownGyroSensDefinitions =
            new Dictionary<string, GyroSensDefinition>();

        private string profileFile;
        public string ProfileFile
        {
            get => profileFile;
            set
            {
                profileFile = value;
            }
        }

        //public delegate void ProfileChangeHandler(Mapper sender, string profilePath);
        //public event ProfileChangeHandler ProfileChanged;

        //private ViGEmClient vigemTestClient = null;
        //private IXbox360Controller outputX360 = null;
        //private IVirtualGamepad outputController = null;
        private X360BusDevice x360busDevice = null;
        private Xbox360ScpOutDevice outputControllerSCP = null;
        private OutputContType outputControlType = OutputContType.None;
        //private Thread contThr;

        private SteamControllerDevice device;
        private SteamControllerReader reader;

        private bool mouseLBDown;
        private bool mouseRBDown;
        private ButtonKeyAssociation buttonBindings;
        private TouchpadKeyAssociation leftTouchBindings;
        private TouchpadKeyAssociation rightTouchBindings;
        private DpadDirections currentLeftDir;
        private DpadDirections previousLeftDir;

        private const int TRACKBALL_INIT_FRICTION = 10;
        private const int TRACKBALL_JOY_FRICTION = 7;
        private const int TRACKBALL_MASS = 45;
        private const double TRACKBALL_RADIUS = 0.0245;

        private double TRACKBALL_INERTIA = 2.0 * (TRACKBALL_MASS * TRACKBALL_RADIUS * TRACKBALL_RADIUS) / 5.0;
        //private double TRACKBALL_SCALE = 0.000023;
        private double TRACKBALL_SCALE = 0.000023;
        private const int TRACKBALL_BUFFER_LEN = 8;
        private double[] trackballXBuffer = new double[TRACKBALL_BUFFER_LEN];
        private double[] trackballYBuffer = new double[TRACKBALL_BUFFER_LEN];
        private int trackballBufferTail = 0;
        private int trackballBufferHead = 0;
        private double trackballAccel = 0.0;
        private double trackballXVel = 0.0;
        private double trackballYVel = 0.0;
        private bool trackballActive = false;
        private double trackballDXRemain = 0.0;
        private double trackballDYRemain = 0.0;

        //private OneEuroFilter filterX = new OneEuroFilter(1.0, 0.5);
        //private OneEuroFilter filterY = new OneEuroFilter(1.0, 0.5);
        //private OneEuroFilter filterX = new OneEuroFilter(2.0, 0.8);
        //private OneEuroFilter filterY = new OneEuroFilter(2.0, 0.8);
        //private double currentRate = 0.0;

        //private FakerInput fakerInput = new FakerInput();
        //private KeyboardReport keyboardReport = new KeyboardReport();
        //private RelativeMouseReport mouseReport = new RelativeMouseReport();
        //private KeyboardEnhancedReport mediaKeyboardReport = new KeyboardEnhancedReport();

        private bool inMapperEvent;
        private bool calibrationFinished;

        private bool keyboardSync = false;
        private bool keyboardEnhancedSync = false;
        //private bool mouseSync = false;

        private bool processCycle = false;
        private List<CycleButton> processCycleList = new List<CycleButton>();

        private AppGlobalData appGlobal;
        public AppGlobalData AppGlobal
        {
            get => appGlobal;
        }

        public InputDeviceType DeviceType => InputDeviceType.SteamController;

        private DeviceActionDefaultsCreator deviceActionDefaults = new SteamControllerActionDefaultsCreator();
        public DeviceActionDefaultsCreator DeviceActionDefaults => deviceActionDefaults;

        //private Xbox360FeedbackReceivedEventHandler outputForceFeedbackDel;

        private bool hasInputEvts;
        //private object eventQueueLock = new object();
        private ReaderWriterLockSlim eventQueueLocker = new ReaderWriterLockSlim();
        private Queue<Action> eventQueue = new Queue<Action>();

        public Mapper(SteamControllerDevice device, string profileFile,
            AppGlobalData appGlobal)
        {
            this.appGlobal = appGlobal;
            this.profileFile = profileFile;
            this.device = device;

            bindingList = new List<InputBindingMeta>()
            {
                new InputBindingMeta("A", "A", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("B", "B", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("X", "X", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("Y", "Y", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("Back", "Back", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("Start", "Start", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("LShoulder", "Left Shoulder", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("RShoulder", "Right Shoulder", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("LSClick", "Stick Click", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("LeftGrip", "Left Grip", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("RightGrip", "Right Grip", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("LT", "Left Trigger", InputBindingMeta.InputControlType.Trigger),
                new InputBindingMeta("RT", "Right Trigger", InputBindingMeta.InputControlType.Trigger),
                new InputBindingMeta("Steam", "Steam", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("Stick", "Stick", InputBindingMeta.InputControlType.Stick),
                new InputBindingMeta("LeftTouchpad", "Left Touchpad", InputBindingMeta.InputControlType.Touchpad),
                new InputBindingMeta("RightTouchpad", "Right Touchpad", InputBindingMeta.InputControlType.Touchpad),
                new InputBindingMeta("LeftPadClick", "Left Pad Click", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("RightPadClick", "Right Pad Click", InputBindingMeta.InputControlType.Button),
                new InputBindingMeta("Gyro", "Gyro", InputBindingMeta.InputControlType.Gyro),
            };

            // Populate Input Binding dictionary
            bindingList.ForEach((item) => bindingDict.Add(item.id, item));

            //trackballAccel = TRACKBALL_RADIUS * TRACKBALL_INIT_FRICTION / TRACKBALL_INERTIA;
            trackballAccel = TRACKBALL_RADIUS * TRACKBALL_JOY_FRICTION / TRACKBALL_INERTIA;

            StickDefinition.StickAxisData lxAxis = new StickDefinition.StickAxisData
            {
                min = -30000,
                max = 30000,
                mid = 0,
                hard_max = 32767,
                hard_min = -32767,
            };
            StickDefinition.StickAxisData lyAxis = new StickDefinition.StickAxisData
            {
                min = -30000,
                max = 30000,
                mid = 0,
                hard_max = 32767,
                hard_min = -32767,
            };
            //StickDefinition lsDefintion = new StickDefinition(STICK_MIN, STICK_MAX, STICK_NEUTRAL, StickActionCodes.LS);
            lsDefintion = new StickDefinition(lxAxis, lyAxis, StickActionCodes.LS);

            TouchpadDefinition.TouchAxisData lpadXAxis = new TouchpadDefinition.TouchAxisData
            {
                min = -32768,
                max = 32767,
                mid = 0,

                hard_max = 32767,
                hard_min = -32768,
            };
            lpadXAxis.PostInit();

            TouchpadDefinition.TouchAxisData lpadYAxis = new TouchpadDefinition.TouchAxisData
            {
                min = -32768,
                max = 32767,
                mid = 0,

                hard_max = 32767,
                hard_min = -32768,
            };
            lpadYAxis.PostInit();

            leftPadDefiniton = new TouchpadDefinition(lpadXAxis, lpadYAxis, TouchpadActionCodes.TouchL,
                elapsedReference: device.BaseElapsedReference, mouseScale: 0.012 * 1.1, mouseOffset: 0.4,
                trackballScale: 0.000023);

            TouchpadDefinition.TouchAxisData rpadXAxis = new TouchpadDefinition.TouchAxisData
            {
                min = -32768,
                max = 32767,
                mid = 0,

                hard_max = 32767,
                hard_min = -32768,
            };
            rpadXAxis.PostInit();

            TouchpadDefinition.TouchAxisData rpadYAxis = new TouchpadDefinition.TouchAxisData
            {
                min = -32768,
                max = 32767,
                mid = 0,

                hard_max = 32767,
                hard_min = -32768,
            };
            rpadYAxis.PostInit();

            rightPadDefinition = new TouchpadDefinition(rpadXAxis, rpadYAxis, TouchpadActionCodes.TouchR,
                elapsedReference: device.BaseElapsedReference, mouseScale: 0.012 * 1.1, mouseOffset: 0.4,
                trackballScale: 0.000023);

            TriggerDefinition.TriggerAxisData ltAxis = new TriggerDefinition.TriggerAxisData
            {
                min = 0,
                max = 255,
                hasClickButton = true,
                fullClickBtnCode = JoypadActionCodes.LTFullPull,
            };

            leftTriggerDefinition = new TriggerDefinition(ltAxis, TriggerActionCodes.LeftTrigger);

            // Copy struct
            TriggerDefinition.TriggerAxisData rtAxis = new TriggerDefinition.TriggerAxisData
            {
                min = 0,
                max = 255,
                hasClickButton = true,
                fullClickBtnCode = JoypadActionCodes.RTFullPull,
            };

            rightTriggerDefinition = new TriggerDefinition(rtAxis, TriggerActionCodes.LeftTrigger);

            gyroSensDefinition = new GyroSensDefinition()
            {
                elapsedReference = 125.0,
                mouseCoefficient = 0.025,
                mouseOffset = 0.3,

                accelMinLeanX = -16384,
                accelMaxLeanX = 16384,
                accelMinLeanY = -16384,
                accelMaxLeanY = 16384,
                accelMinLeanZ = -16384,
                accelMaxLeanZ = 16384,
            };

            knownStickDefinitions.Add("Stick", lsDefintion);
            knownTriggerDefinitions.Add("LT", leftTriggerDefinition);
            knownTriggerDefinitions.Add("RT", rightTriggerDefinition);
            knownTouchpadDefinitions.Add("LeftTouchpad", leftPadDefiniton);
            knownTouchpadDefinitions.Add("RightTouchpad", rightPadDefinition);
            knownGyroSensDefinitions.Add("Gyro", gyroSensDefinition);

            //ReadFromProfile();

            /*CycleButton testCycle = new CycleButton("Weapon Cycle");
            testCycle.Actions.AddRange(new OutputActionData[]
            {
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number1),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number2),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number3),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number4),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number5),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number6),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number7),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number8),
                new OutputActionData(OutputActionData.ActionType.Keyboard, (int)KeyboardKey.Number9),
            });
            testCycle.ResetCycle();
            actionProfile.CycleBindings.Add(testCycle.CycleIdentifier, testCycle);
            */
        }

        //public void Start(ViGEmClient vigemTestClient,
        //    SwitchProDevice proDevice, SwitchProReader proReader)

        public void Start(X360BusDevice busDevice,
            FakerInputHandler fakerInputHandler,
            SteamControllerDevice device, SteamControllerReader reader)
        {
            //bool checkConnect = fakerInput.Connect();
            //this.vigemTestClient = vigemTestClient;
            this.fakerInputHandler = fakerInputHandler;
            this.x360busDevice = busDevice;
            //bool checkConnect = fakerInputHandler.Connect();
            //Trace.WriteLine(checkConnect);

            //PopulateKeyBindings();

            this.reader = reader;
            //reader.Report += ControllerReader_Report;

            if (!string.IsNullOrEmpty(profileFile))
            {
                try
                {
                    ChangeProfile(profileFile);
                }
                catch (JsonException)
                {
                    // Add log message eventually
                    // Blank profile will be loaded before exception is rethrown
                }
            }
            else
            {
                UseBlankProfile();
            }

            //reader.Report += Reader_Calibrate_Gyro;

            reader.StartUpdate();
        }

        private void ReadFromProfile()
        {
            editActionSet = null;
            editLayer = null;

            actionProfile = new Profile();
            Profile tempProfile = actionProfile;

            tempProfile.ActionSets.Clear();
            List<ProfileActionsMapping> tempMappings = null;

            using (StreamReader sreader = new StreamReader(profileFile))
            {
                ProfileSerializer profileSerializer = new ProfileSerializer(tempProfile);

                string json = sreader.ReadToEnd();

                try
                {
                    JsonConvert.PopulateObject(json, profileSerializer);
                }
                catch (JsonSerializationException)
                {
                    UseBlankProfile();
                    return;
                }

                profileSerializer.PopulateProfile();
                tempProfile.ResetAliases();
                tempMappings = profileSerializer.ActionMappings;
            }

            //tempProfile.LeftTouchpadRotation = device.DeviceOptions.LeftTouchpadRotation;
            //tempProfile.RightTouchpadRotation = device.DeviceOptions.RightTouchpadRotation;

            // Populate ActionLayer dicts with default no action elements
            foreach (ActionSet set in tempProfile.ActionSets)
            {
                //ActionLayer layer = set.ActionLayers.First();
                //if (layer != null)

                int layerIndex = 0;
                foreach (ActionLayer layer in set.ActionLayers)
                {
                    if (layerIndex == 0)
                    {
                        foreach (KeyValuePair<string, InputBindingMeta> tempMeta in bindingDict)
                        {
                            switch (tempMeta.Value.controlType)
                            {
                                case InputBindingMeta.InputControlType.Button:
                                    ButtonNoAction btnNoAction = new ButtonNoAction();
                                    btnNoAction.MappingId = tempMeta.Key;
                                    layer.buttonActionDict.Add(tempMeta.Key, btnNoAction);
                                    break;
                                case InputBindingMeta.InputControlType.DPad:
                                    DPadNoAction dpadNoAction = new DPadNoAction();
                                    dpadNoAction.MappingId = tempMeta.Key;
                                    layer.dpadActionDict.Add(tempMeta.Key, dpadNoAction);
                                    break;
                                case InputBindingMeta.InputControlType.Stick:
                                    {
                                        StickNoAction stickNoAct = new StickNoAction();
                                        stickNoAct.MappingId = tempMeta.Key;
                                        if (knownStickDefinitions.TryGetValue(tempMeta.Key,
                                                out StickDefinition tempDef))
                                        {
                                            stickNoAct.StickDefinition = tempDef;
                                        }
                                        layer.stickActionDict.Add(tempMeta.Key, stickNoAct);
                                    }

                                    break;
                                case InputBindingMeta.InputControlType.Trigger:
                                    {
                                        TriggerNoAction trigNoAct = new TriggerNoAction();
                                        trigNoAct.MappingId = tempMeta.Key;
                                        if (knownTriggerDefinitions.TryGetValue(tempMeta.Key,
                                            out TriggerDefinition tempDef))
                                        {
                                            trigNoAct.TriggerDef = tempDef;
                                        }
                                        layer.triggerActionDict.Add(tempMeta.Key, trigNoAct);
                                    }

                                    break;
                                case InputBindingMeta.InputControlType.Touchpad:
                                    {
                                        TouchpadNoAction touchNoAct = new TouchpadNoAction();
                                        touchNoAct.MappingId = tempMeta.Key;
                                        if (knownTouchpadDefinitions.TryGetValue(tempMeta.Key,
                                            out TouchpadDefinition tempDef))
                                        {
                                            touchNoAct.TouchDefinition = tempDef;
                                        }
                                        layer.touchpadActionDict.Add(tempMeta.Key, touchNoAct);
                                    }

                                    break;
                                case InputBindingMeta.InputControlType.Gyro:
                                    {
                                        GyroNoMapAction gyroNoMapAct = new GyroNoMapAction();
                                        gyroNoMapAct.MappingId = tempMeta.Key;
                                        if (knownGyroSensDefinitions.TryGetValue(tempMeta.Key,
                                                out GyroSensDefinition tempDef))
                                        {
                                            gyroNoMapAct.GyroSensDefinition = tempDef;
                                        }
                                        layer.gyroActionDict.Add(tempMeta.Key, gyroNoMapAct);
                                    }

                                    break;
                                default:
                                    break;
                            }
                        }

                        ButtonNoAction btnNoActionSet = new ButtonNoAction();
                        btnNoActionSet.MappingId = $"{ActionSet.ACTION_SET_ACTION_PREFIX}{set.Index}";
                        layer.actionSetActionDict.Add(btnNoActionSet.MappingId, btnNoActionSet);
                    }

                    layerIndex++;
                }
            }

            if (tempMappings != null)
            {
                foreach (ProfileActionsMapping mapping in tempMappings)
                {
                    ActionSet tempSet = null;
                    ActionLayer tempLayer = null;
                    if (mapping.ActionSet >= 0 && mapping.ActionSet < tempProfile.ActionSets.Count)
                    {
                        tempSet = tempProfile.ActionSets[mapping.ActionSet];
                        if (mapping.ActionLayer >= 0 && mapping.ActionLayer < tempSet.ActionLayers.Count)
                        {
                            tempLayer = tempSet.ActionLayers[mapping.ActionLayer];
                        }
                    }

                    if (tempLayer != null)
                    {
                        //ActionLayer parentLayer = (mapping.ActionLayer > 0 && mapping.ActionLayer < tempLayer.LayerActions.Count) ? tempLayer : null;
                        ActionLayer parentLayer = tempLayer != tempSet.DefaultActionLayer ? tempSet.DefaultActionLayer : null;
                        foreach (LayerMapping layerMapping in mapping.LayerMappings)
                        {
                            MapAction tempAction = layerMapping.ActionIndex >= 0 ?
                                tempLayer.LayerActions.Find((act) => act.Id == layerMapping.ActionIndex) : null;
                            if (tempAction != null)// layerMapping.ActionIndex < tempLayer.LayerActions.Count)
                            {
                                //MapAction tempAction = tempLayer.LayerActions[layerMapping.ActionIndex];
                                if (bindingDict.TryGetValue(layerMapping.InputBinding, out InputBindingMeta tempBind))
                                {
                                    switch (tempBind.controlType)
                                    {
                                        case InputBindingMeta.InputControlType.Button:
                                            if (tempAction is ButtonMapAction)
                                            {
                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
                                                if (parentLayer != null && parentLayer.buttonActionDict.TryGetValue(tempBind.id, out ButtonMapAction tempParentBtnAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentBtnAction))
                                                {
                                                    //(tempAction as ButtonMapAction).SoftCopyFromParent(tempParentBtnAction);
                                                    //(tempAction as ButtonMapAction).CopyAction(tempParentBtnAction);
                                                }

                                                //if (parentLayer != null && parentLayer.LayerActions[layerMapping.ActionIndex] is ButtonMapAction)
                                                //{
                                                //    tempLayer.buttonActionDict[tempBind.id] = (tempAction as ButtonMapAction).DuplicateAction();
                                                //}
                                                //else
                                                //{
                                                //    tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
                                                //}
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.DPad:
                                            if (tempAction is DPadMapAction)
                                            {
                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.dpadActionDict[tempBind.id] = tempAction as DPadMapAction;
                                                if (parentLayer != null && parentLayer.dpadActionDict.TryGetValue(tempBind.id, out DPadMapAction tempParentDpadAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentDpadAction))
                                                {
                                                    (tempAction as DPadMapAction).SoftCopyFromParent(tempParentDpadAction);
                                                }
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Stick:
                                            if (tempAction is StickMapAction)
                                            {
                                                StickMapAction tempStickAction = tempAction as StickMapAction;
                                                if (knownStickDefinitions.TryGetValue(tempBind.id,
                                                    out StickDefinition tempDef))
                                                {
                                                    tempStickAction.StickDefinition = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.stickActionDict[tempBind.id] = tempStickAction;

                                                if (parentLayer != null && parentLayer.stickActionDict.TryGetValue(tempBind.id, out StickMapAction tempParentStickAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentStickAction))
                                                {
                                                    (tempAction as StickMapAction).SoftCopyFromParent(tempParentStickAction);
                                                }
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Trigger:
                                            if (tempAction is TriggerMapAction)
                                            {
                                                TriggerMapAction triggerAct = tempAction as TriggerMapAction;
                                                if (knownTriggerDefinitions.TryGetValue(tempBind.id, out TriggerDefinition tempDef))
                                                {
                                                    triggerAct.TriggerDef = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.triggerActionDict[tempBind.id] = tempAction as TriggerMapAction;
                                                if (parentLayer != null && parentLayer.triggerActionDict.TryGetValue(tempBind.id, out TriggerMapAction tempParentTrigAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentTrigAction))
                                                {
                                                    (tempAction as TriggerMapAction).SoftCopyFromParent(tempParentTrigAction);
                                                }
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Touchpad:
                                            if (tempAction is TouchpadMapAction)
                                            {
                                                TouchpadMapAction touchAct = tempAction as TouchpadMapAction;
                                                if (knownTouchpadDefinitions.TryGetValue(tempBind.id, out TouchpadDefinition tempDef))
                                                {
                                                    touchAct.TouchDefinition = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.touchpadActionDict[tempBind.id] = tempAction as TouchpadMapAction;
                                                if (parentLayer != null && parentLayer.touchpadActionDict.TryGetValue(tempBind.id, out TouchpadMapAction tempParentTouchAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentTouchAction))
                                                {
                                                    (tempAction as TouchpadMapAction).SoftCopyFromParent(tempParentTouchAction);
                                                }

                                                touchAct.PrepareActions();
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Gyro:
                                            if (tempAction is GyroMapAction)
                                            {
                                                GyroMapAction gyroAction = tempAction as GyroMapAction;
                                                //if (tempBind.id == "Gyro")
                                                if (knownGyroSensDefinitions.TryGetValue(tempBind.id, out GyroSensDefinition tempDef))
                                                {
                                                    gyroAction.GyroSensDefinition = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.gyroActionDict[tempBind.id] = tempAction as GyroMapAction;
                                                if (parentLayer != null && parentLayer.gyroActionDict.TryGetValue(tempBind.id, out GyroMapAction tempParentGyroAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentGyroAction))
                                                {
                                                    (tempAction as GyroMapAction).SoftCopyFromParent(tempParentGyroAction);
                                                }
                                            }

                                            break;
                                        default:
                                            break;
                                    }
                                }
                                else if (layerMapping.InputBinding == $"{ActionSet.ACTION_SET_ACTION_PREFIX}{mapping.ActionSet}" && tempAction is ButtonMapAction)
                                {
                                    //if (tempAction is ButtonMapAction)
                                    {
                                        //tempAction.DefaultUnbound = false;
                                        tempAction.MappingId = $"{ActionSet.ACTION_SET_ACTION_PREFIX}{mapping.ActionSet}";
                                        tempLayer.actionSetActionDict[tempAction.MappingId] = tempAction as ButtonMapAction;
                                        if (parentLayer != null && parentLayer.actionSetActionDict.TryGetValue(tempAction.MappingId, out ButtonMapAction tempParentBtnAction) &&
                                            MapAction.IsSameType(tempAction, tempParentBtnAction))
                                        {
                                            //(tempAction as ButtonMapAction).SoftCopyFromParent(tempParentBtnAction);
                                            //(tempAction as ButtonMapAction).CopyAction(tempParentBtnAction);
                                        }

                                        //if (parentLayer != null && parentLayer.LayerActions[layerMapping.ActionIndex] is ButtonMapAction)
                                        //{
                                        //    tempLayer.buttonActionDict[tempBind.id] = (tempAction as ButtonMapAction).DuplicateAction();
                                        //}
                                        //else
                                        //{
                                        //    tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
                                        //}
                                    }
                                }
                            }
                        }

                    }
                }
            }

            //tempProfile.CurrentActionSet.CreateDupActionLayer();
            //tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
            //(tempProfile.CurrentActionSet.ActionLayers[1].buttonActionDict["A"] as ButtonAction).ActionFuncs.Clear();
            //(tempProfile.CurrentActionSet.ActionLayers[1].buttonActionDict["A"] as ButtonAction).ActionFuncs.Add(new NormalPressFunc(new OutputActionData(OutputActionData.ActionType.Keyboard, KeyInterop.VirtualKeyFromKey(Key.L))));
            //new ButtonAction(new OutputActionData(OutputActionData.ActionType.Keyboard, KeyInterop.VirtualKeyFromKey(Key.L)));

            // SyncActions for currently active ActionLayer instance
            //foreach (ActionSet set in tempProfile.ActionSets)
            //{
            //    set.CurrentActionLayer.SyncActions();
            //}

            // Compile convenience List for MapActions instances in layers
            foreach (ActionSet set in tempProfile.ActionSets)
            {
                //int layerIdx = -1;
                ActionLayer parentLayer = set.DefaultActionLayer;
                foreach (ActionLayer layer in set.ActionLayers)
                {
                    //layerIdx++;
                    //if (layerIdx > 0)
                    //{
                    //    parentLayer.MergeLayerActions(layer);
                    //}

                    layer.SyncActions();
                }
            }

            // Prepare initial composite ActionLayer instance using
            // base ActionLayer references
            foreach (ActionSet set in tempProfile.ActionSets)
            {
                //ActionLayer parentLayer = set.DefaultActionLayer;
                set.ClearCompositeLayerActions();
                set.PrepareCompositeLayer();
            }

            //tempProfile.CurrentActionSet.SwitchActionLayer(this, 1);

            Trace.WriteLine("IT IS FINISHED");
        }

        public void UseBlankProfile()
        {
            actionProfile = new Profile();
            actionProfile.Name = "Blank";
            Profile tempProfile = actionProfile;
            profileFile = string.Empty;

            //tempProfile.ActionSets.Clear();
            PrepareProfileActions(null);
        }

        private void PrepareProfileActions(List<ProfileActionsMapping> tempMappings)
        {
            Profile tempProfile = actionProfile;

            // Populate ActionLayer dicts with default no action elements
            foreach (ActionSet set in tempProfile.ActionSets)
            {
                //ActionLayer layer = set.ActionLayers.First();
                //if (layer != null)

                int layerIndex = 0;
                foreach (ActionLayer layer in set.ActionLayers)
                {
                    if (layerIndex == 0)
                    {
                        foreach (KeyValuePair<string, InputBindingMeta> tempMeta in bindingDict)
                        {
                            switch (tempMeta.Value.controlType)
                            {
                                case InputBindingMeta.InputControlType.Button:
                                    ButtonNoAction btnNoAction = new ButtonNoAction();
                                    btnNoAction.MappingId = tempMeta.Key;
                                    layer.buttonActionDict.Add(tempMeta.Key, btnNoAction);
                                    break;
                                case InputBindingMeta.InputControlType.DPad:
                                    DPadNoAction dpadNoAction = new DPadNoAction();
                                    dpadNoAction.MappingId = tempMeta.Key;
                                    layer.dpadActionDict.Add(tempMeta.Key, dpadNoAction);
                                    break;
                                case InputBindingMeta.InputControlType.Stick:
                                    {
                                        StickNoAction stickNoAct = new StickNoAction();
                                        stickNoAct.MappingId = tempMeta.Key;
                                        if (knownStickDefinitions.TryGetValue(tempMeta.Key, out StickDefinition tempDef))
                                        {
                                            stickNoAct.StickDefinition = tempDef;
                                        }
                                        layer.stickActionDict.Add(tempMeta.Key, stickNoAct);
                                    }

                                    break;
                                case InputBindingMeta.InputControlType.Trigger:
                                    {
                                        TriggerNoAction trigNoAct = new TriggerNoAction();
                                        trigNoAct.MappingId = tempMeta.Key;
                                        if (knownTriggerDefinitions.TryGetValue(tempMeta.Key, out TriggerDefinition tempDef))
                                        {
                                            trigNoAct.TriggerDef = tempDef;
                                        }
                                        layer.triggerActionDict.Add(tempMeta.Key, trigNoAct);
                                    }

                                    break;
                                case InputBindingMeta.InputControlType.Touchpad:
                                    {
                                        TouchpadNoAction touchNoAct = new TouchpadNoAction();
                                        touchNoAct.MappingId = tempMeta.Key;
                                        if (knownTouchpadDefinitions.TryGetValue(tempMeta.Key, out TouchpadDefinition tempDef))
                                        {
                                            touchNoAct.TouchDefinition = tempDef;
                                        }
                                        layer.touchpadActionDict.Add(tempMeta.Key, touchNoAct);
                                    }

                                    break;
                                case InputBindingMeta.InputControlType.Gyro:
                                    {
                                        GyroNoMapAction gyroNoMapAct = new GyroNoMapAction();
                                        gyroNoMapAct.MappingId = tempMeta.Key;
                                        if (knownGyroSensDefinitions.TryGetValue(tempMeta.Key, out GyroSensDefinition tempDef))
                                        {
                                            gyroNoMapAct.GyroSensDefinition = tempDef;
                                        }

                                        layer.gyroActionDict.Add(tempMeta.Key, gyroNoMapAct);
                                    }

                                    break;
                                default:
                                    break;
                            }
                        }

                        if (!layer.actionSetActionDict.ContainsKey($"{ActionSet.ACTION_SET_ACTION_PREFIX}{set.Index}"))
                        {
                            ButtonNoAction btnNoActionSet = new ButtonNoAction();
                            btnNoActionSet.MappingId = $"{ActionSet.ACTION_SET_ACTION_PREFIX}{set.Index}";
                            layer.actionSetActionDict.Add(btnNoActionSet.MappingId, btnNoActionSet);
                        }
                    }

                    layerIndex++;
                }
            }

            if (tempMappings != null)
            {
                foreach (ProfileActionsMapping mapping in tempMappings)
                {
                    ActionSet tempSet = null;
                    ActionLayer tempLayer = null;
                    if (mapping.ActionSet >= 0 && mapping.ActionSet < tempProfile.ActionSets.Count)
                    {
                        tempSet = tempProfile.ActionSets[mapping.ActionSet];
                        if (mapping.ActionLayer >= 0 && mapping.ActionLayer < tempSet.ActionLayers.Count)
                        {
                            tempLayer = tempSet.ActionLayers[mapping.ActionLayer];
                        }
                    }

                    if (tempLayer != null)
                    {
                        //ActionLayer parentLayer = (mapping.ActionLayer > 0 && mapping.ActionLayer < tempLayer.LayerActions.Count) ? tempLayer : null;
                        ActionLayer parentLayer = tempLayer != tempSet.DefaultActionLayer ? tempSet.DefaultActionLayer : null;
                        foreach (LayerMapping layerMapping in mapping.LayerMappings)
                        {
                            MapAction tempAction = layerMapping.ActionIndex >= 0 ?
                                tempLayer.LayerActions.Find((act) => act.Id == layerMapping.ActionIndex) : null;
                            if (tempAction != null)// layerMapping.ActionIndex < tempLayer.LayerActions.Count)
                            {
                                //MapAction tempAction = tempLayer.LayerActions[layerMapping.ActionIndex];
                                if (bindingDict.TryGetValue(layerMapping.InputBinding, out InputBindingMeta tempBind))
                                {
                                    switch (tempBind.controlType)
                                    {
                                        case InputBindingMeta.InputControlType.Button:
                                            if (tempAction is ButtonMapAction)
                                            {
                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
                                                if (parentLayer != null && parentLayer.buttonActionDict.TryGetValue(tempBind.id, out ButtonMapAction tempParentBtnAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentBtnAction))
                                                {
                                                    //(tempAction as ButtonMapAction).SoftCopyFromParent(tempParentBtnAction);
                                                    //(tempAction as ButtonMapAction).CopyAction(tempParentBtnAction);
                                                }

                                                //if (parentLayer != null && parentLayer.LayerActions[layerMapping.ActionIndex] is ButtonMapAction)
                                                //{
                                                //    tempLayer.buttonActionDict[tempBind.id] = (tempAction as ButtonMapAction).DuplicateAction();
                                                //}
                                                //else
                                                //{
                                                //    tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
                                                //}
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.DPad:
                                            if (tempAction is DPadMapAction)
                                            {
                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.dpadActionDict[tempBind.id] = tempAction as DPadMapAction;
                                                if (parentLayer != null && parentLayer.dpadActionDict.TryGetValue(tempBind.id, out DPadMapAction tempParentDpadAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentDpadAction))
                                                {
                                                    (tempAction as DPadMapAction).SoftCopyFromParent(tempParentDpadAction);
                                                }
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Stick:
                                            if (tempAction is StickMapAction)
                                            {
                                                StickMapAction tempStickAction = tempAction as StickMapAction;
                                                if (knownStickDefinitions.TryGetValue(tempBind.id, out StickDefinition tempDef))
                                                {
                                                    tempStickAction.StickDefinition = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.stickActionDict[tempBind.id] = tempStickAction;

                                                if (parentLayer != null && parentLayer.stickActionDict.TryGetValue(tempBind.id, out StickMapAction tempParentStickAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentStickAction))
                                                {
                                                    (tempAction as StickMapAction).SoftCopyFromParent(tempParentStickAction);
                                                }
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Trigger:
                                            if (tempAction is TriggerMapAction)
                                            {
                                                TriggerMapAction triggerAct = tempAction as TriggerMapAction;
                                                if (knownTriggerDefinitions.TryGetValue(tempBind.id, out TriggerDefinition tempDef))
                                                {
                                                    triggerAct.TriggerDef = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.triggerActionDict[tempBind.id] = tempAction as TriggerMapAction;
                                                if (parentLayer != null && parentLayer.triggerActionDict.TryGetValue(tempBind.id, out TriggerMapAction tempParentTrigAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentTrigAction))
                                                {
                                                    (tempAction as TriggerMapAction).SoftCopyFromParent(tempParentTrigAction);
                                                }
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Touchpad:
                                            if (tempAction is TouchpadMapAction)
                                            {
                                                TouchpadMapAction touchAct = tempAction as TouchpadMapAction;
                                                if (knownTouchpadDefinitions.TryGetValue(tempBind.id, out TouchpadDefinition tempDef))
                                                {
                                                    touchAct.TouchDefinition = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.touchpadActionDict[tempBind.id] = tempAction as TouchpadMapAction;
                                                if (parentLayer != null && parentLayer.touchpadActionDict.TryGetValue(tempBind.id, out TouchpadMapAction tempParentTouchAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentTouchAction))
                                                {
                                                    (tempAction as TouchpadMapAction).SoftCopyFromParent(tempParentTouchAction);
                                                }

                                                touchAct.PrepareActions();
                                            }

                                            break;
                                        case InputBindingMeta.InputControlType.Gyro:
                                            if (tempAction is GyroMapAction)
                                            {
                                                GyroMapAction gyroAction = tempAction as GyroMapAction;
                                                //if (tempBind.id == "Gyro")
                                                if (knownGyroSensDefinitions.TryGetValue(tempBind.id, out GyroSensDefinition tempDef))
                                                {
                                                    gyroAction.GyroSensDefinition = tempDef;
                                                }

                                                //tempAction.DefaultUnbound = false;
                                                tempAction.MappingId = tempBind.id;
                                                tempLayer.gyroActionDict[tempBind.id] = tempAction as GyroMapAction;
                                                if (parentLayer != null && parentLayer.gyroActionDict.TryGetValue(tempBind.id, out GyroMapAction tempParentGyroAction) &&
                                                    MapAction.IsSameType(tempAction, tempParentGyroAction))
                                                {
                                                    (tempAction as GyroMapAction).SoftCopyFromParent(tempParentGyroAction);
                                                }
                                            }

                                            break;
                                        default:
                                            break;
                                    }
                                }
                                else if (layerMapping.InputBinding == $"{ActionSet.ACTION_SET_ACTION_PREFIX}{mapping.ActionSet}" && tempAction is ButtonMapAction)
                                {
                                    //if (tempAction is ButtonMapAction)
                                    {
                                        //tempAction.DefaultUnbound = false;
                                        tempAction.MappingId = $"{ActionSet.ACTION_SET_ACTION_PREFIX}{mapping.ActionSet}";
                                        tempLayer.actionSetActionDict[tempAction.MappingId] = tempAction as ButtonMapAction;
                                        if (parentLayer != null && parentLayer.actionSetActionDict.TryGetValue(tempAction.MappingId, out ButtonMapAction tempParentBtnAction) &&
                                            MapAction.IsSameType(tempAction, tempParentBtnAction))
                                        {
                                            //(tempAction as ButtonMapAction).SoftCopyFromParent(tempParentBtnAction);
                                            //(tempAction as ButtonMapAction).CopyAction(tempParentBtnAction);
                                        }

                                        //if (parentLayer != null && parentLayer.LayerActions[layerMapping.ActionIndex] is ButtonMapAction)
                                        //{
                                        //    tempLayer.buttonActionDict[tempBind.id] = (tempAction as ButtonMapAction).DuplicateAction();
                                        //}
                                        //else
                                        //{
                                        //    tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
                                        //}
                                    }
                                }
                            }
                        }

                    }
                }
            }

            //tempProfile.CurrentActionSet.CreateDupActionLayer();
            //tempLayer.buttonActionDict[tempBind.id] = tempAction as ButtonMapAction;
            //(tempProfile.CurrentActionSet.ActionLayers[1].buttonActionDict["A"] as ButtonAction).ActionFuncs.Clear();
            //(tempProfile.CurrentActionSet.ActionLayers[1].buttonActionDict["A"] as ButtonAction).ActionFuncs.Add(new NormalPressFunc(new OutputActionData(OutputActionData.ActionType.Keyboard, KeyInterop.VirtualKeyFromKey(Key.L))));
            //new ButtonAction(new OutputActionData(OutputActionData.ActionType.Keyboard, KeyInterop.VirtualKeyFromKey(Key.L)));

            // SyncActions for currently active ActionLayer instance
            //foreach (ActionSet set in tempProfile.ActionSets)
            //{
            //    set.CurrentActionLayer.SyncActions();
            //}

            // Compile convenience List for MapActions instances in layers
            foreach (ActionSet set in tempProfile.ActionSets)
            {
                //int layerIdx = -1;
                ActionLayer parentLayer = set.DefaultActionLayer;
                foreach (ActionLayer layer in set.ActionLayers)
                {
                    //layerIdx++;
                    //if (layerIdx > 0)
                    //{
                    //    parentLayer.MergeLayerActions(layer);
                    //}

                    layer.SyncActions();
                }
            }

            // Prepare initial composite ActionLayer instance using
            // base ActionLayer references
            foreach (ActionSet set in tempProfile.ActionSets)
            {
                //ActionLayer parentLayer = set.DefaultActionLayer;
                set.ClearCompositeLayerActions();
                set.PrepareCompositeLayer();
            }

            //tempProfile.CurrentActionSet.SwitchActionLayer(this, 1);

            Trace.WriteLine("IT IS FINISHED");
        }

        public void PrepopulateBlankActionLayer(ActionLayer layer)
        {
            foreach (KeyValuePair<string, InputBindingMeta> tempMeta in bindingDict)
            {
                switch (tempMeta.Value.controlType)
                {
                    case InputBindingMeta.InputControlType.Button:
                        ButtonNoAction btnNoAction = new ButtonNoAction();
                        btnNoAction.MappingId = tempMeta.Key;
                        layer.buttonActionDict.Add(tempMeta.Key, btnNoAction);
                        break;
                    case InputBindingMeta.InputControlType.DPad:
                        DPadNoAction dpadNoAction = new DPadNoAction();
                        dpadNoAction.MappingId = tempMeta.Key;
                        layer.dpadActionDict.Add(tempMeta.Key, dpadNoAction);
                        break;
                    case InputBindingMeta.InputControlType.Stick:
                        {
                            StickNoAction stickNoAct = new StickNoAction();
                            stickNoAct.MappingId = tempMeta.Key;
                            if (knownStickDefinitions.TryGetValue(tempMeta.Key, out StickDefinition tempDef))
                            {
                                stickNoAct.StickDefinition = tempDef;
                            }
                            layer.stickActionDict.Add(tempMeta.Key, stickNoAct);
                        }

                        break;
                    case InputBindingMeta.InputControlType.Trigger:
                        {
                            TriggerNoAction trigNoAct = new TriggerNoAction();
                            trigNoAct.MappingId = tempMeta.Key;
                            if (knownTriggerDefinitions.TryGetValue(tempMeta.Key, out TriggerDefinition tempDef))
                            {
                                trigNoAct.TriggerDef = tempDef;
                            }
                            layer.triggerActionDict.Add(tempMeta.Key, trigNoAct);
                        }

                        break;
                    case InputBindingMeta.InputControlType.Touchpad:
                        {
                            TouchpadNoAction touchNoAct = new TouchpadNoAction();
                            touchNoAct.MappingId = tempMeta.Key;
                            if (knownTouchpadDefinitions.TryGetValue(tempMeta.Key, out TouchpadDefinition tempDef))
                            {
                                touchNoAct.TouchDefinition = tempDef;
                            }
                            layer.touchpadActionDict.Add(tempMeta.Key, touchNoAct);
                        }

                        break;
                    case InputBindingMeta.InputControlType.Gyro:
                        {
                            GyroNoMapAction gyroNoMapAct = new GyroNoMapAction();
                            gyroNoMapAct.MappingId = tempMeta.Key;
                            if (knownGyroSensDefinitions.TryGetValue(tempMeta.Key, out GyroSensDefinition tempDef))
                            {
                                gyroNoMapAct.GyroSensDefinition = tempDef;
                            }

                            layer.gyroActionDict.Add(tempMeta.Key, gyroNoMapAct);
                        }

                        break;
                    default:
                        break;
                }
            }

            layer.SyncActions();
        }

        public void ChangeProfile(string profilePath)
        {
            //if (!inMapperEvent)
            {
                //if (calibrationFinished)
                {
                    // Disconnect event
                    reader.Report -= ControllerReader_Report;
                }

                // Reset actions from current profile
                actionProfile.CurrentActionSet.ReleaseActions(this, true);

                // Relay changes to event systems
                SyncKeyboard();
                SyncMouseButtons();
                fakerInputHandler.Sync();

                // Might use this info later. Output controller device switch?
                EmulatedControllerSettings oldEmuControlSettings =
                    new EmulatedControllerSettings()
                {
                    enabled = actionProfile.OutputGamepadSettings.enabled,
                    outputGamepad = actionProfile.OutputGamepadSettings.outputGamepad,
                };

                // Reset virtual controller if currently connected
                //if (outputController != null)
                //{
                //    outputController.ResetReport();
                //    outputController.SubmitReport();
                //}
                if (outputControllerSCP != null)
                {
                    outputControllerSCP.ResetState();
                }

                // Change profile path
                profileFile = profilePath;

                // Read file
                try
                {
                    ReadFromProfile();
                    ProfileChanged?.Invoke(this, profileFile);
                }
                catch(JsonException e)
                {
                    UseBlankProfile();
                    profileFile = string.Empty;
                    ProfileChanged?.Invoke(this, profileFile);
                    throw e;
                }

                // Check if requested output controller is different than the currently
                // connected type
                if (actionProfile.OutputGamepadSettings.Enabled && outputControllerSCP != null &&
                    actionProfile.OutputGamepadSettings.OutputGamepad != outputControlType)
                {
                    outputControllerSCP.Disconnect();
                    outputControllerSCP = null;
                    outputControlType = OutputContType.None;
                    Thread.Sleep(100); // More of a pre-caution
                }

                // Create virtual controller if desired
                if (actionProfile.OutputGamepadSettings.Enabled && outputControllerSCP == null &&
                    actionProfile.OutputGamepadSettings.OutputGamepad != OutputContType.None)
                {
                    Thread contThr = new Thread(() =>
                    {
                        if (actionProfile.OutputGamepadSettings.OutputGamepad == OutputContType.Xbox360)
                        {
                            Xbox360ScpOutDevice tempoutputX360 = new Xbox360ScpOutDevice(x360busDevice, device.Index);
                            tempoutputX360.Connect();
                            outputControllerSCP = tempoutputX360;
                            outputControlType = OutputContType.Xbox360;
                        }
                    });
                    contThr.Priority = ThreadPriority.Normal;
                    contThr.IsBackground = true;
                    contThr.Start();
                    contThr.Join(); // Wait for bus object start
                    contThr = null;
                }
                else if (!actionProfile.OutputGamepadSettings.enabled && outputControllerSCP != null)
                {
                    outputControllerSCP.Disconnect();
                    outputControllerSCP = null;
                    outputControlType = OutputContType.None;
                }

                // Check for current output controller and check for desired vibration
                // status
                if (outputControllerSCP != null)
                {
                    if (actionProfile.OutputGamepadSettings.ForceFeedbackEnabled &&
                        outputControlType == OutputContType.Xbox360 &&
                        outputControllerSCP.forceFeedbacksDict.Count == 0)
                    {
                        Thread.Sleep(100);
                        EstablishForceFeedback();
                    }
                    else if (!actionProfile.OutputGamepadSettings.ForceFeedbackEnabled &&
                        outputControlType == OutputContType.Xbox360 &&
                        outputControllerSCP.forceFeedbacksDict.Count != 0)
                    {
                        outputControllerSCP.RemoveFeedbacks();
                    }
                }

                // Check for current output controller and check for desired vibration
                // status
                //if (outputController != null)
                //{
                //    if (actionProfile.OutputGamepadSettings.ForceFeedbackEnabled &&
                //        outputForceFeedbackDel == null)
                //    {
                //        outputForceFeedbackDel = (sender, e) =>
                //        {
                //            device.currentLeftAmpRatio = e.LargeMotor / 255.0;
                //            device.currentRightAmpRatio = e.SmallMotor / 255.0;
                //            reader.WriteRumbleReport();
                //        };

                //        outputX360.FeedbackReceived += outputForceFeedbackDel;
                //    }
                //    else if (!actionProfile.OutputGamepadSettings.ForceFeedbackEnabled &&
                //        outputForceFeedbackDel != null)
                //    {
                //        outputX360.FeedbackReceived -= outputForceFeedbackDel;
                //        outputForceFeedbackDel = null;
                //    }
                //}

                //if (calibrationFinished)
                {
                    // Re-connect event
                    reader.Report += ControllerReader_Report;
                }

                //ProfileChanged?.Invoke(this, profilePath);

                //ProfileSerializer profileSerializer = new ProfileSerializer(actionProfile);
                //string tempOutJson = JsonConvert.SerializeObject(profileSerializer, Formatting.Indented,
                //    new JsonSerializerSettings()
                //    {
                //        //Converters = new List<JsonConverter>()
                //        //{
                //        //    new MapActionSubTypeConverter(),
                //        //}
                //        //TypeNameHandling = TypeNameHandling.Objects
                //        //ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                //    });
                //Trace.WriteLine(tempOutJson);
            }
        }

        public void EstablishForceFeedback()
        {
            if (outputControlType == OutputContType.Xbox360)
            {
                outputControllerSCP.FeedbackReceived += (Xbox360ScpOutDevice sender, byte large, byte small, int idx) =>
                {
                    device.currentLeftAmpRatio = large / 255.0;
                    device.currentRightAmpRatio = small / 255.0;
                    reader.WriteRumbleReport();
                };
            }
        }

        public void PopulateKeyBindings()
        {
            /*buttonBindings.A = (ushort)KeyInterop.VirtualKeyFromKey(Key.Space);
            buttonBindings.B = (ushort)KeyInterop.VirtualKeyFromKey(Key.S);
            buttonBindings.X = (ushort)KeyInterop.VirtualKeyFromKey(Key.Return);
            buttonBindings.Y = (ushort)KeyInterop.VirtualKeyFromKey(Key.R);
            buttonBindings.LB = (ushort)KeyInterop.VirtualKeyFromKey(Key.Q);
            buttonBindings.RB = (ushort)KeyInterop.VirtualKeyFromKey(Key.Z);
            buttonBindings.Back = (ushort)KeyInterop.VirtualKeyFromKey(Key.Tab);
            buttonBindings.Start = (ushort)KeyInterop.VirtualKeyFromKey(Key.Escape);
            buttonBindings.Guide = (ushort)KeyInterop.VirtualKeyFromKey(Key.Tab);
            buttonBindings.LGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.X);
            //buttonBindings.RGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.F);
            buttonBindings.RGrip = (ushort)KeyInterop.VirtualKeyFromKey(Key.VolumeMute);
            */

            /*leftTouchBindings.Up = (ushort)KeyInterop.VirtualKeyFromKey(Key.W);
            leftTouchBindings.Left = (ushort)KeyInterop.VirtualKeyFromKey(Key.A);
            leftTouchBindings.Down = (ushort)KeyInterop.VirtualKeyFromKey(Key.S);
            leftTouchBindings.Right = (ushort)KeyInterop.VirtualKeyFromKey(Key.D);
            */

            //leftTouchBindings.Up = (ushort)KeyInterop.VirtualKeyFromKey(Key.Up);
            //leftTouchBindings.Left = (ushort)KeyInterop.VirtualKeyFromKey(Key.Left);
            //leftTouchBindings.Down = (ushort)KeyInterop.VirtualKeyFromKey(Key.Down);
            //leftTouchBindings.Right = (ushort)KeyInterop.VirtualKeyFromKey(Key.Right);

            buttonBindings.A = (ushort)KeyboardKey.Spacebar;
            buttonBindings.B = (ushort)KeyboardKey.C;
            buttonBindings.X = (ushort)KeyboardKey.R;
            buttonBindings.Y = (ushort)KeyboardKey.E;
            buttonBindings.LB = (ushort)KeyboardKey.Q;
            buttonBindings.RB = (ushort)KeyboardKey.Z;
            buttonBindings.Back = (ushort)KeyboardKey.Tab;
            buttonBindings.Start = (ushort)KeyboardKey.Escape;
            buttonBindings.Guide = (ushort)KeyboardKey.Tab;
            buttonBindings.LGrip = (ushort)KeyboardKey.X;
            //buttonBindings.RGrip = (ushort)KeyboardKey.F;
            buttonBindings.RGrip = (ushort)KeyboardKey.Spacebar;

            leftTouchBindings.Up = (ushort)KeyboardKey.W;
            leftTouchBindings.Left = (ushort)KeyboardKey.A;
            leftTouchBindings.Down = (ushort)KeyboardKey.S;
            leftTouchBindings.Right = (ushort)KeyboardKey.D;
            //leftTouchBindings.Up = (ushort)KeyboardKey.UpArrow;
            //leftTouchBindings.Left = (ushort)KeyboardKey.LeftArrow;
            //leftTouchBindings.Down = (ushort)KeyboardKey.DownArrow;
            //leftTouchBindings.Right = (ushort)KeyboardKey.RightArrow;
        }

        /*public void Start(SteamControllerDevice device, SteamControllerReader reader)
        {
            this.reader = reader;
            reader.Report += ControllerReader_Report;
            reader.StartUpdate();
        }
        */

        //private List<ActionFunc> removableFuns = new List<ActionFunc>();

        public void RemoveReleaseFunc(ActionFunc actionFunc)
        {
            pendingReleaseFuns.Remove(actionFunc);
        }

        private List<int> removePendingCandidates = new List<int>();
        private void ControllerReader_Report(SteamControllerReader sender,
            SteamControllerDevice device)
        {
            inMapperEvent = true;

            ref SteamControllerState current = ref device.CurrentStateRef;
            //ref SteamControllerState previous = ref device.PreviousStateRef;

            // Copy state struct data for later mapper manipulation. Leave
            // device state instance alone
            currentMapperState = current;

            int leftRotation = device.DeviceOptions.LeftTouchpadRotation;
            if (leftRotation != 0)
            {
                currentMapperState.LeftPad.Rotate(leftRotation * Math.PI / 180.0);
            }

            int rightRotation = device.DeviceOptions.RightTouchpadRotation;
            if (rightRotation != 0)
            {
                currentMapperState.RightPad.Rotate(rightRotation * Math.PI / 180.0);
            }

            //currentMapperState.LeftPad.Rotate(-18.0 * Math.PI / 180.0);
            //currentMapperState.RightPad.Rotate(8.0 * Math.PI / 180.0);

            mouseX = mouseY = 0.0;

            unchecked
            {
                intermediateState = new IntermediateState();
                currentLatency = currentMapperState.timeElapsed;
                currentRate = 1.0 / currentLatency;

                if (pendingReleaseFuns.Count > 0)
                {
                    ActionFuncEnumerator activeFuncEnumerator =
                        new ActionFuncEnumerator(pendingReleaseFuns);
                    int ind = 0;
                    while (activeFuncEnumerator.MoveNext())
                    {
                        ActionFunc actionFunc = activeFuncEnumerator.Current;
                        actionFunc.Event(this, MapAction.trueStateData);
                        if (!actionFunc.active)
                        {
                            removePendingCandidates.Add(ind);
                        }

                        ind++;
                    }

                    if (removePendingCandidates.Count > 0)
                    {
                        removePendingCandidates.Reverse();
                        foreach (int index in removePendingCandidates)
                        {
                            pendingReleaseFuns.RemoveAt(index);
                        }

                        removePendingCandidates.Clear();
                    }
                }

                if (processCycle)
                {
                    foreach (CycleButton btn in processCycleList)
                    {
                        btn.Prepare(this, false);
                        btn.Event(this);
                    }

                    processCycle = false;
                    processCycleList.Clear();
                }

                ActionLayer currentLayer = actionProfile.CurrentActionSet.CurrentActionLayer;

                if (currentLayer.actionSetActionDict.TryGetValue($"{actionProfile.CurrentActionSet.ActionButtonId}",
                    out ButtonMapAction currentSetAction))
                {
                    currentSetAction.Prepare(this, true);
                    if (currentSetAction.active)
                    {
                        currentSetAction.Event(this);
                    }
                }

                StickMapAction mapAction = currentLayer.stickActionDict["Stick"];
                //if ((currentMapperState.LX != previousMapperState.LX) || (currentMapperState.LY != previousMapperState.LY))
                {
                    //Trace.WriteLine($"{currentMapperState.LX} {currentMapperState.LY}");
                    int LX = Math.Clamp(currentMapperState.LX, STICK_MIN, STICK_MAX);
                    int LY = Math.Clamp(currentMapperState.LY, STICK_MIN, STICK_MAX);
                    mapAction.Prepare(this, LX, LY);
                }

                if (mapAction.active)
                {
                    mapAction.Event(this);
                }

                TriggerMapAction trigMapAction = currentLayer.triggerActionDict["LT"];
                {
                    TriggerEventFrame eventFrame = new TriggerEventFrame
                    {
                        axisValue = currentMapperState.LT,
                        fullClick = currentMapperState.LTClick,
                    };
                    trigMapAction.Prepare(this, ref eventFrame);
                }
                if (trigMapAction.active) trigMapAction.Event(this);

                trigMapAction = currentLayer.triggerActionDict["RT"];
                {
                    TriggerEventFrame eventFrame = new TriggerEventFrame
                    {
                        axisValue = currentMapperState.RT,
                        fullClick = currentMapperState.RTClick,
                    };
                    trigMapAction.Prepare(this, ref eventFrame);
                }
                if (trigMapAction.active) trigMapAction.Event(this);

                ButtonMapAction tempBtnAct = currentLayer.buttonActionDict["A"];
                if (currentMapperState.A || currentMapperState.A != previousMapperState.A)
                {
                    tempBtnAct.Prepare(this, currentMapperState.A);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["B"];
                if (currentMapperState.B || currentMapperState.B != previousMapperState.B)
                {
                    tempBtnAct.Prepare(this, currentMapperState.B);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["X"];
                if (currentMapperState.X || currentMapperState.X != previousMapperState.X)
                {
                    tempBtnAct.Prepare(this, currentMapperState.X);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["Y"];
                if (currentMapperState.Y || currentMapperState.Y != previousMapperState.Y)
                {
                    tempBtnAct.Prepare(this, currentMapperState.Y);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["Back"];
                if (currentMapperState.Back || currentMapperState.Back != previousMapperState.Back)
                {
                    tempBtnAct.Prepare(this, currentMapperState.Back);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["Start"];
                if (currentMapperState.Start || currentMapperState.Start != previousMapperState.Start)
                {
                    tempBtnAct.Prepare(this, currentMapperState.Start);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["LShoulder"];
                if (currentMapperState.LB || currentMapperState.LB != previousMapperState.LB)
                {
                    tempBtnAct.Prepare(this, currentMapperState.LB);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["RShoulder"];
                if (currentMapperState.RB || currentMapperState.RB != previousMapperState.RB)
                {
                    tempBtnAct.Prepare(this, currentMapperState.RB);
                }

                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["LSClick"];
                if (currentMapperState.LSClick || currentMapperState.LSClick != previousMapperState.LSClick)
                {
                    tempBtnAct.Prepare(this, currentMapperState.LSClick);
                }
                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["LeftGrip"];
                if (currentMapperState.LGrip || currentMapperState.LGrip != previousMapperState.LGrip)
                {
                    tempBtnAct.Prepare(this, currentMapperState.LGrip);
                }
                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["RightGrip"];
                if (currentMapperState.RGrip || currentMapperState.RGrip != previousMapperState.RGrip)
                {
                    tempBtnAct.Prepare(this, currentMapperState.RGrip);
                }
                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["LeftPadClick"];
                if (currentMapperState.LeftPad.Click || currentMapperState.LeftPad.Click != previousMapperState.LeftPad.Click)
                {
                    tempBtnAct.Prepare(this, currentMapperState.LeftPad.Click);
                }
                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["RightPadClick"];
                if (currentMapperState.RightPad.Click || currentMapperState.RightPad.Click != previousMapperState.RightPad.Click)
                {
                    tempBtnAct.Prepare(this, currentMapperState.RightPad.Click);
                }
                if (tempBtnAct.active) tempBtnAct.Event(this);

                tempBtnAct = currentLayer.buttonActionDict["Steam"];
                if (currentMapperState.Guide || currentMapperState.Guide != previousMapperState.Guide)
                {
                    tempBtnAct.Prepare(this, currentMapperState.Guide);
                }
                if (tempBtnAct.active) tempBtnAct.Event(this);

                TouchpadMapAction tempTouchAction = currentLayer.touchpadActionDict["LeftTouchpad"];
                //if (currentMapperState.LeftPad.Touch || currentMapperState.LeftPad.Touch != previousMapperState.LeftPad.Touch)
                {
                    //Trace.WriteLine($"{currentMapperState.LeftPad.X} {currentMapperState.LeftPad.Y}");
                    TouchEventFrame eventFrame = new TouchEventFrame
                    {
                        X = Math.Clamp(currentMapperState.LeftPad.X, (short)-32768, (short)32767),
                        Y = Math.Clamp(currentMapperState.LeftPad.Y, (short)-32768, (short)32767),
                        Touch = currentMapperState.LeftPad.Touch,
                        Click = currentMapperState.LeftPad.Click,
                        numTouches = 1,
                        timeElapsed = currentMapperState.timeElapsed,
                    };

                    tempTouchAction.Prepare(this, ref eventFrame);
                    if (tempTouchAction.active) tempTouchAction.Event(this);

                    previousTouchFrameLeftPad = eventFrame;
                }

                {
                    tempTouchAction = currentLayer.touchpadActionDict["RightTouchpad"];
                    //if (currentMapperState.RightPad.Touch || currentMapperState.RightPad.Touch != previousMapperState.RightPad.Touch)
                    TouchEventFrame eventFrame = new TouchEventFrame
                    {
                        X = Math.Clamp(currentMapperState.RightPad.X, (short)-32768, (short)32767),
                        Y = Math.Clamp(currentMapperState.RightPad.Y, (short)-32768, (short)32767),
                        Touch = currentMapperState.RightPad.Touch,
                        Click = currentMapperState.RightPad.Click,
                        numTouches = 1,
                        timeElapsed = currentMapperState.timeElapsed,
                    };

                    tempTouchAction.Prepare(this, ref eventFrame);
                    if (tempTouchAction.active) tempTouchAction.Event(this);

                    previousTouchFrameRightPad = eventFrame;
                }

                GyroMapAction gyroAct = currentLayer.gyroActionDict["Gyro"];
                // Skip if duration is less than 10 ms
                //if (currentMapperState.timeElapsed > 0.01)
                {
                    GyroEventFrame mouseFrame = new GyroEventFrame
                    {
                        GyroYaw = currentMapperState.Motion.GyroYaw,
                        GyroPitch = currentMapperState.Motion.GyroPitch,
                        GyroRoll = currentMapperState.Motion.GyroRoll,
                        AngGyroYaw = currentMapperState.Motion.AngGyroYaw,
                        AngGyroPitch = currentMapperState.Motion.AngGyroPitch,
                        AngGyroRoll = currentMapperState.Motion.AngGyroRoll,
                        AccelX = currentMapperState.Motion.AccelX,
                        AccelY = currentMapperState.Motion.AccelY,
                        AccelZ = currentMapperState.Motion.AccelZ,
                        AccelXG = currentMapperState.Motion.AccelXG,
                        AccelYG = currentMapperState.Motion.AccelYG,
                        AccelZG = currentMapperState.Motion.AccelZG,
                        timeElapsed = currentLatency,
                        elapsedReference = 125.0,
                        //elapsedReference = device.BaseElapsedReference,
                    };

                    gyroAct.Prepare(this, ref mouseFrame);
                    if (gyroAct.active)
                    {
                        gyroAct.Event(this);
                    }
                }

                if (mouseSync)
                {
                    //mouseReport.ResetMousePos();

                    if (mouseX != 0.0 || mouseY != 0.0)
                    {
                        //Console.WriteLine("MOVE: {0}, {1}", (int)mouseX, (int)mouseY);
                        GenerateMouseMoveEvent();
                    }
                    else
                    {
                        // Probably not needed here. Leave as a temporary precaution
                        mouseXRemainder = mouseYRemainder = 0.0;

                        //filterX.Filter(0.0, currentRate); // Smooth on output
                        //filterY.Filter(0.0, currentRate); // Smooth on output
                    }

                    mouseSync = false;
                }
                else if (!mouseEventFired)
                {
                    // Probably not needed here. Leave as a temporary precaution
                    mouseXRemainder = mouseYRemainder = 0.0;
                }

                mouseEventFired = false;

                if (absMouseSync)
                {
                    double outX = absMouseX, outY = absMouseY;
                    if (!appGlobal.absUseAllMonitors)
                    {
                        double tempX = outX, tempY = outY;
                        TranslateCoorToAbsDisplay(tempX, tempY, ref appGlobal.absDisplayBounds,
                            ref appGlobal.fullDesktopBounds, out outX, out outY);
                    }

                    fakerInputHandler.MoveAbsoluteMouse(outX, outY);
                    absMouseSync = false;
                }

                if (mouseWheelSync)
                {
                    fakerInputHandler.PerformMouseWheelEvent(vertical: mouseWheelY,
                        horizontal: mouseWheelX);
                    mouseWheelSync = false;
                }

                SyncMouseButtons();
                //fakerInputDev.UpdateRelativeMouse(mouseReport);

                SyncKeyboard();
                //fakerInputDev.UpdateKeyboard(keyboardReport);
                fakerInputHandler.Sync();

                if (intermediateState.Dirty)
                {
                    if (outputControllerSCP != null)
                    {
                        if (outputControlType == OutputContType.Xbox360)
                        {
                            outputControllerSCP.ConvertandSendReport(intermediateState, outputControllerSCP.SlotIdx);
                        }
                    }

                    intermediateState.LSDirty = intermediateState.RSDirty = false;
                    intermediateState.Dirty = false;
                }

                if (queuedActionSet != -1)
                {
                    //Console.WriteLine("CHANGING SET: {0}", queuedActionSet);
                    //actionProfile.CurrentActionSet.ReleaseActions(this);
                    //actionProfile.SwitchSets(queuedActionSet, this);
                    actionProfile.SwitchSets(queuedActionSet, this);

                    // Switch to possible new ActionLayer before engaging new actions
                    if (queuedActionLayer != -1 &&
                        actionProfile.CurrentActionSet.CurrentActionLayer.Index != queuedActionLayer)
                    {
                        if (switchQueuedActionLayer)
                        {
                            actionProfile.CurrentActionSet.SwitchActionLayer(this, queuedActionLayer);
                        }
                        else if (applyQueuedActionLayer)
                        {
                            actionProfile.CurrentActionSet.AddActionLayer(this, queuedActionLayer);
                        }
                        else if (!applyQueuedActionLayer)
                        {
                            //int tempIndex = actionProfile.CurrentActionSet.CurrentActionLayer.Index;
                            int tempIndex = queuedActionLayer;
                            actionProfile.CurrentActionSet.RemovePartialActionLayer(this, tempIndex);
                            //actionProfile.CurrentActionSet.RemovePartialActionLayer(this, queuedActionLayer);
                        }

                        //actionProfile.CurrentActionSet.SwitchActionLayer(this, queuedActionLayer);
                        queuedActionLayer = -1;
                        applyQueuedActionLayer = false;
                        switchQueuedActionLayer = false;
                    }

                    // Put new actions into an active state
                    //PrepareActionData(ref currentMapperState);
                    queuedActionSet = -1;
                }
                // Check if only an ActionLayer change is happening
                else if (queuedActionLayer != -1)
                {
                    Trace.WriteLine($"Going to Action Layer {queuedActionLayer}");
                    if (switchQueuedActionLayer)
                    {
                        actionProfile.CurrentActionSet.SwitchActionLayer(this, queuedActionLayer);
                    }
                    else if (applyQueuedActionLayer)
                    {
                        actionProfile.CurrentActionSet.AddActionLayer(this, queuedActionLayer);
                    }
                    else if (!applyQueuedActionLayer)
                    {
                        //int tempIndex = actionProfile.CurrentActionSet.CurrentActionLayer.Index;
                        int tempIndex = queuedActionLayer;
                        //actionProfile.CurrentActionSet.RemovePartialActionLayer(this, queuedActionLayer);
                        actionProfile.CurrentActionSet.RemovePartialActionLayer(this, tempIndex);
                    }

                    //actionProfile.CurrentActionSet.SwitchActionLayer(this, queuedActionLayer);

                    // Put new actions into an active state
                    //PrepareActionData(ref currentMapperState);
                    queuedActionLayer = EMPTY_QUEUED_ACTION_LAYER;
                    applyQueuedActionLayer = false;
                    switchQueuedActionLayer = false;

                    /*RequestOSD?.Invoke(this,
                        new RequestOSDArgs($"#{actionProfile.CurrentActionSet.CurrentActionLayer.Index}: {actionProfile.CurrentActionSet.CurrentActionLayer.Name}"));
                    */
                }

                // Make copy of state data as the previous state
                previousMapperState = currentMapperState;

                // Check for any waiting events and call them in this thread
                if (hasInputEvts)
                {
                    using (WriteLocker locker = new WriteLocker(eventQueueLocker))
                    //lock (eventQueueLock)
                    {
                        Action tempAct = null;
                        for (int actInd = 0, actLen = eventQueue.Count;
                            actInd < actLen; actInd++)
                        {
                            tempAct = eventQueue.Dequeue();
                            tempAct.Invoke();
                        }

                        hasInputEvts = false;
                    }
                }
            }

            inMapperEvent = false;
        }

        public void QueueEvent(Action tempAct)
        {
            //lock(eventQueueLock)
            using (WriteLocker locker = new WriteLocker(eventQueueLocker))
            {
                eventQueue.Enqueue(tempAct);
                hasInputEvts = true;
            }
        }

        private const int CALIB_POLL_COUNT = 240; // Roughly 4 seconds of polls
        private int rightGyroCalibPolls = 0;
        private int ignoreGyroCalibPolls = 0;
        private const int TOTAL_IGNORE_CALIB_POLL_COUNT = 67; // Roughly 1 second of polls
        private int gyroYawCalibSum = 0;
        private int gyroPitchCalibSum = 0;
        private int gyroRollCalibSum = 0;

        private void Reader_Calibrate_Gyro(SteamControllerReader sender, SteamControllerDevice device)
        {
            calibrationFinished = false;

            ref SteamControllerState current = ref device.CurrentStateRef;

            if (ignoreGyroCalibPolls < TOTAL_IGNORE_CALIB_POLL_COUNT)
            {
                ignoreGyroCalibPolls++;
                return;
            }

            if (rightGyroCalibPolls == 0)
            {
                Trace.WriteLine("Starting Calib");
            }

            gyroYawCalibSum += current.Motion.GyroYaw;
            gyroPitchCalibSum += current.Motion.GyroPitch;
            gyroRollCalibSum += current.Motion.GyroRoll;

            rightGyroCalibPolls++;
            if (rightGyroCalibPolls >= CALIB_POLL_COUNT)
            {
                //rightGyroCalibrated = true;

                /*Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");

                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");

                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");
                Trace.WriteLine("OBTAINED DESIRED POLL COUNT. CALCULATE OFFSETS.");*/

                device.gyroCalibOffsets[SteamControllerDevice.IMU_YAW_IDX] = (short)(gyroYawCalibSum / rightGyroCalibPolls);
                device.gyroCalibOffsets[SteamControllerDevice.IMU_PITCH_IDX] = (short)(gyroPitchCalibSum / rightGyroCalibPolls);
                device.gyroCalibOffsets[SteamControllerDevice.IMU_ROLL_IDX] = (short)(gyroRollCalibSum / rightGyroCalibPolls);
                Trace.WriteLine(string.Join(",", device.gyroCalibOffsets));

                sender.Report -= Reader_Calibrate_Gyro;
                HookReaderEvent(sender, device);
                //sender.Report += Reader_Right_Mixed_Gyro_Report;
                Trace.WriteLine("CALIB DONE");

                calibrationFinished = true;
            }
        }

        /// <summary>
        /// Method used to hook the desired static mapping routine after gyro calibration is
        /// finished. Needed for now until a more dynamic solution is implemented.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="device"></param>
        private void HookReaderEvent(SteamControllerReader reader, SteamControllerDevice device)
        {
            //reader.Report += ControllerReader_Report;
        }

        public ref TouchEventFrame GetPreviousTouchEventFrame(TouchpadActionCodes padID)
        {
            switch(padID)
            {
                case TouchpadActionCodes.Touch1:
                    return ref previousTouchFrameLeftPad;
                case TouchpadActionCodes.Touch2:
                    return ref previousTouchFrameRightPad;
                default:
                    break;
            }

            return ref previousTouchFrameLeftPad;
        }

        //private void ControllerReader_ReportOld(SteamControllerReader sender,
        //    SteamControllerDevice device)
        //{
        //    ref SteamControllerState current = ref device.CurrentStateRef;
        //    ref SteamControllerState previous = ref device.PreviousStateRef;
        //    mouseSync = keyboardSync = keyboardEnhancedSync = false;

        //    outputX360.ResetReport();
        //    unchecked
        //    {
        //        ushort tempButtons = 0;
        //        if (current.A) tempButtons |= Xbox360Button.A.Value;
        //        if (current.B) tempButtons |= Xbox360Button.B.Value;
        //        if (current.X) tempButtons |= Xbox360Button.X.Value;
        //        if (current.Y) tempButtons |= Xbox360Button.Y.Value;
        //        if (current.Back) tempButtons |= Xbox360Button.Back.Value;
        //        if (current.Start) tempButtons |= Xbox360Button.Start.Value;
        //        if (current.Guide) tempButtons |= Xbox360Button.Guide.Value;
        //        if (current.LB) tempButtons |= Xbox360Button.LeftShoulder.Value;
        //        if (current.RB) tempButtons |= Xbox360Button.RightShoulder.Value;
        //        //if (current.LSClick) tempButtons |= Xbox360Button.LeftThumb.Value;
        //        if (current.LeftPad.Click) tempButtons |= Xbox360Button.LeftThumb.Value;
        //        if (current.RightPad.Click) tempButtons |= Xbox360Button.RightThumb.Value;
        //        if (current.LGrip) tempButtons |= Xbox360Button.A.Value;
        //        if (current.RGrip) tempButtons |= Xbox360Button.X.Value;

        //        /*if (current.DPadUp) tempButtons |= Xbox360Button.Up.Value;
        //        if (current.DPadDown) tempButtons |= Xbox360Button.Down.Value;
        //        if (current.DPadLeft) tempButtons |= Xbox360Button.Left.Value;
        //        if (current.DPadRight) tempButtons |= Xbox360Button.Right.Value;
        //        */

        //        current.LeftPad.Rotate(-18.0 * Math.PI / 180.0);
        //        current.RightPad.Rotate(8.0 * Math.PI / 180.0);
        //        //TouchDPad(ref current, ref previous, ref tempButtons);
        //        //TouchJoystick(ref current, ref previous, ref outputX360);
        //        JoyDPad(ref current, ref previous, ref tempButtons);

        //        outputX360.SetButtonsFull(tempButtons);
        //    }

        //    short temp;
        //    currentRate = current.timeElapsed;
        //    //Console.WriteLine(currentRate);
        //    /*if (current.LeftPad.Touch)
        //    {
        //        temp = Math.Min(Math.Max(current.LeftPad.X, STICK_MIN), STICK_MAX);
        //        temp = AxisScale(temp, false);
        //        outputX360.LeftThumbX = temp;

        //        temp = Math.Min(Math.Max(current.LeftPad.Y, STICK_MIN), STICK_MAX);
        //        temp = AxisScale(temp, false);
        //        outputX360.LeftThumbY = temp;
        //    }
        //    */

        //    /*
        //    // Normal Left Stick
        //    temp = Math.Min(Math.Max(current.LX, STICK_MIN), STICK_MAX);
        //    temp = AxisScale(temp, false);
        //    outputX360.LeftThumbX = temp;

        //    temp = Math.Min(Math.Max(current.LY, STICK_MIN), STICK_MAX);
        //    temp = AxisScale(temp, false);
        //    outputX360.LeftThumbY = temp;
        //    //*/

        //    TouchJoystick(ref current, ref previous, ref outputX360);

        //    // Right Touchpad Mouse-like Joystick
        //    outputX360.RightThumbX = 0;
        //    outputX360.RightThumbY = 0;
        //    //TouchMouseJoystickPad(ref current, ref previous, ref outputX360);
        //    TrackballMouseJoystickProcess(ref current, ref previous, ref outputX360);

        //    outputX360.LeftTrigger = current.LT;
        //    outputX360.RightTrigger = current.RT;

        //    //if (current.A != previous.A)
        //    //{
        //    //    ushort tempKey = buttonBindings.A;
        //    //    if (current.A)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyDown(KeyboardKey.Spacebar);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyUp(KeyboardKey.Spacebar);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.B != previous.B)
        //    //{
        //    //    ushort tempKey = buttonBindings.B;
        //    //    if (current.B)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyDown(KeyboardKey.S);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyUp(KeyboardKey.S);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.X != previous.X)
        //    //{
        //    //    ushort tempKey = buttonBindings.X;
        //    //    if (current.X)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.Y != previous.Y)
        //    //{
        //    //    ushort tempKey = buttonBindings.Y;
        //    //    if (current.Y)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyDown(KeyboardKey.R);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyUp(KeyboardKey.R);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.LB != previous.LB)
        //    //{
        //    //    if (current.LB)
        //    //    {
        //    //        // Wheel Up
        //    //        //InputMethods.MouseWheel(128, 0);
        //    //        //int click = 1;
        //    //        mouseReport.WheelPosition = 1;
        //    //        mouseSync = true;
        //    //    }

        //    //    /*ushort tempKey = buttonBindings.LB;
        //    //    if (current.LB)
        //    //    {
        //    //        InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        InputMethods.performKeyRelease(tempKey);
        //    //    }
        //    //    */
        //    //}

        //    //if (current.RB != previous.RB)
        //    //{
        //    //    if (current.RB)
        //    //    {
        //    //        // Wheel Down
        //    //        //InputMethods.MouseWheel(-128, 0);
        //    //        int click = -1;
        //    //        mouseReport.WheelPosition = (byte)click;
        //    //        mouseSync = true;
        //    //    }

        //    //    /*ushort tempKey = buttonBindings.RB;
        //    //    if (current.RB)
        //    //    {
        //    //        InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        InputMethods.performKeyRelease(tempKey);
        //    //    }
        //    //    */
        //    //}

        //    //if (current.LGrip != previous.LGrip)
        //    //{
        //    //    ushort tempKey = buttonBindings.LGrip;
        //    //    if (current.LGrip)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyDown(KeyboardKey.X);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyUp(KeyboardKey.X);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.RGrip != previous.RGrip)
        //    //{
        //    //    ushort tempKey = buttonBindings.RGrip;
        //    //    if (current.RGrip)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //    //keyboardEnhancedSync = true;
        //    //}

        //    //if (current.Back != previous.Back)
        //    //{
        //    //    ushort tempKey = buttonBindings.Back;
        //    //    if (current.Back)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.Start != previous.Start)
        //    //{
        //    //    ushort tempKey = buttonBindings.Start;
        //    //    if (current.Start)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyDown(KeyboardKey.Escape);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //keyboardReport.KeyUp(KeyboardKey.Escape);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.Guide != previous.Guide)
        //    //{
        //    //    ushort tempKey = buttonBindings.Guide;
        //    //    if (current.Guide)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    //if (current.LSClick != previous.LSClick)
        //    //{
        //    //    ushort tempKey = buttonBindings.LSClick;
        //    //    if (current.LSClick)
        //    //    {
        //    //        keyboardReport.KeyDown((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyPress(tempKey);
        //    //    }
        //    //    else
        //    //    {
        //    //        keyboardReport.KeyUp((KeyboardKey)tempKey);
        //    //        //InputMethods.performKeyRelease(tempKey);
        //    //    }

        //    //    keyboardSync = true;
        //    //}

        //    ////if (current.RTClick != previous.RTClick)
        //    //if ((current.RT > RT_DEADZONE && !mouseLBDown) || (current.RT <= RT_DEADZONE && mouseLBDown))
        //    //{
        //    //    mouseLBDown = current.RT > RT_DEADZONE;
        //    //    //Console.WriteLine("RT: {0} {1}", current.RT, mouseLBDown);
        //    //    if (mouseLBDown)
        //    //    {
        //    //        mouseReport.ButtonDown(FakerInputWrapper.MouseButton.LeftButton);
        //    //    }
        //    //    else
        //    //    {
        //    //        mouseReport.ButtonUp(FakerInputWrapper.MouseButton.LeftButton);
        //    //    }

        //    //    mouseSync = true;
        //    //    //InputMethods.MouseEvent(mouseLBDown ? InputMethods.MOUSEEVENTF_LEFTDOWN :
        //    //    //    InputMethods.MOUSEEVENTF_LEFTUP);
        //    //}

        //    ////if (current.LTClick != previous.LTClick)
        //    ////if ((current.LT > 50) != (previous.LT <= 50))
        //    //if ((current.LT > LT_DEADZONE && !mouseRBDown) || (current.LT <= LT_DEADZONE && mouseRBDown))
        //    //{
        //    //    mouseRBDown = current.LT > LT_DEADZONE;
        //    //    if (mouseRBDown)
        //    //    {
        //    //        mouseReport.ButtonDown(FakerInputWrapper.MouseButton.RightButton);
        //    //    }
        //    //    else
        //    //    {
        //    //        mouseReport.ButtonUp(FakerInputWrapper.MouseButton.RightButton);
        //    //    }

        //    //    mouseSync = true;
        //    //    //InputMethods.MouseEvent(mouseRBDown ? InputMethods.MOUSEEVENTF_RIGHTDOWN :
        //    //    //    InputMethods.MOUSEEVENTF_RIGHTUP);
        //    //}

        //    ///*if (current.RightPad.Touch && previous.RightPad.Touch)
        //    //{
        //    //    // Process normal mouse
        //    //    RightTouchMouse(ref current, ref previous);
        //    //    Console.WriteLine("NORMAL");
        //    //}
        //    //*/

        //    //if (current.LeftPad.Touch || previous.LeftPad.Touch)
        //    //{
        //    //    current.LeftPad.Rotate(-18.0 * Math.PI / 180.0);
        //    //    TouchActionPad(ref current, ref previous);
        //    //}

        //    //current.RightPad.Rotate(8.0 * Math.PI / 180.0);
        //    //TrackballMouseProcess(ref current, ref previous);

        //    //if (mouseX != 0.0 || mouseY != 0.0)
        //    //{
        //    //    //Console.WriteLine("MOVE: {0}, {1}", (int)mouseX, (int)mouseY);
        //    //    GenerateMouseMoveEvent();
        //    //}
        //    //else
        //    //{
        //    //    // Probably not needed here. Leave as a temporary precaution
        //    //    mouseXRemainder = mouseYRemainder = 0.0;

        //    //    filterX.Filter(0.0, 1.0 / currentRate); // Smooth on output
        //    //    filterY.Filter(0.0, 1.0 / currentRate); // Smooth on output
        //    //}

        //    //if (keyboardSync)
        //    //{
        //    //    fakerInput.UpdateKeyboard(keyboardReport);
        //    //}

        //    //if (keyboardEnhancedSync)
        //    //{
        //    //    fakerInput.UpdateKeyboardEnhanced(mediaKeyboardReport);
        //    //}

        //    //if (mouseSync)
        //    //{
        //    //    //fakerInput.UpdateAbsoluteMouse(new AbsoluteMouseReport() { MouseX = 30000, MouseY = 20000, });
        //    //    fakerInput.UpdateRelativeMouse(mouseReport);
        //    //    mouseReport.ResetMousePos();
        //    //}

        //    outputX360.SubmitReport();
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessOutputButtonKey(ushort key, bool pressed)
        {
            if (pressed)
            {
                //keyboardReport.KeyDown((KeyboardKey)key);
                fakerInputHandler.PerformKeyPress(key);
            }
            else
            {
                fakerInputHandler.PerformKeyRelease(key);
                //keyboardReport.KeyUp((KeyboardKey)key);
            }

            keyboardSync = true;
        }

        //     private void TouchMouseJoystickPad(int dx, int dy,
        //         ref SteamControllerState current,
        //         ref SteamControllerState previous, ref IXbox360Controller xbox)
        //     {
        //         const int deadZone = 150;
        //         const int maxDeadZoneAxial = 130;
        //         const int minDeadZoneAxial = 20;

        //         //int dx = 0;
        //         //if (current.RightPad.Touch)
        //         //    dx = current.RightPad.X - previous.RightPad.X;

        //         //int dy = 0;
        //         //if (current.RightPad.Touch)
        //         //    dy = (current.RightPad.Y - previous.RightPad.Y);

        //         //Trace.WriteLine(current.RightPad.Y);

        //         int maxDirX = dx >= 0 ? 32767 : -32768;
        //         int maxDirY = dy >= 0 ? 32767 : -32768;

        //         double tempAngle = Math.Atan2(dy, dx);
        //         double normX = Math.Abs(Math.Cos(tempAngle));
        //         double normY = Math.Abs(Math.Sin(tempAngle));
        //         int signX = Math.Sign(dx);
        //         int signY = Math.Sign(dy);

        //         //double timeElapsed = current.timeElapsed;
        //         // Base speed 8 ms
        //         //double tempDouble = timeElapsed * 125.0;

        //         int maxValX = signX * 430;
        //         int maxValY = signY * 430;

        //         double xratio = 0.0, yratio = 0.0;
        //         double antiX = 0.30 * normX;
        //         double antiY = 0.30 * normY;

        //         int deadzoneX = (int)Math.Abs(normX * deadZone);
        //         int radialDeadZoneY = (int)(Math.Abs(normY * deadZone));
        //         //int deadzoneY = (int)(Math.Abs(normY * deadZone));

        //         int absDX = Math.Abs(dx);
        //         int absDY = Math.Abs(dy);

        //         // Check for radial dead zone first
        //         double mag = (dx * dx) + (dy * dy);
        //         if (mag <= (deadZone * deadZone))
        //         {
        //             dx = 0;
        //             dy = 0;
        //         }
        //         // Past radial. Check for bowtie
        //         else
        //         {
        //             //Trace.WriteLine($"X ({dx}) | Y ({dy})");

        //             // X axis calculated with scaled radial
        //             /*if (absDX > deadzoneX)
        //             {
        //                 dx -= signX * deadzoneX;
        //                 //dx = (dx < 0 && dx < maxValX) ? maxValX :
        //                 //    (dx > 0 && dx > maxValX) ? maxValX : dx;
        //             }
        //             else
        //             {
        //                 dx = 0;
        //             }

        //             if (absDY > radialDeadZoneY)
        //             {
        //                 dy -= signY * radialDeadZoneY;
        //                 //dy = (dy < 0 && dy < maxValY) ? maxValY :
        //                 //    (dy > 0 && dy > maxValY) ? maxValY : dy;
        //             }
        //             else
        //             {
        //                 dy = 0;
        //             }
        //             */

        //             // Need to adjust Y axis dead zone based on X axis input. Bowtie
        //             //int deadzoneY = Math.Max(radialDeadZoneY,
        //             //    (int)(Math.Min(1.0, absDX / (double)maxValX) * maxDeadZoneAxialY));
        //             double tempRangeRatioX = absDX / Math.Abs((double)maxValX);
        //             double tempRangeRatioY = absDY / Math.Abs((double)maxValY);

        //             int axialDeadX = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
        //                 Math.Min(1.0, tempRangeRatioY) + minDeadZoneAxial);
        //             int deadzoneY = (int)((maxDeadZoneAxial - minDeadZoneAxial) *
        //                 Math.Min(1.0, tempRangeRatioX) + minDeadZoneAxial);

        //             //Trace.WriteLine($"Axial {axialDeadX} DX: {dx}");
        //             //Trace.WriteLine(deadzoneY);
        //             //int deadzoneY = Math.Min(maxValX, Math.Abs(dx)) * maxDeadZoneAxialY;
        //             //if (absDY > radialDeadZoneY)
        //             //{
        //             //    dy -= signY * radialDeadZoneY;
        //             //    dy = (dy < 0 && dy < maxValY) ? maxValY :
        //             //        (dy > 0 && dy > maxValY) ? maxValY : dy;
        //             //}
        //             //else if (absDY > deadzoneY)

        //             if (Math.Abs(dx) > axialDeadX)
        //             {
        //                 int tempUseDeadX = deadzoneX > axialDeadX ? deadzoneX : axialDeadX;
        //                 dx -= signX * tempUseDeadX;
        //                 double newMaxValX = Math.Abs(maxValX) - tempUseDeadX;
        //                 double scaleX = Math.Abs(dx) / (double)(newMaxValX);
        //                 dx = (int)(maxValX * scaleX);

        //                 dx = (dx < 0 && dx < maxValX) ? maxValX :
        //                     (dx > 0 && dx > maxValX) ? maxValX : dx;
        //                 //Trace.WriteLine($"{scaleX} {dx}");
        //             }
        //             else
        //             {
        //                 dx = 0;
        //                 //Trace.WriteLine("dx zero");
        //             }

        //             if (Math.Abs(dy) > deadzoneY)
        //             {
        //                 int tempUseDeadY = radialDeadZoneY > deadzoneY ? radialDeadZoneY : deadzoneY;
        //                 dy -= signY * tempUseDeadY;
        //                 double newMaxValY = Math.Abs(maxValY) - tempUseDeadY;
        //                 double scaleY = Math.Abs(dy) / (double)(newMaxValY);
        //                 dy = (int)(maxValY * scaleY);

        //                 dy = (dy < 0 && dy < maxValY) ? maxValY :
        //                     (dy > 0 && dy > maxValY) ? maxValY : dy;
        //             }
        //             else
        //             {
        //                 dy = 0;
        //             }

        //             /*
        //             if (absDY > deadzoneY)
        //             {
        //                 int newMaxValY = signY * (Math.Abs(maxValY) - deadzoneY);
        //                 dy -= signY * deadzoneY;
        //                 //dy = (dy < 0 && dy < maxValY) ? maxValY :
        //                 //    (dy > 0 && dy > maxValY) ? maxValY : dy;
        //                 dy = (dy < 0 && dy < newMaxValY) ? newMaxValY :
        //                       (dy > 0 && dy > newMaxValY) ? newMaxValY : dy;

        //                 double scaleY;
        //                 if (dy == newMaxValY)
        //                 {
        //                     scaleY = 1.0;
        //                 }
        //                 else
        //                 {
        //                     scaleY = (double)(dy - 0.0) / (double)(newMaxValY - 0.0);
        //                 }
        //                 //scaleY = (Math.Abs(newMaxValY) - Math.Abs(dy)) /
        //                 //    (double)(Math.Abs(maxValY) - 0);
        //                 dy = (int)(scaleY * maxValY);

        //                 //Trace.WriteLine($"SCALE: ({scaleY}) | NEW: ({newMaxValY}) | DY({dy})");
        //             }
        //             else
        //             {
        //                 dy = 0;
        //             }
        //             //*/
        //         }

        //         if (dx != 0) xratio = dx / (double)maxValX;
        //         if (dy != 0) yratio = dy / (double)maxValY;

        //         double maxOutRatio = 1.0;
        //         double maxOutXRatio = Math.Abs(Math.Cos(tempAngle)) * maxOutRatio;
        //         double maxOutYRatio = Math.Abs(Math.Sin(tempAngle)) * maxOutRatio;

        //         //Trace.WriteLine($"{maxOutXRatio} {maxOutYRatio}");
        //         // Expand output a bit
        //         maxOutXRatio = Math.Min(maxOutXRatio / 0.95, 1.0);
        //         // Expand output a bit
        //         maxOutYRatio = Math.Min(maxOutYRatio / 0.95, 1.0);

        //         xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
        //         yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);

        //         //Trace.WriteLine($"X ({dx}) | Y ({dy})");

        //         //double maxOutRatio = 0.98;
        //         //// Expand output a bit. Likely not going to get a straight line with Gyro
        //         //double maxOutXRatio = Math.Min(normX / 1.0, 1.0) * maxOutRatio;
        //         //double maxOutYRatio = Math.Min(normY / 1.0, 1.0) * maxOutRatio;

        //         //xratio = Math.Min(Math.Max(xratio, 0.0), maxOutXRatio);
        //         //yratio = Math.Min(Math.Max(yratio, 0.0), maxOutYRatio);

        //         // QuadOut
        //         xratio = 1.0 - ((1.0 - xratio) * (1.0 - xratio));
        //         yratio = 1.0 - ((1.0 - yratio) * (1.0 - yratio));

        //         double xNorm = 0.0, yNorm = 0.0;
        //         if (xratio != 0.0)
        //         {
        //             xNorm = (1.0 - antiX) * xratio + antiX;
        //         }

        //         if (yratio != 0.0)
        //         {
        //             yNorm = (1.0 - antiY) * yratio + antiY;
        //         }


        //         short axisXOut = (short)filterX.Filter(xNorm * maxDirX,
        //             1.0 / currentRate);
        //         short axisYOut = (short)filterY.Filter(yNorm * maxDirY,
        //             1.0 / currentRate);

        //         //Trace.WriteLine($"OutX ({axisXOut}) | OutY ({axisYOut})");

        //         xbox.RightThumbX = axisXOut;
        //         xbox.RightThumbY = axisYOut;
        //     }

        //     private void TouchActionPad(ref SteamControllerState current,
        //         ref SteamControllerState previous)
        //     {
        //         const double DIAGONAL_RANGE = 50.0;
        //         const double CARDINAL_RANGE = 90.0 - DIAGONAL_RANGE;
        //         const double CARDINAL_HALF_RANGE = CARDINAL_RANGE / 2.0;
        //         //const double CARDINAL_HALF_RANGE = 22.5;

        //         const double upLeftEnd = 360 - CARDINAL_HALF_RANGE;
        //         const double upRightBegin = CARDINAL_HALF_RANGE;
        //         const double rightBegin = upRightBegin + DIAGONAL_RANGE;
        //         const double downRightBegin = rightBegin + CARDINAL_RANGE;
        //         const double downBegin = downRightBegin + DIAGONAL_RANGE;
        //         const double downLeftBegin = downBegin + CARDINAL_RANGE;
        //         const double leftBegin = downLeftBegin + DIAGONAL_RANGE;
        //         const double upLeftBegin = leftBegin + CARDINAL_RANGE;

        //         const int deadzoneSquared = 8000 * 8000;

        //         int dist = (current.LeftPad.X * current.LeftPad.X) + (current.LeftPad.Y * current.LeftPad.Y);
        //         if (dist < deadzoneSquared)
        //         {
        //             currentLeftDir = DpadDirections.Centered;
        //         }
        //         else
        //         {
        //             double angleRad = Math.Atan2(current.LeftPad.X, current.LeftPad.Y);
        //             double angle = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;
        //             //Console.WriteLine("{0} {1}", angle, current.LeftPad.Touch);
        //             /*double normX = Math.Abs(Math.Cos(tempAngle));
        //             double normY = Math.Abs(Math.Sin(tempAngle));
        //             int signX = Math.Sign(current.LeftPad.X);
        //             int signY = Math.Sign(current.LeftPad.Y);
        //             */

        //             if (angle == 0.0)
        //             {
        //                 currentLeftDir = DpadDirections.Centered;
        //             }
        //             else if (angle > upLeftEnd || angle < upRightBegin)
        //             {
        //                 currentLeftDir = DpadDirections.Up;
        //             }
        //             else if (angle >= upRightBegin && angle < rightBegin)
        //             {
        //                 currentLeftDir = DpadDirections.UpRight;
        //             }
        //             else if (angle >= rightBegin && angle < downRightBegin)
        //             {
        //                 currentLeftDir = DpadDirections.Right;
        //             }
        //             else if (angle >= downRightBegin && angle < downBegin)
        //             {
        //                 currentLeftDir = DpadDirections.DownRight;
        //             }
        //             else if (angle >= downBegin && angle < downLeftBegin)
        //             {
        //                 currentLeftDir = DpadDirections.Down;
        //             }
        //             else if (angle >= downLeftBegin && angle < leftBegin)
        //             {
        //                 currentLeftDir = DpadDirections.DownLeft;
        //             }
        //             else if (angle >= leftBegin && angle < upLeftBegin)
        //             {
        //                 currentLeftDir = DpadDirections.Left;
        //             }
        //             else if (angle >= upLeftBegin && angle <= upLeftEnd)
        //             {
        //                 currentLeftDir = DpadDirections.UpLeft;
        //             }
        //         }

        //         if (currentLeftDir != previousLeftDir)
        //         {
        //             DpadDirections xor = previousLeftDir ^ currentLeftDir;
        //             DpadDirections remDirs = xor & previousLeftDir;
        //             DpadDirections addDirs = xor & currentLeftDir;
        //             //Console.WriteLine("RELEASED: {0} CURRENT: {1}", remDirs, currentLeftDir);

        //             if ((remDirs & DpadDirections.Up) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Up;
        //                 //InputMethods.performKeyRelease(tempKey);
        //                 //keyboardReport.KeyUp((KeyboardKey)tempKey);
        //                 fakerInputHandler.PerformKeyRelease(tempKey);
        //                 keyboardSync = true;
        //             }
        //             else if ((remDirs & DpadDirections.Down) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Down;
        //                 //InputMethods.performKeyRelease(tempKey);
        //                 fakerInputHandler.PerformKeyRelease(tempKey);
        //                 //keyboardReport.KeyUp((KeyboardKey)tempKey);
        //                 keyboardSync = true;
        //             }

        //             if ((remDirs & DpadDirections.Left) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Left;
        //                 //InputMethods.performKeyRelease(tempKey);
        //                 //keyboardReport.KeyUp((KeyboardKey)tempKey);
        //                 fakerInputHandler.PerformKeyRelease(tempKey);
        //                 keyboardSync = true;
        //             }
        //             else if ((remDirs & DpadDirections.Right) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Right;
        //                 //InputMethods.performKeyRelease(tempKey);
        //                 //keyboardReport.KeyUp((KeyboardKey)tempKey);
        //                 fakerInputHandler.PerformKeyRelease(tempKey);
        //                 keyboardSync = true;
        //             }

        //             if ((addDirs & DpadDirections.Up) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Up;
        //                 //InputMethods.performKeyPress(tempKey);
        //                 //keyboardReport.KeyDown((KeyboardKey)tempKey);
        //                 fakerInputHandler.PerformKeyPress(tempKey);
        //                 keyboardSync = true;
        //             }
        //             else if ((addDirs & DpadDirections.Down) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Down;
        //                 //InputMethods.performKeyPress(tempKey);
        //                 //keyboardReport.KeyDown((KeyboardKey)tempKey);
        //                 fakerInputHandler.PerformKeyPress(tempKey);
        //                 keyboardSync = true;
        //             }

        //             if ((addDirs & DpadDirections.Left) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Left;
        //                 //InputMethods.performKeyPress(tempKey);
        //                 //keyboardReport.KeyDown((KeyboardKey)tempKey);
        //                 fakerInputHandler.PerformKeyPress(tempKey);
        //                 keyboardSync = true;
        //             }
        //             else if ((addDirs & DpadDirections.Right) != 0)
        //             {
        //                 ushort tempKey = leftTouchBindings.Right;
        //                 //InputMethods.performKeyPress(tempKey);
        //                 //keyboardReport.KeyDown((KeyboardKey)tempKey);
        //                 fakerInputHandler.PerformKeyPress(tempKey);
        //                 keyboardSync = true;
        //             }

        //             previousLeftDir = currentLeftDir;
        //         }
        //     }

        //     private void JoyDPad(ref SteamControllerState current,
        //         ref SteamControllerState previous, ref ushort tempButtons)
        //     {
        //         const double DIAGONAL_RANGE = 55.0;
        //         const double CARDINAL_RANGE = 90.0 - DIAGONAL_RANGE;
        //         const double CARDINAL_HALF_RANGE = CARDINAL_RANGE / 2.0;
        //         //const double CARDINAL_HALF_RANGE = 22.5;

        //         const double upLeftEnd = 360 - CARDINAL_HALF_RANGE;
        //         const double upRightBegin = CARDINAL_HALF_RANGE;
        //         const double rightBegin = upRightBegin + DIAGONAL_RANGE;
        //         const double downRightBegin = rightBegin + CARDINAL_RANGE;
        //         const double downBegin = downRightBegin + DIAGONAL_RANGE;
        //         const double downLeftBegin = downBegin + CARDINAL_RANGE;
        //         const double leftBegin = downLeftBegin + DIAGONAL_RANGE;
        //         const double upLeftBegin = leftBegin + CARDINAL_RANGE;

        //         const int deadzone = 16000;
        //         const int deadzoneSquared = deadzone * deadzone;

        //         unchecked
        //         {
        //             int dist = (current.LX * current.LX) + (current.LY * current.LY);
        //             if (dist > deadzoneSquared)
        //             {
        //                 //Trace.WriteLine(current.LY);
        //                 double angleRad = Math.Atan2(current.LX, current.LY);
        //                 double angle = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;

        //                 if (angle == 0.0)
        //                 {
        //                     tempButtons |= Xbox360Button.Up.Value;
        //                 }
        //                 else if (angle > upLeftEnd || angle < upRightBegin)
        //                 {
        //                     tempButtons |= Xbox360Button.Up.Value;
        //                     //currentDir = DpadDirections.Up;
        //                 }
        //                 else if (angle >= upRightBegin && angle < rightBegin)
        //                 {
        //                     tempButtons |= Xbox360Button.Up.Value;
        //                     tempButtons |= Xbox360Button.Right.Value;
        //                     //currentDir = DpadDirections.UpRight;
        //                 }
        //                 else if (angle >= rightBegin && angle < downRightBegin)
        //                 {
        //                     tempButtons |= Xbox360Button.Right.Value;
        //                     //currentDir = DpadDirections.Right;
        //                 }
        //                 else if (angle >= downRightBegin && angle < downBegin)
        //                 {
        //                     tempButtons |= Xbox360Button.Down.Value;
        //                     tempButtons |= Xbox360Button.Right.Value;
        //                     //currentDir = DpadDirections.DownRight;
        //                 }
        //                 else if (angle >= downBegin && angle < downLeftBegin)
        //                 {
        //                     tempButtons |= Xbox360Button.Down.Value;
        //                     //currentDir = DpadDirections.Down;
        //                 }
        //                 else if (angle >= downLeftBegin && angle < leftBegin)
        //                 {
        //                     tempButtons |= Xbox360Button.Down.Value;
        //                     tempButtons |= Xbox360Button.Left.Value;
        //                     //currentDir = DpadDirections.DownLeft;
        //                 }
        //                 else if (angle >= leftBegin && angle < upLeftBegin)
        //                 {
        //                     tempButtons |= Xbox360Button.Left.Value;
        //                     //currentDir = DpadDirections.Left;
        //                 }
        //                 else if (angle >= upLeftBegin && angle <= upLeftEnd)
        //                 {
        //                     tempButtons |= Xbox360Button.Up.Value;
        //                     tempButtons |= Xbox360Button.Left.Value;
        //                     //currentDir = DpadDirections.UpLeft;
        //                 }
        //             }
        //         }
        //     }

        //     private void TouchJoystick(ref SteamControllerState current,
        //         ref SteamControllerState previous, ref IXbox360Controller xbox)
        //     {
        //         const int deadzone = 12000;
        //         const int deadzoneSquared = deadzone * deadzone;

        //         unchecked
        //         {
        //             if (current.LeftPad.Touch)
        //             {
        //                 int dist = (current.LeftPad.X * current.LeftPad.X) + (current.LeftPad.Y * current.LeftPad.Y);
        //                 if (dist > deadzoneSquared)
        //                 {
        //                     double angleRad = Math.Atan2(current.LeftPad.Y, current.LeftPad.X);
        //                     //double angle = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;
        //                     double normX = Math.Abs(Math.Cos(angleRad));
        //                     double normY = Math.Abs(Math.Sin(angleRad));

        //                     int xVal = current.LeftPad.X;
        //                     int yVal = current.LeftPad.Y;

        //                     int maxDirX = xVal >= 0 ? 32767 : -32768;
        //                     int maxDirY = yVal >= 0 ? 32767 : -32768;
        //                     int maxZoneX = xVal >= 0 ? 30000 : -30000;
        //                     int maxZoneY = yVal >= 0 ? 30000 : -30000;
        //                     int absDX = Math.Abs(xVal);
        //                     int absDY = Math.Abs(yVal);
        //                     int signX = Math.Sign(xVal);
        //                     int signY = Math.Sign(yVal);

        //                     //Trace.WriteLine(yVal);

        //                     xVal = Math.Clamp(xVal, -30000, 30000);
        //                     yVal = Math.Clamp(yVal, -30000, 30000);

        //                     int deadzoneX = (int)(normX * deadzone * signX);
        //                     int radialDeadZoneY = (int)(normY * deadzone * signY);

        //                     double xratio = (xVal - deadzoneX) / (double)(maxZoneX - deadzoneX);
        //                     double yratio = (yVal - radialDeadZoneY) / (double)(maxZoneY - radialDeadZoneY);

        //                     //Trace.WriteLine($"{yratio} {radialDeadZoneY} {maxDirY}");

        //                     const double antidead = 0.24;
        //                     double antiX = antidead * normX;
        //                     double antiY = antidead * normY;

        //                     double xNorm = 0.0, yNorm = 0.0;
        //                     if (xratio != 0.0)
        //                     {
        //                         xNorm = (1.0 - antiX) * xratio + antiX;
        //                     }

        //                     if (yratio != 0.0)
        //                     {
        //                         yNorm = (1.0 - antiY) * yratio + antiY;
        //                     }

        //                     short axisXOut = (short)(xNorm * maxDirX);
        //                     short axisYOut = (short)(yNorm * maxDirY);

        //                     xbox.LeftThumbX = axisXOut;
        //                     xbox.LeftThumbY = axisYOut;
        //                 }
        //             }
        //         }
        //     }

        //     private void TouchDPad(ref SteamControllerState current,
        //         ref SteamControllerState previous, ref ushort tempButtons)
        //     {
        //         const double DIAGONAL_RANGE = 10.0;
        //         const double CARDINAL_RANGE = 90.0 - DIAGONAL_RANGE;
        //         const double CARDINAL_HALF_RANGE = CARDINAL_RANGE / 2.0;
        //         //const double CARDINAL_HALF_RANGE = 22.5;

        //         const double upLeftEnd = 360 - CARDINAL_HALF_RANGE;
        //         const double upRightBegin = CARDINAL_HALF_RANGE;
        //         const double rightBegin = upRightBegin + DIAGONAL_RANGE;
        //         const double downRightBegin = rightBegin + CARDINAL_RANGE;
        //         const double downBegin = downRightBegin + DIAGONAL_RANGE;
        //         const double downLeftBegin = downBegin + CARDINAL_RANGE;
        //         const double leftBegin = downLeftBegin + DIAGONAL_RANGE;
        //         const double upLeftBegin = leftBegin + CARDINAL_RANGE;

        //         const int deadzoneSquared = 8000 * 8000;

        //         unchecked
        //         {
        //             if (current.LeftPad.Touch)
        //             {
        //                 int dist = (current.LeftPad.X * current.LeftPad.X) + (current.LeftPad.Y * current.LeftPad.Y);
        //                 if (dist > deadzoneSquared)
        //                 {
        //                     double angleRad = Math.Atan2(current.LeftPad.X, current.LeftPad.Y);
        //                     double angle = (angleRad >= 0 ? angleRad : (2 * Math.PI + angleRad)) * 180 / Math.PI;
        //                     //Console.WriteLine(angle);
        //                     /*double normX = Math.Abs(Math.Cos(tempAngle));
        //                     double normY = Math.Abs(Math.Sin(tempAngle));
        //                     int signX = Math.Sign(current.LeftPad.X);
        //                     int signY = Math.Sign(current.LeftPad.Y);
        //                     */
        //                     if (angle == 0.0)
        //                     {
        //                     }
        //                     else if (angle > upLeftEnd || angle < upRightBegin)
        //                     {
        //                         tempButtons |= Xbox360Button.Up.Value;
        //                         //currentDir = DpadDirections.Up;
        //                     }
        //                     else if (angle >= upRightBegin && angle < rightBegin)
        //                     {
        //                         tempButtons |= Xbox360Button.Up.Value;
        //                         tempButtons |= Xbox360Button.Right.Value;
        //                         //currentDir = DpadDirections.UpRight;
        //                     }
        //                     else if (angle >= rightBegin && angle < downRightBegin)
        //                     {
        //                         tempButtons |= Xbox360Button.Right.Value;
        //                         //currentDir = DpadDirections.Right;
        //                     }
        //                     else if (angle >= downRightBegin && angle < downBegin)
        //                     {
        //                         tempButtons |= Xbox360Button.Down.Value;
        //                         tempButtons |= Xbox360Button.Right.Value;
        //                         //currentDir = DpadDirections.DownRight;
        //                     }
        //                     else if (angle >= downBegin && angle < downLeftBegin)
        //                     {
        //                         tempButtons |= Xbox360Button.Down.Value;
        //                         //currentDir = DpadDirections.Down;
        //                     }
        //                     else if (angle >= downLeftBegin && angle < leftBegin)
        //                     {
        //                         tempButtons |= Xbox360Button.Down.Value;
        //                         tempButtons |= Xbox360Button.Left.Value;
        //                         //currentDir = DpadDirections.DownLeft;
        //                     }
        //                     else if (angle >= leftBegin && angle < upLeftBegin)
        //                     {
        //                         tempButtons |= Xbox360Button.Left.Value;
        //                         //currentDir = DpadDirections.Left;
        //                     }
        //                     else if (angle >= upLeftBegin && angle <= upLeftEnd)
        //                     {
        //                         tempButtons |= Xbox360Button.Up.Value;
        //                         tempButtons |= Xbox360Button.Left.Value;
        //                         //currentDir = DpadDirections.UpLeft;
        //                     }
        //                 }
        //             }
        //         }
        //     }

        //     private void TrackballMouseProcess(ref SteamControllerState current,
        //         ref SteamControllerState previous)
        //     {
        //         if (current.RightPad.Touch && !previous.RightPad.Touch)
        //         {
        //             if (trackballActive)
        //             {
        //                 //Console.WriteLine("CHECKING HERE");
        //             }

        //             // Initial touch
        //             Array.Clear(trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
        //             Array.Clear(trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
        //             trackballXVel = 0.0;
        //             trackballYVel = 0.0;
        //             trackballActive = false;
        //             trackballBufferTail = 0;
        //             trackballBufferHead = 0;
        //             trackballDXRemain = 0.0;
        //             trackballDYRemain = 0.0;

        //             //Console.WriteLine("INITIAL");
        //         }
        //         else if (current.RightPad.Touch && previous.RightPad.Touch)
        //         {
        //             // Process normal mouse
        //             RightTouchMouse(ref current, ref previous);
        //             //Console.WriteLine("NORMAL");
        //         }
        //         else if (!current.RightPad.Touch && previous.RightPad.Touch)
        //         {
        //             // Initially released. Calculate velocity and start Trackball
        //             double currentWeight = 1.0;
        //             double finalWeight = 0.0;
        //             double x_out = 0.0, y_out = 0.0;
        //             int idx = -1;
        //             for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != trackballBufferHead; i++)
        //             {
        //                 idx = (trackballBufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
        //                 x_out += trackballXBuffer[idx] * currentWeight;
        //                 y_out += trackballYBuffer[idx] * currentWeight;
        //                 finalWeight += currentWeight;
        //                 currentWeight *= 1.0;
        //             }

        //             x_out /= finalWeight;
        //             trackballXVel = x_out;
        //             y_out /= finalWeight;
        //             trackballYVel = y_out;

        //             double dist = Math.Sqrt(trackballXVel * trackballXVel + trackballYVel * trackballYVel);
        //             if (dist >= 1.0)
        //             {
        //                 trackballActive = true;

        //                 //Debug.WriteLine("START TRACK {0}", dist);
        //                 ProcessTrackballFrame(ref current, ref previous);
        //             }
        //         }
        //         else if (!current.RightPad.Touch && trackballActive)
        //         {
        //             //Console.WriteLine("CONTINUE TRACK");
        //             // Trackball Running
        //             ProcessTrackballFrame(ref current, ref previous);
        //         }
        //     }

        //     private void TrackballMouseJoystickProcess(ref SteamControllerState current,
        //         ref SteamControllerState previous, ref IXbox360Controller xbox)
        //     {
        //         if (current.RightPad.Touch && !previous.RightPad.Touch)
        //         {
        //             if (trackballActive)
        //             {
        //                 //Console.WriteLine("CHECKING HERE");
        //             }

        //             // Initial touch
        //             Array.Clear(trackballXBuffer, 0, TRACKBALL_BUFFER_LEN);
        //             Array.Clear(trackballYBuffer, 0, TRACKBALL_BUFFER_LEN);
        //             trackballXVel = 0.0;
        //             trackballYVel = 0.0;
        //             trackballActive = false;
        //             trackballBufferTail = 0;
        //             trackballBufferHead = 0;
        //             trackballDXRemain = 0.0;
        //             trackballDYRemain = 0.0;

        //             //Console.WriteLine("INITIAL");
        //         }
        //         else if (current.RightPad.Touch && previous.RightPad.Touch)
        //         {
        //             // Process normal mouse
        //             //RightTouchMouse(ref current, ref previous);
        //             //Console.WriteLine("NORMAL");
        //             RightTouchMouseJoystick(ref current, ref previous, ref xbox);

        //         }
        //         else if (!current.RightPad.Touch && previous.RightPad.Touch)
        //         {
        //             // Initially released. Calculate velocity and start Trackball
        //             double currentWeight = 1.0;
        //             double finalWeight = 0.0;
        //             double x_out = 0.0, y_out = 0.0;
        //             int idx = -1;
        //             for (int i = 0; i < TRACKBALL_BUFFER_LEN && idx != trackballBufferHead; i++)
        //             {
        //                 idx = (trackballBufferTail - i - 1 + TRACKBALL_BUFFER_LEN) % TRACKBALL_BUFFER_LEN;
        //                 x_out += trackballXBuffer[idx] * currentWeight;
        //                 y_out += trackballYBuffer[idx] * currentWeight;
        //                 finalWeight += currentWeight;
        //                 currentWeight *= 1.0;
        //             }

        //             x_out /= finalWeight;
        //             trackballXVel = x_out;
        //             y_out /= finalWeight;
        //             trackballYVel = y_out;

        //             double dist = Math.Sqrt(trackballXVel * trackballXVel + trackballYVel * trackballYVel);
        //             if (dist >= 1.0)
        //             {
        //                 trackballActive = true;

        //                 //Debug.WriteLine("START TRACK {0}", dist);
        //                 ProcessTrackballJoystickFrame(ref current, ref previous, ref xbox);
        //             }
        //         }
        //         else if (!current.RightPad.Touch && trackballActive)
        //         {
        //             //Console.WriteLine("CONTINUE TRACK");
        //             // Trackball Running
        //             ProcessTrackballJoystickFrame(ref current, ref previous, ref xbox);
        //         }
        //     }

        //     private void RightTouchMouseJoystick(ref SteamControllerState current,
        //         ref SteamControllerState previous, ref IXbox360Controller xbox)
        //     {
        //         int dx = current.RightPad.X - previous.RightPad.X;
        //         int dy = -(current.RightPad.Y - previous.RightPad.Y);
        //         //int rawDeltaX = dx, rawDeltaY = dy;

        //         //Console.WriteLine("DELTA X: {0} Y: {1}", dx, dy);

        //         // Fill trackball entry
        //         int iIndex = trackballBufferTail;
        //         trackballXBuffer[iIndex] = (dx * TRACKBALL_SCALE) / current.timeElapsed;
        //         trackballYBuffer[iIndex] = (dy * TRACKBALL_SCALE) / current.timeElapsed;
        //         trackballBufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
        //         if (trackballBufferHead == trackballBufferTail)
        //             trackballBufferHead = (trackballBufferHead + 1) % TRACKBALL_BUFFER_LEN;

        //         TouchMouseJoystickPad(dx, -dy, ref current, ref previous, ref xbox);
        //     }

        //     private void ProcessTrackballFrame(ref SteamControllerState current,
        //         ref SteamControllerState _)
        //     {
        //         double tempAngle = Math.Atan2(-trackballYVel, trackballXVel);
        //         double normX = Math.Abs(Math.Cos(tempAngle));
        //         double normY = Math.Abs(Math.Sin(tempAngle));
        //         int signX = Math.Sign(trackballXVel);
        //         int signY = Math.Sign(trackballYVel);

        //         double trackXvDecay = Math.Min(Math.Abs(trackballXVel), trackballAccel * current.timeElapsed * normX);
        //         double trackYvDecay = Math.Min(Math.Abs(trackballYVel), trackballAccel * current.timeElapsed * normY);
        //         double xVNew = trackballXVel - (trackXvDecay * signX);
        //         double yVNew = trackballYVel - (trackYvDecay * signY);
        //         double xMotion = (xVNew * current.timeElapsed) / TRACKBALL_SCALE;
        //         double yMotion = (yVNew * current.timeElapsed) / TRACKBALL_SCALE;
        //         if (xMotion != 0.0)
        //         {
        //             xMotion += trackballDXRemain;
        //         }
        //         else
        //         {
        //             trackballDXRemain = 0.0;
        //         }

        //         int dx = (int)xMotion;
        //         trackballDXRemain = xMotion - dx;

        //         if (yMotion != 0.0)
        //         {
        //             yMotion += trackballDYRemain;
        //         }
        //         else
        //         {
        //             trackballDYRemain = 0.0;
        //         }

        //         int dy = (int)yMotion;
        //         trackballDYRemain = yMotion - dy;

        //         trackballXVel = xVNew;
        //         trackballYVel = yVNew;

        //         //Console.WriteLine("DX: {0} DY: {1}", dx, dy);

        //         if (dx == 0 && dy == 0)
        //         {
        //             trackballActive = false;
        //             //Console.WriteLine("ENDING TRACK");
        //         }
        //         else
        //         {
        //             TouchMoveMouse(dx, dy, ref current);
        //         }
        //     }

        //     private void ProcessTrackballJoystickFrame(ref SteamControllerState current,
        //         ref SteamControllerState previous, ref IXbox360Controller xbox)
        //     {
        //         double tempAngle = Math.Atan2(-trackballYVel, trackballXVel);
        //         double normX = Math.Abs(Math.Cos(tempAngle));
        //         double normY = Math.Abs(Math.Sin(tempAngle));
        //         int signX = Math.Sign(trackballXVel);
        //         int signY = Math.Sign(trackballYVel);

        //         double trackXvDecay = Math.Min(Math.Abs(trackballXVel), trackballAccel * current.timeElapsed * normX);
        //         double trackYvDecay = Math.Min(Math.Abs(trackballYVel), trackballAccel * current.timeElapsed * normY);
        //         double xVNew = trackballXVel - (trackXvDecay * signX);
        //         double yVNew = trackballYVel - (trackYvDecay * signY);
        //         double xMotion = (xVNew * current.timeElapsed) / TRACKBALL_SCALE;
        //         double yMotion = (yVNew * current.timeElapsed) / TRACKBALL_SCALE;
        //         if (xMotion != 0.0)
        //         {
        //             xMotion += trackballDXRemain;
        //         }
        //         else
        //         {
        //             trackballDXRemain = 0.0;
        //         }

        //         int dx = (int)xMotion;
        //         trackballDXRemain = xMotion - dx;

        //         if (yMotion != 0.0)
        //         {
        //             yMotion += trackballDYRemain;
        //         }
        //         else
        //         {
        //             trackballDYRemain = 0.0;
        //         }

        //         int dy = (int)yMotion;
        //         trackballDYRemain = yMotion - dy;

        //         trackballXVel = xVNew;
        //         trackballYVel = yVNew;

        //         //Console.WriteLine("DX: {0} DY: {1}", dx, dy);

        //         if (dx == 0 && dy == 0)
        //         {
        //             trackballActive = false;
        //             //Console.WriteLine("ENDING TRACK");
        //         }
        //         else
        //         {
        //             TouchMouseJoystickPad(dx, -dy, ref current, ref previous, ref xbox);
        //         }
        //     }

        //     private void TouchMoveMouse(int dx, int dy, ref SteamControllerState current)
        //     {
        //         //const int deadZone = 18;
        //         //const int deadZone = 12;
        //const int deadZone = 8;

        //         double tempAngle = Math.Atan2(-dy, dx);
        //         double normX = Math.Abs(Math.Cos(tempAngle));
        //         double normY = Math.Abs(Math.Sin(tempAngle));
        //         int signX = Math.Sign(dx);
        //         int signY = Math.Sign(dy);

        //         double timeElapsed = current.timeElapsed;
        //         double coefficient = TOUCHPAD_COEFFICIENT;
        //         double offset = TOUCHPAD_MOUSE_OFFSET;
        //         // Base speed 8 ms
        //         double tempDouble = timeElapsed * 125.0;

        //         int deadzoneX = (int)Math.Abs(normX * deadZone);
        //         int deadzoneY = (int)Math.Abs(normY * deadZone);

        //         if (Math.Abs(dx) > deadzoneX)
        //         {
        //             dx -= signX * deadzoneX;
        //         }
        //         else
        //         {
        //             dx = 0;
        //         }

        //         if (Math.Abs(dy) > deadzoneY)
        //         {
        //             dy -= signY * deadzoneY;
        //         }
        //         else
        //         {
        //             dy = 0;
        //         }

        //         double xMotion = dx != 0 ? coefficient * (dx * tempDouble)
        //             + (normX * (offset * signX)) : 0;

        //         double yMotion = dy != 0 ? coefficient * (dy * tempDouble)
        //             + (normY * (offset * signY)) : 0;

        //         double throttla = 1.428;
        //         //double offman = 10;
        //         //double throttla = 1.4;
        //         double offman = 12;

        //         double absX = Math.Abs(xMotion);
        //         if (absX <= normX * offman)
        //         {
        //             //double before = xMotion;
        //             //double adjOffman = normX != 0.0 ? normX * offman : offman;
        //             xMotion = signX * Math.Pow(absX / offman, throttla) * offman;
        //             //Console.WriteLine("Before: {0} After {1}", before, xMotion);
        //             //Console.WriteLine(absX / adjOffman);
        //         }

        //         double absY = Math.Abs(yMotion);
        //         if (absY <= normY * offman)
        //         {
        //             //double adjOffman = normY != 0.0 ? normY * offman : offman;
        //             yMotion = signY * Math.Pow(absY / offman, throttla) * offman;
        //             //Console.WriteLine(absY / adjOffman);
        //         }

        //         mouseX = xMotion; mouseY = yMotion;
        //     }

        //     private const double TOUCHPAD_MOUSE_OFFSET = 0.375;
        //     //private const double TOUCHPAD_COEFFICIENT = 0.012;
        //     private const double TOUCHPAD_COEFFICIENT = 0.012 * 1.1;
        //     private void RightTouchMouse(ref SteamControllerState current,
        //         ref SteamControllerState previous)
        //     {
        //         int dx = current.RightPad.X - previous.RightPad.X;
        //         int dy = -(current.RightPad.Y - previous.RightPad.Y);
        //         //int rawDeltaX = dx, rawDeltaY = dy;

        //         //Console.WriteLine("DELTA X: {0} Y: {1}", dx, dy);

        //         // Fill trackball entry
        //         int iIndex = trackballBufferTail;
        //         trackballXBuffer[iIndex] = (dx * TRACKBALL_SCALE) / current.timeElapsed;
        //         trackballYBuffer[iIndex] = (dy * TRACKBALL_SCALE) / current.timeElapsed;
        //         trackballBufferTail = (iIndex + 1) % TRACKBALL_BUFFER_LEN;
        //         if (trackballBufferHead == trackballBufferTail)
        //             trackballBufferHead = (trackballBufferHead + 1) % TRACKBALL_BUFFER_LEN;

        //         TouchMoveMouse(dx, dy, ref current);
        //     }

        //     private void GenerateMouseMoveEvent()
        //     {
        //         if (mouseX != 0.0 || mouseY != 0.0)
        //         {
        //             if ((mouseX > 0.0 && mouseXRemainder > 0.0) || (mouseX < 0.0 && mouseXRemainder < 0.0))
        //             {
        //                 mouseX += mouseXRemainder;
        //             }
        //             else
        //             {
        //                 mouseXRemainder = 0.0;
        //             }

        //             if ((mouseY > 0.0 && mouseYRemainder > 0.0) || (mouseY < 0.0 && mouseYRemainder < 0.0))
        //             {
        //                 mouseY += mouseYRemainder;
        //             }
        //             else
        //             {
        //                 mouseYRemainder = 0.0;
        //             }

        //             //mouseX = filterX.Filter(mouseX, 1.0 / 0.016);
        //             //mouseY = filterY.Filter(mouseY, 1.0 / 0.016);
        //             mouseX = filterX.Filter(mouseX, 1.0 / currentRate);
        //             mouseY = filterY.Filter(mouseY, 1.0 / currentRate);

        //             double mouseXTemp = mouseX - (remainderCutoff(mouseX * 100.0, 1.0) / 100.0);
        //             int mouseXInt = (int)(mouseXTemp);
        //             mouseXRemainder = mouseXTemp - mouseXInt;

        //             double mouseYTemp = mouseY - (remainderCutoff(mouseY * 100.0, 1.0) / 100.0);
        //             int mouseYInt = (int)(mouseYTemp);
        //             mouseYRemainder = mouseYTemp - mouseYInt;
        //             //mouseReport.MouseX = (short)mouseXInt;
        //             //mouseReport.MouseY = (short)mouseYInt;
        //             fakerInputHandler.MoveRelativeMouse(mouseXInt, mouseYInt);
        //             mouseSync = true;
        //             //fakerInput.UpdateRelativeMouse(mouseReport);
        //             //InputMethods.MoveCursorBy(mouseXInt, mouseYInt);
        //         }
        //         else
        //         {
        //             mouseXRemainder = mouseYRemainder = 0.0;
        //             //mouseX = filterX.Filter(0.0, 1.0 / 0.016);
        //             //mouseY = filterY.Filter(0.0, 1.0 / 0.016);
        //             filterX.Filter(mouseX, 1.0 / currentRate);
        //             filterY.Filter(mouseY, 1.0 / currentRate);
        //         }

        //         mouseX = mouseY = 0.0;
        //     }

        private void GenerateMouseMoveEvent()
        {
            if (mouseX != 0.0 || mouseY != 0.0)
            {
                if ((mouseX > 0.0 && mouseXRemainder > 0.0) || (mouseX < 0.0 && mouseXRemainder < 0.0))
                {
                    mouseX += mouseXRemainder;
                }
                else
                {
                    mouseXRemainder = 0.0;
                }

                if ((mouseY > 0.0 && mouseYRemainder > 0.0) || (mouseY < 0.0 && mouseYRemainder < 0.0))
                {
                    mouseY += mouseYRemainder;
                }
                else
                {
                    mouseYRemainder = 0.0;
                }

                //mouseX = filterX.Filter(mouseX, 1.0 / 0.016);
                //mouseY = filterY.Filter(mouseY, 1.0 / 0.016);
                //mouseX = filterX.Filter(mouseX, currentRate);
                //mouseY = filterY.Filter(mouseY, currentRate);

                //// Filter does not go back to absolute zero for reasons.Check
                //// for low number and reset to zero
                //if (Math.Abs(mouseX) < 0.0001) mouseX = 0.0;
                //if (Math.Abs(mouseY) < 0.0001) mouseY = 0.0;

                double mouseXTemp = mouseX - (remainderCutoff(mouseX * 100.0, 1.0) / 100.0);
                int mouseXInt = (int)(mouseXTemp);
                mouseXRemainder = mouseXTemp - mouseXInt;

                double mouseYTemp = mouseY - (remainderCutoff(mouseY * 100.0, 1.0) / 100.0);
                int mouseYInt = (int)(mouseYTemp);
                mouseYRemainder = mouseYTemp - mouseYInt;
                fakerInputHandler.MoveRelativeMouse(mouseXInt, mouseYInt);
                //mouseReport.MouseX = (short)mouseXInt;
                //mouseReport.MouseY = (short)mouseYInt;
                //InputMethods.MoveCursorBy(mouseXInt, mouseYInt);
            }
            else
            {
                mouseXRemainder = mouseYRemainder = 0.0;
                //mouseX = filterX.Filter(0.0, 1.0 / 0.016);
                //mouseY = filterY.Filter(0.0, 1.0 / 0.016);
                //filterX.Filter(mouseX, currentRate);
                //filterY.Filter(mouseY, currentRate);
            }

            mouseX = mouseY = 0.0;
        }

        public void GenerateMouseEventFiltered(OneEuroFilter filterX, OneEuroFilter filterY)
        {
            if (mouseX != 0.0 || mouseY != 0.0)
            {
                if ((mouseX > 0.0 && mouseXRemainder > 0.0) || (mouseX < 0.0 && mouseXRemainder < 0.0))
                {
                    mouseX += mouseXRemainder;
                }
                else
                {
                    mouseXRemainder = 0.0;
                }

                if ((mouseY > 0.0 && mouseYRemainder > 0.0) || (mouseY < 0.0 && mouseYRemainder < 0.0))
                {
                    mouseY += mouseYRemainder;
                }
                else
                {
                    mouseYRemainder = 0.0;
                }

                //mouseX = filterX.Filter(mouseX, 1.0 / 0.016);
                //mouseY = filterY.Filter(mouseY, 1.0 / 0.016);
                mouseX = filterX.Filter(mouseX, currentRate);
                mouseY = filterY.Filter(mouseY, currentRate);

                // Filter does not go back to absolute zero for reasons.Check
                // for low number and reset to zero
                if (Math.Abs(mouseX) < 0.0001) mouseX = 0.0;
                if (Math.Abs(mouseY) < 0.0001) mouseY = 0.0;

                double mouseXTemp = mouseX - (remainderCutoff(mouseX * 100.0, 1.0) / 100.0);
                int mouseXInt = (int)(mouseXTemp);
                mouseXRemainder = mouseXTemp - mouseXInt;

                double mouseYTemp = mouseY - (remainderCutoff(mouseY * 100.0, 1.0) / 100.0);
                int mouseYInt = (int)(mouseYTemp);
                mouseYRemainder = mouseYTemp - mouseYInt;
                fakerInputHandler.MoveRelativeMouse(mouseXInt, mouseYInt);
                //mouseReport.MouseX = (short)mouseXInt;
                //mouseReport.MouseY = (short)mouseYInt;
                //InputMethods.MoveCursorBy(mouseXInt, mouseYInt);
            }
            else
            {
                mouseXRemainder = mouseYRemainder = 0.0;
                //mouseX = filterX.Filter(0.0, 1.0 / 0.016);
                //mouseY = filterY.Filter(0.0, 1.0 / 0.016);
                filterX.Filter(mouseX, currentRate);
                filterY.Filter(mouseY, currentRate);
            }

            mouseX = mouseY = 0.0;
        }

        private double remainderCutoff(double dividend, double divisor)
        {
            return dividend - (divisor * (int)(dividend / divisor));
        }

        private short AxisScale(int value, bool flip)
        {
            unchecked
            {
                float temp = (value - STICK_MIN) * reciprocalInputResolution;
                if (flip) temp = (temp - 0.5f) * -1.0f + 0.5f;
                return (short)(temp * OUTPUT_X360_RESOLUTION + X360_STICK_MIN);
            }
        }

        public void GamepadFromButtonInput(OutputActionData data, bool pressed)
        {
            data.activatedEvent = true;

            switch (data.JoypadCode)
            {
                case JoypadActionCodes.AxisLX:
                    intermediateState.LX = pressed ? (data.Negative ? -1.0 : 1.0) : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisLY:
                    intermediateState.LY = pressed ? (data.Negative ? -1.0 : 1.0) : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRX:
                    intermediateState.RX = pressed ? (data.Negative ? -1.0 : 1.0) : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRY:
                    intermediateState.RY = pressed ? (data.Negative ? -1.0 : 1.0) : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisLTrigger:
                    intermediateState.LTrigger = pressed ? 1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRTrigger:
                    intermediateState.RTrigger = pressed ? 1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.AxisLXNeg:
                    intermediateState.LX = pressed ? -1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisLXPos:
                    intermediateState.LX = pressed ? 1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.AxisLYNeg:
                    intermediateState.LY = pressed ? -1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisLYPos:
                    intermediateState.LY = pressed ? 1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.AxisRXNeg:
                    intermediateState.RX = pressed ? -1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRXPos:
                    intermediateState.RX = pressed ? 1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.AxisRYNeg:
                    intermediateState.RY = pressed ? -1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRYPos:
                    intermediateState.RY = pressed ? 1.0 : 0.0;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.BtnDPadUp:
                    intermediateState.DpadUp = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnDPadDown:
                    intermediateState.DpadDown = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnDPadLeft:
                    intermediateState.DpadLeft = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnDPadRight:
                    intermediateState.DpadRight = pressed;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.BtnNorth:
                    intermediateState.BtnNorth = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnEast:
                    intermediateState.BtnEast = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnSouth:
                    intermediateState.BtnSouth = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnWest:
                    intermediateState.BtnWest = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnMode:
                    intermediateState.BtnMode = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnStart:
                    intermediateState.BtnStart = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnSelect:
                    intermediateState.BtnSelect = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnLShoulder:
                    intermediateState.BtnLShoulder = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnRShoulder:
                    intermediateState.BtnRShoulder = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnThumbL:
                    intermediateState.BtnThumbL = pressed;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnThumbR:
                    intermediateState.BtnThumbR = pressed;
                    intermediateState.Dirty = true;
                    break;

                default:
                    break;
            }
        }

        public void GamepadFromAxisInput(OutputActionData data, double norm)
        {
            bool active = norm != 0.0 ? true : false;
            data.activatedEvent = active;

            switch (data.JoypadCode)
            {
                case JoypadActionCodes.AxisLX:
                    intermediateState.LX = norm;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisLY:
                    intermediateState.LY = norm;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRX:
                    intermediateState.RX = norm;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRY:
                    intermediateState.RY = norm;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisLTrigger:
                    intermediateState.LTrigger = norm;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.AxisRTrigger:
                    intermediateState.RTrigger = norm;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.BtnDPadUp:
                    intermediateState.DpadUp = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnDPadDown:
                    intermediateState.DpadDown = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnDPadLeft:
                    intermediateState.DpadLeft = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnDPadRight:
                    intermediateState.DpadRight = active;
                    intermediateState.Dirty = true;
                    break;

                case JoypadActionCodes.BtnNorth:
                    intermediateState.BtnNorth = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnEast:
                    intermediateState.BtnEast = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnSouth:
                    intermediateState.BtnSouth = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnWest:
                    intermediateState.BtnWest = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnMode:
                    intermediateState.BtnMode = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnStart:
                    intermediateState.BtnStart = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnSelect:
                    intermediateState.BtnSelect = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnLShoulder:
                    intermediateState.BtnLShoulder = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnRShoulder:
                    intermediateState.BtnRShoulder = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnThumbL:
                    intermediateState.BtnThumbL = active;
                    intermediateState.Dirty = true;
                    break;
                case JoypadActionCodes.BtnThumbR:
                    intermediateState.BtnThumbR = active;
                    intermediateState.Dirty = true;
                    break;
                default:
                    break;
            }
        }

        public void GamepadFromStickInput(OutputActionData data,
            double xNorm, double yNorm, bool force = true)
        {
            data.activatedEvent = true;

            switch (data.StickCode)
            {
                case StickActionCodes.LS:
                    if (force)
                    {
                        intermediateState.LX = xNorm;
                        intermediateState.LY = yNorm;
                        intermediateState.LSDirty = true;
                    }
                    else
                    {
                        if (!intermediateState.LSDirty)
                        {
                            intermediateState.LX = xNorm;
                            intermediateState.LY = yNorm;
                            intermediateState.LSDirty = true;
                        }
                    }

                    intermediateState.Dirty = true;
                    break;
                case StickActionCodes.RS:
                    if (force)
                    {
                        intermediateState.RX = xNorm;
                        intermediateState.RY = yNorm;
                        intermediateState.RSDirty = true;
                    }
                    else
                    {
                        if (!intermediateState.RSDirty)
                        {
                            intermediateState.RX = xNorm;
                            intermediateState.RY = yNorm;
                            intermediateState.RSDirty = true;
                        }
                    }

                    intermediateState.Dirty = true;
                    break;

                default:
                    break;
            }
        }

        public void GamepadFromDpadInput(OutputActionData data, DpadDirections direction)
        {
            data.activatedEvent = true;

            bool dpadUp = false, dpadLeft = false, dpadDown = false, dpadRight = false;
            unchecked
            {
                if ((direction & DpadDirections.Up) != 0)
                    dpadUp = true;
                if ((direction & DpadDirections.Left) != 0)
                    dpadLeft = true;
                if ((direction & DpadDirections.Down) != 0)
                    dpadDown = true;
                if ((direction & DpadDirections.Right) != 0)
                    dpadRight = true;
            }

            switch (data.DpadCode)
            {
                case DPadActionCodes.DPad1:
                    intermediateState.DpadUp = dpadUp;
                    intermediateState.DpadLeft = dpadLeft;
                    intermediateState.DpadDown = dpadDown;
                    intermediateState.DpadRight = dpadRight;
                    intermediateState.Dirty = true;
                    break;

                default:
                    break;
            }
        }

        //private void PopulateDualShock4()
        //{
        //    IDualShock4Controller tempDS4 = outputController as IDualShock4Controller;

        //    unchecked
        //    {
        //        ushort tempButtons = 0;
        //        DualShock4DPadDirection tempDPad = DualShock4DPadDirection.None;
        //        ushort tempSpecial = 0;
        //        if (intermediateState.BtnSouth) tempButtons |= DualShock4Button.Cross.Value;
        //        if (intermediateState.BtnEast) tempButtons |= DualShock4Button.Circle.Value;
        //        if (intermediateState.BtnWest) tempButtons |= DualShock4Button.Square.Value;
        //        if (intermediateState.BtnNorth) tempButtons |= DualShock4Button.Triangle.Value;
        //        if (intermediateState.BtnStart) tempButtons |= DualShock4Button.Options.Value;
        //        if (intermediateState.BtnSelect) tempButtons |= DualShock4Button.Share.Value;

        //        if (intermediateState.BtnLShoulder) tempButtons |= DualShock4Button.ShoulderLeft.Value;
        //        if (intermediateState.BtnRShoulder) tempButtons |= DualShock4Button.ShoulderRight.Value;
        //        if (intermediateState.BtnMode) tempSpecial |= DualShock4SpecialButton.Ps.Value;

        //        if (intermediateState.BtnThumbL) tempButtons |= DualShock4Button.ThumbLeft.Value;
        //        if (intermediateState.BtnThumbR) tempButtons |= DualShock4Button.ThumbRight.Value;

        //        if (intermediateState.DpadUp && intermediateState.DpadRight) tempDPad = DualShock4DPadDirection.Northeast;
        //        else if (intermediateState.DpadUp && intermediateState.DpadLeft) tempDPad = DualShock4DPadDirection.Northwest;
        //        else if (intermediateState.DpadUp) tempDPad = DualShock4DPadDirection.North;
        //        else if (intermediateState.DpadRight && intermediateState.DpadDown) tempDPad = DualShock4DPadDirection.Southeast;
        //        else if (intermediateState.DpadRight) tempDPad = DualShock4DPadDirection.East;
        //        else if (intermediateState.DpadDown && intermediateState.DpadLeft) tempDPad = DualShock4DPadDirection.Southwest;
        //        else if (intermediateState.DpadDown) tempDPad = DualShock4DPadDirection.South;
        //        else if (intermediateState.DpadLeft) tempDPad = DualShock4DPadDirection.West;

        //        tempDS4.SetButtonsFull(tempButtons);
        //        tempDS4.SetSpecialButtonsFull((byte)tempSpecial);
        //        tempDS4.SetDPadDirection(tempDPad);
        //    }

        //    tempDS4.LeftThumbX = (byte)((intermediateState.LX >= 0 ? (DS4_STICK_MAX - DS4_STICK_MID) : -(DS4_STICK_MIN - DS4_STICK_MID)) * intermediateState.LX + DS4_STICK_MID);
        //    tempDS4.LeftThumbY = (byte)((intermediateState.LY >= 0 ? -(DS4_STICK_MIN - DS4_STICK_MID) : (DS4_STICK_MAX - DS4_STICK_MID)) * -intermediateState.LY + DS4_STICK_MID);

        //    tempDS4.RightThumbX = (byte)((intermediateState.RX >= 0 ? (DS4_STICK_MAX - DS4_STICK_MID) : -(DS4_STICK_MIN - DS4_STICK_MID)) * intermediateState.RX + DS4_STICK_MID);
        //    tempDS4.RightThumbY = (byte)((intermediateState.RY >= 0 ? -(DS4_STICK_MIN - DS4_STICK_MID) : (DS4_STICK_MAX - DS4_STICK_MID)) * -intermediateState.RY + DS4_STICK_MID);

        //    tempDS4.LeftTrigger = (byte)(intermediateState.LTrigger * 255);
        //    tempDS4.RightTrigger = (byte)(intermediateState.RTrigger * 255);
        //}

        //private void PopulateXbox()
        //{
        //    IXbox360Controller outputX360 = outputController as IXbox360Controller;

        //    unchecked
        //    {
        //        ushort tempButtons = 0;
        //        if (intermediateState.BtnSouth) tempButtons |= Xbox360Button.A.Value;
        //        if (intermediateState.BtnEast) tempButtons |= Xbox360Button.B.Value;
        //        if (intermediateState.BtnWest) tempButtons |= Xbox360Button.X.Value;
        //        if (intermediateState.BtnNorth) tempButtons |= Xbox360Button.Y.Value;
        //        if (intermediateState.BtnStart) tempButtons |= Xbox360Button.Start.Value;
        //        if (intermediateState.BtnSelect) tempButtons |= Xbox360Button.Back.Value;

        //        if (intermediateState.BtnLShoulder) tempButtons |= Xbox360Button.LeftShoulder.Value;
        //        if (intermediateState.BtnRShoulder) tempButtons |= Xbox360Button.RightShoulder.Value;
        //        if (intermediateState.BtnMode) tempButtons |= Xbox360Button.Guide.Value;

        //        if (intermediateState.BtnThumbL) tempButtons |= Xbox360Button.LeftThumb.Value;
        //        if (intermediateState.BtnThumbR) tempButtons |= Xbox360Button.RightThumb.Value;

        //        if (intermediateState.DpadUp) tempButtons |= Xbox360Button.Up.Value;
        //        if (intermediateState.DpadDown) tempButtons |= Xbox360Button.Down.Value;
        //        if (intermediateState.DpadLeft) tempButtons |= Xbox360Button.Left.Value;
        //        if (intermediateState.DpadRight) tempButtons |= Xbox360Button.Right.Value;

        //        outputX360.SetButtonsFull(tempButtons);
        //    }

        //    outputX360.LeftThumbX = (short)(intermediateState.LX * (intermediateState.LX >= 0 ? X360_STICK_MAX : -X360_STICK_MIN));
        //    outputX360.LeftThumbY = (short)(intermediateState.LY * (intermediateState.LY >= 0 ? X360_STICK_MAX : -X360_STICK_MIN));

        //    outputX360.RightThumbX = (short)(intermediateState.RX * (intermediateState.RX >= 0 ? X360_STICK_MAX : -X360_STICK_MIN));
        //    outputX360.RightThumbY = (short)(intermediateState.RY * (intermediateState.RY >= 0 ? X360_STICK_MAX : -X360_STICK_MIN));

        //    outputX360.LeftTrigger = (byte)(intermediateState.LTrigger * 255);
        //    outputX360.RightTrigger = (byte)(intermediateState.RTrigger * 255);
        //}

        public double GetStickAngle(StickActionCodes code)
        {
            double result = 0.0;

            switch (code)
            {
                case StickActionCodes.LS:
                    result = 0.0;
                    break;

                default:
                    break;
            }

            return result;
        }

        public void SyncKeyboard()
        {
            var removed = releasedKeys.Except(activeKeys);
            var added = activeKeys.Except(releasedKeys);
            foreach (uint vk in removed)
            {
                if (keyReferenceCountDict.TryGetValue(vk, out int refCount))
                {
                    refCount--;
                    if (refCount <= 0)
                    {
                        fakerInputHandler.PerformKeyRelease(vk);
                        //keyboardReport.KeyUp((KeyboardKey)vk);
                        //InputMethods.performKeyRelease((ushort)vk);
                        keyReferenceCountDict.Remove(vk);
                    }
                    else
                    {
                        keyReferenceCountDict[vk] = refCount;
                    }
                }
            }

            foreach (uint vk in added)
            {
                if (!keyReferenceCountDict.TryGetValue(vk, out int refCount))
                {
                    fakerInputHandler.PerformKeyPress(vk);
                    //keyboardReport.KeyDown((KeyboardKey)vk);
                    //InputMethods.performKeyPress((ushort)vk);
                    keyReferenceCountDict.Add(vk, 1);
                }
                else
                {
                    keyReferenceCountDict[vk] = refCount++;
                }
            }

            releasedKeys.Clear();
            activeKeys.Clear();
        }

        public void SyncMouseButtons()
        {
            var removed = releasedMouseButtons.Except(activeMouseButtons);
            var added = activeMouseButtons.Except(releasedMouseButtons);

            foreach (int mouseCode in removed)
            {
                if (currentMouseButtons.Contains(mouseCode))
                {
                    uint mouseButton = 0;
                    switch (mouseCode)
                    {
                        case MouseButtonCodes.MOUSE_LEFT_BUTTON:
                            mouseButton = (uint)FakerInputWrapper.MouseButton.LeftButton;
                            //mouseButton = InputMethods.MOUSEEVENTF_LEFTUP;
                            break;
                        case MouseButtonCodes.MOUSE_MIDDLE_BUTTON:
                            mouseButton = (uint)FakerInputWrapper.MouseButton.MiddleButton;
                            //mouseButton = InputMethods.MOUSEEVENTF_MIDDLEUP;
                            break;
                        case MouseButtonCodes.MOUSE_RIGHT_BUTTON:
                            mouseButton = (uint)FakerInputWrapper.MouseButton.RightButton;
                            //mouseButton = InputMethods.MOUSEEVENTF_RIGHTUP;
                            break;
                        default:
                            break;
                    }

                    if (mouseButton != 0)
                    {
                        fakerInputHandler.PerformMouseButtonEvent(mouseButton);
                        //mouseReport.ButtonUp((FakerInputWrapper.MouseButton)mouseButton);
                        //InputMethods.MouseEvent(mouseButton);
                        currentMouseButtons.Remove(mouseCode);
                    }
                }
            }

            foreach (int mouseCode in added)
            {
                if (!currentMouseButtons.Contains(mouseCode))
                {
                    uint mouseButton = 0;
                    switch (mouseCode)
                    {
                        case MouseButtonCodes.MOUSE_LEFT_BUTTON:
                            mouseButton = (uint)FakerInputWrapper.MouseButton.LeftButton;
                            //mouseButton = InputMethods.MOUSEEVENTF_LEFTDOWN;
                            break;
                        case MouseButtonCodes.MOUSE_MIDDLE_BUTTON:
                            mouseButton = (uint)FakerInputWrapper.MouseButton.MiddleButton;
                            //mouseButton = InputMethods.MOUSEEVENTF_MIDDLEDOWN;
                            break;
                        case MouseButtonCodes.MOUSE_RIGHT_BUTTON:
                            mouseButton = (uint)FakerInputWrapper.MouseButton.RightButton;
                            //mouseButton = InputMethods.MOUSEEVENTF_RIGHTDOWN;
                            break;
                        default:
                            break;
                    }

                    if (mouseButton != 0)
                    {
                        fakerInputHandler.PerformMouseButtonPress(mouseButton);
                        //mouseReport.ButtonDown((FakerInputWrapper.MouseButton)mouseCode);
                        //InputMethods.MouseEvent(mouseButton);
                        currentMouseButtons.Add(mouseCode);
                    }
                }
            }

            releasedMouseButtons.Clear();
            activeMouseButtons.Clear();
        }

        public void TranslateCoorToAbsDisplay(double inX, double inY,
            ref Rect absDisplayBounds, ref Rect fullDesktopBounds,
            out double outX, out double outY)
        {
            //outX = outY = 0.0;
            //int topLeftX = (int)absDisplayBounds.Left;
            //double testLeft = 0.0;
            //double testRight = 0.0;
            //double testTop = 0.0;
            //double testBottom = 0.0;

            double widthRatio = (absDisplayBounds.Left + absDisplayBounds.Right) / fullDesktopBounds.Width;
            double heightRatio = (absDisplayBounds.Top + absDisplayBounds.Bottom) / fullDesktopBounds.Height;
            double bX = absDisplayBounds.Left / fullDesktopBounds.Width;
            double bY = absDisplayBounds.Top / fullDesktopBounds.Height;

            outX = widthRatio * inX + bX;
            outY = heightRatio * inY + bY;
            //outX = (absDisplayBounds.TopRight.X - absDisplayBounds.TopLeft.X) * inX + absDisplayBounds.TopLeft.X;
            //outY = (absDisplayBounds.BottomRight.Y - absDisplayBounds.TopLeft.Y) * inY + absDisplayBounds.TopLeft.Y;
        }

        public void RunEventFromRelative(OutputActionData actionData, bool pressed, double outputValue,
            bool fullRelease = true)
        {
            switch (actionData.OutputType)
            {
                case OutputActionData.ActionType.MouseWheel:
                    if (pressed && !actionData.activatedEvent)
                    {
                        int vWheel = 0; int hWheel = 0;
                        double absValue = Math.Abs(outputValue);
                        switch (actionData.OutputCode)
                        {
                            case 1: // Wheel Up
                                    //vWheel = 120;
                                vWheel = (int)(1 * absValue);
                                mouseWheelY = vWheel;
                                mouseWheelSync = true;
                                break;
                            case 2: // Wheel Down
                                    //vWheel = -120;
                                vWheel = (int)(-1 * absValue);
                                mouseWheelY = vWheel;
                                mouseWheelSync = true;
                                break;
                            case 3: // Wheel Left
                                    //hWheel = 120;
                                hWheel = (int)(1 * absValue);
                                mouseWheelX = hWheel;
                                mouseWheelSync = true;
                                break;
                            case 4: // Wheel Right
                                    //hWheel = -120;
                                hWheel = (int)(-1 * absValue);
                                mouseWheelX = hWheel;
                                mouseWheelSync = true;
                                break;
                            default:
                                break;
                        }

                        //fakerInputHandler.PerformMouseWheelEvent(vWheel, hWheel);
                        //InputMethods.MouseWheel(vWheel, hWheel);
                        actionData.activatedEvent = true;
                    }
                    else if (!pressed)
                    {
                        actionData.activatedEvent = false;
                    }

                    break;
                case OutputActionData.ActionType.RelativeMouse:
                    {
                        if (pressed)
                        {
                            double distance = 0.0;
                            double absValue = Math.Abs(outputValue);
                            bool xDir = false;
                            bool yDir = false;
                            switch (actionData.mouseDir)
                            {
                                case OutputActionData.RelativeMouseDir.MouseUp:
                                    distance = -1.0 * absValue;
                                    xDir = false;
                                    yDir = true;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseDown:
                                    distance = 1.0 * absValue;
                                    xDir = false;
                                    yDir = true;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseLeft:
                                    distance = -1.0 * absValue;
                                    xDir = true;
                                    yDir = false;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseRight:
                                    distance = 1.0 * absValue;
                                    xDir = true;
                                    yDir = false;
                                    break;
                                default:
                                    break;
                            }

                            int xSpeed = actionData.extraSettings.mouseXSpeed;
                            int ySpeed = actionData.extraSettings.mouseYSpeed;

                            const int MOUSESPEEDFACTOR = 20;
                            const double MOUSE_VELOCITY_OFFSET = 0.013;
                            double timeDelta = CurrentLatency;
                            int mouseVelocity = xDir ? xSpeed * MOUSESPEEDFACTOR : ySpeed * MOUSESPEEDFACTOR;
                            double mouseOffset = MOUSE_VELOCITY_OFFSET * mouseVelocity;
                            double tempMouseOffset = mouseOffset;

                            if (xDir)
                            {
                                double xMotion = ((mouseVelocity - tempMouseOffset) * timeDelta * distance + (mouseOffset * timeDelta));
                                MouseX = xMotion;
                                MouseSync = true;
                            }
                            else if (yDir)
                            {
                                double yMotion = ((mouseVelocity - tempMouseOffset) * timeDelta * distance + (mouseOffset * timeDelta));
                                MouseY = yMotion;
                                MouseSync = true;
                            }
                            //xMotion = ((mouseVelocity - tempMouseOffsetX) * timeDelta * absXNorm + (tempMouseOffsetX * timeDelta)) * xSign;
                            //yMotion = ((mouseVelocity - tempMouseOffsetY) * timeDelta * absYNorm + (tempMouseOffsetY * timeDelta)) * -ySign;
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        public void RunEventFromAnalog(OutputActionData actionData, bool pressed, double outputNorm,
            double axisUnit, bool fullRelease = true)
        {
            switch(actionData.OutputType)
            {
                case OutputActionData.ActionType.MouseWheel:
                    {
                        if (pressed && !actionData.activatedEvent)
                        {
                            int vWheel = 0; int hWheel = 0;
                            switch (actionData.OutputCode)
                            {
                                case 1: // Wheel Up
                                        //vWheel = 120;
                                    vWheel = 1;
                                    mouseWheelY = vWheel;
                                    mouseWheelSync = true;
                                    break;
                                case 2: // Wheel Down
                                        //vWheel = -120;
                                    vWheel = -1;
                                    mouseWheelY = vWheel;
                                    mouseWheelSync = true;
                                    break;
                                case 3: // Wheel Left
                                        //hWheel = 120;
                                    hWheel = 1;
                                    mouseWheelX = hWheel;
                                    mouseWheelSync = true;
                                    break;
                                case 4: // Wheel Right
                                        //hWheel = -120;
                                    hWheel = -1;
                                    mouseWheelX = hWheel;
                                    mouseWheelSync = true;
                                    break;
                                default:
                                    break;
                            }

                            //fakerInputHandler.PerformMouseWheelEvent(vWheel, hWheel);
                            //InputMethods.MouseWheel(vWheel, hWheel);
                            actionData.activatedEvent = true;
                        }
                        else if (!pressed)
                        {
                            actionData.activatedEvent = false;
                        }
                    }

                    break;
                case OutputActionData.ActionType.Keyboard:
                    {
                        if (pressed)
                        {
                            if (!actionData.activatedEvent)
                            {
                                activeKeys.Add(actionData.OutputCodeAlias);
                                actionData.activatedEvent = true;
                            }
                        }
                        else
                        {
                            if (actionData.activatedEvent)
                            {
                                releasedKeys.Add(actionData.OutputCodeAlias);
                                actionData.activatedEvent = false;
                            }
                        }
                    }

                    break;
                case OutputActionData.ActionType.MouseButton:
                    {
                        switch (actionData.OutputCode)
                        {
                            case MouseButtonCodes.MOUSE_LEFT_BUTTON:
                            case MouseButtonCodes.MOUSE_MIDDLE_BUTTON:
                            case MouseButtonCodes.MOUSE_RIGHT_BUTTON:
                                if (pressed)
                                {
                                    if (!currentMouseButtons.Contains(actionData.OutputCode))
                                    {
                                        activeMouseButtons.Add(actionData.OutputCode);
                                        actionData.activatedEvent = true;
                                    }
                                }
                                else
                                {
                                    if (currentMouseButtons.Contains(actionData.OutputCode))
                                    {
                                        releasedMouseButtons.Add(actionData.OutputCode);
                                        actionData.activatedEvent = false;
                                    }
                                }

                                break;
                            default:
                                break;
                        }
                    }

                    break;
                case OutputActionData.ActionType.RelativeMouse:
                    {
                        if (pressed)
                        {
                            double distance = 0.0;
                            double absNorm = Math.Abs(outputNorm);
                            bool xDir = false;
                            bool yDir = false;
                            switch (actionData.mouseDir)
                            {
                                case OutputActionData.RelativeMouseDir.MouseUp:
                                    distance = -1.0 * absNorm;
                                    xDir = false;
                                    yDir = true;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseDown:
                                    distance = 1.0 * absNorm;
                                    xDir = false;
                                    yDir = true;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseLeft:
                                    distance = -1.0 * absNorm;
                                    xDir = true;
                                    yDir = false;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseRight:
                                    distance = 1.0 * absNorm;
                                    xDir = true;
                                    yDir = false;
                                    break;
                                default:
                                    break;
                            }

                            int xSpeed = actionData.extraSettings.mouseXSpeed;
                            int ySpeed = actionData.extraSettings.mouseYSpeed;

                            const int MOUSESPEEDFACTOR = 20;
                            const double MOUSE_VELOCITY_OFFSET = 0.013;
                            double timeDelta = CurrentLatency;
                            int mouseVelocity = xDir ? xSpeed * MOUSESPEEDFACTOR : ySpeed * MOUSESPEEDFACTOR;
                            double mouseOffset = MOUSE_VELOCITY_OFFSET * mouseVelocity;
                            double tempMouseOffset = axisUnit * mouseOffset;

                            if (xDir)
                            {
                                double xMotion = ((mouseVelocity - tempMouseOffset) * timeDelta * distance + (mouseOffset * timeDelta));
                                MouseX = xMotion;
                                MouseSync = true;
                            }
                            else if (yDir)
                            {
                                double yMotion = ((mouseVelocity - tempMouseOffset) * timeDelta * distance + (mouseOffset * timeDelta));
                                MouseY = yMotion;
                                MouseSync = true;
                            }
                            //xMotion = ((mouseVelocity - tempMouseOffsetX) * timeDelta * absXNorm + (tempMouseOffsetX * timeDelta)) * xSign;
                            //yMotion = ((mouseVelocity - tempMouseOffsetY) * timeDelta * absYNorm + (tempMouseOffsetY * timeDelta)) * -ySign;
                        }
                    }

                    break;
                case OutputActionData.ActionType.GamepadControl:
                    {
                        //actionData.activatedEvent = pressed;
                        GamepadFromAxisInput(actionData, outputNorm);
                    }

                    break;
                case OutputActionData.ActionType.SwitchSet:
                case OutputActionData.ActionType.SwitchActionLayer:
                case OutputActionData.ActionType.ApplyActionLayer:
                case OutputActionData.ActionType.RemoveActionLayer:
                case OutputActionData.ActionType.HoldActionLayer:
                case OutputActionData.ActionType.CycleStep:
                    RunEventFromButton(actionData, pressed);
                    break;
                case OutputActionData.ActionType.Empty:
                    break;
                default:
                    break;
            }
        }

        public void RunEventFromButton(OutputActionData actionData, bool pressed, bool fullRelease = true)
        {
            switch (actionData.OutputType)
            {
                case OutputActionData.ActionType.Keyboard:
                    {
                        if (pressed)
                        {
                            if (!actionData.activatedEvent)
                            {
                                activeKeys.Add(actionData.OutputCodeAlias);
                                actionData.activatedEvent = true;
                            }
                        }
                        else
                        {
                            if (actionData.activatedEvent)
                            {
                                releasedKeys.Add(actionData.OutputCodeAlias);
                                actionData.activatedEvent = false;
                            }
                        }
                    }

                    break;
                case OutputActionData.ActionType.MouseButton:
                    {
                        switch (actionData.OutputCode)
                        {
                            case MouseButtonCodes.MOUSE_LEFT_BUTTON:
                            case MouseButtonCodes.MOUSE_MIDDLE_BUTTON:
                            case MouseButtonCodes.MOUSE_RIGHT_BUTTON:
                                if (pressed)
                                {
                                    if (!currentMouseButtons.Contains(actionData.OutputCode))
                                    {
                                        activeMouseButtons.Add(actionData.OutputCode);
                                        actionData.activatedEvent = true;
                                    }
                                }
                                else
                                {
                                    if (currentMouseButtons.Contains(actionData.OutputCode))
                                    {
                                        releasedMouseButtons.Add(actionData.OutputCode);
                                        actionData.activatedEvent = false;
                                    }
                                }

                                break;
                            default:
                                break;
                        }

                        break;
                    }
                case OutputActionData.ActionType.MouseWheel:
                    {
                        if (pressed && !actionData.activatedEvent)
                        {
                            int vWheel = 0; int hWheel = 0;
                            switch (actionData.OutputCode)
                            {
                                case 1: // Wheel Up
                                        //vWheel = 120;
                                    vWheel = 1;
                                    mouseWheelY = vWheel;
                                    mouseWheelSync = true;
                                    break;
                                case 2: // Wheel Down
                                        //vWheel = -120;
                                    vWheel = -1;
                                    mouseWheelY = vWheel;
                                    mouseWheelSync = true;
                                    break;
                                case 3: // Wheel Left
                                        //hWheel = 120;
                                    hWheel = 1;
                                    mouseWheelX = hWheel;
                                    mouseWheelSync = true;
                                    break;
                                case 4: // Wheel Right
                                        //hWheel = -120;
                                    hWheel = -1;
                                    mouseWheelX = hWheel;
                                    mouseWheelSync = true;
                                    break;
                                default:
                                    break;
                            }

                            //fakerInputHandler.PerformMouseWheelEvent(vWheel, hWheel);
                            //InputMethods.MouseWheel(vWheel, hWheel);
                            actionData.activatedEvent = true;
                        }
                        else if (!pressed)
                        {
                            actionData.activatedEvent = false;
                        }

                        break;
                    }
                case OutputActionData.ActionType.RelativeMouse:
                    {
                        if (pressed)
                        {
                            double distance = 0.0;
                            bool xDir = false;
                            bool yDir = false;
                            switch (actionData.mouseDir)
                            {
                                case OutputActionData.RelativeMouseDir.MouseUp:
                                    distance = -1.0;
                                    xDir = false;
                                    yDir = true;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseDown:
                                    distance = 1.0;
                                    xDir = false;
                                    yDir = true;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseLeft:
                                    distance = -1.0;
                                    xDir = true;
                                    yDir = false;
                                    break;
                                case OutputActionData.RelativeMouseDir.MouseRight:
                                    distance = 1.0;
                                    xDir = true;
                                    yDir = false;
                                    break;
                                default:
                                    break;
                            }

                            int xSpeed = actionData.extraSettings.mouseXSpeed;
                            int ySpeed = actionData.extraSettings.mouseYSpeed;

                            const int MOUSESPEEDFACTOR = 20;
                            const double MOUSE_VELOCITY_OFFSET = 0.013;
                            double timeDelta = CurrentLatency;
                            int mouseXVelocity = xSpeed * MOUSESPEEDFACTOR;
                            int mouseYVelocity = ySpeed * MOUSESPEEDFACTOR;
                            double mouseXOffset = MOUSE_VELOCITY_OFFSET * mouseXVelocity;
                            double mouseYOffset = MOUSE_VELOCITY_OFFSET * mouseYVelocity;

                            if (xDir)
                            {
                                double xMotion = ((mouseXVelocity - mouseXOffset) * timeDelta * distance + (mouseXOffset * timeDelta));
                                MouseX = xMotion;
                                MouseSync = true;
                            }

                            if (yDir)
                            {
                                double yMotion = ((mouseYVelocity - mouseYOffset) * timeDelta * distance + (mouseYOffset * timeDelta));
                                MouseY = yMotion;
                                MouseSync = true;
                            }
                            //xMotion = ((mouseVelocity - tempMouseOffsetX) * timeDelta * absXNorm + (tempMouseOffsetX * timeDelta)) * xSign;
                            //yMotion = ((mouseVelocity - tempMouseOffsetY) * timeDelta * absYNorm + (tempMouseOffsetY * timeDelta)) * -ySign;
                        }
                    }

                    break;
                case OutputActionData.ActionType.GamepadControl:
                    actionData.activatedEvent = pressed;
                    GamepadFromButtonInput(actionData, pressed);
                    break;

                case OutputActionData.ActionType.SwitchSet:
                    OutputActionData.SetChangeCondition cond = actionData.ChangeCondition;
                    actionData.activatedEvent = pressed;
                    if (pressed)
                    {
                        if (cond == OutputActionData.SetChangeCondition.Pressed)
                        {
                            queuedActionSet = actionData.ChangeToSet;
                        }
                    }
                    else
                    {
                        if (cond == OutputActionData.SetChangeCondition.Released)
                        {
                            queuedActionSet = actionData.ChangeToSet;
                        }
                    }

                    break;
                //case OutputActionData.ActionType.SwitchActionLayer:
                //    actionData.activatedEvent = pressed;
                //    if (pressed)
                //    {
                //        queuedActionLayer = actionData.ChangeToLayer;
                //    }
                //    else
                //    {
                //        // Revert to default layer
                //        queuedActionLayer = 0;
                //    }

                //    break;

                case OutputActionData.ActionType.SwitchActionLayer:
                    actionData.activatedEvent = pressed;
                    if (pressed)
                    {
                        OutputActionData.ActionLayerChangeCondition layerSwitchCond = actionData.LayerChangeCondition;
                        if (layerSwitchCond == OutputActionData.ActionLayerChangeCondition.Pressed)
                        {
                            queuedActionLayer = actionData.ChangeToLayer;
                            switchQueuedActionLayer = true;
                        }
                    }
                    else
                    {
                        OutputActionData.ActionLayerChangeCondition layerSwitchCond = actionData.LayerChangeCondition;
                        if (layerSwitchCond == OutputActionData.ActionLayerChangeCondition.Released)
                        {
                            queuedActionLayer = actionData.ChangeToLayer;
                            switchQueuedActionLayer = true;
                        }
                    }

                    break;
                case OutputActionData.ActionType.ApplyActionLayer:
                    OutputActionData.ActionLayerChangeCondition layerApplyCond = actionData.LayerChangeCondition;
                    actionData.activatedEvent = pressed;
                    //Trace.WriteLine("Change Action Layer {0}", actionData.ChangeToLayer.ToString());
                    if (pressed)
                    {
                        if (layerApplyCond == OutputActionData.ActionLayerChangeCondition.Pressed)
                        {
                            Trace.WriteLine($"Add Action Layer {actionData.ChangeToLayer}");
                            queuedActionLayer = actionData.ChangeToLayer;
                            applyQueuedActionLayer = true;
                        }
                    }
                    else
                    {
                        if (layerApplyCond == OutputActionData.ActionLayerChangeCondition.Released)
                        {
                            queuedActionLayer = actionData.ChangeToLayer;
                            applyQueuedActionLayer = true;
                        }
                    }

                    break;
                case OutputActionData.ActionType.RemoveActionLayer:
                    OutputActionData.ActionLayerChangeCondition layerRemoveCond = actionData.LayerChangeCondition;
                    actionData.activatedEvent = pressed;
                    //Trace.WriteLine("Remove Action Layer {0}", "0");
                    if (pressed)
                    {
                        if (layerRemoveCond == OutputActionData.ActionLayerChangeCondition.Pressed)
                        {
                            Trace.WriteLine("Removing Action Layer");
                            //queuedActionLayer = ActionSet.DEFAULT_ACTION_LAYER_INDEX;
                            queuedActionLayer = actionData.ChangeToLayer;
                            applyQueuedActionLayer = false;
                        }
                    }
                    else
                    {
                        if (layerRemoveCond == OutputActionData.ActionLayerChangeCondition.Released)
                        {
                            Trace.WriteLine("Removing Action Layer");
                            //queuedActionLayer = ActionSet.DEFAULT_ACTION_LAYER_INDEX;
                            //queuedActionLayer = actionProfile.CurrentActionSet.CurrentActionLayer.Index;
                            queuedActionLayer = actionData.ChangeToLayer;
                            applyQueuedActionLayer = false;
                        }
                    }

                    break;
                case OutputActionData.ActionType.HoldActionLayer:
                    //actionData.activatedEvent = pressed;
                    //Trace.WriteLine("Remove Action Layer {0}", "0");
                    if (pressed)
                    {
                        if (!actionData.activatedEvent)
                        //if (!actionData.waitForRelease)
                        {
                            Trace.WriteLine($"Hold Action Layer {actionData.ChangeToLayer}");
                            actionData.activatedEvent = true;
                            queuedActionLayer = actionData.ChangeToLayer;
                            applyQueuedActionLayer = true;
                            // Temporarily skip release step. Need to reset the flag after
                            actionData.skipRelease = true;
                            actionData.waitForRelease = true;
                        }
                    }
                    else if (!pressed)
                    {
                        //if (actionData.activatedEvent && fullRelease)
                        if (actionData.activatedEvent)
                        //if (!actionData.skipRelease && actionData.waitForRelease)
                        {
                            Trace.WriteLine($"Release Action Layer");
                            actionData.activatedEvent = false;
                            //queuedActionLayer = ActionSet.DEFAULT_ACTION_LAYER_INDEX;
                            queuedActionLayer = actionData.ChangeToLayer;
                            applyQueuedActionLayer = false;
                            actionData.waitForRelease = false;
                        }

                        // Happens on initial Release call from default ActionLayer
                        actionData.skipRelease = false;
                    }
                    break;
                case OutputActionData.ActionType.CycleStep:
                    {
                        if (pressed)
                        {
                            if (!actionData.activatedEvent)
                            {
                                ActivateCycle(actionData.cycleStepAct.cycleId, actionData.cycleStepAct);
                                actionData.activatedEvent = true;
                            }
                        }
                        else
                        {
                            actionData.activatedEvent = false;
                        }
                    }

                    break;
                default:
                    break;
            }

        }

        public bool IsButtonActive(JoypadActionCodes code)
        {
            bool result = false;
            switch (code)
            {
                case JoypadActionCodes.AlwaysOn:
                    result = true;
                    break;
                case JoypadActionCodes.BtnSouth:
                    result = currentMapperState.A;
                    break;
                case JoypadActionCodes.BtnEast:
                    result = currentMapperState.B;
                    break;
                case JoypadActionCodes.BtnNorth:
                    result = currentMapperState.Y;
                    break;
                case JoypadActionCodes.BtnWest:
                    result = currentMapperState.X;
                    break;
                case JoypadActionCodes.BtnLShoulder:
                    result = currentMapperState.LB;
                    break;
                case JoypadActionCodes.BtnRShoulder:
                    result = currentMapperState.RB;
                    break;
                case JoypadActionCodes.BtnStart:
                    result = currentMapperState.Start;
                    break;
                case JoypadActionCodes.BtnMode:
                    result = currentMapperState.Guide;
                    break;
                case JoypadActionCodes.AxisLTrigger:
                    result = currentMapperState.LT > 0;
                    break;
                case JoypadActionCodes.AxisRTrigger:
                    result = currentMapperState.RT > 0;
                    break;
                case JoypadActionCodes.LPadTouch:
                    result = currentMapperState.LeftPad.Touch;
                    break;
                case JoypadActionCodes.RPadTouch:
                    result = currentMapperState.RightPad.Touch;
                    break;
                case JoypadActionCodes.LPadClick:
                    result = currentMapperState.LeftPad.Click;
                    break;
                case JoypadActionCodes.RPadClick:
                    result = currentMapperState.RightPad.Click;
                    break;
                case JoypadActionCodes.LTFullPull:
                    result = currentMapperState.LTClick;
                    break;
                case JoypadActionCodes.RTFullPull:
                    result = currentMapperState.RTClick;
                    break;
                default:
                    break;
            }

            return result;
        }

        public bool IsButtonsActiveDraft(IEnumerable<JoypadActionCodes> codes,
            bool andEval = true)
        {
            bool result = false;
            foreach (JoypadActionCodes code in codes)
            {
                switch (code)
                {
                    case JoypadActionCodes.AlwaysOn:
                        result = true;
                        break;
                    case JoypadActionCodes.BtnSouth:
                        result = currentMapperState.A;
                        break;
                    case JoypadActionCodes.BtnEast:
                        result = currentMapperState.B;
                        break;
                    case JoypadActionCodes.BtnNorth:
                        result = currentMapperState.Y;
                        break;
                    case JoypadActionCodes.BtnWest:
                        result = currentMapperState.X;
                        break;
                    case JoypadActionCodes.BtnLShoulder:
                        result = currentMapperState.LB;
                        break;
                    case JoypadActionCodes.BtnRShoulder:
                        result = currentMapperState.RB;
                        break;
                    case JoypadActionCodes.BtnStart:
                        result = currentMapperState.Start;
                        break;
                    case JoypadActionCodes.BtnMode:
                        result = currentMapperState.Guide;
                        break;
                    case JoypadActionCodes.AxisLTrigger:
                        result = currentMapperState.LT > 0;
                        break;
                    case JoypadActionCodes.AxisRTrigger:
                        result = currentMapperState.RT > 0;
                        break;
                    case JoypadActionCodes.LPadTouch:
                        result = currentMapperState.LeftPad.Touch;
                        break;
                    case JoypadActionCodes.RPadTouch:
                        result = currentMapperState.RightPad.Touch;
                        break;
                    case JoypadActionCodes.LPadClick:
                        result = currentMapperState.LeftPad.Click;
                        break;
                    case JoypadActionCodes.RPadClick:
                        result = currentMapperState.RightPad.Click;
                        break;
                    case JoypadActionCodes.LTFullPull:
                        result = currentMapperState.LTClick;
                        break;
                    case JoypadActionCodes.RTFullPull:
                        result = currentMapperState.RTClick;
                        break;
                    default:
                        break;
                }

                if (andEval && !result)
                {
                    break;
                }
                else if (!andEval && result)
                {
                    break;
                }
            }

            return result;
        }

        public void ActivateCycle(string cycleId,
            OutputActionData.CycleStepAction stepAction)
        {
            if (actionProfile.CycleBindings.TryGetValue(cycleId, out CycleButton testCycle))
            {
                switch(stepAction.stepActionType)
                {
                    case OutputActionData.CycleStepActionType.Forward:
                        {
                            testCycle.MoveNext();
                            testCycle.Prepare(this, true);
                            testCycle.Event(this);
                        }

                        break;
                    case OutputActionData.CycleStepActionType.Backward:
                        {
                            testCycle.MovePrevious();
                            testCycle.Prepare(this, true);
                            testCycle.Event(this);
                        }

                        break;
                    case OutputActionData.CycleStepActionType.MoveToFront:
                        {
                            testCycle.ResetCycle();
                            testCycle.Prepare(this, true);
                            testCycle.Event(this);
                        }

                        break;
                    case OutputActionData.CycleStepActionType.MoveToStep:
                        {
                            testCycle.MoveToStep(stepAction.stepNum);
                            testCycle.Prepare(this, true);
                            testCycle.Event(this);
                        }

                        break;
                    case OutputActionData.CycleStepActionType.MoveToEnd:
                        {
                            testCycle.MoveToEnd();
                            testCycle.Prepare(this, true);
                            testCycle.Event(this);
                        }

                        break;
                    default:
                        break;
                }

                processCycleList.Add(testCycle);
                processCycle = true;
            }
        }

        public void Stop(bool finalSync = false)
        {
            //reader.StopUpdate();

            quit = true;

            actionProfile.CurrentActionSet.ReleaseActions(this, true);

            editActionSet = null;
            editLayer = null;

            // Relay changes to event systems
            SyncKeyboard();
            SyncMouseButtons();
            if (finalSync)
            {
                fakerInputHandler.Sync();
            }

            //outputController?.Disconnect();
            //outputController = null;

            outputControllerSCP?.Disconnect();
            outputControllerSCP = null;
            outputControlType = OutputContType.None;

            // Let BackendManager handle disconnecting from event generator
            //fakerInputHandler.Disconnect();
            //fakerInput.UpdateKeyboard(new KeyboardReport());
            //fakerInput.Disconnect();
            //fakerInput.Free();
        }
    }
}
