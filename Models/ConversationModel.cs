using System;
using System.Collections.Generic;

namespace Models
{
    public class ConversationModel
    {
        public ConversationModel()
        {
        }
        public string Id { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public MessageModel LastMessage { get; set; }
        public List<UserModel> Users { get; set; }
    }
}

