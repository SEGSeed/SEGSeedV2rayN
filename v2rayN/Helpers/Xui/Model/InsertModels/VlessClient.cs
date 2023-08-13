namespace v2rayN.Helpers.Xui.Model.InsertModels
{

    public class VlessClient
    {
        public string id { get; set; }
        public string flow { get; set; }
        public string email { get; set; }
        public int limitIp { get; set; }
        public bool enable { get; set; }
        public long totalGB { get; set; }
        public long? expiryTime { get; set; }
    }

}