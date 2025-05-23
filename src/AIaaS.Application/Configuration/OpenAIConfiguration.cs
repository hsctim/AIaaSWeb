﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.Configuration
{
    public  class OpenAiConfiguration
    {
        public bool IsEnabled { get; set; }

        public string Organization { get; set; }

        public string ApiKey { get; set; }

        public string Engine { get; set; }

        public double Temperature { get; set; }

        public int MaxTokens { get; set; }

        public string MessagesTemplate { get; set; }
    }
}
