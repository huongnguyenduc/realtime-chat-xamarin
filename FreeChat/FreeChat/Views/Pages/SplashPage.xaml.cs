using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FreeChat.Views.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SplashPage : BasePage
    {
        public SplashPage()
        {
            InitializeComponent();
            // Todo: Try to call GET profile
            // --> OK: Navigate to ConversationsPage
            // |-> Fail: Redirect to Login page
            // Can wait for 2~3 secs to get all things ready

        }
    }
}

