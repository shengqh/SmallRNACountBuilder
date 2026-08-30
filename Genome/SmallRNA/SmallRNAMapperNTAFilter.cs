using CQS.Genome.Feature;
using CQS.Genome.Sam;
using RCPA.Gui;
using System;
using System.Collections.Generic;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNAMapperNTAFilter : ProgressClass, IReadMapper
  {
    public string MapperName
    {
      get
      {
        return "NTAFilter";
      }
    }

    private bool hasNTA;
    private bool keepTrnaNTA;
    private bool keepMirnaNTA;

    private HashSet<string> cca;

    public SmallRNAMapperNTAFilter(bool hasNTA, bool keepTrnaNTA, bool keepMirnaNTA, HashSet<string> cca)
    {
      this.hasNTA = hasNTA;
      this.keepTrnaNTA = keepTrnaNTA;
      this.keepMirnaNTA = keepMirnaNTA;
      this.cca = cca;
    }

    public void MapReadToFeatureAndRemoveFromMap(List<FeatureLocation> features, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap)
    {
      if (hasNTA)
      {
        if (keepTrnaNTA)
        {
          //remove reads with NTA, except CCA/CCAA and CC in CCA file
          foreach (var strandMap in chrStrandReadMap.Values)
          {
            foreach (var locList in strandMap.Values)
            {
              locList.RemoveAll(k =>
              {
                if (!k.Parent.Qname.Contains(SmallRNAConsts.NTA_TAG))
                {
                  return false;
                }

                var nta = k.Parent.Qname.StringAfter(SmallRNAConsts.NTA_TAG);
                if (nta.Length == 0)
                {
                  return false;
                }
                if (nta.Equals("CCA") || nta.Equals("CCAA"))
                {
                  return false;
                }
                if (nta.Equals("CC") && cca.Contains(k.Parent.Qname.StringBefore(SmallRNAConsts.NTA_TAG)))
                {
                  return false;
                }
                return true;
              });
            }
          }
        }
        else if (keepMirnaNTA)
        {
          //remove reads with CCAA
          foreach (var strandMap in chrStrandReadMap.Values)
          {
            foreach (var locList in strandMap.Values)
            {
              locList.RemoveAll(k =>
              {
                if (!k.Parent.Qname.Contains(SmallRNAConsts.NTA_TAG))
                {
                  return false;
                }

                var nta = k.Parent.Qname.StringAfter(SmallRNAConsts.NTA_TAG);
                return nta.Equals("CCAA");
              });
            }
          }
        }
        else
        {
          //remove all reads with NTA
          foreach (var strandMap in chrStrandReadMap.Values)
          {
            foreach (var locList in strandMap.Values)
            {
              locList.RemoveAll(k => k.Parent.Qname.HasNTA());
            }
          }
        }
      }
    }

    public void MapReadToFeature(List<FeatureLocation> features, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap)
    {
    }
  }
}
