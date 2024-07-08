using System;
using System.Collections.Generic;

using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.IO;


namespace Kollus_MultiDRM_Sample
{
    public partial class Sample : System.Web.UI.Page
    {

        string accessKey = "{ACCESS_KEY}"; //Inak Multi DRM Access Key
        string siteKey = "{SITE_KEY}"; //Inka Multi DRM Site Key
        string siteId = "{SITE_ID}"; //Inka Multi DRM Site ID
        string securityKey = "{SECURITY_KEY}"; //Kollus Security Key
        string customKey = "{CUSTOM_KEY}"; //Kollus Custom User Key
        string licenseUrl = ""; //Inka License Url
        string certificateUrl = "{CERTIFICATION_URL}"; //Inka Certification URL(FPS)
        string iv = "0123456789abcdef";
        int duration = 3600;
        string videoGateWayKR = "https://v.kr.kollus.com/";
        string videoGateWayJP = "https://v.jp.kollus.com/";

        protected void Page_Load(object sender, EventArgs e)
        {
            string uploadFileKey = "{UPLOAD_FILE_KEY}"; //Kollus Upload FIle Key
            string mediaContentKey = "{MEDIA_CONTENT_KEY"; //Kollus Media Content Key
            string clientUserId = "{HOMEPAGE_USER_ID}"; // End User ID
            
            string webTokenUrl = videoGateWayKR + "s?jwt=" + System.Web.HttpUtility.UrlEncode( CreateWebtoken(uploadFileKey, mediaContentKey, clientUserId)) + "&custom_key=" + customKey;
            Response.Write(Request.UserAgent + "<br>");
            string[] drmType = BrowserCheck(Request.UserAgent);
            if (drmType != null)
            {
                Response.Write(drmType[0] + " " + drmType[1]);

                playerFrame.Attributes["src"] = webTokenUrl;
            }
            Response.Write("<a href='" + webTokenUrl + "'>Link</a>");
        }


        private string CreateWebtoken(string uploadFileKey, string mediaContentKey, string userId)
        {
            string token = "";

            JObject payload = new JObject();
            JArray mediaContentArray = new JArray();
            JObject mediaContent = new JObject();
            JObject drmPolicy = new JObject();
            JObject data = new JObject();
            JObject customHeader = new JObject();

            string[] drmType = BrowserCheck(Request.UserAgent);
            payload.Add("expt", ConvertToUnixTimestamp(DateTime.Now) + duration);
            payload.Add("cuid", userId);

            mediaContent.Add("mckey", mediaContentKey);
            drmPolicy.Add("kind", "inka");
            drmPolicy.Add("streaming_type", drmType[1]);
            data.Add("license_url", licenseUrl);
            data.Add("certificate_url", certificateUrl);
            customHeader.Add("key", "pallycon-customdata-v2");
            string inkaToken = CreateInkaPayload(uploadFileKey, userId);
            customHeader.Add("value", inkaToken);
            data.Add("custom_header", customHeader);
            drmPolicy.Add("data", data);
            mediaContent.Add("drm_policy", drmPolicy);
            mediaContentArray.Add(mediaContent);
            payload.Add("mc", mediaContentArray);

            JObject jwtHead = new JObject();
            jwtHead.Add("typ", "JWT");
            jwtHead.Add("alg", "HS256");
            string jwtHeadString = ToBase64Encoding(jwtHead.ToString());
            string payloadString = ToBase64Encoding(payload.ToString());
            string message = jwtHeadString + "." + payloadString;
            string signature = SHA256Encrypt(jwtHeadString + "." + payloadString, securityKey);
            token = jwtHeadString + "." + payloadString + "." + signature;
            return token;
        }

        private string CreateInkaPayload(string contentId, string userId)
        {
            string result = "";
            
            string timeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd") + "T" + DateTime.UtcNow.ToString("HH:mm:ss") + "Z";
            string[] drmType = BrowserCheck(Request.UserAgent);
            if (drmType != null)
            {
                JObject inkaPayload = new JObject();
                JObject inkaToken = new JObject();
                JObject playBackPolicy = new JObject();
                JObject security_policy = new JObject();
                playBackPolicy.Add("limit", true);
                playBackPolicy.Add("persistent", false);
                playBackPolicy.Add("duration", duration);
                inkaToken.Add("playback_policy", playBackPolicy);
                //inkaToken.Add("allow_mobile_abnormal_device", false);
                inkaToken.Add("playready_security_level", 150);

                var token = Convert.ToBase64String(AESEncrypt256(inkaToken.ToString(), siteKey));
                string hash = accessKey + drmType[0] + siteId + userId + contentId + token + timeStamp;
                SHA256Managed sha256 = new SHA256Managed();
                SHA256 sha256Hash = SHA256.Create();
                hash = Convert.ToBase64String(sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(hash)));
                inkaPayload.Add("drm_type", drmType[0]);
                inkaPayload.Add("site_id", siteId);
                inkaPayload.Add("user_id", userId);
                inkaPayload.Add("cid", contentId);
                inkaPayload.Add("token", token);
                inkaPayload.Add("timestamp", timeStamp);
                inkaPayload.Add("hash", hash);
                result = ToBase64Encoding(inkaPayload.ToString());
                result = HttpUtility.UrlEncode(result);
            }
            else
            {
                result = "";
            }

            return result;
        }

        private string[] BrowserCheck(string userAgent)
        {
            string[] result = new string[2];

            string[] browsers = new string[] { "Trident", "MSIE", "Opera", "Safari", "Chrome", "Firefox", "Edg", "Edge", "CriOS" };
//            string[] browsers = new string[] { "Safari", "CriOS", "Edge", "Edg", "Firefox", "Chrome", "Opera", "MSIE", "Trident" };

            foreach (string browser in browsers)
            {
                int index = userAgent.IndexOf(browser);
                if (System.Text.RegularExpressions.Regex.IsMatch(userAgent,browser, System.Text.RegularExpressions.RegexOptions.None))
                {
                    switch (browser)
                    {
                        case "MSIE":
                            result[0] = "PlayReady";
                            result[1] = "dash";
                            break;
                        case "Trident":
                            result[0] = "PlayReady";
                            result[1] = "dash";
                            break;
                        case "Edge":
                            result[0] = "Widevine";
                            result[1] = "dash";
                            break;
                        case "Edg":
                            result[0] = "Widevine";
                            result[1] = "dash";
                            break;
                        case "Chrome":
                            result[0] = "Widevine";
                            result[1] = "dash";
                            break;
                        case "Firefox":
                            result[0] = "Widevine";
                            result[1] = "dash";
                            break;
                        case "Opera":
                            result[0] = "PlayReady";
                            result[1] = "dash";
                            break;
                        case "Safari":
                            result[0] = "FairPlay";
                            result[1] = "hls";
                            break;
                        case "CriOS":
                            result[0] = "FairPlay";
                            result[1] = "hls";
                            break;
                        default:
                            result = null;
                            break;
                    }
                }
            }
            return result;
        }

        static readonly char[] padding = { '=' };
        private string ToBase64Encoding(string text)
        {
            byte[] arr = System.Text.Encoding.UTF8.GetBytes(text);
            string rtnValue = System.Convert.ToBase64String(arr).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
            return rtnValue;
        }
        private string ToBase64Encoding(byte[] text)
        {
            string rtnValue = System.Convert.ToBase64String(text).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
            return rtnValue;
        }
        static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }


        static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public string SHA256Encrypt(string message, string secret)
        {
            byte[] keyByte = System.Text.Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            using (System.Security.Cryptography.HMACSHA256 hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

        public Byte[] AESEncrypt256(String Input, String key)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = Encoding.UTF8.GetBytes(iv);

            var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] xBuff = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Encoding.UTF8.GetBytes(Input);
                    cs.Write(xXml, 0, xXml.Length);
                }

                xBuff = ms.ToArray();
            }

            String Output = Convert.ToBase64String(xBuff);
            return xBuff;
        }


        public String AESDecrypt256(String Input, String key)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var decrypt = aes.CreateDecryptor();
            byte[] xBuff = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Convert.FromBase64String(Input);
                    cs.Write(xXml, 0, xXml.Length);
                }

                xBuff = ms.ToArray();
            }

            String Output = Encoding.UTF8.GetString(xBuff);
            return Output;
        }
    }




}
