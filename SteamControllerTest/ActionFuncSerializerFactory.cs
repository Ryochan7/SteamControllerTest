using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;

namespace SteamControllerTest
{
    public static class ActionFuncSerializerFactory
    {
        public static ActionFuncSerializer CreateSerializer(ActionFunc tempFunc)
        {
            ActionFuncSerializer serializer = null;
            switch(tempFunc)
            {
                case NormalPressFunc:
                    serializer = new NormalPressFuncSerializer(tempFunc);
                    break;
                case HoldPressFunc:
                    serializer = new HoldPressFuncSerializer(tempFunc);
                    break;
                case DoublePressFunc:
                    break;
                case DistanceFunc:
                    serializer = new DistanceFuncSerializer(tempFunc);
                    break;
                case ChordedPressFunc:
                    serializer = new ChordedPressFuncSerializer(tempFunc);
                    break;
                case AnalogFunc:
                    serializer = new AnalogFuncSerializer(tempFunc);
                    break;
                default:
                    break;
            }

            return serializer;
        }
    }
}
