using FreeChat.Services.Navigation;
using FreeChat.Views.Pages;
using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace FreeChat
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Shell.SetTabBarIsVisible(this, false);
            Routing.RegisterRoute(Constants.MessagesPageUrl, typeof(MessagesPage));
            Routing.RegisterRoute(Constants.ConversationsPageUrl, typeof(ConversationsPage));
            Routing.RegisterRoute(Constants.SignInPageUrl, typeof(SignInPage));
            Routing.RegisterRoute(Constants.SignUpPageUrl, typeof(SignUpPage));
            Routing.RegisterRoute(Constants.SettingsPageUrl, typeof(SettingsPage));
            Routing.RegisterRoute(Constants.SplashPageUrl, typeof(SettingsPage));
            Routing.RegisterRoute(Constants.SearchPageUrl, typeof(SearchPage));
            Routing.RegisterRoute(Constants.ProfilePageUrl, typeof(ProfilePage));
        }
    }
}
