using HueApi.Entertainment.Extensions;
using HueApi.Entertainment.Models;
using HueLightDJ.Effects;
using HueLightDJ.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HueLightDJ.Services
{
  public static class GroupService
  {
    public static List<GroupModel> GetAll(EntertainmentLayer? layer = null)
    {
      if (layer == null)
        layer = StreamingSetup.GetFirstLayer();

      var center = EffectSettings.LocationCenter;

      var allLightsOrdered = layer.OrderBy(x => x.LightLocation.X).ThenBy(x => x.LightLocation.Y).ToList();

      var leftRight = new[] { allLightsOrdered.GetLeft(), allLightsOrdered.GetRight() }.ToList();
      var frontBack = new[] { allLightsOrdered.GetFront(), allLightsOrdered.GetBack() }.ToList();
      var quarter = new[] { layer.GetLeft().GetFront(), layer.GetLeft().GetBack(), layer.GetRight().GetBack(), layer.GetRight().GetFront() }.ToList();
      var alternating = allLightsOrdered.ChunkByGroupNumber(2);
      var alternatingFour = allLightsOrdered.ChunkByGroupNumber(4);
      var orderedByDistance = layer.OrderBy(x => x.LightLocation.Distance(center.X, center.Y, center.Z)).ChunkByGroupNumber(3);
      var orderedByAngle = layer.OrderBy(x => x.LightLocation.Angle(center.X, center.Y)).ChunkBy(6);

      var leftToRight = allLightsOrdered.GroupBy(x => (int)(((x.LightLocation.X + 1) / 2) * 50)).OrderBy(x => x.Key);
      var fronToBack = allLightsOrdered.GroupBy(x => (int)(((x.LightLocation.Y + 1) / 2) * 50)).OrderBy(x => x.Key);
      var ring = allLightsOrdered.GroupBy(x => (int)((x.LightLocation.Distance(center.X, center.Y, center.Z) / 1.5) * 2));
      var tentacles = allLightsOrdered.GroupBy(x => (int)((x.LightLocation.Angle(center.X, center.Y) / 3.6 / 3))).OrderBy(x => x.Key);

      var result = new List<GroupModel>()
      {
        //new GroupModel("All", allLightsOrdered.To2DGroup()),
        new GroupModel("Left/Right", leftRight),
        new GroupModel("Left To Right", leftToRight, 2),
        new GroupModel("Front/Back", frontBack),
        new GroupModel("Front To Back", fronToBack, 2),
        new GroupModel("Quarter", quarter, 2),
        new GroupModel("Alternating", alternating),
        new GroupModel("Alternating by 4", alternatingFour),
        //new GroupModel("Distance from center", orderedByDistance),
        //new GroupModel("Order by Angle from center", orderedByAngle),
        new GroupModel("Ring", ring, 2),
        new GroupModel("Tentacles", tentacles),
        new GroupModel("Random", GetRandomGroup()),
      };

      if (StreamingSetup.CurrentConnection?.Name == "Ster" || StreamingSetup.CurrentConnection?.Name == "DEMO Ster")
      {
        result.Add(new GroupModel("Tentacles (alternating 2)", tentacles.ChunkByGroupNumber(2).Select(x => x.SelectMany(l => l)), 2));
        result.Add(new GroupModel("Tentacles (alternating 3)", tentacles.ChunkByGroupNumber(3).Select(x => x.SelectMany(l => l)), 3));
        result.Add(new GroupModel("Tentacles (alternating 4)", tentacles.ChunkByGroupNumber(4).Select(x => x.SelectMany(l => l)), 4));
      }

      return result;
    }

    public static IEnumerable<IEnumerable<EntertainmentLight>> GetRandomGroup()
    {
      var layer = StreamingSetup.GetFirstLayer();
      var orderRandom = layer.OrderBy(x => Guid.NewGuid());

      var min = 2;
      var max = layer.Count / 2;

      if (max < min)
        max = min;

      Random r = new Random();
      var numberOfGroups = r.Next(min, max);

      return orderRandom.ChunkBy(numberOfGroups);

    }
  }
}
