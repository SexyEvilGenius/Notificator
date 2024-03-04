using Android.Media;
using System;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Android;

[BroadcastReceiver]
public class AlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        if ("STOP_ALARM".Equals(intent.Action))
        {
            // Stop the alarm service
            Intent serviceIntent = new Intent(context, typeof(AlarmService));
            context.StopService(serviceIntent);
        }
    }
}

[Service(Label = "AlarmService", Exported = false)]
public class AlarmService : Service
{
    static bool isRunning = false;
    private MediaPlayer mediaPlayer;

    public override IBinder OnBind(Intent intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        if(isRunning)
        {
            return StartCommandResult.NotSticky;
        }
        // Create a notification channel for Android O and above
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel("alarm_service", "Alarm Service", NotificationImportance.Default);
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        // Intent to stop the alarm
        var stopIntent = new Intent(this, typeof(AlarmReceiver));
        stopIntent.SetAction("STOP_ALARM");
        var stopPendingIntent = PendingIntent.GetBroadcast(this, 0, stopIntent, PendingIntentFlags.Immutable);
        var deletePendingIntent = PendingIntent.GetBroadcast(this, 0, stopIntent, PendingIntentFlags.Immutable);

        // Create a notification with an action to stop the alarm
        var notification = new NotificationCompat.Builder(this, "alarm_service")
            .SetContentTitle("Alarm")
            .SetContentText("Tap to stop the alarm.")
            .SetSmallIcon(Resource.Drawable.IcLockIdleAlarm)
            .AddAction(Resource.Drawable.ButtonStar, "Stop", stopPendingIntent)
            .SetDeleteIntent(deletePendingIntent) // Set the intent that will fire when the notification is removed.
            .Build();

        StartForeground(1, notification);

        // Initialize and start the default alarm sound
        mediaPlayer = MediaPlayer.Create(this, Android.Provider.Settings.System.DefaultAlarmAlertUri);
        mediaPlayer.Looping = true;
        mediaPlayer.Start();
        isRunning = true;

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (mediaPlayer != null)
        {
            mediaPlayer.Stop();
            mediaPlayer.Release();
            isRunning = false;
        }
    }
}