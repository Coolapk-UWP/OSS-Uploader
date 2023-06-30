using System;

namespace CoolapkUWP.OSSUploader.Helpers
{
    public enum UriType
    {
        OOSUploadPrepare
    }

    public static class UriHelper
    {
        public static readonly Uri BaseUri = new Uri("https://api.coolapk.com");
        public static readonly Uri Base2Uri = new Uri("https://api2.coolapk.com");
        public static readonly Uri CoolapkUri = new Uri("https://www.coolapk.com");

        public static readonly Uri ITHomeUri = new Uri("https://qapi.ithome.com");
        public static readonly Uri DevUri = new Uri("https://developer.coolapk.com");
        public static readonly Uri BilibiliUri = new Uri("https://api.vc.bilibili.com");

        public const string LoginUri = "https://account.coolapk.com/auth/loginByCoolapk";

        public static Uri GetUri(UriType type, params object[] args)
        {
            string u = string.Format(GetTemplate(type), args);
            return new Uri(Base2Uri, u);
        }

        public static Uri GetOldUri(UriType type, params object[] args)
        {
            string u = string.Format(GetTemplate(type), args);
            return new Uri(BaseUri, u);
        }

        public static Uri GetLinkUri(UriType type, params object[] args)
        {
            string u = string.Format(GetTemplate(type), args);
            Uri BaseUri = BilibiliUri;
            return new Uri(BaseUri, u);
        }

        private static string GetTemplate(UriType type)
        {
            switch (type)
            {
                case UriType.OOSUploadPrepare: return "/v6/upload/ossUploadPrepare";
                default: throw new ArgumentException($"{typeof(UriType).FullName}值错误");
            }
        }
    }
}
