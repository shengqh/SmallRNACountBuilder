using CQS.Genome.Feature;
using CQS.Genome.Sam;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNAMapperMicroRNA : SmallRNAMapperBiotype
  {
    private bool hasNTA;

    public SmallRNAMapperMicroRNA(ISmallRNACountProcessorOptions options, bool hasNTA) : base(SmallRNABiotype.miRNA, options)
    {
      this.hasNTA = hasNTA;
    }

    public override AcceptResult AcceptLocationPair(FeatureLocation floc, SAMAlignedLocation sloc)
    {
      var result = base.AcceptLocationPair(floc, sloc);

      if (!result.Accepted)
      {
        return result;
      }

      var offset = sloc.Offset(floc);
      result.Accepted = Options.Offsets.Contains(offset);

      return result;
    }

    public override void MapReadToFeature(List<FeatureLocation> features, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap)
    {
      base.MapReadToFeature(features, chrStrandReadMap);

      //For each query, keep the one with the best offset
      var fsls = (from m in features
                  from l in m.SamLocations
                  select l).GroupBy(m => m.SamLocation.Parent).ToList().ConvertAll(m => m.ToArray());

      //filter offset by priority
      foreach (var fsl in fsls)
      {
        if (fsl.Count() == 1)
        {
          continue;
        }

        var bestOffset = fsl.Min(m => Options.Offsets.IndexOf(m.Offset));
        foreach (var f in fsl)
        {
          if (Options.Offsets.IndexOf(f.Offset) != bestOffset)
          {
            f.FeatureLocation.SamLocations.Remove(f);
            f.SamLocation.Features.Remove(f.FeatureLocation);
          }
        }
      }

      //filter NTA
      if (hasNTA)
      {
        //remove all CCAA NTA which is designed for tRNA
        features.RemoveAll(m =>
        {
          m.SamLocations.RemoveAll(s => s.SamLocation.Parent.Qname.StringAfter(SmallRNAConsts.NTA_TAG).Equals("CCAA"));
          return m.SamLocations.Count == 0;
        });

        SmallRNAUtils.SelectBestMatchedNTA(features);
      }
    }
  }
}
