using System;
using FreeChat.Services;
using Xamarin.Forms;
[assembly: Dependency(typeof(FreeChat.Droid.Service.WebViewService))]
namespace FreeChat.Droid.Service
{
    public class WebViewService : IWebViewService
    {
        public string GetContent()
        {
            return "file:///android_asset/call.html";
        }
    }
}

