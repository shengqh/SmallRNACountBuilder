using CQS.Genome.Sam;
using RCPA.Gui;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.Mapping
{
  public abstract class AbstractSAMAlignedItemCandidateBuilder : ProgressClass, ICandidateBuilder
  {
    protected ISAMFormat _format;

    protected ISAMAlignedItemParserOptions _options;

    /// <summary>
    /// Constructor of SAMAlignedItemCandidateBuilder
    /// </summary>
    /// <param name="engineType">1:bowtie1, 2:bowtie2, 3:bwa</param>
    public AbstractSAMAlignedItemCandidateBuilder(int engineType)
    {
      this._options = new SAMAlignedItemParserOptions()
      {
        EngineType = engineType,
        MaximumMismatch = int.MaxValue,
        MinimumReadLength = 0
      };
    }

    public AbstractSAMAlignedItemCandidateBuilder(ISAMAlignedItemParserOptions options)
    {
      this._options = options;
    }

    public virtual List<T> Build<T>(string fileName, out List<QueryInfo> totalQueries) where T : SAMAlignedItem, new()
    {
      var samlist = DoBuild<T>(fileName, out totalQueries);

      return DoAddCompleted(samlist);
    }

    protected abstract List<T> DoBuild<T>(string fileName, out List<QueryInfo> totalQueries) where T : SAMAlignedItem, new();

    protected virtual List<T> DoAddCompleted<T>(List<T> samlist) where T : SAMAlignedItem, new()
    {
      if (_options.EngineType == 4 || _options.GetSAMFormat().HasAlternativeHits || samlist.Count == 0)
      {
        return samlist;
      }

      Progress.ShowCurrentMemory("Sorting mapped reads by name");
      SAMUtils.SortByName(samlist);

      Progress.ShowCurrentMemory("Merge reads from same query");
      var result = new List<T>();
      result.Add(samlist[0]);
      T last = samlist[0];

      for (int i = 1; i < samlist.Count; i++)
      {
        var sam = samlist[i];
        if (!last.Qname.Equals(sam.Qname))
        {
          last = sam;
          result.Add(last);
        }
        else
        {
          last.AddLocations(sam.Locations);
          sam.ClearLocations();
        }
      }

      samlist.Clear();
      samlist = null;

      Progress.ShowCurrentMemory("Keep unique location");
      KeepUniqueLocation<T>(result);

      Progress.ShowCurrentMemory(string.Format("Total {0} read(s) mapped", result.Count));

      return result;
    }

    protected virtual void KeepUniqueLocation<T>(List<T> result) where T : SAMAlignedItem, new()
    {
      foreach (var sam in result)
      {
        //Filter location by number of miss cleavage
        var minMiss = sam.Locations.Min(l => l.NumberOfMismatch);
        var locs = new List<SAMAlignedLocation>(sam.Locations);
        foreach (var loc in locs)
        {
          if (loc.NumberOfMismatch != minMiss)
          {
            sam.RemoveLocation(loc);
          }
        }

        //Keep only one read with same start location
        var grp = sam.Locations.GroupBy(m => m.GetKey()).Where(m => m.Count() > 1).ToArray();
        if (grp.Length > 0)
        {
          foreach (var g in grp)
          {
            var gg = g.ToArray();
            for (int l = gg.Length - 1; l > 0; l--)
            {
              sam.RemoveLocation(gg[l]);
            }
          }
        }
      }
    }
  }
}
