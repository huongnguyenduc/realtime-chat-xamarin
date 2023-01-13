using System;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using NativeMedia;
using Refit;

namespace FreeChat.Services
{
    public interface IChatApi
    {
        [Post("/auth/signup")]
        Task<HttpResponseMessage> SignUp([Body] SignUpModel signUpModel);

        [Post("/auth/login")]
        Task<HttpResponseMessage> SignIn([Body] SignInModel signInModel);

        [Get("/profile/me")]
        Task<HttpResponseMessage> GetProfile([Header("Authorization")] string token);

        [Put("/profile")]
        Task<HttpResponseMessage> UpdateProfile([Header("Authorization")] string token, ProfileModel profile);

        [Multipart]
        [Post("/s3/upload")]
        Task<HttpResponseMessage> UploadFile([AliasAs("file")] StreamPart stream);

        [Get("/profile?keyword={keyword}&page={page}&take={take}")]
        Task<HttpResponseMessage> GetProfiles([Header("Authorization")] string token, string keyword, int page, int take);

        [Get("/conversation/me")]
        Task<HttpResponseMessage> GetConversations([Header("Authorization")] string token);

        [Post("/conversation")]
        Task<HttpResponseMessage> CreateConversation([Header("Authorization")] string token, [Body] ConversationRequest conversationRequest);

        [Get("/conversation/{conversationId}")]
        Task<HttpResponseMessage> GetConversation([Header("Authorization")] string token, string conversationId);

        [Get("/message/conversation/{conversationId}")]
        Task<HttpResponseMessage> GetMessages([Header("Authorization")] string token, string conversationId);

        [Post("/auth/logout")]
        Task<HttpResponseMessage> Logout([Header("Authorization")] string token);
    }
}

