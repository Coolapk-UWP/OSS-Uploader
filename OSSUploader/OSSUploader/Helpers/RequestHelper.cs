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

namespace CoolapkUWP.OSSUploader.Helpers
{
    public static class RequestHelper
    {
        public static async Task<(bool isSucceed, JToken result)> PostDataAsync(Uri uri, HttpContent content = null, bool isBackground = false)
        {
            string json = await NetworkHelper.PostAsync(uri, content, NetworkHelper.GetCoolapkCookies(uri), isBackground);
            if (string.IsNullOrEmpty(json)) { return (false, null); }
            JObject token;
            try { token = JObject.Parse(json); }
            catch (Exception)
            {
                return (false, null);
            }
            if (!token.TryGetValue("data", out JToken data) && token.TryGetValue("message", out JToken _))
            {
                bool _isSucceed = token.TryGetValue("error", out JToken error) && error.ToObject<int>() == 0;
                return (_isSucceed, token);
            }
            else
            {
                return data != null && !string.IsNullOrWhiteSpace(data.ToString())
                    ? (true, data)
                    : (token != null && !string.IsNullOrEmpty(token.ToString()), token);
            }
        }

        public static async Task<List<string>> UploadImages(IEnumerable<UploadFileFragment> images)
        {
            List<string> responses = new List<string>();
            using (MultipartFormDataContent content = new MultipartFormDataContent())
            {
                string json = JsonConvert.SerializeObject(images, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                using (StringContent uploadBucket = new StringContent("image"))
                using (StringContent uploadDir = new StringContent("feed"))
                using (StringContent is_anonymous = new StringContent("0"))
                using (StringContent uploadFileList = new StringContent(json))
                {
                    content.Add(uploadBucket, "uploadBucket");
                    content.Add(uploadDir, "uploadDir");
                    content.Add(is_anonymous, "is_anonymous");
                    content.Add(uploadFileList, "uploadFileList");
                    (bool isSucceed, JToken result) = await PostDataAsync(UriHelper.GetUri(UriType.OOSUploadPrepare), content);
                    if (isSucceed)
                    {
                        UploadPicturePrepareResult data = result.ToObject<UploadPicturePrepareResult>();
                        foreach (UploadFileInfo info in data.FileInfo)
                        {
                            UploadFileFragment image = images.FirstOrDefault((x) => x.MD5 == info.MD5);
                            if (image == null) { continue; }
                            using (Stream stream = image.Bytes.GetStream())
                            {
                                string response = await Task.Run(() => OSSUploadHelper.OssUpload(data.UploadPrepareInfo, info, stream, "image/png"));
                                if (!string.IsNullOrEmpty(response))
                                {
                                    try
                                    {
                                        JObject token = JObject.Parse(response);
                                        if (token.TryGetValue("data", out JToken value)
                                            && ((JObject)value).TryGetValue("url", out JToken url)
                                            && !string.IsNullOrEmpty(url.ToString()))
                                        {
                                            responses.Add(url.ToString());
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return responses;
        }
    }
}
