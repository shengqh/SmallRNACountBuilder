using CQS.Genome.Sam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CQS.Genome.Mapping
{
  public class MappedItemGroup : List<MappedItem>
  {
    private string _displayLocation;
    private string _displayName;
    private string _queryNames = string.Empty;

    public MappedItemGroup()
    {
    }

    public MappedItemGroup(MappedItem item)
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

    public string DisplayName
    {
      get
      {
        if (!string.IsNullOrEmpty(_displayName))
        {
          return _displayName;
        }
        return (from id in this
                select id.Name).Merge(";");
      }
      set { _displayName = value; }
    }

    public string DisplayLocation
    {
      get
      {
        if (!string.IsNullOrEmpty(_displayLocation))
        {
          return _displayLocation;
        }
        return (from id in this
                select id.Locations).Merge(";");
      }
      set { _displayLocation = value; }
    }

    public string DisplaySequence
    {
      get
      {
        return (from id in this
                select id.Sequence).Merge(";");
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
                from pos in id.MappedRegions
                from l in pos.AlignedLocations
                select l.Parent).Distinct().Sum(m => m.QueryCount);
      }
    }

    public override string ToString()
    {
      return Name;
    }

    public List<SAMAlignedLocation> GetAlignedLocations()
    {
      return (from id in this
              from pos in id.MappedRegions
              from q in pos.AlignedLocations
              select q).Distinct().OrderBy(m => m.Parent.Qname).ToList();
    }

    public void InitializeQueryNames()
    {
      _queryNames = (from id in this
                     from pos in id.MappedRegions
                     from l in pos.AlignedLocations
                     select l.Parent.Qname).Distinct().OrderBy(l => l).Merge(";");
    }
  }

  public static class MappedItemGroupExtension
  {
    public static List<SAMAlignedItem> GetQueries(this IEnumerable<MappedItemGroup> items)
    {
      return (from mirna in items
              from item in mirna.GetAlignedLocations()
              select item.Parent).Distinct().OrderBy(m => m.Qname).ToList();
    }

    public static void RemoveRead(this IEnumerable<MappedItemGroup> items, string qname)
    {
      foreach (var m in items)
      {
        m.ForEach(n => n.MappedRegions.ForEach(l => l.AlignedLocations.RemoveAll(g => g.Parent.Qname.Equals(qname))));
      }
    }

    private static readonly Regex TRNA = new Regex(@"(?:chr){0,1}([^.]+)\.tRNA(\d+)");

    public static void SortTRna(this List<MappedItemGroup> items)
    {
      if (items.All(m => TRNA.Match(m.Name).Success))
      {
        GenomeUtils.SortChromosome(items, m => TRNA.Match(m.Name).Groups[1].Value,
          m => int.Parse(TRNA.Match(m.Name).Groups[2].Value));
      }
    }
  }
}