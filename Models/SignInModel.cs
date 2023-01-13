using System;
namespace Models
{
    public class SignInModel
    {
        public SignInModel() { }
        public SignInModel(string email, string password)
        {
            Email = email;
            Password = password;
        }

        public string Email { get; set; }
        public string Password { get; set; }
    }
}

