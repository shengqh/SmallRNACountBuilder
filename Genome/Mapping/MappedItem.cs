using CQS.Genome.Sam;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.Mapping
{
  public class MappedItem
  {
    public MappedItem()
    {
      MappedRegions = new List<SequenceRegionMapped>();
      Name = string.Empty;
    }

    public List<SequenceRegionMapped> MappedRegions { get; private set; }

    public string Name { get; set; }

    public double GetEstimatedCount()
    {
      return MappedRegions.Sum(m => m.GetEstimatedCount());
    }

    public double GetEstimateCount(Func<SAMAlignedLocation, bool> func)
    {
      return MappedRegions.Sum(m => m.GetEstimatedCount(func));
    }

    public double QueryCount
    {
      get
      {
        return (from region in MappedRegions
                from loc in region.AlignedLocations
                select loc.Parent).Distinct().Sum(m => m.QueryCount);
      }
    }

    public string Locations
    {
      get
      {
        return (from loc in MappedRegions
                select loc.Region.GetLocation()).Merge(",");
      }
    }

    public string Sequence
    {
      get
      {
        return (from loc in MappedRegions
                select loc.Region.Sequence).Merge(",");
      }
    }

    public override string ToString()
    {
      return Name;
    }

    public void FilterLocations(HashSet<long> allowedOffsets)
    {
      foreach (var region in MappedRegions)
      {
        region.AlignedLocations.RemoveAll(loc =>
        {
          var offset = loc.Offset(region.Region);
          return !allowedOffsets.Contains(offset);
        });
      }

      MappedRegions.RemoveAll(region => region.AlignedLocations.Count == 0);
    }
  }

  public static class MappedItemExtension
  {
    public static List<MappedItemGroup> GroupByIdenticalQuery(this List<MappedItem> items)
    {
      var dic = items.GroupBy(m => (from r in m.MappedRegions
                                    from l in r.AlignedLocations
                                    select l.Parent.Qname).Distinct().OrderBy(l => l).Merge(";")).ToList();
      var result = new List<MappedItemGroup>();
      foreach (var curItems in dic)
      {
        var group = new MappedItemGroup();
        group.AddRange(from item in curItems orderby item.Name select item);
        result.Add(group);
      }

      return result;
    }
  }
}