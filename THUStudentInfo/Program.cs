using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;

namespace THUStudentInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("JSESSIONID: ");
            var jses = Console.ReadLine();
            var cookies = new CookieCollection();
            cookies.Add(new Cookie("JSESSIONID", jses,"/", "bylx.cic.tsinghua.edu.cn"));
            Console.Write("Start ID: ");
            var sid = long.Parse(Console.ReadLine());
            Console.Write("End ID: ");
            var eid = long.Parse(Console.ReadLine());
            Console.Write("Request interval (s): ");
            var interval = int.Parse(Console.ReadLine()) * 1000;
            Console.Write("Current year? [y/n] ");
            var url = Console.ReadLine() == "n" ? "http://bylx.cic.tsinghua.edu.cn/lxsxbl_history.jsp?xh=" : "http://bylx.cic.tsinghua.edu.cn/lxsxbl.jsp?xh=";
            using (var sw = new StreamWriter("result.csv", true, Encoding.GetEncoding("gbk")))
                for (long i = sid; i <= eid; i++)
                {
                    try
                    {
                        var os = "";
                        var html = HttpGet(url + i, cookiesin: cookies);
                        var part1 = Regex.Match(html, "Layer3.+?<\\/div>",RegexOptions.Singleline);
                        if (!part1.Success) throw new Exception(i + " not exist.");
                        var matches = Regex.Matches(part1.Value, @"<b>(.+?)<\/b>", RegexOptions.Singleline);
                        foreach (Match match in matches)
                        {
                            var str = match.Groups[1].Value;
                            os += "\"" + str + "\",";
                        }
                        matches = Regex.Matches(html, "<td class=\"font10\"(.+?)<\\/", RegexOptions.Singleline);
                        foreach (Match match in matches)
                        {
                            var str = match.Groups[1].Value;
                            str = str.Substring(str.LastIndexOf('>') + 1);
                            os += "\"" + str + "\",";
                        }
                        os += "\"\"";
                        os = os.Replace("\r", "").Replace("\n", "").Replace("&nbsp;", " ");
                        Console.WriteLine(os);
                        sw.WriteLine(os);
                        sw.Flush();
                        HttpGetFile("http://bylx.cic.tsinghua.edu.cn/image.jsp?bylx=bylx&xh=" + i, i + ".jpg", cookies);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Thread.Sleep(interval);
                }
        }

        public static string GetResponse(ref HttpWebRequest req, out CookieCollection cookies, bool GetLocation = false, bool GetRange = false, bool NeedResponse = true)
        {
            HttpWebResponse res = null;
            cookies = null;
            try
            {
                res = (HttpWebResponse)req.GetResponse();
                cookies = res.Cookies;
            }
            catch (WebException e)
            {
                StreamReader ereader = new StreamReader(e.Response.GetResponseStream(), Encoding.GetEncoding("gbk"));
                string erespHTML = ereader.ReadToEnd();
                Console.WriteLine(erespHTML);
                throw new Exception(erespHTML);
            }
            if (GetLocation)
            {
                string ts = res.Headers["Location"];
                res.Close();
                Console.WriteLine("Location: " + ts);
                return ts;
            }
            if (GetRange && res.ContentLength == 0)
            {
                string ts = res.Headers["Range"];
                Console.WriteLine("Range: " + ts);
                return ts;
            }
            if (NeedResponse)
            {
                StreamReader reader = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("gbk"));
                string respHTML = reader.ReadToEnd();
                res.Close();
                //Console.WriteLine(respHTML);
                return respHTML;
            }
            else
            {
                res.Close();
                return "";
            }
        }
        public static void GetResponseFile(ref HttpWebRequest req, string savepath)
        {
            HttpWebResponse res = null;
            try
            {
                res = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException e)
            {
                StreamReader ereader = new StreamReader(e.Response.GetResponseStream(), Encoding.GetEncoding("gbk"));
                string erespHTML = ereader.ReadToEnd();
                Console.WriteLine(erespHTML);
                throw new Exception(erespHTML);
            }
            var sw = new FileStream(savepath, FileMode.Create);
            res.GetResponseStream().CopyTo(sw);
            res.Close();
            sw.Close();
        }
        public static string GetResponse(ref HttpWebRequest req, bool GetLocation = false, bool GetRange = false, bool NeedResponse = true)
        {
            CookieCollection c;
            return GetResponse(ref req, out c, GetLocation, GetRange, NeedResponse);
        }
        public static HttpWebRequest GenerateRequest(string URL, string Method, string token, bool KeepAlive = false, string ContentType = null, byte[] data = null, int offset = 0, int length = 0, string ContentRange = null, bool PreferAsync = false, int Timeout = 20 * 1000, string host = null, string Referer = null, string Accept = null, CookieCollection cookies = null)
        {
            Uri httpUrl = new Uri(URL);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(httpUrl);
            req.ProtocolVersion = new System.Version("1.0");
            req.Timeout = Timeout;
            req.ReadWriteTimeout = Timeout;
            req.Method = Method;
            if (token != null) req.Headers.Add("Authorization", "Bearer " + token);
            req.KeepAlive = KeepAlive;
            if (ContentType != null) req.ContentType = ContentType;
            if (ContentRange != null) req.Headers.Add("Content-Range", ContentRange);
            if (PreferAsync == true) req.Headers.Add("Prefer", "respond-async");
            if (Referer != null) req.Referer = Referer;
            if (Accept != null) req.Accept = Accept;
            if (cookies != null)
            {
                req.CookieContainer = new CookieContainer();
                req.CookieContainer.Add(cookies);
            }
            if (data != null)
            {
                req.ContentLength = length;
                Stream stream = req.GetRequestStream();
                stream.Write(data, offset, length);
                stream.Close();
            }
            return req;
        }
        public static string HttpGet(string URL, string token = null, bool GetLocation = false, bool AllowAutoRedirect = true, bool NeedResponse = true, int Timeout = 5 * 1000, string host = null, CookieCollection cookiesin = null)
        {
            HttpWebRequest req = GenerateRequest(URL, "GET", token, false, null, null, 0, 0, null, false, Timeout, host, cookies: cookiesin);
            if (AllowAutoRedirect == false) req.AllowAutoRedirect = false;
            return GetResponse(ref req, GetLocation, false, NeedResponse);
        }
        public static void HttpGetFile(string URL,string savepath, CookieCollection cookiesin = null)
        {
            var req = GenerateRequest(URL, "GET", null, cookies: cookiesin);
            GetResponseFile(ref req, savepath);
        }
        public static string HttpGet(string URL, out CookieCollection cookies, string token = null, bool GetLocation = false, bool AllowAutoRedirect = true, bool NeedResponse = true, int Timeout = 5 * 1000, string host = null, CookieCollection cookiesin = null)
        {
            HttpWebRequest req = GenerateRequest(URL, "GET", token, false, null, null, 0, 0, null, false, Timeout, host, cookies: cookiesin);
            if (AllowAutoRedirect == false) req.AllowAutoRedirect = false;
            return GetResponse(ref req, out cookies, GetLocation, false, NeedResponse);
        }
        public static string HttpPost(string URL, string token, byte[] data, int offset = 0, int length = -1, bool NeedResponse = true, int Timeout = 20 * 1000, string host = null, string ContentType = null, string Referer = null, string Accept = null, CookieCollection cookiesin = null)
        {
            if (length == -1) length = data.Length;
            HttpWebRequest req = GenerateRequest(URL, "POST", token, false, ContentType, data, 0, data.Length, null, false, Timeout, host, Referer, Accept, cookiesin);
            return GetResponse(ref req, false, false, NeedResponse);
        }
        public static string HttpPost(string URL, string token, byte[] data, out CookieCollection cookies, int offset = 0, int length = -1, bool NeedResponse = true, int Timeout = 20 * 1000, string host = null, string ContentType = null, string Referer = null, string Accept = null, CookieCollection cookiesin = null)
        {
            if (length == -1) length = data.Length;
            HttpWebRequest req = GenerateRequest(URL, "POST", token, false, ContentType, data, 0, data.Length, null, false, Timeout, host, Referer, Accept, cookiesin);
            return GetResponse(ref req, out cookies, false, false, NeedResponse);
        }
    }
}
