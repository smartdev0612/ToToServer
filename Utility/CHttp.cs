using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace LSportsServer
{
    public static class CHttp
    {
        public static async Task<string> GetResponseStringAsync(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Timeout = 0x1_86a0;
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.0.5) Gecko/2008120122 Firefox/3.0.5";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                Task<string> Response;
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream streamResponse = response.GetResponseStream())
                using (StreamReader streamReader = new StreamReader(streamResponse))
                {
                    Response = streamReader.ReadToEndAsync();
                }

                return await Response;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return null;
            }
        }

        public static string GetResponseString(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Timeout = 0x1_86a0;
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.0.5) Gecko/2008120122 Firefox/3.0.5";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (Stream streamResponse = response.GetResponseStream())
                using (StreamReader streamReader = new StreamReader(streamResponse))
                {
                    string str = streamReader.ReadToEnd();
                    return str;
                }


            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return string.Empty;
            }
        }

    }
}
