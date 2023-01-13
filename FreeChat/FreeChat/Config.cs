using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FreeChat
{
    public static class Config
    {
        public static string ApiUrl = "http://13.212.54.45";

        public static string ApiHostName
        {
            get
            {
                var apiHostName = Regex.Replace(ApiUrl, @"^(?:http(?:s)?://)?(?:www(?:[0-9]+)?\.)?", string.Empty, RegexOptions.IgnoreCase)
                    .Replace("/", string.Empty);
                return apiHostName;
            }
        }
    }
}

