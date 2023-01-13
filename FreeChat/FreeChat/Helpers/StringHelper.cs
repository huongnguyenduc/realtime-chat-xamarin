using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Models;

namespace FreeChat.Helpers
{
    public static class StringHelper
    {
        public static bool IsValidUrl(string url)
        {
            return (Uri.IsWellFormedUriString(url, UriKind.Absolute));
        }

        public static List<ImageModel> ExtractImagesFromMessage(string message)
        {
            List<ImageModel> images = new List<ImageModel>();
            //Match url = Regex.Match(message, @"(?:https?:\/\/|www\.)\S+");
            //MatchCollection matchUris = Regex.Matches(message, @"((https?|ftp|file)\://|www.)[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\(\)\*\-\._~%]*)*", RegexOptions.IgnoreCase);
            MatchCollection matchUris = Regex.Matches(message, @"((https?|ftp|file)\://|www.)[A-Za-z0-9\.\-]+(/[A-Za-z0-9\?\&\=;\+!'\*\-\._~%]*)*", RegexOptions.IgnoreCase);
            foreach (Match matchUri in matchUris)
            {
                string uri = matchUri.Value;
                string imageName = Path.GetFileName($@"{uri}");
                ImageModel image = new ImageModel { Url = uri, Name = imageName };
                Console.WriteLine("Uri ne");
                Console.WriteLine(uri);
                if (IsValidUrl(uri))
                {
                    Console.WriteLine("Uri neeeeeeee");
                    images.Add(image);
                }
            }
            return images;
        }

        public static string ExtractContentFromMessage(string message)
        {
            // Remove image Link with Markdown Format
            string content = Regex.Replace(message, @"\!\[.+\]\(((https?|ftp|file)\:\/\/|www.)[A-Za-z0-9\.\-]+(\/[A-Za-z0-9\?\&\=;\+!'\(\)\*\-\._~%]*)*\)", "");
            return content;
        }

        public static string CreateMessageWithImageLink(string message, List<ImageModel> images)
        {
            string resultMessage = message;
            foreach (ImageModel image in images)
            {
                resultMessage = $"{resultMessage}![{image.Name}]({image.Url})";
            }
            return resultMessage;
        }
    }
}

