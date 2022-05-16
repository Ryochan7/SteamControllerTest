using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.MapperUtil
{
    public class OutputActionData
    {
        public enum ActionType : uint
        {
            Empty,
            Keyboard,
            RelativeMouse,
            AbsoluteMouse,
            MouseButton,
            MouseWheel,
            GamepadControl,
            SwitchSet,
            SwitchActionLayer,
            ApplyActionLayer,
            RemoveActionLayer,
            HoldActionLayer,
            Wait,
            CycleStep,
        }

        public enum SetChangeCondition : uint
        {
            None,
            Pressed,
            Released,
        }

        public enum ActionLayerChangeCondition : uint
        {
            None,
            Pressed,
            Released,
        }

        private ActionType outputType;

        public struct NotchResultData
        {
            public double notches;
            public bool useAnalog;
        }

        public enum RelativeMouseDir
        {
            MouseUp,
            MouseDown,
            MouseLeft,
            MouseRight,
        }

        public enum CycleStepActionType
        {
            None,
            Forward,
            Backward,
            MoveToFront,
            MoveToEnd,
            MoveToStep,
        }

        public struct CycleStepAction
        {
            public string cycleId;
            public CycleStepActionType stepActionType;
            public int stepNum;
        }

        private int outputCode;
        private int outputCodeAlias;
        // Hold string as stored in the profile
        private string outputCodeProfileStr;

        private JoypadActionCodes joypadCode;
        private bool negative;

        private JoypadAxesCodes axisCode;
        private StickActionCodes stickCode;
        private DPadActionCodes dpadCode;
        private JoypadActionCodeMapping outputPadMap = new JoypadActionCodeMapping();
        public bool repeat = true;
        public bool activatedEvent = false;
        public bool firstRun = true;
        // Used for HoldActionLayer to skip releasing an action on ActionLayer change
        public bool skipRelease = false;
        public bool waitForRelease = false;
        private int changeToSet = -1;
        private SetChangeCondition changeCondition;
        public bool checkTick;
        public RelativeMouseDir mouseDir;
        public CycleStepAction cycleStepAct;

        public bool useNotches;
        public double currentNotches;
        public bool processOutput;

        // Flag to have process stop processing output actions in an ActionFunc sequence
        public bool breakSequence;
        private bool tickTimerActive;
        private int changeToLayer = -1;
        private ActionLayerChangeCondition layerChangeCondition;

        private Stopwatch elapsed = new Stopwatch();
        // Base line duration for an action
        private int durationMs = 100;
        public int DurationMs
        {
            get => durationMs;
            set
            {
                durationMs = value;
                effectiveDurationMs = value;
            }
        }

        // Adjustable duration to use for analog actions. Should mostly
        // be the same value as durationMs
        private int effectiveDurationMs = 100;
        public int EffectiveDurationMs
        {
            get => effectiveDurationMs;
            set => effectiveDurationMs = value;
        }

        public OutputActionDataBindSettings extraSettings = new OutputActionDataBindSettings();

        public ActionType OutputType { get => outputType; set => outputType = value; }
        public int OutputCode { get => outputCode; set => outputCode = value; }
        public int OutputCodeAlias { get => outputCodeAlias; set => outputCodeAlias = value; }
        public string OutputCodeStr { get => outputCodeProfileStr; set => outputCodeProfileStr = value; }
        public JoypadActionCodes JoypadCode { get => joypadCode; set => joypadCode = value; }
        public bool Negative { get => negative; set => negative = value; }
        public JoypadAxesCodes AxisCode { get => axisCode; set => axisCode = value; }
        public StickActionCodes StickCode { get => stickCode; set => stickCode = value; }
        public DPadActionCodes DpadCode { get => dpadCode; set => dpadCode = value; }
        public JoypadActionCodeMapping OutputPadMap
        {
            get => outputPadMap; set => outputPadMap = value;
        }

        public int ChangeToSet { get => changeToSet; set => changeToSet = value; }
        public SetChangeCondition ChangeCondition { get => changeCondition; set => changeCondition = value; }
        public bool CheckTick { get => checkTick; set => checkTick = value; }
        public Stopwatch Elapsed { get => elapsed; }
        public int ChangeToLayer { get => changeToLayer; set => changeToLayer = value; }
        public ActionLayerChangeCondition LayerChangeCondition
        {
            get => layerChangeCondition;
            set => layerChangeCondition = value;
        }

        public OutputActionData(ActionType type, int code, int codeAlias = 0)
        {
            outputType = type;
            outputCode = code;
            if (codeAlias == 0)
            {
                outputCodeAlias = code;
            }
            else
            {
                outputCodeAlias = codeAlias;
            }

            if (type == ActionType.Keyboard)
            {
                repeat = false;
            }
            else if (type == ActionType.RelativeMouse)
            {
                if (Enum.TryParse(code.ToString(), out RelativeMouseDir tempDir))
                {
                    mouseDir = tempDir;
                }
            }

            processOutput = UseProcessForAction();
        }

        public OutputActionData(ActionType type, JoypadActionCodes actionCode, bool negative = false)
        {
            outputType = type;
            if (type == ActionType.GamepadControl)
            {
                joypadCode = actionCode;
                this.negative = negative;
            }

            processOutput = UseProcessForAction();
        }

        public OutputActionData(ActionType type, JoypadAxesCodes axisCode)
        {
            outputType = type;
            if (type == ActionType.GamepadControl)
            {
                this.axisCode = axisCode;
                outputPadMap.type = JoypadActionCodeMapping.ControlType.Axis;
                outputPadMap.outputValue.axisCode = axisCode;
            }

            processOutput = UseProcessForAction();
        }

        public OutputActionData(ActionType type, StickActionCodes stickCode)
        {
            outputType = type;
            if (type == ActionType.GamepadControl)
            {
                this.stickCode = stickCode;
                outputPadMap.type = JoypadActionCodeMapping.ControlType.Stick;
                outputPadMap.outputValue.stickCode = stickCode;
            }

            processOutput = UseProcessForAction();
        }

        public OutputActionData(ActionType type, DPadActionCodes dpadCode)
        {
            outputType = type;
            if (type == ActionType.GamepadControl)
            {
                this.dpadCode = dpadCode;
                outputPadMap.type = JoypadActionCodeMapping.ControlType.DPad;
                outputPadMap.outputValue.dpadCode = dpadCode;
            }

            processOutput = UseProcessForAction();
        }

        public OutputActionData(OutputActionData secondData)
        {
            secondData.outputType = outputType;
            secondData.outputCode = outputCode;
            secondData.outputCodeAlias = outputCodeAlias;
            secondData.dpadCode = dpadCode;
            secondData.stickCode = stickCode;
            secondData.joypadCode = joypadCode;
            secondData.negative = negative;
            secondData.repeat = repeat;
            secondData.changeToSet = changeToSet;
            secondData.changeCondition = ChangeCondition;
            secondData.durationMs = durationMs;
            secondData.effectiveDurationMs = effectiveDurationMs;
            secondData.outputPadMap = new JoypadActionCodeMapping(outputPadMap);
            secondData.useNotches = useNotches;
            secondData.processOutput = processOutput;
        }

        public void Prepare(ActionType type, int code)
        {
            outputType = type;
            outputCode = code;

            if (type == ActionType.Keyboard)
            {
                repeat = false;
            }
            else if (type == ActionType.RelativeMouse)
            {
                if (Enum.TryParse(code.ToString(), out RelativeMouseDir tempDir))
                {
                    mouseDir = tempDir;
                }
            }

            processOutput = UseProcessForAction();
        }

        private bool UseProcessForAction()
        {
            bool process = false;
            switch(outputType)
            {
                case ActionType.Wait:
                    process = true;
                    break;
                default:
                    break;
            }
            return process;
        }

        public bool ProcessAction()
        {
            bool active = activatedEvent;
            switch(outputType)
            {
                case ActionType.Wait:
                    {
                        if (!tickTimerActive)
                        {
                            elapsed.Restart();
                            tickTimerActive = true;
                        }

                        if (activatedEvent)
                        {
                            breakSequence = false;
                        }
                        else if (elapsed.ElapsedMilliseconds >= effectiveDurationMs)
                        {
                            elapsed.Reset();
                            active = true;
                            activatedEvent = true;
                            breakSequence = false;
                        }
                        else
                        {
                            active = false;
                            activatedEvent = false;
                            breakSequence = true;
                        }
                    }

                    break;
                default:
                    break;
            }
            return active;
        }

        public bool ProcessTick()
        {
            bool active = activatedEvent;
            //bool result = false;
            switch(outputType)
            {
                case ActionType.MouseWheel:
                {
                    if (!tickTimerActive)
                    {
                        elapsed.Restart();
                        tickTimerActive = true;
                    }

                    if (!firstRun)
                    {
                        if (elapsed.ElapsedMilliseconds >= effectiveDurationMs)
                        {
                            elapsed.Restart();
                            active = true;
                        }
                        else
                        {
                            active = false;
                        }
                    }
                    else
                    {
                        // Process first event immediately
                        active = true;
                        effectiveDurationMs = durationMs;
                    }

                    break;
                }
                /*case ActionType.Wait:
                {
                    if (!tickTimerActive)
                    {
                        elapsed.Restart();
                        tickTimerActive = true;
                    }

                    if (elapsed.ElapsedMilliseconds >= effectiveDurationMs)
                    {
                        elapsed.Reset();
                        active = true;
                        breakSequence = false;
                    }
                    else
                    {
                        active = false;
                        breakSequence = true;
                    }

                    break;
                }
                */
                default: break;
            }

            activatedEvent = !active;
            return active;
        }

        public NotchResultData ProcessNotches()
        {
            NotchResultData result = new NotchResultData();
            switch (outputType)
            {
                case ActionType.Keyboard:
                case ActionType.MouseButton:
                case ActionType.GamepadControl:
                    result.notches = 1.0;
                    currentNotches -= 1.0;
                    break;
                case ActionType.MouseWheel:
                    result.notches = (int)currentNotches;
                    result.useAnalog = true;
                    currentNotches = currentNotches - result.notches;
                    break;
                default:
                    break;
            }

            return result;
        }

        public void ComputeActionFlags()
        {
            processOutput = UseProcessForAction();
        }

        public void Release()
        {
            if (elapsed.IsRunning)
            {
                elapsed.Reset();
            }

            tickTimerActive = false;
            activatedEvent = false;
            firstRun = true;
            breakSequence = false;
            effectiveDurationMs = durationMs;
            skipRelease = false;
            waitForRelease = false;
            currentNotches = 0.0;
        }

        public void Reset()
        {
            Release();

            outputType = ActionType.Empty;
            outputCode = 0;
            outputCodeProfileStr = "";
        }
    }
}
