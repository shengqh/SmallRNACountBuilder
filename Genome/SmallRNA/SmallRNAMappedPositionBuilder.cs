using CQS.Genome.Feature;
using RCPA.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNAMappedPositionBuilder
  {
    public static void Build(List<FeatureItemGroup> features, string title, string outputFile, Func<FeatureItemGroup, string> getName, bool positionByPercentage = false, int minimumReadCount = 5)
    {
      //var format = new MappedItemGroupXmlFileFormat();

      var groups = minimumReadCount > 0 ? features.Where(l => l.GetEstimatedCount() >= minimumReadCount).ToList() : features;

      groups.Sort((m1, m2) => m2.GetEstimatedCount().CompareTo(m1.GetEstimatedCount()));

      //groups.ForEach(l => Console.WriteLine("{0} => {1}", l.DisplayName, l.GetEstimatedCount()));

      using (StreamWriter sw = new StreamWriter(outputFile))
      {
        sw.WriteLine("File\tFeature\tStrand\tCount\tPosition\tPercentage");

        foreach (var group in groups)
        {
          Dictionary<long, double> positionCount = new Dictionary<long, double>();
          foreach (var item in group)
          {
            foreach (var region in item.Locations)
            {
              var regionLength = region.Length;
              foreach (var loc in region.SamLocations)
              {
                Dictionary<long, double> curCount = new Dictionary<long, double>();
                var estimatedCount = loc.SamLocation.Parent.GetEstimatedCount();

                var offsetStart = region.Strand == '+' ? loc.SamLocation.Start - region.Start : region.End - loc.SamLocation.End;
                var offsetEnd = region.Strand == '+' ? loc.SamLocation.End - region.Start : region.End - loc.SamLocation.Start;
                if (positionByPercentage)
                {
                  offsetStart = offsetStart * 100 / regionLength;
                  offsetEnd = offsetEnd * 100 / regionLength;
                }

                double v;
                for (var offset = offsetStart; offset <= offsetEnd; offset++)
                {
                  if (!curCount.TryGetValue(offset, out v) || v < estimatedCount)
                  {
                    curCount[offset] = estimatedCount;
                  }
                }

                foreach (var cc in curCount)
                {
                  double vv;
                  if (!positionCount.TryGetValue(cc.Key, out vv))
                  {
                    positionCount[cc.Key] = cc.Value;
                  }
                  else
                  {
                    positionCount[cc.Key] = vv + cc.Value;
                  }
                }
              }
            }
          }

          var groupName = getName(group);
          var allcount = group.GetEstimatedCount();
          var keys = positionCount.Keys.ToList();
          keys.Sort();
          foreach (var key in keys)
          {
            sw.WriteLine("{0}\t{1}\t{2}\t{3:0.##}\t{4}\t{5:0.00}",
              title,
              groupName,
              group[0].Locations[0].Strand,
              positionCount[key],
              key,
              positionCount[key] / allcount);
          }
        }
      }
    }
  }
}
