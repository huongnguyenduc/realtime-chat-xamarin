using Models.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class User : BaseModel
    {
        public string Name { get; set; }
        public string Bio { get; set; }
        public int NumberOfConversations { get; set; }
        public int NumberOfMessagesSent { get; set; }
        public string ProfilePic { get; set; }
        public bool IsOnline { get; set; }
        public string Email { get; set; }

        public User()
        {

        }

        public User(string name, string bio, string profilePic, int numberOfConversations,
            int numberOfMessagesSent, string email = "")
        {
            Name = name;
            NumberOfConversations = numberOfConversations;
            Bio = bio;
            ProfilePic = profilePic;
            NumberOfMessagesSent = numberOfMessagesSent;
            Email = email;
        }
    }
}
