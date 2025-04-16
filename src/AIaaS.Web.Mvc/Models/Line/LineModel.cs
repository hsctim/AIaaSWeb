using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIaaS.Web.Models.Line
{
    public class LineEvents
    {
        public string destination { get; set; }
        public Event[] events { get; set; }
    }

    public class Event
    {
        public string replyToken { get; set; }
        public string type { get; set; }
        public long timestamp { get; set; }
        public Source source { get; set; }
        public Message message { get; set; }
    }

    public class Source
    {
        public string type { get; set; }
        public string userId { get; set; }
    }

    public class Message
    {
        public string id { get; set; }
        public string type { get; set; }
        public string text { get; set; }
    }

    public class TextMessage
    {
        public string type { get { return "text"; } }
        public string text { get; set; }
    }

    public class ReplyMessageModel
    {
        public string replyToken { get; set; }
        public bool notificationDisabled { get; set; }
        public List<object> messages { get; set; }
    }
}
