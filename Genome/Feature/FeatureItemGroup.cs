using CQS.Genome.Sam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CQS.Genome.Feature
{
  public class FeatureItemGroup : List<FeatureItem>
  {
    private string _displayLocations = null;
    private string _displaySequence = null;
    private string _displayName = null;
    private string _queryNames = string.Empty;

    public FeatureItemGroup()
    {
    }

    public FeatureItemGroup(FeatureItem item)
    {
      Add(item);
    }

    public string Name
    {
      get
      {
        return Count == 0 ? string.Empty : this[0].Name;
      }
    }

    public double GetEstimatedCount()
    {
      return this.Sum(m => m.GetEstimatedCount());
    }

    public double GetEstimatedCount(Func<FeatureSamLocation, bool> accept)
    {
      return this.Sum(m => m.GetEstimatedCount(accept));
    }

    public string DisplayName
    {
      get
      {
        if (_displayName != null)
        {
          return _displayName;
        }
        return (from id in this
                select id.Name).Merge(";");
      }
      set { _displayName = value; }
    }

    public string DisplayNameWithoutCategory
    {
      get
      {
        return (from id in this
                let name = string.IsNullOrEmpty(id.Category) ? id.Name : id.Name.StringAfter(":")
                select name).Merge(";");
      }
    }

    public string DisplayLocations
    {
      get
      {
        if (_displayLocations != null)
        {
          return _displayLocations;
        }
        return (from id in this
                select id.DisplayLocations).Merge(";");
      }
      set { _displayLocations = value; }
    }

    public string DisplaySequence
    {
      get
      {
        if (_displaySequence != null)
        {
          return _displaySequence;
        }

        if (this.All(l => string.IsNullOrEmpty(l.Sequence)))
        {
          return string.Empty;
        }
        else
        {
          return (from id in this select id.Sequence).Merge(";");
        }
      }
      set
      {
        _displaySequence = value;
      }
    }

    public string QueryNames
    {
      get
      {
        if (string.IsNullOrEmpty(_queryNames))
        {
          InitializeQueryNames();
        }
        return _queryNames;
      }
    }

    public int QueryCount
    {
      get
      {
        return (from id in this
                from pos in id.Locations
                from l in pos.SamLocations
                select l.SamLocation.Parent).Distinct().Sum(m => m.QueryCount);
      }
    }

    public override string ToString()
    {
      return Name;
    }

    public List<SAMAlignedLocation> GetAlignedLocations()
    {
      return (from id in this
              from pos in id.Locations
              from q in pos.SamLocations
              select q.SamLocation).Distinct().OrderBy(m => m.Parent.Qname).ToList();
    }

    public void InitializeQueryNames()
    {
      _queryNames = (from id in this
                     from pos in id.Locations
                     from l in pos.SamLocations
                     select l.SamLocation.Parent.Qname).Distinct().OrderBy(l => l).Merge(";");
    }
  }

  public static class FeatureItemGroupExtension
  {
    public static List<SAMAlignedItem> GetQueries(this IEnumerable<FeatureItemGroup> items)
    {
      var set = new HashSet<SAMAlignedItem>();
      foreach (var mirna in items)
      {
        foreach (var item in mirna.GetAlignedLocations())
        {
          set.Add(item.Parent);
        }
      }

      var result = set.ToList();
      result.SortByName();
      return result;
    }

    public static void RemoveRead(this IEnumerable<FeatureItemGroup> items, string qname)
    {
      foreach (var m in items)
      {
        m.ForEach(n => n.Locations.ForEach(l => l.SamLocations.RemoveAll(g => g.SamLocation.Parent.Qname.Equals(qname))));
      }
    }

    private static readonly Regex TRNA = new Regex(@"(?:chr){0,1}([^.]+)\.tRNA(\d+)");

    public static void SortTRna(this List<FeatureItemGroup> items)
    {
      if (items.All(m => TRNA.Match(m.Name).Success))
      {
        GenomeUtils.SortChromosome(items, m => TRNA.Match(m.Name).Groups[1].Value,
          m => int.Parse(TRNA.Match(m.Name).Groups[2].Value));
      }
    }
  }
}