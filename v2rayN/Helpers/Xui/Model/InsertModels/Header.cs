using Newtonsoft.Json;

namespace v2rayN.Helpers.Xui.Model.InsertModels
{
    public class Header
    {
        public string type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Request request { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Response response { get; set; }
    }

}