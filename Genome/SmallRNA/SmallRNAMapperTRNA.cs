using CQS.Genome.Feature;
using CQS.Genome.Sam;
using System;
using System.Collections.Generic;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNAMapperTRNA : SmallRNAMapperBiotypes
  {
    private bool allowNTA;
    private HashSet<string> allowedNTAs = new HashSet<string>(new[] { "CC", "CCA", "CCAA" });
    private HashSet<string> cca;

    public SmallRNAMapperTRNA(ISmallRNACountProcessorOptions options, bool allowNTA, HashSet<string> cca) :
      base(new SmallRNABiotype[] { SmallRNABiotype.tRNA, SmallRNABiotype.mt_tRNA }, options)
    {
      this.allowNTA = allowNTA;
      this.cca = cca;
    }

    public override void MapReadToFeature(List<FeatureLocation> features, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap)
    {
      base.MapReadToFeature(features, chrStrandReadMap);

      if (allowNTA)
      {
        //NTA has to be at the end of tRNA.
        features.RemoveAll(m =>
        {
          m.SamLocations.RemoveAll(l =>
          {
            var loc = l.SamLocation;
            if (!loc.Parent.Qname.Contains(SmallRNAConsts.NTA_TAG))
            {
              return false;
            }

            var nta = loc.Parent.Qname.StringAfter(SmallRNAConsts.NTA_TAG);
            if (nta.Length == 0)
            {
              return false;
            }

            if (loc.End != l.FeatureLocation.End || !allowedNTAs.Contains(nta))
            {
              return true;
            }

            if (nta.Equals("CC"))
            {
              return !cca.Contains(loc.Parent.Qname.StringBefore(SmallRNAConsts.NTA_TAG));
            }
            return false;
          });
          return m.SamLocations.Count == 0;
        });

        SmallRNAUtils.SelectBestMatchedNTA(features);
      }
      else
      {
        //all queries with NTA will be removed.
        features.RemoveAll(m =>
        {
          m.SamLocations.RemoveAll(l =>
          {
            var loc = l.SamLocation;
            if (!loc.Parent.Qname.Contains(SmallRNAConsts.NTA_TAG))
            {
              return false;
            }

            var nta = loc.Parent.Qname.StringAfter(SmallRNAConsts.NTA_TAG);
            return nta.Length > 0;
          });
          return m.SamLocations.Count == 0;
        });
      }
    }
  }
}
