using System;
namespace Models
{
    public class UserModel
    {
        public UserModel()
        {
        }
        public string Id { get; set; }
        public string Email { get; set; }
        public Profile Profile { get; set; }
        public bool IsOnline { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }
}

