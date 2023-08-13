namespace v2rayN.Helpers.Xui.Model.InsertModels
{
    public class TlsSettings
    {
        public string serverName { get; set; }
        public string minVersion => "1.0";
        public string maxVersion => "1.2";
        public string cipherSuites => "";
        public Certificate[] certificates { get; set; }
        public string[] alpn => new[] { "" };
        //"h2", "http/1.1"

        public TlsOption[] settings { get; set; }

    }


}