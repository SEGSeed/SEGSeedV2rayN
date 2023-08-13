namespace v2rayN.Helpers.Xui.Model.InsertModels
{

    public class VlessSettings
    {
        public VlessClient[] clients { get; set; }

        public string decryption => "none";
        public Fallback[] fallbacks => new List<Fallback>().ToArray();
    }

}

