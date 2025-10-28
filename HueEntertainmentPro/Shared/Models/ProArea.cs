using ProtoBuf;

namespace HueEntertainmentPro.Shared.Models
{
  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class ProArea
  {
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public IEnumerable<BridgeGroupConnection> Connections { get; set; } = new List<BridgeGroupConnection>();

    // public SimpleHuePosition? LocationCenter { get; set; }
    // public bool IsAlwaysVisible { get; set; }
    //  public bool HideDisconnect { get; set; }
  }
}
