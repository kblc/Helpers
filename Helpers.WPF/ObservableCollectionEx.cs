using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace Helpers.WPF
{
    /// <summary>
    /// Represents a dynamic data extended collection that provides notifications when items get added, removed, or when the whole list is refreshed.
    /// Collection extended by: Protected OnCollectionChanged() event, some Range*() methods
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Occurs when an item is added, removed, changed, moved, or the entire list is refreshed.   
        /// </summary>
        public override event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Raises the System.Collections.ObjectModel.ObservableCollection<T>.CollectionChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!supressNotifications)
            // Be nice - use BlockReentrancy like MSDN said
            using (BlockReentrancy())
            {
                System.Collections.Specialized.NotifyCollectionChangedEventHandler eventHandler = CollectionChanged;
                if (eventHandler == null)
                    return;

                Delegate[] delegates = eventHandler.GetInvocationList();
                // Walk thru invocation list
                foreach (System.Collections.Specialized.NotifyCollectionChangedEventHandler handler in delegates)
                {
                    DispatcherObject dispatcherObject = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcherObject != null && dispatcherObject.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcherObject.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, e);
                    }
                    else // Execute handler as is
                        handler(this, e);
                }
            }
        }

        /// <summary>
        /// Raises the System.Collections.ObjectModel.ObservableCollection<T>.PropertyChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!supressNotifications)
                base.OnPropertyChanged(e);
        }

        private bool supressNotifications = false;

        /// <summary>
        /// Add items range to collection
        /// </summary>
        /// <param name="items">Items to add</param>
        /// <exception cref="ArgumentNullException">Items cannot be null</exception>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            this.CheckReentrancy();
            int startingIndex = this.Count;

            supressNotifications = true;
            try
            { 
                foreach (var item in items)
                    this.Items.Add(item);
            }
            finally
            {
                supressNotifications = false;
            }
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));

            var changedItems = new List<T>(items);
            this.OnCollectionChanged(
                new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                        System.Collections.Specialized.NotifyCollectionChangedAction.Add, changedItems, startingIndex
                    ));
        }
    }
}
