using Newtonsoft.Json;

namespace v2rayN.Helpers.TlsHelpers.Models
{

    public class TlsModel
    {
        public string loc { get; set; }
        public string testedUrl { get; set; }
        public long time { get; set; }
        public string protocols { get; set; }
        public List<Protocols>? protocolsObj => JsonConvert.DeserializeObject<List<Protocols>>(protocols);
        public string permalink { get; set; }
    }


    public class Protocols
    {
        public string name { get; set; }
        public string version { get; set; }
        public Suite[] suites { get; set; }
    }

    public class Suite
    {
        public string name { get; set; }
        public int cipherStrength { get; set; }
        public string q { get; set; }
    }


}