using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;

namespace Helpers
{
    public class ConcurrencyObjects<T>
    {
        private int max = 0;
        public int Max
        {
            get { return max; }
            set { if (value < 0) throw new ArgumentOutOfRangeException(@"Max value can't be less 0"); max = value; }
        }

        private ConcurrentDictionary<T, bool> objectsDictionary = new ConcurrentDictionary<T, bool>();

        private TimeSpan timeToGetObject = new TimeSpan(0,0,0,0,1000);
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

        public ConcurrencyObjects()
        {

        }


        private object lockGetObject = new object();
        public T GetObject()
        {
            T result = default(T);

            lock(lockGetObject)
            {
                result = objectsDictionary
                            .Where(i => i.Value == false)
                            .Select(i => i.Key)
                            .FirstOrDefault();

                if (result == null)
                {
                    var dt = DateTime.Now;
                    while (objectsDictionary.Count >= Max && Max > 0 && (dt + timeToGetObject > DateTime.Now) && result == null)
                    { 
                        Thread.Sleep(20);
                        result = objectsDictionary
                                    .Where(i => i.Value == false)
                                    .Select(i => i.Key)
                                    .FirstOrDefault();
                    }

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
            }
            return result;
        }

        public void ReturnObject(T obj)
        {
            objectsDictionary.TryUpdate(obj, false, true);
        }

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

        public Func<T> GetNewObject = null;
    }
}
