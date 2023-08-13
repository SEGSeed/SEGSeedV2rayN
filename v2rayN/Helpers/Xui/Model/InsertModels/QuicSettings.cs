namespace v2rayN.Helpers.Xui.Model.InsertModels
{

    public class QuicSettings
    {
        public string security { get; set; }
        public string key { get; set; }
        public QuicHeader header { get; set; }
    }

    public class QuicHeader
    {
        public string type { get; set; }
    }

}

