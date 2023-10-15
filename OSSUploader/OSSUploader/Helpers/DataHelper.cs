using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace CoolapkUWP.OSSUploader.Helpers
{
    public static partial class DataHelper
    {
        public static string GetMD5(this string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            using (MD5 md5Hasher = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                string results = BitConverter.ToString(data).ToLowerInvariant();
                return results.Replace("-", "");
            }
        }

        public static string GetBase64(this string input, bool isRaw = false)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            string result = Convert.ToBase64String(bytes);
            if (!isRaw) { result = result.Replace("=", ""); }
            return result;
        }

        public static string Reverse(this string text)
        {
            char[] charArray = text.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static async Task<IBuffer> GetBufferAsync(this IRandomAccessStream randomStream)
        {
            using (Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(randomStream.GetInputStreamAt(0)))
            {
                return await stream.GetBufferAsync();
            }
        }

        public static async Task<byte[]> GetBytesAsync(this IRandomAccessStream randomStream)
        {
            using (Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(randomStream.GetInputStreamAt(0)))
            {
                return await stream.GetBytesAsync();
            }
        }

        public static async Task<IBuffer> GetBufferAsync(this Stream stream)
        {
            byte[] bytes = stream != null ? await stream.GetBytesAsync() : Array.Empty<byte>();
            return bytes.AsBuffer();
        }

        public static async Task<byte[]> GetBytesAsync(this Stream stream)
        {
            if (stream.CanSeek) // stream.Length 已确定
            {
                byte[] bytes = new byte[stream.Length];
                _ = await stream.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                stream.Seek(0, SeekOrigin.Begin);
                return bytes;
            }
            else // stream.Length 不确定
            {
                int initialLength = 32768; // 32k

                byte[] buffer = new byte[initialLength];
                int read = 0;

                int chunk;
                while ((chunk = await stream.ReadAsync(buffer, read, buffer.Length - read).ConfigureAwait(false)) > 0)
                {
                    read += chunk;

                    if (read == buffer.Length)
                    {
                        int nextByte = stream.ReadByte();

                        if (nextByte == -1)
                        {
                            return buffer;
                        }

                        byte[] newBuffer = new byte[buffer.Length * 2];
                        Array.Copy(buffer, newBuffer, buffer.Length);
                        newBuffer[read] = (byte)nextByte;
                        buffer = newBuffer;
                        read++;
                    }
                }

                byte[] ret = new byte[read];
                Array.Copy(buffer, ret, read);
                return ret;
            }
        }

        public static Stream GetStream(this byte[] bytes)
        {
            Stream stream = new MemoryStream(bytes);
            return stream;
        }
    }
}
