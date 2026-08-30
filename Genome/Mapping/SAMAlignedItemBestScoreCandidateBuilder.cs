using CQS.Genome.Sam;
using System.Collections.Generic;

namespace CQS.Genome.Mapping
{
  public class SAMAlignedItemBestScoreCandidateBuilder : SAMAlignedItemCandidateBuilder
  {
    public SAMAlignedItemBestScoreCandidateBuilder(ISAMAlignedItemParserOptions options) : base(options) { }

    protected override List<T> DoAddCompleted<T>(List<T> samlist)
    {
      if (_options.GetSAMFormat().HasAlternativeHits || samlist.Count == 0)
      {
        return samlist;
      }

      Progress.SetMessage("Sorting mapped reads by name and score...");
      samlist.SortByNameAndScore(_options.GetSAMFormat());

      Progress.SetMessage("Merge reads from same query...");
      var result = new List<T> { samlist[0] };
      var last = samlist[0];

      for (var i = 1; i < samlist.Count; i++)
      {
        var sam = samlist[i];
        if (!last.Qname.Equals(sam.Qname))
        {
          last = sam;
          result.Add(last);
        }
        else if (last.AlignmentScore == sam.AlignmentScore)
        {
          last.AddLocations(sam.Locations);
          sam.ClearLocations();

        }
      }

      samlist.Clear();
      samlist = null;

      KeepUniqueLocation<T>(result);
      Progress.SetMessage("Total {0} read(s) mapped.", result.Count);

      return result;
    }
  }
}
