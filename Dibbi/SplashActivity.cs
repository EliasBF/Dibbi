using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;

namespace Dibbi
{
    [
        Activity(Label = "Dibbi",
        MainLauncher = true,
        NoHistory = true,
        Theme = "@style/DibbiTheme.Splash")
    ]
    public class SplashActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected async override void OnResume()
        {
            base.OnResume();

            await Task.Delay(1000);
            StartActivity(new Intent(this, typeof(MainActivity)));
        }

        public override void OnBackPressed()
        {
            // Previene la cancelación del proceso inicial (splash) a través de "back button".
        }
    }
}