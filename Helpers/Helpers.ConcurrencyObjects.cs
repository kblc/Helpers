using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;

namespace Helpers
{
    /// <summary>
    /// Concurency object list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrencyObjects<T>
    {
        /// <summary>
        /// Max concurency objects count inside. 0 = infinity
        /// </summary>
        public uint Max { get; set; }

        private ConcurrentDictionary<T, bool> objectsDictionary = new ConcurrentDictionary<T, bool>();

        private TimeSpan timeToGetObject = new TimeSpan(0,0,0,0,1000);
        /// <summary>
        /// Max await objects get
        /// </summary>
        public TimeSpan TimeToGetObject
        {
            get { return timeToGetObject; }
            set 
            { 
                if (timeToGetObject == value) 
                    return;
                timeToGetObject = value;
            }
        }
        /// <summary>
        /// Create instance
        /// </summary>
        public ConcurrencyObjects() { }

        private object lockGetObject = new object();
        /// <summary>
        /// Get first free object or new
        /// </summary>
        /// <returns>New or free object</returns>
        public T GetObject()
        {
            T result = default(T);

            lock(lockGetObject)
            {

                var dt = DateTime.Now;
                do
                {
                    result = objectsDictionary
                                .Where(i => i.Value == false)
                                .Select(i => i.Key)
                                .FirstOrDefault();
                    if (result == null)
                        Thread.Sleep(50);
                }
                while (objectsDictionary.Count >= Max && Max > 0 && (dt + timeToGetObject > DateTime.Now) && result == null);

                if (result == null)
                { 
                    if (Max == 0 || objectsDictionary.Count < Max)
                    {
                        result = GetNewObjectInside();
                        objectsDictionary.TryAdd(result, true);
                    }
                    else
                        throw new Exception(string.Format(@"Can't get object. Time is over."));
                } else
                    objectsDictionary.TryUpdate(result, true, false);
            }
            return result;
        }

        /// <summary>
        /// Return object to bag
        /// </summary>
        /// <param name="obj">Object to return</param>
        public void ReturnObject(T obj)
        {
            objectsDictionary.TryUpdate(obj, false, true);
        }

        /// <summary>
        /// Remove object from bag
        /// </summary>
        /// <param name="obj">Object to remove</param>
        public void RemoveObject(T obj)
        {
            bool res;
            objectsDictionary.TryRemove(obj, out res);
        }
        /// <summary>
        /// Get new object instance from function (is exists) or create it from default constructor
        /// </summary>
        /// <returns>Instance</returns>
        protected virtual T GetNewObjectInside()
        {
            if (GetNewObject != null)
                return GetNewObject();

            T result = default(T);
            var ci = typeof(T).GetConstructor(new Type[] { });
            if (ci != null)
                result = (T)ci.Invoke(new object[] { });
            return result;
        }

        /// <summary>
        /// Function to get new instance of object
        /// </summary>
        public Func<T> GetNewObject { get; set; }
    }
}
