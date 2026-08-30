using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNACountItem
  {
    public string Qname { get; set; }
    public int SequenceLength { get; set; }
    public int Count { get; set; }
  }

  public class SmallRNACountMap : CountMap
  {
    public Dictionary<string, SmallRNACountItem> ItemMap { get; private set; }

    public SmallRNACountMap() { }

    public SmallRNACountMap(string countFile) : base(countFile) { }

    protected override void ReadCountFile(string countFile)
    {
      int nline = 0;
      using (var sr = new StreamReader(countFile))
      {
        string line = sr.ReadLine();
        while ((line = sr.ReadLine()) != null)
        {
          nline++;
        }
      }

      this.ItemMap = new Dictionary<string, SmallRNACountItem>(nline);
      var list = new List<SmallRNACountItem>(nline);
      using (var sr = new StreamReader(countFile))
      {
        string line = sr.ReadLine();
        while ((line = sr.ReadLine()) != null)
        {
          var parts = line.Split('\t');
          list.Add(new SmallRNACountItem()
          {
            Qname = parts[0],
            Count = int.Parse(parts[1]),
            SequenceLength = parts[2].Length
          });
        }
      }

      foreach (var l in list)
      {
        var originalQueryName = l.Qname.StringBefore(SmallRNAConsts.NTA_TAG);
        Counts[l.Qname] = l.Count;
        Counts[originalQueryName] = l.Count;
        ItemMap[originalQueryName] = l;
      }
    }

    public override int GetTotalCount()
    {
      return (from c in Counts
              where !c.Key.Contains(SmallRNAConsts.NTA_TAG)
              select c.Value).Sum();
    }
  }
}
