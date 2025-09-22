using ProtoBuf;
using System.Collections.Generic;

namespace HueLightDJ.Services.Interfaces.Models
{
  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class EffectsVM
  {
    public List<EffectList> BaseEffects { get; set; } = new();
    public List<EffectViewModel> ShortEffects { get; set; } = new();
    public List<EffectViewModel> GroupEffects { get; set; } = new();
    public List<string> IteratorModes { get; set; } = new();
    public List<string> SecondaryIteratorModes { get; set; } = new();

  }

  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class EffectList
  {
    public required string Title { get; set; }
    public List<EffectViewModel> Effects { get; set; } = new();
  }

  [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
  public class EffectViewModel
  {
    public required string Name { get; set; }

    public required string TypeName { get; set; }
    public bool HasColorPicker { get; set; }


    //VueJS properties:
    public string? Color { get; set; }

    public bool IsRandom { get; set; } = true;

  }


}
