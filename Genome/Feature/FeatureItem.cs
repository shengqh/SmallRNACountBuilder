using CQS.Genome.SmallRNA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.Feature
{
  public class FeatureItem
  {
    public FeatureItem()
    {
      Locations = new List<FeatureLocation>();
      Name = string.Empty;
    }

    public List<FeatureLocation> Locations { get; private set; }

    public string Category { get; set; }

    public string Name { get; set; }

    public double GetEstimatedCount()
    {
      return Locations.Sum(m => m.GetEstimateCount());
    }

    public double GetEstimatedCount(Func<FeatureSamLocation, bool> accept)
    {
      return Locations.Sum(m => m.GetEstimateCount(accept));
    }

    public int QueryCount
    {
      get
      {
        return (from region in Locations
                from loc in region.SamLocations
                select loc.SamLocation.Parent).Distinct().Sum(m => m.QueryCount);
      }
    }

    public string DisplayLocations
    {
      get
      {
        return (from loc in Locations
                select loc.GetLocation()).Merge(",");
      }
    }

    public string Sequence
    {
      get
      {
        return Locations.Count > 0 ? Locations[0].Sequence : string.Empty;
      }
    }

    public override string ToString()
    {
      return Name;
    }

    public void CombineLocations()
    {
      //deal with the item with multiple regions but one of them contains others
      if (this.Locations.Count > 1)
      {
        var removed = new List<FeatureLocation>();
        for (int i = 0; i < this.Locations.Count; i++)
        {
          var regi = this.Locations[i];
          for (int j = i + 1; j < this.Locations.Count; j++)
          {
            var regj = this.Locations[j];
            if (removed.Contains(regj))
            {
              continue;
            }

            var con = regi.Contains(regj);
            if (con == 1)
            {
              //if i contains j and all mapped reads mapped to j, remove i. Small range is perfered.
              if (regj.ContainLocations(regi))
              {
                removed.Add(regi);
                break;
              }

              //if i contains j and all mapped reads from j were contained in i, remove j 
              if (regj.ContainLocations(regi))
              {
                removed.Add(regj);
                continue;
              }
            }
            else if (con == -1)
            {
              //if j contains i and all mapped reads mapped to i, remove j. Small range is perfered.
              if (regi.ContainLocations(regj))
              {
                removed.Add(regj);
                continue;
              }

              //if j contains i and all mapped reads from i were contained in j, remove i 
              if (regj.ContainLocations(regi))
              {
                removed.Add(regi);
                break;
              }
            }
          }
        }

        //remove the feature from SAMAlignedItem feature list.
        removed.ForEach(m => m.SamLocations.ForEach(l => l.SamLocation.Features.Remove(m)));

        this.Locations.RemoveAll(m => removed.Contains(m));
      }
    }
  }

  public static class FeatureItemExtension
  {
    public static void RemoveNTAReads(this List<FeatureItem> items)
    {
      foreach (var item in items)
      {
        foreach (var location in item.Locations)
        {
          location.SamLocations.RemoveAll(l => l.SamLocation.Parent.Qname.HasNTA());
        }
        item.Locations.RemoveAll(l => l.SamLocations.Count == 0);
      }
      items.RemoveAll(l => l.Locations.Count == 0);
    }

    public static List<FeatureItemGroup> GroupByIdenticalQuery(this IEnumerable<FeatureItem> items)
    {
      Func<FeatureItem, string> func = m =>
      {
        return (from r in m.Locations
                from l in r.SamLocations
                select l.SamLocation.Parent.Qname).Distinct().OrderBy(l => l).Merge(";");
      };

      return items.GroupByFunction(func);
    }

    public static List<FeatureItemGroup> GroupBySequence(this IEnumerable<FeatureItem> items)
    {
      Func<FeatureItem, string> func = m =>
      {
        if (string.IsNullOrEmpty(m.Sequence))
        {
          return m.GetHashCode().ToString();
        }
        else
        {
          return m.Sequence;
        }
      };

      return items.GroupByFunction(func);
    }

    public static List<FeatureItemGroup> GroupByFunction(this IEnumerable<FeatureItem> items, Func<FeatureItem, string> func, bool updateName = false)
    {
      var result = new List<FeatureItemGroup>();

      var dic = items.GroupBy(m => func(m)).ToList();
      foreach (var curItems in dic)
      {
        var group = new FeatureItemGroup();
        group.AddRange(from item in curItems orderby item.Name select item);
        if (updateName)
        {
          group.DisplayName = func(curItems.First());
        }
        result.Add(group);
      }

      return result;
    }

    public static List<FeatureItemGroup> ConvertToGroup(this IEnumerable<FeatureItem> items)
    {
      var result = new List<FeatureItemGroup>();

      foreach (var curItem in items)
      {
        var group = new FeatureItemGroup();
        group.Add(curItem);
        result.Add(group);
      }

      return result;
    }

  }
}