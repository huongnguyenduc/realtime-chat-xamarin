using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Acr.UserDialogs;
using FreeChat.Services;
using Models;
using ReactiveUI;

namespace FreeChat.ViewModels
{
    public abstract class AuthBaseViewModel : ReactiveObject, IViewModel
    {
        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }
        protected IDataStore<User> _userDataStore;
        protected IObservable<bool> _notBusyObservable;
        public IApiManager ApiManager;
        public SocketManager SocketManager;

        public AuthBaseViewModel(IDataStore<User> userDataStore, IApiManager apiManager, SocketManager socketManager)
        {
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

