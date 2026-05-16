using CoolapkUWP.OSSUploader.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using HttpClient = System.Net.Http.HttpClient;
using HttpResponseMessage = System.Net.Http.HttpResponseMessage;

namespace CoolapkUWP.OSSUploader.Helpers
{
    public static partial class NetworkHelper
    {
        public static readonly HttpClientHandler ClientHandler;
        public static readonly HttpClient Client;

        static NetworkHelper()
        {
            ClientHandler = new HttpClientHandler();
            Client = new HttpClient(ClientHandler);
            SetRequestHeaders();
        }

        public static void SetRequestHeaders()
        {
            HttpRequestHeaders headers = Client.DefaultRequestHeaders;
            headers.Add("X-Sdk-Int", "30");
            headers.Add("X-Sdk-Locale", "zh-CN");
            headers.Add("X-App-Mode", "universal");
            headers.Add("X-App-Channel", "coolapk");
            headers.Add("X-App-Id", "com.coolapk.market");
            headers.Add("X-Dark-Mode", "0");
        }

        public static void SetLoginCookie(string Uid, string UserName, string Token)
        {
            if (!string.IsNullOrEmpty(Uid) && !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Token))
            {
                using (HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter())
                {
                    HttpCookieManager cookieManager = filter.CookieManager;
                    HttpCookie uid = new HttpCookie("uid", ".coolapk.com", "/");
                    HttpCookie username = new HttpCookie("username", ".coolapk.com", "/");
                    HttpCookie token = new HttpCookie("token", ".coolapk.com", "/");
                    uid.Value = Uid;
                    username.Value = UserName;
                    token.Value = Token;
                    cookieManager.SetCookie(uid);
                    cookieManager.SetCookie(username);
                    cookieManager.SetCookie(token);
                }
            }
        }

        public static void SetRequestHeaders(string userAgent)
        {
            HttpRequestHeaders headers = Client.DefaultRequestHeaders;
            headers.UserAgent.ParseAdd(userAgent);
            APIVersion version = APIVersion.Parse(userAgent);
            headers.Add("X-App-Version", version.Version);
            headers.Add("X-Api-Supported", version.VersionCode);
            headers.Add("X-App-Code", version.VersionCode);
            headers.Add("X-Api-Version", version.MajorVersion);
        }

        public static void SetRequestHeaders(string deviceCode, string appToken)
        {
            HttpRequestHeaders headers = Client.DefaultRequestHeaders;
            headers.Add("X-App-Device", deviceCode);
            headers.Add("X-App-Token", appToken);
        }

        private static void ReplaceRequested(this HttpRequestHeaders headers, string request)
        {
            const string name = "X-Requested-With";
            _ = headers.Remove(name);
            if (request != null) { headers.Add(name, request); }
        }

        private static void ReplaceCoolapkCookie(this CookieContainer container, HttpCookieCollection cookies, Uri uri)
        {
            if (cookies == null) { return; }
            Uri host = GetHost(uri);
            foreach (HttpCookie cookie in cookies)
            {
                container.SetCookies(host, $"{cookie.Name}={cookie.Value}");
            }
        }

        private static void BeforeGetOrPost(HttpCookieCollection coolapkCookies, Uri uri, string request)
        {
            ClientHandler.CookieContainer.ReplaceCoolapkCookie(coolapkCookies, uri);
            Client.DefaultRequestHeaders.ReplaceRequested(request);
        }
    }

    public static partial class NetworkHelper
    {
        public static async Task<string> PostAsync(Uri uri, HttpContent content, HttpCookieCollection coolapkCookies)
        {
            try
            {
                BeforeGetOrPost(coolapkCookies, uri, "XMLHttpRequest");
                HttpResponseMessage response = await Client.PostAsync(uri, content).ConfigureAwait(false);
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public static partial class NetworkHelper
    {
        public static Uri GetHost(Uri uri) => new Uri("https://" + uri.Host);
    }
}
