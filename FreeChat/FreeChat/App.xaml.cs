using System;
using Xamarin.Forms;
using FreeChat.Services;
using FreeChat.Views.Pages;
using Prism;
using Prism.Ioc;

namespace FreeChat
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            AppLocator.Initialize();
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
