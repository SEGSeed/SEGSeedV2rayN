using Newtonsoft.Json;

namespace v2rayN.Helpers.Xui.Model.InsertModels
{

    public class StreamSettings
    {
        public string network { get; set; }
        public string security { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TcpSettings tcpSettings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public WsSettings wsSettings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public HttpSettings httpSettings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public QuicSettings quicSettings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public GrpcSettings grpcSettings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TlsSettings tlsSettings { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RealitySettings realitySettings { get; set; }
    }
}