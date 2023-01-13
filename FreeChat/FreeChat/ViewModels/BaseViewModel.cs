using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Xamarin.Forms;

using FreeChat.Models;
using FreeChat.Services;
using ReactiveUI;
using Models;
using System.Threading.Tasks;
using Acr.UserDialogs;
using System.Diagnostics;

namespace FreeChat.ViewModels
{
    public abstract class BaseViewModel : ReactiveObject, IViewModel
    {
        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }
        protected IDataStore<User> _userDataStore;
        protected IMessageDataStore _messageDataStore;
        protected IConversationsDataStore _conversationDataStore;
        protected IObservable<bool> _notBusyObservable;
        public IApiManager ApiManager;
        public SocketManager SocketManager;

        public BaseViewModel(IDataStore<User> userDataStore,
            IConversationsDataStore convDataStore, IMessageDataStore messageDataStore, IApiManager apiManager, SocketManager socketManager)
        {
            _conversationDataStore = convDataStore;
            _messageDataStore = messageDataStore;
            _userDataStore = userDataStore;
            _notBusyObservable = this.WhenAnyValue(vm => vm.IsBusy, isBusy => !isBusy);
            ApiManager = apiManager;
            SocketManager = socketManager;
        }

        public async Task RunSafe(Task task, bool showLoading = true, string loadingMessage = null)
        {
            try
            {
                if (isBusy) return;

                IsBusy = true;

                if (showLoading) UserDialogs.Instance.ShowLoading(loadingMessage ?? "Loading");

                await task;
            }
            catch (Exception e)
            {
                IsBusy = false;
                UserDialogs.Instance.HideLoading();
                Debug.WriteLine(e.ToString());
                await App.Current.MainPage.DisplayAlert("Error", "Check your internet connection", "Ok");
            }
            finally
            {
                IsBusy = false;
                if (showLoading) UserDialogs.Instance.HideLoading();
            }
        }

        public abstract Task Initialize();

        public abstract Task Stop();
    }
}
