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
        }
    }
}