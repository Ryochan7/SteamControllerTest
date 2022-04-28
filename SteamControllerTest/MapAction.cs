using SteamControllerTest.ActionUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest
{
    public abstract class MapAction
    {
        protected int id;
        public int Id
        {
            get => id;
            set => id = value;
        }

        protected string name;
        public string Name { get => name; set => name = value; }

        private string actionTypeName;
        public string ActionTypeName { get => actionTypeName; set => actionTypeName = value; }

        protected MapAction parentAction;
        public MapAction ParentAction { get => parentAction; set => parentAction = value; }

        protected bool hasLayeredAction;
        public bool HasLayeredAction
        {
            get => hasLayeredAction;
        }

        protected string mappingId;
        public string MappingId
        {
            get => mappingId;
            set => mappingId = value;
        }

        // Keep track of properties that have been explicitly edited in child action.
        // Allows keeping proper track when property in parentAction is changed to match child action
        // or to default
        protected HashSet<string> changedProperties = new HashSet<string>();
        public HashSet<string> ChangedProperties { get => changedProperties; }


        protected ActionFuncStateData stateData = new ActionFuncStateData();
        public ActionFuncStateData StateData
        {
            get => stateData;
            set => stateData = value;
        }

        public static ActionFuncStateData falseStateData =
            new ActionFuncStateData()
            {
                state = false,
                axisNormValue = 0.0,
            };

        public static ActionFuncStateData trueStateData =
            new ActionFuncStateData()
            {
                state = true,
                axisNormValue = 1.0,
            };

        //protected ActionFuncStateData actionStateData = new ActionFuncStateData();

        public abstract void Event(Mapper mapper);

        public abstract void Release(Mapper mapper, bool resetState = true, bool ignoreReleaseActions = false);

        public virtual void SoftRelease(Mapper mapper, MapAction checkAction,
            bool resetState = true)
        {
        }

        public static bool IsSameType(MapAction action1, MapAction action2)
        {
            return action1.GetType() == action2.GetType();
        }

        public void CopyBaseProps(MapAction secondAction)
        {
            secondAction.name = name;
            secondAction.mappingId = mappingId;
        }
    }
}
