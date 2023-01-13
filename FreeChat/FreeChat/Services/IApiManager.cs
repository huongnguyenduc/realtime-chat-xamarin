using System;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NativeMedia;
using Refit;

namespace FreeChat.Services
{
    public interface IApiManager
    {
        Task<HttpResponseMessage> SignUp(SignUpModel signUpModel);
        Task<HttpResponseMessage> SignIn(SignInModel signInModel);
        Task<HttpResponseMessage> GetProfile(string token);
        Task<HttpResponseMessage> UpdateProfile(string token, ProfileModel profile);
        Task<HttpResponseMessage> UploadFile(StreamPart stream);
        Task<HttpResponseMessage> GetProfiles(string token, string keyword, int page, int take);
        Task<HttpResponseMessage> GetConversations(string token);
        Task<HttpResponseMessage> GetConversation(string token, string conversationId);
        Task<HttpResponseMessage> CreateConversation(string token, ConversationRequest conversationRequest);
        Task<HttpResponseMessage> GetMessages(string token, string conversationId);
        Task<HttpResponseMessage> Logout(string token);
    }
}

