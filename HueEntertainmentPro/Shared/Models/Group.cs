using ProtoBuf;

namespace HueEntertainmentPro.Shared.Models
{

  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class Group
  {
    public Guid Id { get; set; }
  }
}
