using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AIaaS.Helper
{

    public class NlpDataParserHelper
    {
        public class NameValue
        {
            public NameValue(string _name, object _value)
            {
                name = _name; value = _value;
            }

            public string name { get; set; }
            public object value { get; set; }
        }

        //private const string regexString = "@{(?<json>.*)}@";
        private static Regex nlpRx = null;

        public static List<NameValue> ParserJson(string text)
        {
            nlpRx = (nlpRx == null ? new Regex(@"\$\{(?<jsonData>.*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase) : nlpRx);

            MatchCollection matches = nlpRx.Matches(text);

            List<NameValue> list = new List<NameValue>();
            if (matches.Count == 0)
            {
                list.Add(new NameValue("text", text));
                return list;
            }

            int start = 0;
            foreach (Match match in matches)
            {
                if (start != match.Index)
                    list.Add(new NameValue("text", text.Substring(start, match.Index - start)));

                start = match.Index + match.Length;

                try
                {
                    string jsonString = "{" + match.Groups["jsonData"].Value + "}";
                    list.Add(new NameValue("json", jsonString));
                }
                catch (Exception)
                {
                }
            }

            if (start <= text.Length - 1)
                list.Add(new NameValue("text", text.Substring(start, text.Length - start)));

            return list;
        }
    }
}
