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
    using AndroidX.Core.App;
    using System.Runtime.Remoting.Contexts;
    
    public class NotificatorConstants
    {
        public const string NOTIFICATION_REMOVED_INTENT_STRING = "com.ScreamingArmadillo.Notificator.NOTIFICATION_REMOVED";
        public const int SERVICE_NOTIFICATION_ID = 1337;
    }

    //exported NotificatorListenerService
    [Service(Label = "NotificatorListenerService", Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE", Exported = false)]
    [IntentFilter(new string[] { "android.service.notification.NotificationListenerService" })]
    public class NotificatorListenerService : NotificationListenerService
    {
        public override IBinder OnBind(Intent intent)
        {
            return base.OnBind(intent);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Intent notificationIntent = new Intent(this, typeof(MainActivity));

            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0,
                notificationIntent, PendingIntentFlags.Immutable);

            NotificationCompat.Builder notificationBuilder = new NotificationCompat.Builder(this)
                        .SetSmallIcon(Resource.Mipmap.ic_launcher_foreground)
                        .SetContentTitle("Notification Listener Service")
                        .SetContentText("Service is active")
                        .SetCategory(NotificationCompat.CategoryService)
                        .SetPriority((int)NotificationPriority.Min)
                        .SetContentIntent(pendingIntent)
                        .SetNotificationSilent();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationManager notificationManager = NotificationManager.FromContext(ApplicationContext);

                NotificationChannel channel;
                var channelName = ApplicationContext.PackageName+"SeviceChannel";
                channel = notificationManager.GetNotificationChannel(channelName);
                if (channel == null)
                {
                    channel = new NotificationChannel(channelName, channelName, NotificationImportance.Default)
                    {
                        LockscreenVisibility = NotificationVisibility.Public
                    };
                    notificationManager.CreateNotificationChannel(channel);
                }
                channel.Dispose();

                notificationBuilder.SetChannelId(channelName);
            }
            StartForeground(NotificatorConstants.SERVICE_NOTIFICATION_ID, notificationBuilder.Build());
        }

        // notification ids that were already triggered
        private List<int> triggeredNotifications = new List<int>();
        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            base.OnNotificationPosted(sbn);
            // check if should notify
            if (!Xamarin.Essentials.Preferences.Get("shouldNotify", false))
            {
                return;
            }
            if (sbn.PackageName == Android.App.Application.Context.PackageName && sbn.Id == NotificatorConstants.SERVICE_NOTIFICATION_ID)
            {
                return;
            }
            string[] apps = Xamarin.Essentials.Preferences.Get("apps", "").Split(',');
            // check if it is microsoft teams notification
            if (!apps.Contains(sbn.PackageName))
            {
                    return;
            }
            Console.WriteLine("Notification Posted");
            Intent intent = new Intent("com.yourapp.ALARM_TRIGGERED");
            //print title
            intent.PutExtra("notification_info", sbn.Notification.ToString());
            SendBroadcast(intent);
            triggeredNotifications.Add(sbn.Id);
            // create NotifierActivity
            Android.App.Application.Context.StartService(new Android.Content.Intent(Android.App.Application.Context, typeof(AlarmService)));
        }

        public override void OnNotificationRemoved(StatusBarNotification sbn)
        {
            base.OnNotificationRemoved(sbn);
            if(triggeredNotifications.Contains(sbn.Id))
            {
                triggeredNotifications.Remove(sbn.Id);
                if(triggeredNotifications.Count == 0)
                {
                    StopService(new Intent(this, typeof(AlarmService)));
                }
            }
            Console.WriteLine("Notification Removed");
        }

        public override void OnDestroy()
        {
            // close main activity
            Intent intent = new Intent(MainActivity.ACTION_CLOSE_ACTIVITY);
            SendBroadcast(intent);

            StopForeground(StopForegroundFlags.Remove | StopForegroundFlags.Detach);
            StopService(new Intent(this, typeof(NotificatorListenerService)));
            base.OnDestroy();
        }

        public override void OnRebind(Intent intent)
        {
            base.OnRebind(intent);
        }

        public override void OnListenerConnected()
        {
            base.OnListenerConnected();
        }
    }
}