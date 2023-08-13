namespace v2rayN.Helpers.Xui.Model.InsertModels
{


    public class Sniffing
    {
        public bool enabled { get; set; }
        public string[] destOverride => new[] { "http", "tls" };
    }


}