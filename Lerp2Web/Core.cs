using Lerp2Web.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Lerp2Web
{
    #region "Lerp2Web"

    #region "Ajax Requests"

    public enum PHPSuccess
    {
        Register,
        Login,
        Logout,
        GetProfile,
        GetTags,
        Getreee,
        GetAppId,
        RegenAuth,
        RememberAuth,
        CreateAuth,
        RegisterEntity,
        StartAppSession,
        EndAppSession
    }

    public enum PHPRequestMethod
    {
        GET,
        POST
    }

    public class Lerp2Web
    {
        //Esto va para la otra API Lerp2Web

        public static Lerp2Web instance;
        private static DateTime StartTime;

        internal const bool outputWebRequests = true;

        private static string _apiServerUrl;

        public static string APIServer //= "http://localhost/lerp2php"
        {
            get
            {
                if (!string.IsNullOrEmpty(_apiServerUrl))
                    return _apiServerUrl;
                string localhost = "localhost/lerp2php",
                       lerp2dev = "lerp2dev.com/misc/Lerp2PHP",
                       concatUrl = "http://{0}",
                       localhostUrl = string.Format(concatUrl, localhost),
                       concatCheck = string.Concat(localhostUrl, "Check.php");
                try
                {
                    var request = WebRequest.Create(concatCheck);
                    using (var response = request.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            // Process the stream
                            using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                            {
                                if (reader.ReadToEnd() == "OK")
                                    _apiServerUrl = localhostUrl;
                                else
                                    _apiServerUrl = lerp2dev;
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine("Something unexpected ocurred when tried to get APIServer url. Message:\n\n{0}", ex.ToString());
                    _apiServerUrl = localhostUrl;
                }
                return _apiServerUrl;
            }
        }

        public const int MIN_USERNAME_LENGTH = 4,
                         MAX_USERNAME_LENGTH = 16,
                         MIN_PASSWORD_LENGTH = 8,
                         MAX_PASSWORD_LENGTH = 31;

        internal static ActionResponse responses = new ActionResponse();

        public static string EntitySha = "",
                             TokenSha = "";

        public bool Notifications;
        private bool _off;

        public string appPrefix,
                      sessionSha;

        public int appId;

        internal delegate void ModifyFormTextCallback(string text);

        public bool OfflineMode
        {
            get
            {
                return _off;
            }
            set
            {
                //Si Notification es true, entonces mostrar un mensaje en la toolbar del menu o un alert diciendo que ha si activado o desactivado el offlinemode (ademas de cambiar el titulo)
                _off = value;
                if (Form.ActiveForm != null)
                {
                    if (_off)
                        SafeModifyActiveFormText(string.Format("[Offline Mode] {0}", Form.ActiveForm.Text));
                    else
                        SafeModifyActiveFormText(Form.ActiveForm.Text.Replace("[Offline Mode] ", ""));
                }
                //Mostrar alerta
            }
        }

        public static int AuthId
        {
            get
            {
                return responses["createAuth"]["id"].ToObject<int>();
            }
        }

        public string strId
        {
            get
            {
                return appId.ToString();
            }
        }

        public static string Username
        {
            get
            {
                try
                {
                    return responses["createAuth"]["data"]["user_data"]["username"].ToObject<string>();
                }
                catch
                {
                    return "Invitado";
                }
            }
        }

        public static string AuthTime
        {
            get
            {
                try
                {
                    return responses["createAuth"]["data"]["creation_date"].ToObject<string>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There was a problem getting the authtime: {0}", ex.ToString());
                    return "";
                }
            }
        }

        public static int UserId
        {
            get
            {
                return responses["createAuth"]["data"]["user_data"]["id"].ToObject<int>();
            }
        }

        public Lerp2Web(string appPrefix)
        {
            //Singleton
            instance = this;

            StartTime = DateTime.Now;
            this.appPrefix = appPrefix;

            //Solicitar el appId
            JObject obj = JGet(new ActionRequest("getAppId")
                {
                    { "prefix", appPrefix }
                });

            this.appId = obj["data"].ToObject<int>();

            //And load everything...
            Load();
        }

        internal void Load()
        {
            //Console.WriteLine(new ActionRequest("") { { } });
            if (RegisterEntity() == null)
                Console.WriteLine("Couldn't create a new entity!");
        }

        public static JObject JGet(NameValueCollection col, string url = "", bool hideRes = false, bool thrExc = false)
        {
            JObject res = JsonConvert.DeserializeObject<JObject>(Get(col, url, hideRes, thrExc));
            RequestMade(col, res);
            return res;
        }

        public static T JGet<T>(NameValueCollection col, string url = "", bool hideRes = false, bool thrExc = false) where T : JObject
        {
            T res = JsonConvert.DeserializeObject<T>(Get(col, url, hideRes, thrExc));
            RequestMade(col, res);
            return res;
        }

        public static string Get(NameValueCollection col, string url = "", bool hideRes = false, bool thrExc = false)
        {
            Lerp2WebDebug lerpedDebug = new Lerp2WebDebug(PHPRequestMethod.GET, col);
            string _u = string.IsNullOrEmpty(url) ? string.Format("{0}/AppAjax.php", APIServer) : url;
            try
            {
                using (WebClient client = new WebClient())
                {
                    if (col != null) client.QueryString = col;
                    string res = client.DownloadString(_u);
                    lerpedDebug.DebugResponse(hideRes ? "HideRes is true" : res, _u);
                    return res;
                }
            }
            catch (Exception ex)
            {
                instance.OfflineMode = true;
                lerpedDebug.DebugException(_u, ex);
                if (thrExc)
                    throw new Exception(ex.ToString());
                return "";
            }
        }

        public static JObject JPost(NameValueCollection col, string url = "", bool hideRes = false, bool thrExc = false)
        {
            JObject res = JsonConvert.DeserializeObject<JObject>(Post(col, url, hideRes, thrExc));
            RequestMade(col, res);
            return res;
        }

        public static T JPost<T>(NameValueCollection col, string url = "", bool hideRes = false, bool thrExc = false) where T : JObject
        {
            T res = JsonConvert.DeserializeObject<T>(Post(col, url, hideRes, thrExc));
            RequestMade(col, res);
            return res;
        }

        public static string Post(NameValueCollection col, string url = "", bool hideRes = false, bool thrExc = false)
        {
            Lerp2WebDebug lerpedDebug = new Lerp2WebDebug(PHPRequestMethod.POST, col);
            string _u = string.IsNullOrEmpty(url) ? string.Format("{0}/AppAjax.php", APIServer) : url;
            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] val = client.UploadValues(_u, col);
                    string res = Encoding.Default.GetString(val);
                    lerpedDebug.DebugResponse(hideRes ? "HideRes is true" : res, _u);
                    return res;
                }
            }
            catch (Exception ex)
            {
                instance.OfflineMode = true;
                lerpedDebug.DebugException(_u, ex);
                if (thrExc)
                    throw new Exception(ex.ToString());
                return "";
            }
        }

        internal static void RequestMade<T>(NameValueCollection col, T obj) where T : JObject
        {
            if (responses != null && col != null && col["action"] != null)
                responses[col["action"]] = obj;
        }

        public string StartSession()
        {
            if (!OfflineMode)
            {
                //Once we know we already loaded OfflineSession from settings, we can search if we have stored Offline Session and send them because we have now Internet
                if (OfflineSession.HasStoredSessions()) //This needs that the config loads before it, if not this will throw and excepcion... (Maybe not)
                    OfflineSession.SendStoredSessions();

                //Later, we create a new session
                JObject obj = null;

                try
                {
                    obj = JPost(new EActionRequest("startAppSession", true)
                        {
                            { "app_id", strId }
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There was a problem starting the session! Message:\n\n{0}", ex.ToString());
                    return "null";
                }

                //Retrieve errors...
                JProperty errors = obj.GetErrors();
                if (obj != null && errors == null || (errors != null && !errors.HasValues))
                    return obj["data"]["sha"].ToObject<string>();
            }
            else
                OfflineSession.StartSession(GetNewSessionSha());
            return "null";
        }

        public bool EndSession(string sessionSha)
        {
            if (!OfflineMode)
            {
                JObject obj = JPost(new EActionRequest("endAppSession")
                        {
                            { "sha", sessionSha }
                        });
                JProperty errors = obj.GetErrors();
                return errors == null || (errors != null && !errors.HasValues);
            }
            else
            { //Puede que se llame lo de la offline session...
                try
                {
                    //Aquí tengo que diferenciar si se empezó o no con Sha
                    if (string.IsNullOrEmpty(sessionSha)) //Si no hay Sha, pues creamos un nuevo registro sin fecha de entrada (puesto que esta está en internet)
                        OfflineSession.AddUnstartedSession(GetNewSessionSha());
                    else //Si hay Sha, entonces añadimos la session con la Sha que teniamos guardada...
                        OfflineSession.AddSession(sessionSha, StartTime);
                    Settings.Default.EndSessionConfig = OfflineSession.ToString();
                }
                catch
                {
                    Console.WriteLine("There was a problem saving offline session!");
                }
                return false;
            }
        }

        internal JObject RegisterEntity()
        {
            EntitySha = IdentifierHelpers.GetMachineUniqueID().CreateMD5();
            if (!OfflineMode)
                try
                {
                    return JPost(new EActionRequest("registerEntity"));
                }
                catch
                {
                    return null;
                }
            return null;
        }

        public int CreateAuth(string username, string md5password)
        {
            if (!OfflineMode)
            {
                try
                {
                    JObject obj = JPost(new EActionRequest("createAuth", true)
                    {
                        { "username", username },
                        { "password", md5password.IsValidMD5() ? md5password : md5password.CreateMD5() }
                    });
                    try
                    {
                        TokenSha = obj["data"]["token_sha"].ToObject<string>();
                        if (obj != null && obj["data"] != null && obj["data"]["id"] != null)
                            return obj["data"]["id"].ToObject<int>();
                        return -1;
                    }
                    catch
                    {
                        return -1;
                    }
                }
                catch
                {
                    return -1;
                }
            }
            else
                return -1;
        }

        public bool ValidatingAuth(Action timeoutAct)
        {
            Console.WriteLine("Validating auth!");
            try
            {
                JObject obj = JPost(new ETActionRequest("rememberAuth", true)
                {
                    { "creation_date", AuthTime },
                    { "remember", API.RememberingAuth.ToString() }
                });
                JTokenType type = obj["data"].Type;
                if (type == JTokenType.Boolean)
                    Console.WriteLine("Auth is active yet.");
                else if (type == JTokenType.String)
                {
                    timeoutAct();
                    MessageBox.Show("Session timed out!");
                }
                else
                    TokenSha = obj["data"]["token_sha"].ToObject<string>();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem checking if the auth is valid! Message:\n\n{0}", ex.ToString());
                OfflineMode = true;
                return false;
            }
        }

        internal static void SafeModifyActiveFormText(string text)
        {
            if (Form.ActiveForm.InvokeRequired)
            {
                ModifyFormTextCallback d = new ModifyFormTextCallback(SafeModifyActiveFormText);
                Form.ActiveForm.Invoke(d, new object[] { text });
            }
            else
            {
                Form.ActiveForm.Text = text;
            }
        }

        public static string GetNewSessionSha()
        {
            return string.Concat(IdentifierHelpers.GetMachineUniqueID(), DateTime.Now.ToSQLDateTime()).CreateMD5();
        }
    }

    public class Lerp2WebDebug
    {
        protected PHPRequestMethod Method;

        //protected string Response;
        protected string Query;

        protected bool Backtrace;

        public Lerp2WebDebug(PHPRequestMethod method, NameValueCollection col, bool trace = false)
        {
            Method = method;
            Query = col != null ? col.BuildQueryString() : "NULL QueryString passed";
            Backtrace = trace;
        }

        public void DebugResponse(string res, string url)
        {
            Debug(res, url);
        }

        public void DebugException(string url, Exception ex)
        {
            Debug("", url, ex);
        }

        private void Debug(string res, string url, Exception ex = null)
        {
            if (!Lerp2Web.outputWebRequests) return;
            Console.WriteLine(string.Format(
                "\n------------\n\n[{0}] Request made at {1} has returned the following:\n\n{2}\n\nQuery string: {3}\n\nUrl: {4}{5}{6}\n\n------------\n",
                Method,
                DateTime.Now.ToSQLDateTime(),
                !string.IsNullOrEmpty(res) ? res.JsonPrettify() : "",
                Query,
                url,
                ex != null ? string.Format("\n\nException:\n\n{0}", ex.ToString()) : "",
                Backtrace ? string.Format("\n\nTrace:\n\n{0}", new StackTrace().ToString()) : ""));
        }
    }

    public class OfflineSession
    {
        internal static int lastId = 0;
        internal static List<OfflineSession> offlineSessions = new List<OfflineSession>();

        public int Id;
        public string Sha;

        public DateTime StartDate,
                        EndDate;

        internal OfflineSession(string sh, DateTime s, DateTime e)
        {
            Id = lastId;
            Sha = sh;
            StartDate = s;
            EndDate = e;
            ++lastId;
        }

        public OfflineSession(string sh, DateTime s)
            : this(sh, s, default(DateTime))
        { }

        public static void Load(string str)
        {
            offlineSessions = ToObject(str);
        }

        public static void StartSession(string sha)
        {
            StartSession(sha, DateTime.Now);
        }

        public static void AddSession(string sha, DateTime start)
        {
            AddSession(sha, start, DateTime.Now);
        }

        public static void AddUnstartedSession(string sha)
        {
            AddUnstartedSession(sha, DateTime.Now);
        }

        public static void StartSession(string sha, DateTime start = default(DateTime))
        {
            offlineSessions.Add(new OfflineSession(sha, start == default(DateTime) ? DateTime.Now : start));
        }

        public static void AddSession(string sha, DateTime start, DateTime end = default(DateTime))
        {
            offlineSessions.Add(new OfflineSession(sha, start, end == default(DateTime) ? DateTime.Now : end));
        }

        public static void AddUnstartedSession(string sha, DateTime end = default(DateTime))
        {
            offlineSessions.Add(new OfflineSession(sha, default(DateTime), end == default(DateTime) ? DateTime.Now : end));
        }

        private static void EndLastSession(bool forceNew = true)
        {
            EndLastSession(DateTime.Now, forceNew);
        }

        private static void EndLastSession(DateTime end, bool forceNew = true)
        {
            OfflineSession obj = offlineSessions.SingleOrDefault(x => x.Id == lastId);
            if (obj == null && forceNew)
            {
                obj = new OfflineSession(obj.Sha, default(DateTime), end);
                offlineSessions.Add(obj);
            }
            else if (obj != null && obj.EndDate == default(DateTime))
                obj.EndDate = end;
        }

        //Send the actual stored sessions
        public static bool SendStoredSessions()
        {
            //Finish last session if needed
            EndLastSession(false);
            try
            {
                if (offlineSessions.Count > 0)
                    foreach (OfflineSession off in offlineSessions)
                    {
                        if (off.StartDate == null && off.EndDate != null)
                        { //Este es para el caso de que empiezes con internet y te quedes sin internet
                            Lerp2Web.Post(new EActionRequest("endStartedSession")
                            {
                                { "sha", off.Sha },
                                { "end_time", off.EndDate.ToSQLDateTime() }
                            });
                        }
                        else if (off.StartDate != null && off.EndDate != null)
                        { //Este es para el caso de una sesion que nunca ha tenido internet
                            Lerp2Web.Post(new EActionRequest("recordNewSession")
                            {
                                { "sha", off.Sha },
                                { "app_id", Lerp2Web.instance.strId },
                                { "start_time", off.StartDate.ToSQLDateTime() },
                                { "end_time", off.EndDate.ToSQLDateTime() }
                            });
                        }
                    }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static bool HasStoredSessions()
        {
            return offlineSessions.Count > 0;
        }

        public static string ToString()
        {
            return JsonConvert.SerializeObject(offlineSessions.ToArray());
        }

        public static List<OfflineSession> ToObject(string str)
        {
            return JsonConvert.DeserializeObject<OfflineSession[]>(str).ToList();
        }
    }

    public class ActionRequest : IEnumerable
    {
        public string Action;
        public bool Detailed;

        protected readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

        public string this[string key]
        {
            get { return _dict[key]; }
            set { _dict.Add(key, value); }
        }

        public ActionRequest(string act, bool det = false)
        {
            Action = act;
            Detailed = det;
        }

        public void Add(string key, string val)
        {
            _dict.Add(key, val);
        }

        public IEnumerator GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public static implicit operator NameValueCollection(ActionRequest req)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("action", req.Action);
            if (req._dict.Count > 0)
                foreach (KeyValuePair<string, string> kv in req._dict)
                    col.Add(kv.Key, kv.Value);
            if (req.Detailed) col.Add("detailed", "");
            return col;
        }
    }

    public class EActionRequest : ActionRequest
    {
        public string EntityKey;

        public EActionRequest(string act, bool det = false) : this(act, Lerp2Web.EntitySha, det)
        {
        }

        public EActionRequest(string act, string entKey, bool det = false) : base(act, det)
        {
            EntityKey = entKey;
        }

        public static implicit operator NameValueCollection(EActionRequest req)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("action", req.Action);
            col.Add("ek", req.EntityKey);
            if (req._dict.Count > 0)
                foreach (KeyValuePair<string, string> kv in req._dict)
                    col.Add(kv.Key, kv.Value);
            if (req.Detailed) col.Add("detailed", "");
            return col;
        }
    }

    public class TActionRequest : ActionRequest
    {
        public string TokenKey;

        public TActionRequest(string act, string tokenKey, bool det = false) : base(act, det)
        {
            TokenKey = tokenKey;
        }

        public static implicit operator NameValueCollection(TActionRequest req)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("action", req.Action);
            col.Add("tk", req.TokenKey);
            if (req._dict.Count > 0)
                foreach (KeyValuePair<string, string> kv in req._dict)
                    col.Add(kv.Key, kv.Value);
            if (req.Detailed) col.Add("detailed", "");
            return col;
        }
    }

    public class ETActionRequest : EActionRequest
    {
        public string TokenKey;

        public ETActionRequest(string act, bool det = false) : this(act, Lerp2Web.EntitySha, Lerp2Web.TokenSha, det)
        {
        }

        public ETActionRequest(string act, string entKey, string tokenKey, bool det = false) : base(act, entKey, det)
        {
            //Console.WriteLine("Token: " + tokenKey);
            TokenKey = tokenKey;
        }

        public static implicit operator NameValueCollection(ETActionRequest req)
        {
            NameValueCollection col = new NameValueCollection();
            col.Add("action", req.Action);
            col.Add("ek", req.EntityKey);
            col.Add("tk", req.TokenKey);
            if (req._dict.Count > 0)
                foreach (KeyValuePair<string, string> kv in req._dict)
                    col.Add(kv.Key, kv.Value);
            if (req.Detailed) col.Add("detailed", "");
            return col;
        }
    }

    public class ActionResponse
    {
        protected static Dictionary<string, JObject> _dict = new Dictionary<string, JObject>();

        public JObject this[string key]
        {
            get { return _dict.ContainsKey(key) ? _dict[key] : null; }
            set
            {
                if (!_dict.ContainsKey(key))
                    _dict.Add(key, value);
                else
                    _dict[key] = value;
            }
        }
    }

    #endregion "Ajax Requests"

    #endregion "Lerp2Web"
}