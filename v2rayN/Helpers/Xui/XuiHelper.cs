using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Renci.SshNet;
using v2rayN.Helpers.Xui.Model;
using v2rayN.Tool;

namespace Common.Xui
{
    public static class XuiHelper
    {


        //public static async Task<StringContent> GetRequestBody(MainConnection mainConnection, XuiConnection xuiConnection = null)
        //{

        //    if (xuiConnection == null)
        //    {
        //        xuiConnection = GenericBiz<XuiConnection>.FirstOrDefault(q => q.MainConnectionID == mainConnection.MainConnectionID);
        //    }
        //    //var usage = MainConnectionBiz.GetConnectionUsage(mainConnection.MainConnectionID);
        //    ////var softEtherUsage = SoftetherConnectionBiz.GetSoftetherConnectionUsage(subscription);

        //    //var upload = usage.UploadUsageByte;
        //    //var download = usage.DownloadUsageByte;


        //    var config = GenericBiz<ConfigurationTemplate>.First(q =>
        //        q.ConfigurationTemplateID == mainConnection.ConfigurationTemplateID && q.IsDeleted == false &&
        //        q.ProtocolTypeID == (int)ProtocolTypeID.Vless);

        //    var vlessSettings = SettingHelper.GetVlessSettings(mainConnection, config, xuiConnection);
        //    var streamSettings = StreamSettingHelper.GetStreamSettings(mainConnection.ConfigurationTemplateID.To<int>(), mainConnection);
        //    var sniffing = SniffingHelper.GetSniffing(config.Sniffing.To<bool>());

        //    var existingInbound = await GetInbound(mainConnection.IP, mainConnection.IdentifierOnServer.To<int>());

        //    var formData = new Dictionary<string, string>
        //    {
        //        { "up", existingInbound==null?"0":existingInbound.obj.up.ToString() },
        //        { "down", existingInbound==null?"0":existingInbound.obj.down.ToString()  },
        //        { "total", "" },//not important
        //        { "remark", mainConnection.Name+" "+config.ConfigurationTemplateTitle },
        //        { "enable", "true" },//each client should handle this 
        //        { "expiryTime", "0" },//not important
        //        { "listen", "" },
        //        { "port", mainConnection.Port.ToString() },
        //        { "protocol", "vless" },
        //        { "settings", JsonConvert.SerializeObject(vlessSettings) },
        //        { "streamSettings", JsonConvert.SerializeObject(streamSettings) },
        //        { "sniffing", JsonConvert.SerializeObject(sniffing) },
        //    };

        //    //var formContent = await new FormUrlEncodedContent(formData).ReadAsStringAsync();
        //    //return new StringContent(formContent, Encoding.UTF8, "application/x-www-form-urlencoded"); ;

        //    var encodedItems = formData.Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
        //    var encodedContent = new StringContent(String.Join("&", encodedItems), null, "application/x-www-form-urlencoded");

        //    return encodedContent;
        //}
        public static async Task<string> LoginAndReturnToken(string ip, int port = 54321, string username = @"admin", string password = "admin")
        {
            var token = "";
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri($"http://{ip}:{port}");
                    var request = new HttpRequestMessage(HttpMethod.Post, "/login");

                    string body = $"username={username}&password={password}";
                    request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    token = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
                    string pattern = @"session=(.+?);";
                    RegexOptions options = RegexOptions.Multiline;
                    token = Regex.Match(token, pattern, options).Groups[1].Value;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return token;
        }

        public static async Task<XuiUserListModel> GetUserList(string ip)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.CookieContainer.Add(new Uri($"http://{ip}:54321"), new Cookie("session", await LoginAndReturnToken(ip)));

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri($"http://{ip}:54321");
                    var request = new HttpRequestMessage(HttpMethod.Post, "/xui/inbound/list");

                    var response = await client.SendAsync(request);
                    var s = await response.Content.ReadAsStringAsync();
                    var res = JsonConvert.DeserializeObject<XuiUserListModel>(s);
                    return res;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        //public static int GetVlessUserIDByName(string ip, string name)
        //{
        //    return GetUserList(ip).obj.Where(q => q.protocol == "vless").OrderBy(q => q.id).Last(q => q.remark == name).id;//last one is the newly created one
        //}

        //public static XuiObjModel GetVlessUserByID(string ip, int Id)
        //{
        //    return GetUserList(ip).obj.Where(q => q.protocol == "vless").ToList().First(q => q.id == Id);
        //}

        public static string GetVlessString(string ip, XuiAddUserModel model)
        {
            return
                $"vless://{model.UUID}@{ip}:{model.Port}?type=tcp&security=reality&pbk={model.PublicKey}&fp=firefox&sni={model.TestingDomain}&sid={model.ShortID}&spx=%2F#{model.Remark}";
        }
        public static async Task<XuiUserModel> AddUser(string ip, XuiAddUserModel model)
        {

            try
            {
                //if (await GetInbound(mainConnectionXuiIncluded.IP, mainConnectionXuiIncluded.IdentifierOnServer.To<int>()) != null)
                //{
                //    //should edit because inbound exists
                //    return await EditUser(mainConnectionXuiIncluded);
                //}

                var handler = new HttpClientHandler();
                handler.CookieContainer.Add(new Uri($"http://{ip}:54321"), new Cookie("session", await LoginAndReturnToken(ip).ConfigureAwait(false)));

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri($"http://{ip}:54321");
                    client.Timeout = Timeout.InfiniteTimeSpan;
                    var request = new HttpRequestMessage(HttpMethod.Post, "/xui/inbound/add");


                    var body = $"up=0&down=0&total=0&remark={model.Remark}&enable=true&expiryTime=0&listen=&port={model.Port}&protocol=vless&settings=%7B%0A%20%20%22clients%22%3A%20%5B%0A%20%20%20%20%7B%0A%20%20%20%20%20%20%22id%22%3A%20%22{model.UUID}%22%2C%0A%20%20%20%20%20%20%22flow%22%3A%20%22%22%2C%0A%20%20%20%20%20%20%22email%22%3A%20%22{model.Email}%22%2C%0A%20%20%20%20%20%20%22totalGB%22%3A%200%2C%0A%20%20%20%20%20%20%22expiryTime%22%3A%200%2C%0A%20%20%20%20%20%20%22enable%22%3A%20true%2C%0A%20%20%20%20%20%20%22tgId%22%3A%20%22%22%2C%0A%20%20%20%20%20%20%22subId%22%3A%20%22%22%0A%20%20%20%20%7D%0A%20%20%5D%2C%0A%20%20%22decryption%22%3A%20%22none%22%2C%0A%20%20%22fallbacks%22%3A%20%5B%5D%0A%7D&streamSettings=%7B%0A%20%20%22network%22%3A%20%22tcp%22%2C%0A%20%20%22security%22%3A%20%22reality%22%2C%0A%20%20%22realitySettings%22%3A%20%7B%0A%20%20%20%20%22show%22%3A%20false%2C%0A%20%20%20%20%22xver%22%3A%200%2C%0A%20%20%20%20%22dest%22%3A%20%22{model.TestingDomain}%3A443%22%2C%0A%20%20%20%20%22serverNames%22%3A%20%5B%0A%20%20%20%20%20%20%22{model.TestingDomain}%22%2C%0A%20%20%20%20%20%20%22www.{model.TestingDomain}%22%0A%20%20%20%20%5D%2C%0A%20%20%20%20%22privateKey%22%3A%20%22{model.PrivateKey}%22%2C%0A%20%20%20%20%22minClient%22%3A%20%22%22%2C%0A%20%20%20%20%22maxClient%22%3A%20%22%22%2C%0A%20%20%20%20%22maxTimediff%22%3A%200%2C%0A%20%20%20%20%22shortIds%22%3A%20%5B%0A%20%20%20%20%20%20%22{model.ShortID}%22%0A%20%20%20%20%5D%2C%0A%20%20%20%20%22settings%22%3A%20%7B%0A%20%20%20%20%20%20%22publicKey%22%3A%20%22{model.PublicKey}%22%2C%0A%20%20%20%20%20%20%22fingerprint%22%3A%20%22firefox%22%2C%0A%20%20%20%20%20%20%22serverName%22%3A%20%22%22%2C%0A%20%20%20%20%20%20%22spiderX%22%3A%20%22%2F%22%0A%20%20%20%20%7D%0A%20%20%7D%2C%0A%20%20%22tcpSettings%22%3A%20%7B%0A%20%20%20%20%22acceptProxyProtocol%22%3A%20false%2C%0A%20%20%20%20%22header%22%3A%20%7B%0A%20%20%20%20%20%20%22type%22%3A%20%22none%22%0A%20%20%20%20%7D%0A%20%20%7D%0A%7D&sniffing=%7B%0A%20%20%22enabled%22%3A%20true%2C%0A%20%20%22destOverride%22%3A%20%5B%0A%20%20%20%20%22http%22%2C%0A%20%20%20%20%22tls%22%0A%20%20%5D%0A%7D";
                    //var body = await GetRequestBody(mainConnectionXuiIncluded, xuiConnection);
                    request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                    var response = await client.SendAsync(request);

                    var jsonSerializerSettings = new JsonSerializerSettings();
                    jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    var res = JsonConvert.DeserializeObject<XuiUserModel>(await response.Content.ReadAsStringAsync());
                    if (!res.success)
                        throw new Exception(await response.Content.ReadAsStringAsync());

                    return res;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public static async Task<bool> DeleteUser(string ip, int? vlessUserId)
        {
            if (!vlessUserId.HasValue)
                return false;

            var inbound = await GetInbound(ip, vlessUserId.To<int>());

            if (inbound != null)
            {
                if (inbound.obj.settingsObj.clients.Length >= 2)//more than 2 clients
                {
                    // just editing whole connection would do the trick and delete all current users


                    //if (mainConnection == null)// when there is no connection in db for this connection, just delete the whole inbound
                    //{
                    return await DeleteInboundByIpAndId(ip, vlessUserId);
                    //}
                    //return (await EditUser(mainConnection)).success;

                }
                else
                {
                    return await DeleteInboundByIpAndId(ip, vlessUserId);
                }

            }

            return true;

        }
        public static async Task<bool> DeleteInboundByIpAndId(string ip, int? vlessUserId)
        {
            if (!vlessUserId.HasValue)
                return false;
            try
            {
                var handler = new HttpClientHandler();
                handler.CookieContainer.Add(new Uri($"http://{ip}:54321"), new Cookie("session", await LoginAndReturnToken(ip)));

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri($"http://{ip}:54321");
                    var request = new HttpRequestMessage(HttpMethod.Post, $"/xui/inbound/del/{vlessUserId}");

                    var response = await client.SendAsync(request);
                    var ignoreMissingMembersJsonSerializerSettings = new JsonSerializerSettings();
                    ignoreMissingMembersJsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

                    var s = await response.Content.ReadAsStringAsync();
                    var res = JsonConvert.DeserializeObject<XuiSuccessModel>(s, ignoreMissingMembersJsonSerializerSettings);

                    if (!res.success)
                        throw new Exception(s);

                    return res.success;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }
        public static async Task<bool> DeleteUser(string ip, int idOnServer)
        {
            //return DeleteUser(connection.IP, connection.IdentifierOnServer.To<int>());

            var inbound = await GetInbound(ip, idOnServer);

            if (inbound != null)
            {
                if (inbound.obj.settingsObj.clients.Length >= 2)//more than 2 clients
                {
                    // just editing whole connection would do the trick and delete all current users
                    //return (await EditUser(connection)).success;

                }
                else
                {
                    await DeleteInboundByIpAndId(ip, idOnServer);
                }

            }

            return false;

        }



        //private static async Task<string> GetXuiLatestVersion(string ip)
        //{

        //    try
        //    {
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://{ip}:54321/server/getXrayVersion");

        //        request.KeepAlive = true;
        //        request.Accept = "application/json, text/plain, */*";
        //        request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
        //        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36";
        //        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
        //        request.Headers.Add("Origin", $@"http://{ip}:54321");
        //        request.Referer = $"http://{ip}:54321/";
        //        request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
        //        request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9,fa;q=0.8,fr;q=0.7");
        //        request.Headers.Set(HttpRequestHeader.Cookie, await LoginAndReturnToken(ip));


        //        request.Method = "POST";
        //        request.ServicePoint.Expect100Continue = false;

        //        string body = @"";
        //        byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
        //        request.ContentLength = postBytes.Length;
        //        using (Stream stream = request.GetRequestStream())
        //        {
        //            stream.Write(postBytes, 0, postBytes.Length);
        //            stream.Close();

        //            using (var response = (HttpWebResponse)request.GetResponse())
        //            {
        //                using (var responseStream = response.GetResponseStream())
        //                {
        //                    using (var reader = new StreamReader(responseStream, Encoding.UTF8))
        //                    {
        //                        var s = reader.ReadToEnd();
        //                        var ss = JsonConvert.DeserializeObject<XuiAvailableVersionsModel>(s);
        //                        return ss.obj[0];
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return "";
        //    }

        //}

        public static async Task<XuiUserListModel> GetAllInbounds(string ip)
        {
            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(new Uri($"http://{ip}:54321"), new Cookie("session", await LoginAndReturnToken(ip)));

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"http://{ip}:54321");
                var request = new HttpRequestMessage(HttpMethod.Get, "/xui/API/inbounds");

                var response = await client.SendAsync(request);
                var ignoreMissingMembersJsonSerializerSettings = new JsonSerializerSettings();
                ignoreMissingMembersJsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

                var s = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<XuiUserListModel>(s, ignoreMissingMembersJsonSerializerSettings);

                if (!res.success)
                {
                    if (res.msg.Contains("record not found"))
                    {
                        return null;
                    }
                    else
                    {
                        throw new Exception($"error in get inbound with ip {ip} \n{s}");
                    }
                }

                return res;
            }
        }
        public static async Task<XuiUserModel> GetInbound(string ip, int id)
        {
            var handler = new HttpClientHandler();
            handler.CookieContainer.Add(new Uri($"http://{ip}:54321"), new Cookie("session", await LoginAndReturnToken(ip).ConfigureAwait(false)));

            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri($"http://{ip}:54321");
                var request = new HttpRequestMessage(HttpMethod.Get, $"/xui/API/inbounds/get/{id}");

                var response = await client.SendAsync(request);
                var ignoreMissingMembersJsonSerializerSettings = new JsonSerializerSettings();
                ignoreMissingMembersJsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

                var s = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<XuiUserModel>(s, ignoreMissingMembersJsonSerializerSettings);

                if (!res.success)
                {
                    if (res.msg.Contains("record not found"))
                    {
                        return null;
                    }
                    else
                    {
                        throw new Exception($"error in get inbound with ip {ip} and id {id} \n{s}");
                    }
                }

                return res;
            }
        }



        public static async Task<bool> RestartPanel(string ip)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.CookieContainer.Add(new Uri($"http://{ip}:54321"), new Cookie("session", await LoginAndReturnToken(ip)));

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri($"http://{ip}:54321");
                    var request = new HttpRequestMessage(HttpMethod.Post, "/xui/setting/statusPanel");

                    await client.SendAsync(request);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
        //public static async Task<bool> MigrateDb(int groupId)
        //{

        //    var group = GenericBiz<Group>.First(q => q.GroupID == groupId);
        //    var scriptResult = "";

        //    try
        //    {
        //        using (var client = new SshClient(group.SshIp, group.SshUsername, group.SshPassword))
        //        {

        //            client.Connect();
        //            scriptResult = client.RunCommand("/usr/local/x-ui/x-ui migrate").Result;
        //            client.Disconnect();

        //        }
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        return false;
        //    }

        //}
        public static bool IsXuiInstalled(string ip, string username, string password)
        {

            var scriptResult = "";

            try
            {
                using (var client = new SshClient(ip, username, password))
                {

                    client.Connect();
                    scriptResult = client.RunCommand("x-ui status").Result;
                    client.Disconnect();

                }
                if (scriptResult.Contains("x-ui.service - x-ui Service"))
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }

        }
        public static async Task<XuiCertModel> GetX25519Cert(string ip)
        {
            try
            {


                var handler = new HttpClientHandler();
                handler.CookieContainer.Add(new Uri($"http://{ip}:54321"),
                    new Cookie("session", await LoginAndReturnToken(ip)));

                using (var client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri($"http://{ip}:54321");
                    var request = new HttpRequestMessage(HttpMethod.Post, $"/server/getNewX25519Cert");

                    var response = await client.SendAsync(request);
                    var jsonSerializerSettings = new JsonSerializerSettings();
                    jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

                    var s = await response.Content.ReadAsStringAsync();
                    var res = JsonConvert.DeserializeObject<XuiCertModel>(s, jsonSerializerSettings);

                    if (!res.success)
                        throw new Exception(s);

                    return res;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }


    }
}