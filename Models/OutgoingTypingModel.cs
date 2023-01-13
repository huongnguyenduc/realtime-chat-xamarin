using System;
namespace Models
{
    public class OutgoingTypingModel
    {
        public OutgoingTypingModel()
        {
        }

        public string conversationId { get; set; }
        public bool isTyping { get; set; }
    }
}

