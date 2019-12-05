using System.Windows;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            AppCenter.SetCountryCode(GeoRegionHelper.GetCountryCode());
            AppCenter.Start(
                "4994c871-2830-49da-9db7-77a9d53126eb",
                typeof(Analytics), typeof(Crashes));
        }
    }
}
