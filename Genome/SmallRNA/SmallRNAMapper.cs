using CQS.Genome.Feature;
using CQS.Genome.Sam;
using RCPA.Gui;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNAMapper : ProgressClass, IReadMapper
  {
    public Func<FeatureLocation, bool> Accept { get; set; }

    public ISmallRNACountProcessorOptions Options { get; }

    public string MapperName { get; set; }

    public SmallRNAMapper(ISmallRNACountProcessorOptions options)
    {
      this.MapperName = "Unknown";
      this.Options = options;
      this.Accept = m => true;
    }

    public SmallRNAMapper(string mapperName, ISmallRNACountProcessorOptions options, Func<FeatureLocation, bool> accept)
    {
      this.MapperName = mapperName;
      this.Options = options;
      this.Accept = accept;
    }

    private AcceptResult CheckNoPenaltyMutation(FeatureLocation floc, SAMAlignedLocation sloc)
    {
      if (sloc.NumberOfNoPenaltyMutation > 0)
      {
        var polys = sloc.GetGsnapMismatches();
        var mismatch = 0;
        if (floc.Strand == sloc.Strand) //the non-penalty mutation has to be T2C
        {
          mismatch = polys.Count(m => m.RefAllele != 'T' || m.SampleAllele != 'C');
        }
        else
        {
          mismatch = polys.Count(m => m.RefAllele != 'A' || m.SampleAllele != 'G');
        }

        var nnpm = sloc.NumberOfMismatch + sloc.NumberOfNoPenaltyMutation - mismatch;
        if (mismatch > Options.MaximumMismatch || nnpm > Options.MaximumNoPenaltyMutationCount)
        {
          return new AcceptResult()
          {
            Accepted = false
          };
        }

        return new AcceptResult()
        {
          Accepted = true,
          NumberOfMismatch = mismatch,
          NumberOfNoPenaltyMutation = sloc.NumberOfMismatch + sloc.NumberOfNoPenaltyMutation - mismatch
        };
      }
      else if (sloc.NumberOfMismatch > Options.MaximumMismatch)
      {
        return new AcceptResult()
        {
          Accepted = false
        };
      }
      else
      {
        return new AcceptResult()
        {
          Accepted = true,
          NumberOfMismatch = sloc.NumberOfMismatch,
          NumberOfNoPenaltyMutation = 0
        };
      }
    }

    public virtual AcceptResult AcceptLocationPair(FeatureLocation floc, SAMAlignedLocation sloc)
    {
      var result = CheckNoPenaltyMutation(floc, sloc);
      if (!result.Accepted)
      {
        return result;
      }

      result.OverlapPercentage = floc.OverlapPercentage(sloc);
      result.Accepted = result.OverlapPercentage > 0 && result.OverlapPercentage >= Options.MinimumOverlapPercentage;

      return result;
    }

    public virtual void MapReadToFeature(List<FeatureLocation> features, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap)
    {
      if (chrStrandReadMap.Count == 0 || features.Count == 0)
      {
        return;
      }

      //mapping to genome, considering offset limitation
      //Progress.SetRange(0, smallRNAs.Count);
      foreach (var smallRNA in features)
      {
        //Progress.Increment(1);
        Dictionary<char, List<SAMAlignedLocation>> curMatchedMap;

        if (!chrStrandReadMap.TryGetValue(smallRNA.Seqname, out curMatchedMap))
        {
          continue;
        }

        //mapped query must have same oritation with miRNA defined at gff or bed file.
        var matches = curMatchedMap[smallRNA.Strand];
        foreach (var m in matches)
        {
          var r = AcceptLocationPair(smallRNA, m);
          if (r.Accepted)
          {
            var fsl = new FeatureSamLocation(smallRNA);
            fsl.SamLocation = m;
            fsl.NumberOfMismatch = r.NumberOfMismatch;
            fsl.NumberOfNoPenaltyMutation = r.NumberOfNoPenaltyMutation;
            fsl.OverlapPercentage = r.OverlapPercentage;
          }
        }
      }
    }

    public virtual void MapReadToFeatureAndRemoveFromMap(List<FeatureLocation> allFeatures, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap)
    {
      var features = allFeatures.Where(l => Accept(l)).ToList();

      Progress.SetMessage("Mapping reads to {0} {1} entries.", features.Count, MapperName);
      if (features.Count > 0)
      {
        MapReadToFeature(features, chrStrandReadMap);

        var reads = SmallRNAUtils.GetMappedReads(features);
        Progress.SetMessage("There are {0} SAM entries mapped to {1} entries.", reads.Count, MapperName);

        SmallRNAUtils.RemoveReadsFromMap(chrStrandReadMap, reads);
      }
      else
      {
        Progress.SetMessage("There are 0 SAM entries mapped to {0} entries.", MapperName);
      }
    }
  }
}
