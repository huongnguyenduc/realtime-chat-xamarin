using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Fusillade;
using Models;
using NativeMedia;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;
using Polly;
using Refit;
using Xamarin.Essentials;

namespace FreeChat.Services
{
    public class ApiManager : IApiManager
    {
        IUserDialogs _userDialogs = UserDialogs.Instance;
        IConnectivity _connectivity = CrossConnectivity.Current;
        public bool IsConnected { get; set; }
        public bool IsReachable { get; set; }
        IApiService<IChatApi> chatApi;
        Dictionary<int, CancellationTokenSource> runningTasks = new Dictionary<int, CancellationTokenSource>();
        Dictionary<string, Task<HttpResponseMessage>> taskContainer = new Dictionary<string, Task<HttpResponseMessage>>();
        private CancellationTokenSource _throttleCts = new CancellationTokenSource();

        public ApiManager(IApiService<IChatApi> _chatApi)
        {
            Debug.WriteLine(":>> hello");
            Console.WriteLine(":>> hi");
            chatApi = _chatApi;
            IsConnected = _connectivity.IsConnected;

            _connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        void OnConnectivityChanged(object sender, Plugin.Connectivity.Abstractions.ConnectivityChangedEventArgs e)
        {
            IsConnected = e.IsConnected;

            if (!e.IsConnected)
            {
                // Cancel all running tasks
                var items = runningTasks.ToList();
                foreach (var item in items)
                {
                    item.Value.Cancel();
                    runningTasks.Remove(item.Key);
                }
            }
        }

        protected async Task<TData> RemoteRequestAsync<TData>(Task<TData> task)
            where TData : HttpResponseMessage,
            new()
        {
            TData data = new TData();

            if (!IsConnected)
            {
                var stringResponse = "There's not a network connection";
                data.StatusCode = HttpStatusCode.BadRequest;
                data.Content = new StringContent(stringResponse);
                Debug.WriteLine("Cannot connect");
                _userDialogs.Toast(stringResponse, TimeSpan.FromSeconds(1));
                return data;
            }

            IsReachable = await _connectivity.IsRemoteReachable(Config.ApiHostName);

            if (!IsReachable)
            {
                var stringResponse = "Can't reach server!";
                data.StatusCode = HttpStatusCode.BadRequest;
                data.Content = new StringContent(stringResponse);
                Debug.WriteLine("Cannot reach");
                _userDialogs.Toast(stringResponse, TimeSpan.FromSeconds(1));
                return data;
            }

            data = await Policy
                .Handle<WebException>()
                .Or<ApiException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 1,
                    sleepDurationProvider: retryAttemp => TimeSpan.FromSeconds(Math.Pow(2, retryAttemp)))
                .ExecuteAsync(async () =>
                {
                    Console.WriteLine("Calling API");
                    var result = await task;

                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("Called API Failed");
                        SecureStorage.RemoveAll();
                        Console.WriteLine("dispose ne");
                        AppLocator.SocketManager.dispose();
                        Console.WriteLine("Navigate to root ne");
                        await AppShell.Current.Navigation.PopToRootAsync();
                        Console.WriteLine("Dialog ne");
                        _userDialogs.Toast("Try to log in again", TimeSpan.FromSeconds(1));
                        // Logout user
                        // Or call refresh token
                        Console.WriteLine("Called API Failed Done");
                    }

                    Console.WriteLine("Called API");
                    var fileResponse = await result.Content.ReadAsStringAsync();
                    Console.WriteLine(result);
                    Console.WriteLine(fileResponse);

                    return result;
                });

            return data;
        }

        public async Task<HttpResponseMessage> SignUp(SignUpModel signUpModel)
        {
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).SignUp(signUpModel));
            runningTasks.Add(task.Id, cts);
            return await task;
        }

        public async Task<HttpResponseMessage> SignIn(SignInModel signInModel)
        {
            Console.WriteLine("Signin");
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).SignIn(signInModel));
            runningTasks.Add(task.Id, cts);
            return await task;
        }

        public async Task<HttpResponseMessage> GetProfile(string token)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).GetProfile(tokenFull));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> UpdateProfile(string token, ProfileModel profile)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).UpdateProfile(tokenFull, profile));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> UploadFile(StreamPart stream)
        {
            //string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).UploadFile(stream));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> GetProfiles(string token, string keyword, int page, int take)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).GetProfiles(tokenFull, keyword, page, take));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> GetConversations(string token)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).GetConversations(tokenFull));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> Logout(string token)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).Logout(tokenFull));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> GetMessages(string token, string conversationId)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).GetMessages(tokenFull, conversationId));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> GetConversation(string token, string conversationId)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).GetConversation(tokenFull, conversationId));
            runningTasks.Add(task.Id, cts);

            return await task;
        }

        public async Task<HttpResponseMessage> CreateConversation(string token, ConversationRequest conversationRequest)
        {
            string tokenFull = $"Bearer {token}";
            //var cts = new CancellationTokenSource();
            var cts = _throttleCts;
            var task = RemoteRequestAsync<HttpResponseMessage>(chatApi.GetApi(Priority.UserInitiated).CreateConversation(tokenFull, conversationRequest));
            runningTasks.Add(task.Id, cts);

            return await task;
        }
    }


}

