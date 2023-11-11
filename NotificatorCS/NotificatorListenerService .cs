using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Notificator
{
    using Android.Service.Notification;
    //exported NotificatorListenerService
    [Service(Label = "NotificatorListenerService", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE", Exported = false)]
    [IntentFilter(new string[] { "android.service.notification.NotificationListenerService" })]
    public class NotificatorListenerService : NotificationListenerService
    {
        public override void OnCreate()
        {
            base.OnCreate();
            Console.WriteLine("Service created!");
        }

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            base.OnNotificationPosted(sbn);
            Console.WriteLine("Notification Posted");
            Intent intent = new Intent("com.yourapp.ALARM_TRIGGERED");
            //print title
            intent.PutExtra("notification_info", sbn.Notification.ToString());
            SendBroadcast(intent);
        }

        public override void OnNotificationRemoved(StatusBarNotification sbn)
        {
            base.OnNotificationRemoved(sbn);
            Console.WriteLine("Notification Removed");
            //get main activity
            MainActivity mainActivity = Android.App.Application.Context as MainActivity;
        }
    }
}