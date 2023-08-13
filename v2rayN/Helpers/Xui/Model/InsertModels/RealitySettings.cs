using System.Collections.Generic;

namespace v2rayN.Helpers.Xui.Model.InsertModels;

public class RealitySettings
{
    public bool show { get; set; }
    public int xver { get; set; }
    public string dest { get; set; }
    public string[] serverNames { get; set; }
    public string privateKey { get; set; }
    public string minClient => "";
    public string maxClient => "";
    public int maxTimediff => 0;
    public string[] shortIds { get; set; }
    public RealityInnerSettings settings { get; set; }

}

public class RealityInnerSettings
{
    public string publicKey { get; set; }
    public string fingerprint { get; set; }
    public string serverName { get; set; }
    public string spiderX { get; set; }
}