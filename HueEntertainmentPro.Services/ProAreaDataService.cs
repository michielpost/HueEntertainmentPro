using HueEntertainmentPro.Database;
using HueEntertainmentPro.Database.Models;
using HueEntertainmentPro.Services.Extensions;
using HueEntertainmentPro.Shared.Interfaces;
using HueEntertainmentPro.Shared.Models.Requests;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace HueEntertainmentPro.Services
{
  public class ProAreaDataService(HueEntertainmentProDbContext dbContext) : IProAreaDataService
  {
    public static Guid demo1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static Guid demo2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public async Task<HueEntertainmentPro.Shared.Models.ProArea> AddBridgeGroup(AddBridgeGroupRequest req, CallContext context = default)
    {
      if (req.BridgeId == null || req.GroupId == null || req.ProAreaId == null)
        throw new ArgumentException("BridgeId and GroupId and ProAreaId are required.");

      // Find the bridge and group
      var bridge = await dbContext.Bridges.FirstOrDefaultAsync(b => b.Id == req.BridgeId.Value);
      var proArea = await dbContext.ProAreas.FirstOrDefaultAsync(b => b.Id == req.ProAreaId.Value);

      if (bridge == null || proArea == null || req.GroupId == null)
        throw new InvalidOperationException("Bridge or Group or ProArea not found.");

      // Find the ProArea for this group (assuming GroupId is unique per ProAreaBridgeGroup)


      // Add new bridge group connection
      var newGroup = new ProAreaBridgeGroup
      {
        Id = Guid.NewGuid(),
        ProAreaId = proArea.Id,
        BridgeId = bridge.Id,
        GroupId = req.GroupId.Value,
        Name = req.Name,
        CreatedDate = DateTime.UtcNow
      };
      dbContext.ProAreaGroups.Add(newGroup);

      await dbContext.SaveChangesAsync();

      var area = await GetProArea(new GuidRequest { Id = proArea.Id }, context);
      if (area == null)
      {
        throw new NullReferenceException($"Area is null. Id: {proArea.Id}");
      }
      return area;
    }

    public async Task DeleteBridgeGroup(GuidRequest req, CallContext context = default)
    {
      var group = await dbContext.ProAreaGroups.FirstOrDefaultAsync(pg => pg.Id == req.Id);
      if (group != null)
      {
        dbContext.ProAreaGroups.Remove(group);
        await dbContext.SaveChangesAsync();
      }
    }

    public async Task DeleteProArea(GuidRequest req, CallContext context = default)
    {
      var area = await dbContext.ProAreas.FirstOrDefaultAsync(pa => pa.Id == req.Id);
      if (area != null)
      {
        // Remove all related bridge groups
        var groups = dbContext.ProAreaGroups.Where(pg => pg.ProAreaId == area.Id);
        dbContext.ProAreaGroups.RemoveRange(groups);

        dbContext.ProAreas.Remove(area);
        await dbContext.SaveChangesAsync();
      }
    }

    public async Task<HueEntertainmentPro.Shared.Models.ProArea> UpdateProArea(UpdateProAreaRequest req, CallContext context = default)
    {
      var existing = await dbContext.ProAreas.Where(x => x.Id == req.Id).FirstOrDefaultAsync();
      if (existing != null)
      {
        existing.Name = req.Name;
        await dbContext.SaveChangesAsync();
      }

      var area = await GetProArea(new GuidRequest { Id = req.Id }, context);
      if (area == null)
      {
        throw new NullReferenceException($"Area is null. Id: {req.Id}");
      }
      return area;
    }

    public async Task<HueEntertainmentPro.Shared.Models.ProArea> CreateProArea(CreateProAreaRequest req, CallContext context = default)
    {
      var newArea = new Database.Models.ProArea
      {
        Id = Guid.NewGuid(),
        Name = req.Name,
        CreatedDate = DateTime.UtcNow
      };

      dbContext.ProAreas.Add(newArea);
      await dbContext.SaveChangesAsync();

      var area = await GetProArea(new GuidRequest { Id = newArea.Id }, context);
      if (area == null)
      {
        throw new NullReferenceException($"Area is null. Id: {newArea.Id}");
      }
      return area;

    }

    public async Task<HueEntertainmentPro.Shared.Models.ProArea?> GetProArea(GuidRequest req, CallContext context = default)
    {
      if (req.Id == demo1Id)
      {
        return new HueEntertainmentPro.Shared.Models.ProArea
        {
          Id = demo1Id,
          Name = "Demo Area",
          Connections = new List<HueEntertainmentPro.Shared.Models.BridgeGroupConnection>
           {
              new Shared.Models.BridgeGroupConnection
              {
                Bridge =  new Shared.Models.Bridge
                {
                  Id = Guid.Empty,
                  Name = "Demo Bridge 1",
                  Ip = "demoLocations1",
                  StreamingClientKey = "demoLocations1",
                  Username = "demoLocations1"
                }
              },
              new Shared.Models.BridgeGroupConnection
              {
                Bridge =  new Shared.Models.Bridge
                {
                  Id = Guid.Empty,
                  Name = "Demo Bridge 2",
                  Ip = "demoLocations2",
                  StreamingClientKey = "demoLocations2",
                  Username = "demoLocations2"
                }
              }
          }
        };
      }

      if (req.Id == demo2Id)
      {
        return new HueEntertainmentPro.Shared.Models.ProArea
        {
          Id = demo1Id,
          Name = "Q42 Star Demo",
          Connections = new List<HueEntertainmentPro.Shared.Models.BridgeGroupConnection>
           {
              new Shared.Models.BridgeGroupConnection
              {
                Bridge =  new Shared.Models.Bridge
                {
                  Id = Guid.Empty,
                  Name = "Q42 Star Demo",
                  Ip = "sterDemoLocations1",
                  StreamingClientKey = "sterDemoLocations1",
                  Username = "sterDemoLocations1"
                }
              },
          }
        };
      }

      var area = await dbContext.ProAreas
        .Include(x => x.ProAreaBridgeGroups).ThenInclude(bg => bg.Bridge)
        .FirstOrDefaultAsync(pa => pa.Id == req.Id);

      if (area == null)
        return null;

      return area.ToApiModel();
    }

    public async Task<IEnumerable<HueEntertainmentPro.Shared.Models.ProArea>> GetProAreas(CallContext context = default)
    {
      var areas = await dbContext.ProAreas
        .Include(x => x.ProAreaBridgeGroups).ThenInclude(bg => bg.Bridge)
        .ToListAsync();

      return areas.Select(area => area.ToApiModel());
    }
  }
}
