using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helpers
{
    /// <summary>
    /// Class to calculate percentage progress for many values
    /// </summary>
    public class PercentageProgress
    {
        /// <summary>
        /// Event arguments for percentage progress change events
        /// </summary>
        public class PercentageProgressEventArgs : EventArgs
        {
            public PercentageProgressEventArgs() { }
            public PercentageProgressEventArgs(float value)
            {
                Value = value;
            }

            /// <summary>
            /// Current percentage progress value
            /// </summary>
            public readonly float Value = 0;
        }

        public PercentageProgress() { }

        private object childLocks = new Object();
        private List<PercentageProgress> childs = new List<PercentageProgress>();

        private float value = 0;

        /// <summary>
        /// Get or set current percentage value for this part (from 0 to 100)
        /// </summary>
        public float Value
        {
            get
            {
                lock (childLocks)
                    return (childs.Count == 0) ? value : (childs.Sum(i => i.Value) / childs.Count);
            }
            set
            {
                bool needRaise = false;

                if (value > 100 || value < 0)
                    throw new ArgumentException("Значение должно быть в диапазоне от 0 до 100");

                lock (childLocks)
                    if (childs.Count == 0)
                    {
                        needRaise = this.value != value;
                        this.value = value;
                    }
                    else
                    {
                        lockRaise = true;
                        try
                        {
                            foreach (var c in childs)
                            {
                                needRaise = needRaise || (c.value != value);
                                c.value = value;
                            }
                        }
                        finally
                        {
                            lockRaise = false;
                        }
                    }

                if (needRaise)
                    RaiseChange();
            }
        }

        /// <summary>
        /// Get if current part has child
        /// </summary>
        public bool HasChilds
        {
            get { lock (childLocks) return childs.Count > 0; }
        }

        /// <summary>
        /// Get new child with default value
        /// </summary>
        /// <param name="value">Percentage value for new child</param>
        /// <returns>Child with default value for this item</returns>
        public PercentageProgress GetChild(float value = 0)
        {
            PercentageProgress result = new PercentageProgress() { Value = value };
            result.Change += child_Change;
            lock (childLocks)
            {
                childs.Add(result);
            }
            RaiseChange();
            return result;
        }

        /// <summary>
        /// Remove child
        /// </summary>
        /// <param name="child">Child to remove</param>
        public void RemoveChild(PercentageProgress child)
        {
            lock (childLocks)
                if (childs.Contains(child))
                {
                    child.Change -= child_Change;
                    childs.Remove(child);
                }
            RaiseChange();
        }

        private void child_Change(object sender, PercentageProgressEventArgs e)
        {
            RaiseChange();
        }

        private bool lockRaise = false;
        private void RaiseChange()
        {
            if (Change != null && !lockRaise)
                Change(this, new PercentageProgressEventArgs(Value));
        }

        /// <summary>
        /// Occurs when a property Value changes
        /// </summary>
        public event EventHandler<PercentageProgressEventArgs> Change;
    }

}
