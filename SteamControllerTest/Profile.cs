using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest
{
    public class Profile
    {
        protected List<ActionSet> actionSets = new List<ActionSet>(8);
        public List<ActionSet> ActionSets
        {
            get => actionSets; set => actionSets = value;
        }

        private ActionSet currentActionSet;
        public ActionSet CurrentActionSet { get => currentActionSet; }

        private ActionSet defaultActionSet;
        public ActionSet DefaultActionSet { get => defaultActionSet; }

        private int currentActionSetIndex = 0;
        public int CurrentActionSetIndex
        {
            get => currentActionSetIndex;
        }

        protected string name;
        public string Name { get => name; set => name = value; }

        protected string description;
        public string Description { get => description; set => description = value; }

        protected string creator;
        public string Creator { get => creator; set => creator = value; }

        protected DateTime creationDate;
        public DateTime CreationDate { get => creationDate; set => creationDate = value; }

        protected int leftStickRotation;
        public int LeftStickRotation { get => leftStickRotation; set => leftStickRotation = value; }

        protected int rightStickRotation;
        public int RightStickRotation { get => rightStickRotation; set => rightStickRotation = value; }

        public Profile()
        {
            // Add one empty ActionSet by default
            ActionSet actionSet = new ActionSet(0, "Set 1");
            actionSets.Add(actionSet);

            currentActionSet = actionSet;
            defaultActionSet = currentActionSet;
        }

        public void ResetAliases()
        {
            currentActionSet = null;
            currentActionSetIndex = 0;
            defaultActionSet = null;
            if (actionSets.Count > 0)
            {
                currentActionSet = actionSets[0];
                defaultActionSet = currentActionSet;

                foreach(ActionSet set in actionSets)
                {
                    set.ResetAliases();
                }
            }
        }

        public void SwitchSets(int index, Mapper mapper)
        {
            if (index >= 0 && index < actionSets.Count)
            {
                currentActionSet.ReleaseActions(mapper);
                currentActionSet = actionSets[index];
                currentActionSetIndex = index;
            }
        }
    }
}
