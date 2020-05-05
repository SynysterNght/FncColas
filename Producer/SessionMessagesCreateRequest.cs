using System;
using System.Collections.Generic;
using System.Text;

namespace Producer
{
    class SessionMessagesCreateRequest
    {
        public string SessionId { get; set; }
        public int MessageId { get; set; }
    }
}
