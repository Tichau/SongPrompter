namespace SongPrompter
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            Routing.RegisterRoute("home", typeof(MainPage));
            Routing.RegisterRoute("song", typeof(SongPage));
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Force dark mode for Android 9 and lower.
            if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                Application.Current.UserAppTheme = AppTheme.Dark;
                this.RequestedThemeChanged += (s, e) => { Application.Current.UserAppTheme = AppTheme.Dark; };
            }
        }
    }
}