using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
}