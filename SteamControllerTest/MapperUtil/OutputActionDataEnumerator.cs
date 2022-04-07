using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.MapperUtil
{
    public struct OutputActionDataEnumerator : IEnumerator<OutputActionData>
    {
        private List<OutputActionData> _collection;
        private OutputActionData currentOutputAction;
        private int currentIndex;

        public OutputActionDataEnumerator(List<OutputActionData> collection)
        {
            _collection = collection;
            currentIndex = -1;
            currentOutputAction = null;
        }

        public OutputActionData Current => currentOutputAction;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            //Avoids going beyond the end of the collection.
            if (++currentIndex >= _collection.Count)
            {
                return false;
            }
            else
            {
                // Set current box to next item in collection.
                currentOutputAction = _collection[currentIndex];
            }
            return true;
        }

        public bool MovePrevious()
        {
            //Avoids going beyond the end of the collection.
            if (--currentIndex < 0)
            {
                return false;
            }
            else
            {
                // Set current box to next item in collection.
                currentOutputAction = _collection[currentIndex];
            }
            return true;
        }

        public void MoveToEnd()
        {
            if (_collection.Count > 0)
            {
                currentIndex = _collection.Count;
                currentOutputAction = null;
            }
            else
            {
                Reset();
            }
        }

        public void Reset()
        {
            currentIndex = -1;
            currentOutputAction = null;
        }
    }
}
