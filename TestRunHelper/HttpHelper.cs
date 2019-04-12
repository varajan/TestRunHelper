using System;
using System.Configuration;
using System.IO;
using System.Net;
using TestRunHelper.Helpers;

namespace TestRunHelper
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
    }
}