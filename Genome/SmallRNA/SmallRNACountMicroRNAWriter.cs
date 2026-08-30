using CQS.Genome.Feature;
using RCPA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNACountMicroRNAWriter : IFileWriter<List<FeatureItemGroup>>
  {
    private List<long> offsets;

    public SmallRNACountMicroRNAWriter(List<long> offsets)
    {
      this.offsets = offsets;
    }

    public void WriteToFile(string fileName, List<FeatureItemGroup> mirnas)
    {
      var items = mirnas.OrderByDescending(m => m.GetEstimatedCount()).ToList();

      using (StreamWriter sw = new StreamWriter(fileName))
      {
        sw.WriteLine("miRNA\tLocation\tSequence\tTotalCount\t" + (from p in this.offsets select "Count" + p.ToString()).Merge("\t"));

        foreach (var mirna in items)
        {
          var counts = (from p in this.offsets
                        select (from m in mirna select m.GetEstimatedCount(l => l.Offset == p)).Sum()).ToList();

          sw.WriteLine("{0}\t{1}\t{2}\t{3:0.##}\t{4}",
            mirna.DisplayNameWithoutCategory,
            mirna.DisplayLocations,
            mirna[0].Sequence, //since the mirna in the group should contains identical sequence, only one sequence will be exported.
            counts.Sum(),
            counts.ConvertAll(m => string.Format("{0:0.##}", m)).Merge("\t"));
        }
      }
    }
  }
}
