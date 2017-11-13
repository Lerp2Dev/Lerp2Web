using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Lerp2Web
{
    public static class TimeHelpers
    {
        public const ulong validMillisecondsDelay = 300;

        public static ulong UnixTimestamp
        {
            get
            {
                return (ulong) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            }
        }

        public static bool IsValid(this DateTime tim, ulong lastTimestamp)
        {
            return tim.Diff(lastTimestamp) > validMillisecondsDelay;
        }

        public static ulong Diff(this DateTime tim, ulong lastTimestamp)
        {
            return (ulong) (tim.Subtract(UnixTimeStampToDateTime(lastTimestamp))).TotalMilliseconds;
        }

        public static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string ToSQLDateTime(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public static class DictionaryHelpers
    {
        public static void Swap<T>(this Dictionary<int, T> dict, int key1, int key2)
        {
            if (key1 != key2)
            {
                T swap = dict[key1];
                dict[key1] = dict[key2];
                dict[key2] = swap;
            }
        }
    }

    public static class ConfigurationHelpers
    {
        public static bool IsEmpty(this KeyValueConfigurationCollection settings, string key)
        {
            return settings[key] == null || (settings[key] != null && string.IsNullOrEmpty(settings[key].Value));
        }
    }

    public static class IdentifierHelpers
    {
        public static string GetMachineUniqueID()
        {
            ManagementObjectCollection mbsList = null;
            ManagementObjectSearcher mbs = new ManagementObjectSearcher("SELECT * FROM Win32_processor");
            mbsList = mbs.Get();
            string id = "";
            foreach (ManagementObject mo in mbsList)
                id = mo["ProcessorID"].ToString();

            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            ManagementObjectCollection moc = mos.Get();
            string motherBoard = "";
            foreach (ManagementObject mo in moc)
                motherBoard = (string) mo["SerialNumber"];

            return id + motherBoard;
        }
    }

    public static class CryptHelpers
    {
        public static string CreateMD5(this string input)
        {
            // Use input string to calculate MD5 hash
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString().ToLower();
            }
        }

        public static bool IsValidMD5(this string md5) => md5 != null && md5.Length == 32 && md5.All(x => (x >= '0' && x <= '9') || (x >= 'a' && x <= 'f') || (x >= 'A' && x <= 'F'));
    }

    public static class CollectionHelpers
    {
        public static string BuildQueryString(this NameValueCollection nvc, bool allValues = false)
        {
            if (allValues) // Create query string with all values
                return string.Join("&", nvc.AllKeys.Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key]))));
            else // Omit empty values
                return string.Join("&", nvc.AllKeys.Where(key => !string.IsNullOrWhiteSpace(nvc[key])).Select(key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key]))));
        }

        public static void ForEach<T>(
                                        this IEnumerable<T> source,
                                        Action<T> action)
        {
            foreach (T element in source)
                action(element);
        }

        public static Queue<T> SetFirstTo<T>(this Queue<T> q, int index)
        {
            Queue<T> queue = new Queue<T>();
            queue.Enqueue(q.ElementAt(index));
            for (int i = 0; i < q.Count; ++i)
                if (i != index)
                    q.Enqueue(q.ElementAt(i));
            return queue;
        }
    }

    public static class JsonUtil
    {
        public static string JsonPrettify(this string json)
        {
            if (!json.IsValidJson()) return json;
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }

        public static bool IsValidJson(this string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static JProperty GetErrors(this JObject obj)
        {
            return obj != null ? obj.Property("error") : default(JProperty);
        }
    }

    public static class ArrayExtensions
    {
        public static void Append<T>(ref T[] array, T append)
        {
            if (array == null) array = new T[0];
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = append;    // < Adds an extra element to my array
        }
    }

    public static class UriExtensions
    {
        public static bool ValidUrl(this string Url)
        {
            Uri uriResult;
            return Uri.TryCreate(Url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static Uri GetBuildedUri(this string Url, NameValueCollection Col)
        {
            return new Uri(string.Concat(Url, "?", Col.BuildQueryString()));
        }
    }

    public static class WebExtensions
    {
        public static string DownloadString(string add)
        { //DownloadString for https
            try
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        client.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15");
                        return client.DownloadString(add);
                    }
                    catch
                    {
                        Console.WriteLine("File not found!");
                        return "";
                    }
                }
            }
            catch
            {
                Console.WriteLine("No internet connection!");
                return "";
            }
        }

        public static ulong GetFileLength(string Url)
        {
            using (WebClient client = new WebClient())
            {
                client.OpenRead(Url);
                return ulong.Parse(client.ResponseHeaders["Content-Length"]);
            }
        }
    }

    public static class IOExtensions
    {
        public static void Empty(this DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }
    }

    public static class StringExtensions
    {
        public static string BytesToString(this int byteCount)
        {
            return ((double) byteCount).BytesToString();
        }

        public static string BytesToString(this long byteCount)
        {
            return ((double) byteCount).BytesToString();
        }

        public static string BytesToString(this double byteCount)
        {
            try
            {
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (byteCount == 0)
                    return "0" + suf[0];
                double bytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + suf[place];
            }
            catch
            {
                if (double.IsInfinity(byteCount))
                    return "Inf";
                return "0";
            }
        }

        public static string GetFileStr(this string name, int len = 12)
        {
            string withoutExt = Path.GetFileNameWithoutExtension(name);
            return withoutExt.Length > len ? withoutExt.Substring(0, len) + "..." + name.Replace(withoutExt, "") : name;
        }
    }
}