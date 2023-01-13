using System;
namespace Models
{
    public class OutgoingMessage
    {
        public OutgoingMessage()
        {
        }

        public OutgoingMessage(string conversationId, string content, string replyToId)
        {
            this.conversationId = conversationId;
            this.content = content;
            this.replyToId = replyToId;
        }

        public OutgoingMessage(string conversationId, string content)
        {
            this.conversationId = conversationId;
            this.content = content;
        }

        public string conversationId { get; set; }
        public string content { get; set; }
        public string replyToId { get; set; }
    }
}

