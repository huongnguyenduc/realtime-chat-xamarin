using System;
namespace Models
{
    public class AuthModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public AuthModel(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }
    }
}

