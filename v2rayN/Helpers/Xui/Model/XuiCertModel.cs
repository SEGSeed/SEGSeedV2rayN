namespace v2rayN.Helpers.Xui.Model
{

    public class XuiCertModel
    {
        public bool success { get; set; }
        public string msg { get; set; }
        public KeysModel obj { get; set; }
    }

    public class KeysModel
    {
        public string privateKey { get; set; }
        public string publicKey { get; set; }
    }
}