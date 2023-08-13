
using Newtonsoft.Json;

namespace v2rayN.Helpers.Xui.Model
{
    public class XuiObjModel
    {
        public int id { get; set; }
        public long up { get; set; }
        public long down { get; set; }
        public long total { get; set; }
        public string remark { get; set; }
        public bool enable { get; set; }
        public long expiryTime { get; set; }
        public Clientstat[] clientStats { get; set; }
        public string listen { get; set; }
        public int port { get; set; }
        public string protocol { get; set; }
        public string settings { get; set; }
        public XuiSettingsObj settingsObj => JsonConvert.DeserializeObject<XuiSettingsObj>(settings);
        public string streamSettings { get; set; }
        public string tag { get; set; }
        public string sniffing { get; set; }
    }

    public class Clientstat
    {
        public int id { get; set; }
        public int inboundId { get; set; }
        public bool enable { get; set; }
        public string email { get; set; }
        public long up { get; set; }
        public long down { get; set; }
        public long expiryTime { get; set; }
        public long total { get; set; }
    }
    public class XuiSettingsObj
    {
        public XuiClientObj[] clients { get; set; }
        public string decryption { get; set; }
        public object[] fallbacks { get; set; }
    }

    public class XuiClientObj
    {
        public string id { get; set; }
        public string flow { get; set; }
        public string email { get; set; }
        public long totalGB { get; set; }
        public long expiryTime { get; set; }
        public bool enable { get; set; }
        public string tgId { get; set; }
        public string subId { get; set; }
    }




}