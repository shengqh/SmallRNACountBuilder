using CQS.Genome.Sam;
using CQS.Genome.SmallRNA;
using RCPA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.Mapping
{
  public abstract class AbstractCountProcessor<T> : AbstractThreadProcessor where T : ICountProcessorOptions
  {
    protected T options;

    private SmallRNACountMap _counts;
    public SmallRNACountMap Counts
    {
      get
      {
        if (_counts == null)
        {
          Progress.SetMessage("Reading count map ...");
          _counts = options.GetCountMap();
          Progress.SetMessage("Reading count map done ...");
        }
        return _counts;
      }
    }

    protected AbstractCountProcessor(T options)
    {
      this.options = options;
    }

    protected virtual List<SAMAlignedItem> ParseCandidates(string fileName, string outputFile, out List<QueryInfo> totalQueries)
    {
      return ParseCandidates(new string[] { fileName }.ToList(), outputFile, out totalQueries);
    }

    protected virtual List<SAMAlignedItem> ParseCandidates(IList<string> fileNames, string outputFile, out List<QueryInfo> totalQueries)
    {
      var candiateBuilder = GetCandidateBuilder();

      totalQueries = new List<QueryInfo>();

      var result = new List<SAMAlignedItem>();
      foreach (var fileName in fileNames)
      {
        Progress.SetMessage("processing file: " + fileName);

        List<QueryInfo> curQueries;
        var curResult = candiateBuilder.Build<SAMAlignedItem>(fileName, out curQueries);
        result.AddRange(curResult);
        totalQueries.AddRange(curQueries);
      }

      FilterAlignedItems(result);

      FillReadCount(result);

      result.ForEach(m => m.SortLocations());

      return result;
    }

    protected virtual void FillReadCount(List<SAMAlignedItem> result)
    {
      result.ForEach(m =>
      {
        m.QueryCount = Counts.GetCount(m.Qname, m.OriginalQname);
      });
    }

    protected virtual void FilterAlignedItems(List<SAMAlignedItem> result)
    {
      SmallRNAUtils.InitializeSmallRnaNTA(result);

      result.ForEach(l =>
      {
        foreach (var loc in l.Locations)
        {
          if (loc.Seqname.StartsWith("chr"))
          {
            loc.Seqname = loc.Seqname.StringAfter("chr");
          }
        }
      });
    }

    protected virtual ICandidateBuilder GetCandidateBuilder()
    {
      var candiateBuilder = options.GetCandidateBuilder();
      candiateBuilder.Progress = this.Progress;
      return candiateBuilder;
    }
  }
}
