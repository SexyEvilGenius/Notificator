using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Views;
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
using static Android.Telephony.CarrierConfigManager;
using static Java.Util.Jar.Attributes;

namespace Notificator
{
    public class NotificationReceiver : BroadcastReceiver
    {
        Android.Widget.TextView textView;
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

    //connection 
    public class NotificatorListenerServiceConnection : Java.Lang.Object, IServiceConnection
    {
        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Console.WriteLine("Service connected!");
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Console.WriteLine("Service disconnected!");
        }
    };

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        NotificationReceiver Receiver;
        NotificatorListenerServiceConnection Connection;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            Connection = new NotificatorListenerServiceConnection();
            Android.Content.Context context = Android.App.Application.Context;
            context.BindService(new Android.Content.Intent(context, typeof(NotificatorListenerService)), Connection, Bind.AutoCreate);
            Console.WriteLine(typeof(NotificatorListenerService).FullName);

            //check ACCESS_NOTIFICATION_POLICY BIND_NOTIFICATION_LISTENER_SERVICE
            Console.WriteLine("BindNotificationListenerService: " + CheckSelfPermission(Android.Manifest.Permission.BindNotificationListenerService));
            if (CheckSelfPermission(Android.Manifest.Permission.BindNotificationListenerService) == Android.Content.PM.Permission.Denied)
            {
                if(ShouldShowRequestPermissionRationale(Android.Manifest.Permission.BindNotificationListenerService))
                {
                    ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.BindNotificationListenerService }, 0);
                    FindViewById<Android.Widget.TextView>(Resource.Id.NotificationText).Text = "cant request";
                }
                else
                {
                    //request permission
                    ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.BindNotificationListenerService }, 0);
                    Intent intent = new Intent(Android.Provider.Settings.ActionNotificationListenerSettings);
                    StartActivity(intent);
                }
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == 0)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
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
        }

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }
        Notification ForegroundNotification(string title, string message)
        {
            using (var notificationManager = NotificationManager.FromContext(ApplicationContext))
            {
                var notificationBuilder = new Notification.Builder(ApplicationContext)
                                                  .SetContentTitle(title)
                                                                .SetContentText(message)
                                                                .SetSmallIcon(Resource.Drawable.ic_menu_camera);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    NotificationChannel channel;
                    var channelName = ApplicationContext.PackageName;
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
                    notificationBuilder = notificationBuilder
                                                      .SetChannelId(channelName);
                }
                return notificationBuilder.Build();
            }
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            FindViewById<Android.Widget.TextView>(Resource.Id.NotificationText).Text = "one two three";
            //check ACCESS_NOTIFICATION_POLICY BIND_NOTIFICATION_LISTENER_SERVICE
            if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Android.Content.PM.Permission.Denied)
            {
                RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 0);
            }
            //send simplke notification
            var notificationManager = AndroidX.Core.App.NotificationManagerCompat.From(this);
            notificationManager.Notify(0, ForegroundNotification("StackOverflow", "Totally Rocks"));
            FindViewById<Android.Widget.TextView>(Resource.Id.NotificationText).Text = notificationManager.AreNotificationsEnabled() ? "Notifications enabled" : "Notifications disabled";
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            int id = item.ItemId;

            if (id == Resource.Id.nav_camera)
            {
                // Handle the camera action
            }

            else if (id == Resource.Id.nav_gallery)
            {

            }
            else if (id == Resource.Id.nav_slideshow)
            {

            }
            else if (id == Resource.Id.nav_manage)
            {

            }
            else if (id == Resource.Id.nav_share)
            {

            }
            else if (id == Resource.Id.nav_send)
            {

            }

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }
    };
}