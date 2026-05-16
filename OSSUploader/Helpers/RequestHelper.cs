using CoolapkUWP.OSSUploader.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace CoolapkUWP.OSSUploader.Helpers
{
    public static class RequestHelper
    {
        public static HttpCookieCollection GetCoolapkCookies(Uri uri)
        {
            using (HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter())
            {
                HttpCookieManager cookieManager = filter.CookieManager;
                return cookieManager.GetCookies(NetworkHelper.GetHost(uri));
            }
        }

        public static async Task<(bool isSucceed, JToken result)> PostDataAsync(Uri uri, HttpContent content = null)
        {
            string json = await NetworkHelper.PostAsync(uri, content, GetCoolapkCookies(uri)).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json)) { return (false, null); }
            JObject token;
            try { token = JObject.Parse(json); }
            catch (Exception)
            {
                return (false, null);
            }
            if (!token.TryGetValue("data", out JToken data) && token.TryGetValue("message", out JToken _))
            {
                bool _isSucceed = token.TryGetValue("error", out JToken error) && error.ToString() == "0";
                return (_isSucceed, token);
            }
            else
            {
                return data != null && !string.IsNullOrWhiteSpace(data.ToString())
                    ? (true, data)
                    : (token != null && !string.IsNullOrEmpty(token.ToString()), token);
            }
        }

        public static async Task<IEnumerable<string>> UploadImages(IEnumerable<UploadFileFragment> images, string bucket, string dir, string uid)
        {
            using (MultipartFormDataContent content = new MultipartFormDataContent())
            {
                string json = JsonConvert.SerializeObject(images, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                using (StringContent uploadBucket = new StringContent(bucket))
                using (StringContent uploadDir = new StringContent(dir))
                using (StringContent is_anonymous = new StringContent("0"))
                using (StringContent uploadFileList = new StringContent(json))
                using (StringContent toUid = new StringContent(uid))
                {
                    content.Add(uploadBucket, "uploadBucket");
                    content.Add(uploadDir, "uploadDir");
                    content.Add(is_anonymous, "is_anonymous");
                    content.Add(uploadFileList, "uploadFileList");
                    content.Add(toUid, "toUid");
                    (bool isSucceed, JToken result) = await PostDataAsync(UriHelper.GetOldUri(UriType.OOSUploadPrepare), content).ConfigureAwait(false);
                    if (isSucceed)
                    {
                        UploadPicturePrepareResult data = result.ToObject<UploadPicturePrepareResult>();
                        return await Task.WhenAll(data.FileInfo.Select(async info =>
                        {
                            UploadFileFragment image = images.FirstOrDefault((x) => x.MD5 == info.MD5);
                            if (image == null) { return null; }
                            using (Stream stream = image.Bytes.GetStream())
                            {
                                string response = await OSSUploadHelper.OssUploadAsync(data.UploadPrepareInfo, info, stream, "image/png");
                                if (!string.IsNullOrEmpty(response))
                                {
                                    try
                                    {
                                        JObject token = JObject.Parse(response);
                                        if (token.TryGetValue("data", out JToken value)
                                            && ((JObject)value).TryGetValue("url", out JToken url)
                                            && !string.IsNullOrEmpty(url.ToString()))
                                        {
                                            return url.ToString();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        return null;
                                    }
                                }
                            }
                            return null;
                        })).ContinueWith(x => x.Result.Where(y => !string.IsNullOrWhiteSpace(y))).ConfigureAwait(false);
                    }
                }
            }
            return Array.Empty<string>();
        }
    }
}
