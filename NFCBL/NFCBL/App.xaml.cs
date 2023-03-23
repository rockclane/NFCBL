using NFCBL.Services;
using NFCBL.Views;
using Plugin.BluetoothClassic.Abstractions;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NFCBL
{
    public partial class App : Application
    {
        public static IBluetoothManagedConnection CurrentBluetoothConnection { get; internal set; }
        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
