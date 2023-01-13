using System;
namespace Models
{
    public class SignUpModel : SignInModel
    {
        public SignUpModel() { }
        public SignUpModel(string name, string email, string password) : base(email, password)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}

