using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.Feature
{
  public class FeatureLocation : SequenceRegion
  {
    public FeatureLocation()
    {
      SamLocations = new List<FeatureSamLocation>();
      Category = string.Empty;
      Name = string.Empty;
    }

    public FeatureLocation(ISequenceRegion source)
      : base(source)
    {
      this.SamLocations = new List<FeatureSamLocation>();
    }

    public string Category { get; set; }

    public List<FeatureSamLocation> SamLocations { get; set; }

    public double GetEstimateCount(Func<FeatureSamLocation, bool> accept)
    {
      return SamLocations.Where(m => accept(m)).Sum(m => m.SamLocation.Parent.GetEstimatedCount());
    }

    public double GetEstimateCount()
    {
      return SamLocations.Sum(m => m.SamLocation.Parent.GetEstimatedCount());
    }

    public int QueryCount
    {
      get
      {
        return SamLocations.Sum(m => m.SamLocation.Parent.QueryCount);
      }
    }

    public int QueryCountBeforeFilter { get; set; }

    public double PValue { get; set; }

    /// <summary>
    /// If current feature location contains all sam locations of another feature location
    /// </summary>
    /// <param name="another"></param>
    /// <returns></returns>
    public bool ContainLocations(FeatureLocation another)
    {
      return another.SamLocations.All(m => this.SamLocations.Any(l => l.SamLocation == m.SamLocation));
    }
  }

  public static class FeatureLocationExtension
  {
    public static List<FeatureItem> GroupByName(this IEnumerable<FeatureLocation> locations)
    {
      var result = new List<FeatureItem>();

      foreach (var curregions in locations.GroupBy(m => m.Name).ToList())
      {
        var mi = new FeatureItem();
        mi.Name = curregions.Key;
        mi.Category = curregions.First().Category;
        mi.Locations.AddRange(curregions);
        result.Add(mi);
      }

      return result;
    }
  }
}
