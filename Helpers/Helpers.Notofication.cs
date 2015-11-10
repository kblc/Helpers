using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helpers
{
    public class Notification
    {
        private List<string> notificationList = new List<string>();

        public void AddNotification(string notification)
        {
            lock(notificationList)
                notificationList.Add(notification);
        }

        public void AddNotifications(string[] notifications)
        {
            lock (notificationList)
                notificationList.AddRange(notifications);
        }

        public bool HasNotifications()
        {
            lock(notificationList)
                return notificationList.Count > 0;
        }
    }
}
