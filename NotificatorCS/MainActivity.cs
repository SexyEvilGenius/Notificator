using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.Annotations;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.App;
using AndroidX.Core.View;
using AndroidX.Core.Widget;
using AndroidX.DrawerLayout.Widget;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using Google.Android.Material.Snackbar;
using Java.Lang;
using Java.Util;
using Java.Util.Prefs;
using static Android.Telephony.CarrierConfigManager;
using static Java.Util.Jar.Attributes;

namespace Notificator
{
    [BroadcastReceiver]
    public class NotificationReceiver : BroadcastReceiver
    {
        Android.Widget.TextView textView;
        public NotificationReceiver()
        {}
        public NotificationReceiver(Android.Widget.TextView textView)
        {
            this.textView = textView;
        }
        public override void OnReceive(Context context, Intent intent)
        {
            string notificationInfo = intent.GetStringExtra("notification_info");
            // Do something with the notification info, e.g., show it in a TextView
            textView.Text = notificationInfo;
        }
    };

    // close activity receiver
    [BroadcastReceiver]
    public class CloseActivityReceiver : BroadcastReceiver
    {
        Activity mainActivity;
        public CloseActivityReceiver()
        { }
        public CloseActivityReceiver(Activity activity)
        {
            mainActivity = activity;
        }
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action.Equals(MainActivity.ACTION_CLOSE_ACTIVITY))
            {
                // Close the activity
                mainActivity.Finish();
            }
        }
    };

    public class RadioChangedListener : Java.Lang.Object, RadioGroup.IOnCheckedChangeListener
    {          
        // array of app id for teams and telegram
        static string[] Apps = { "org.telegram.messenger", "com.microsoft.teams", "org.telegram.messenger,com.microsoft.teams" };

        public void OnCheckedChanged(RadioGroup Group, int id)
        {
            if (id < 1000) return;
            System.Collections.Generic.Dictionary<int, int> buttons = new System.Collections.Generic.Dictionary<int, int> { { Resource.Id.tgButton, 0 }, { Resource.Id.teamsButton, 1 }, { Resource.Id.bothButton, 2 } };
            // get the app id
            string appName = Apps[buttons[id]];
            // save the app id
            Xamarin.Essentials.Preferences.Set("apps", appName);
        }
    }

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        public static string ACTION_CLOSE_ACTIVITY = "com.example.yourapp.ACTION_CLOSE_ACTIVITY";

        NotificationReceiver Receiver;
        CloseActivityReceiver CloseReceiver;
        RadioGroup radioGroup;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            AndroidX.AppCompat.Widget.Toolbar toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            Android.Content.Context context = Android.App.Application.Context;

            //check ACCESS_NOTIFICATION_POLICY BIND_NOTIFICATION_LISTENER_SERVICE
            if (!IsNotificationServiceEnabled(context))
            {
                //request permission
                Intent intent = new Intent(Android.Provider.Settings.ActionNotificationListenerSettings);
                StartActivity(intent);
            }
            // add to CheckedTextView
            Android.Widget.CheckedTextView ctv = FindViewById<Android.Widget.CheckedTextView>(Resource.Id.ListOfApps);
            ctv.Checked = IsNotificationServiceEnabled(context);
            ctv.Text = "NotificationListenerService enabled: " + ctv.Checked;

            if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Android.Content.PM.Permission.Denied)
            {
                RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 0);
            }

            radioGroup = FindViewById<RadioGroup>(Resource.Id.radioGroup1);
            radioGroup.SetOnCheckedChangeListener(new RadioChangedListener());
            radioGroup.Check(2);

            CheckBox shouldNotify = FindViewById<CheckBox>(Resource.Id.ShouldNotify);
            shouldNotify.Checked = Xamarin.Essentials.Preferences.Get("shouldNotify", false);
            shouldNotify.CheckedChange += (sender, e) =>
            {
                Xamarin.Essentials.Preferences.Set("shouldNotify", e.IsChecked);
            };
        }
        private bool IsNotificationServiceEnabled(Context context)
        {
            string flat = Android.Provider.Settings.Secure.GetString(context.ContentResolver, "enabled_notification_listeners");
            if (!TextUtils.IsEmpty(flat))
            {
                string[] names = flat.Split(":");
                for (int i = 0; i < names.Length; i++)
                {
                    ComponentName cn = ComponentName.UnflattenFromString(names[i]);
                    if (cn != null)
                    {
                        if (TextUtils.Equals(context.PackageName, cn.PackageName))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == 0)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    Android.Content.Context context = Android.App.Application.Context;
                    //context.StartService(new Android.Content.Intent(context, typeof(NotificatorListenerService)));
                    Console.WriteLine("Permission.Granted!");
                }
                else
                {
                    Console.WriteLine("Permission.NotGranted!");
                }
            }
        }

        // on resume
        protected override void OnResume()
        {
            base.OnResume();
            // Register the broadcast receiver
            Receiver = new NotificationReceiver(FindViewById<Android.Widget.TextView>(Resource.Id.NotificationText));
            IntentFilter filter = new IntentFilter("com.yourapp.ALARM_TRIGGERED");
            RegisterReceiver(Receiver, filter);

            CloseReceiver = new CloseActivityReceiver(this);
            IntentFilter closeFilter = new IntentFilter(ACTION_CLOSE_ACTIVITY);
            RegisterReceiver(CloseReceiver, closeFilter);
        }
    };
}