using System;
namespace Models
{
    public class IncomeMessage
    {
        public IncomeMessage()
        {
        }

        public IncomeMessage(string userId, string content)
        {
            UserId = userId;
            Content = content;
        }

        public IncomeMessage(string userId, string content, string ReplyToId)
        {
            UserId = userId;
            Content = content;
            this.ReplyToId = ReplyToId;
        }

        public string UserId { get; set; }
        public string Content { get; set; }
        public string ReplyToId { get; set; }
        public string Id { get; set; }
    }
}

