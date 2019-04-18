using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

namespace TestRunHelper.Helpers
{
    public static class HttpHelper
    {
        public static bool GetFile(string url, string filePath)
        {
            try
            {
                var username = ConfigurationManager.AppSettings["login"];
                var password = ConfigurationManager.AppSettings["password"];
                var client = new WebClient {Credentials = new NetworkCredential(username, password)};
                client.DownloadFile(url, filePath);
                
                if (new FileInfo(filePath).Length == 0)
                {
                    File.Delete(filePath);
                    throw new FileNotFoundException($"File not found by url: '{url}'");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }

            return true;
        }

        public static List<string> GetFileContent(string url)
        {
            var randomName = DateTime.Now.ToString(CultureInfo.InvariantCulture).HashString();
            var successful = GetFile(url, randomName);
            var result = successful ? File.ReadLines(randomName).ToList() : new List<string>();
            if (successful) File.Delete(randomName);

            return result;
        }
    }
}