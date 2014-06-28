using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;

namespace Helpers.WPF
{
    public class PropertyChangedBase : System.ComponentModel.INotifyPropertyChanged
    {
        private class RaiseItem
        {
            public string PropertyName;
            public string[] WhenPropertyNames;
        }

        #region Property changed

        private List<RaiseItem> afterItems = new List<RaiseItem>();
        private List<RaiseItem> beforeItems = new List<RaiseItem>();

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                foreach (var item in beforeItems.Where(bI => bI.WhenPropertyNames.Any(wP => propertyName.Like(wP))))
                    RaisePropertyChange(item.PropertyName);

                //foreach (var item in beforeItems)
                //    if (item.WhenPropertyNames.Contains(propertyName))
                //        RaisePropertyChange(item.PropertyName);

                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));

                foreach (var item in afterItems.Where(bI => bI.WhenPropertyNames.Any(wP => propertyName.Like(wP))))
                    RaisePropertyChange(item.PropertyName);

                //foreach (var item in afterItems)
                //    if (item.WhenPropertyNames.Contains(propertyName))
                //        RaisePropertyChange(item.PropertyName);
            }
        }
        /// <summary>
        /// Manage to raise property <b>propertyName</b> before any of <b>afterPropertyNames</b> raised
        /// </summary>
        /// <param name="afterPropertyNames">Array of property to wath.<br/><i>Can be mask like "*Value"</i></param>
        /// <param name="propertyName">Property name to raise</param>
        protected void RaisePropertyAfterChange(string[] afterPropertyNames, string propertyName)
        {
            afterItems.Add(new RaiseItem() { PropertyName = propertyName, WhenPropertyNames = afterPropertyNames });
        }

        /// <summary>
        /// Manage to raise property <b>propertyName</b> after any of <b>afterPropertyNames</b> raised
        /// </summary>
        /// <param name="afterPropertyNames">Array of property to wath.<br/><i>Can be mask like "*Value"</i></param>
        /// <param name="propertyName">Property name to raise</param>
        protected void RaisePropertyBeforeChange(string[] beforePropertyNames, string propertyName)
        {
            beforeItems.Add(new RaiseItem() { PropertyName = propertyName, WhenPropertyNames = beforePropertyNames });
        }

        #endregion
    }

    public static partial class Extensions
    {
        /// <summary>
        /// Check object is Design mode now
        /// </summary>
        /// <param name="obj">Source object</param>
        /// <returns>Returns true if object is in design mode now</returns>
        public static bool IsDesignMode(this object obj)
        {
            return IsInDesignMode;
        }

        /// <summary>
        /// Returns true if current mode is design mode
        /// </summary>
        public static bool IsInDesignMode
        {
            get { return System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()); }
        }
    }
}
