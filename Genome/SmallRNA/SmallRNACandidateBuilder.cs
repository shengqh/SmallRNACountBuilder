using CQS.Genome.Mapping;
using CQS.Genome.Sam;
using System;
using System.Collections.Generic;
using System.IO;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNACandidateBuilder : SAMAlignedItemCandidateBuilder
  {
    private SmallRNACountProcessorOptions options;
    private HashSet<string> rangeQueries;

    public SmallRNACandidateBuilder(SmallRNACountProcessorOptions options) : base(options)
    {
      this.options = options;
    }

    protected override bool AcceptQueryName(string qname)
    {
      return rangeQueries.Contains(qname);
    }

    protected override List<T> DoBuild<T>(string fileName, out List<QueryInfo> totalQueries)
    {
      Progress.SetMessage("Find queries overlapped with coordinates...");
      rangeQueries = new HashSet<string>();

      var miss1file = options.CoordinateFile + ".miss1";
      var miss0file = options.CoordinateFile + ".miss0";

      if (File.Exists(miss1file) && File.Exists(miss0file) && !options.T2cAsNoPenaltyMutation)
      {
        var miss1Queries = new HashSet<string>();
        using (var sr = SAMFactory.GetReader(fileName, true, miss1file))
        {
          string line;
          while ((line = sr.ReadLine()) != null)
          {
            if (line.StartsWith("@"))
            {
              continue;
            }
            var qname = line.StringBefore("\t");
            miss1Queries.Add(qname);
          }
        }
        Progress.SetMessage("Miss 1 queries : {0}", miss1Queries.Count);

        var miss0Queries = new HashSet<string>();
        using (var sr = SAMFactory.GetReader(fileName, true, miss0file))
        {
          string line;
          while ((line = sr.ReadLine()) != null)
          {
            if (line.StartsWith("@"))
            {
              continue;
            }
            if (line.Contains("NM:i:0"))
            {
              var qname = line.StringBefore("\t");
              miss0Queries.Add(qname);
            }
          }
        }
        Progress.SetMessage("Miss 0 queries : {0}", miss0Queries.Count);
        rangeQueries.UnionWith(miss1Queries);
        rangeQueries.UnionWith(miss0Queries);
        miss1Queries.Clear();
        miss0Queries.Clear();
      }
      else
      {
        using (var sr = SAMFactory.GetReader(fileName, true, options.CoordinateFile))
        {
          string line;
          while ((line = sr.ReadLine()) != null)
          {
            if (line.StartsWith("@"))
            {
              continue;
            }
            var qname = line.StringBefore("\t");
            rangeQueries.Add(qname);
          }
        }
      }

      Progress.SetMessage("{0} queries overlaped with coordinates.", rangeQueries.Count);

      return base.DoBuild<T>(fileName, out totalQueries);
    }
  }
}
