using Acr.UserDialogs;
using FreeChat.Helpers;
using FreeChat.Services;
using Models;
using NativeMedia;
using Newtonsoft.Json;
using ReactiveUI;
using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FreeChat.ViewModels
{
    public class ProfileViewModel : BaseViewModel
    {
        public ProfileViewModel(IDataStore<User> userDataStore, IConversationsDataStore convDataStore,
            IMessageDataStore messageDataStore, IApiManager apiManager, SocketManager socketManager) : base(userDataStore, convDataStore, messageDataStore, apiManager, socketManager)
        {
        }

        public override async Task Initialize()
        {
            IsBusy = true;
            await LoadUserProfile();
            IsBusy = false;
        }

        public override Task Stop()
        {
            return Task.CompletedTask;
        }

        private Command _updateCommand;
        public Command UpdateCommand
        {
            get
            {
                return _updateCommand ?? (_updateCommand = new Command(async () => await RunSafe(ExecuteUpdateCommand())));
            }
        }

        private Command _pickImageCommand;
        public Command PickImageCommand
        {
            get
            {
                return _pickImageCommand ?? (_pickImageCommand = new Command(async () => await RunSafe(ExecutePickImageCommand())));
            }
        }

        public async Task ExecuteUpdateCommand()
        {
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token == "")
            {
                // Call API to update user profile
                // Back to settings if success
                await AppShell.Current.Navigation.PopModalAsync();
                return;
            }
            await AppShell.Current.Navigation.PopToRootAsync();
        }

        private User _userProfile;
        public User UserProfile
        {
            get { return _userProfile; }
            set { this.RaiseAndSetIfChanged(ref _userProfile, value); }
        }

        private string _avatar;
        public string Avatar
        {
            get { return _avatar; }
            set { this.RaiseAndSetIfChanged(ref _avatar, value); }
        }

        private IMediaFile imageFile;

        public async Task LoadUserProfile()
        {
            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token == "")
            {
                await AppShell.Current.Navigation.PopToRootAsync();
                return;
            }

            var getProfileResponse = await ApiManager.GetProfile(token);

            if (getProfileResponse.IsSuccessStatusCode)
            {
                var userResponse = await getProfileResponse.Content.ReadAsStringAsync();
                var userJson = await Task.Run(() => JsonConvert.DeserializeObject<UserModel>(userResponse));
                Avatar = userJson?.Profile?.ProfilePic ?? "john.jpg";
                UserProfile = new User(
                       userJson.Profile?.Name ?? "Unknown",
                       userJson.Profile?.Bio ?? "I like chatting online and making new friends on social media",
                       userJson?.Profile?.ProfilePic ?? "john.jpg", 5, 230,
                       userJson?.Email ?? "example@abc.com"
                       )
                { Id = userJson?.Id ?? Guid.NewGuid().ToString(), IsOnline = userJson.IsOnline };
            }


        }

        public async Task ExecutePickImageCommand()
        {
            var cts = new CancellationTokenSource();
            IMediaFile[] files = null;

            try
            {
                var request = new MediaPickRequest(1, MediaFileType.Image, MediaFileType.Video)
                {
                    PresentationSourceBounds = System.Drawing.Rectangle.Empty,
                    UseCreateChooser = true,
                    Title = "Select"
                };

                cts.CancelAfter(TimeSpan.FromMinutes(5));

                var results = await MediaGallery.PickAsync(request, cts.Token);
                files = results?.Files?.ToArray();
            }
            catch (OperationCanceledException)
            {
                // handling a cancellation request
            }
            catch (Exception)
            {
                // handling other exceptions
            }
            finally
            {
                cts.Dispose();
            }


            if (files == null)
                return;

            foreach (var file in files)
            {
                var fileName = file.NameWithoutExtension; //Can return an null or empty value
                var extension = file.Extension;
                var contentType = file.ContentType;
                var stream = await file.OpenReadAsync();
                string filePath = await FilesHelper.SaveToCacheAsync(stream, fileName);
                Console.WriteLine(filePath);
                UserProfile.ProfilePic = $"{filePath}";
                Avatar = $"{filePath}";
                imageFile = file;
                Console.WriteLine("Image");
                Console.WriteLine(extension);
                Console.WriteLine(contentType);
                //...
                //file.Dispose();
            }
        }

        private Command _updateProfileCommand;
        public Command UpdateProfileCommand
        {
            get
            {
                return _updateProfileCommand ?? (_updateProfileCommand = new Command(async () => await RunSafe(ExecuteUpdateProfileCommand())));
            }
        }

        public async Task ExecuteUpdateProfileCommand()
        {
            Console.WriteLine(UserProfile.Bio);
            Console.WriteLine(UserProfile.Name);
            Console.WriteLine(UserProfile.ProfilePic);
            Console.WriteLine(Avatar);
            Console.WriteLine(imageFile);

            string token = await SecureStorage.GetAsync(FreeChat.Helpers.Constants.AccessTokenKey);
            if (token == null || token == "")
            {
                await AppShell.Current.Navigation.PopToRootAsync();
                return;
            }

            if (imageFile != null)
            {
                var fileName = imageFile.NameWithoutExtension;
                var contentType = imageFile.ContentType;
                var stream = await imageFile.OpenReadAsync();

                var uploadFileResponse = await ApiManager.UploadFile(new StreamPart(stream, fileName, contentType));

                if (uploadFileResponse.IsSuccessStatusCode)
                {

                    var fileResponse = await uploadFileResponse.Content.ReadAsStringAsync();
                    var fileJson = await Task.Run(() => JsonConvert.DeserializeObject<FileModel>(fileResponse));

                    ProfileModel profileModel = new ProfileModel { ProfilePic = fileJson.Url, Name = UserProfile.Name, Bio = UserProfile.Bio };

                    var updateResponse = await ApiManager.UpdateProfile(token, profileModel);

                    if (updateResponse.IsSuccessStatusCode)
                    {
                        var userResponse = await updateResponse.Content.ReadAsStringAsync();
                        var userJson = await Task.Run(() => JsonConvert.DeserializeObject<UserModel>(userResponse));
                        // Save user info
                        await SecureStorage.SetAsync(Constants.UserIdKey, userJson.Id);
                        await SecureStorage.SetAsync(Constants.ProfileIdKey, userJson.Profile.Id);
                        await SecureStorage.SetAsync(Constants.UsernameKey, userJson.Profile.Name);
                        if (userJson.Profile.ProfilePic != null)
                        {
                            await SecureStorage.SetAsync(Constants.AvatarKey, userJson.Profile.ProfilePic);
                        }
                        UserDialogs.Instance.Toast("Update successfully", TimeSpan.FromSeconds(3));
                        await LoadUserProfile();
                        imageFile.Dispose();
                        imageFile = null;
                    }
                    else
                    {
                        UserDialogs.Instance.Toast("Something went wrong when update profile", TimeSpan.FromSeconds(3));
                    }
                }
                else
                {
                    UserDialogs.Instance.Toast("Something went wrong when uploading file", TimeSpan.FromSeconds(3));
                }
            }
            else
            {
                ProfileModel profileModel = new ProfileModel { ProfilePic = UserProfile.ProfilePic, Name = UserProfile.Name, Bio = UserProfile.Bio };

                var updateResponse = await ApiManager.UpdateProfile(token, profileModel);

                if (updateResponse.IsSuccessStatusCode)
                {
                    UserDialogs.Instance.Toast("Update successfully", TimeSpan.FromSeconds(3));
                    await LoadUserProfile();
                }
                else
                {
                    UserDialogs.Instance.Toast("Something went wrong when update profile", TimeSpan.FromSeconds(3));
                }

            }
        }
    }
}

