using System;
namespace Models
{
    public class MessageModel
    {
        public MessageModel()
        {
        }

        public string Id { get; set; }
        public string Content { get; set; }
        public string ConversationId { get; set; }
        public string SenderId { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string ReplyToId { get; set; }
    }
}

