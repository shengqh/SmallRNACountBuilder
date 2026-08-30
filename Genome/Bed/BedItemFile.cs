using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CQS.Genome.Bed
{
  public class BedItemFile<T> : AbstractTableFile<T> where T : BedItem, new()
  {
    public bool HasHeader { get; set; }

    public int MaxColumn { get; set; }

    public BedItemFile(int maxColumn = int.MaxValue)
      : base()
    {
      this.HasHeader = false;
      this.MaxColumn = maxColumn;
    }

    public BedItemFile(string filename, int maxColumn = int.MaxValue)
      : base(filename)
    {
      this.HasHeader = false;
      this.MaxColumn = maxColumn;
    }

    protected override void DoAfterOpen()
    {
      base.DoAfterOpen();
      if (this.HasHeader)
      {
        base.reader.ReadLine();
      }
    }
    /// <summary>
    /// at least contains three colomns : chromosome, start and end
    /// </summary>
    protected override int MinLength
    {
      get
      {
        return 3;
      }
    }

    public int HeaderColumnCount
    {
      get
      {
        return 6;
      }
    }

    public string GetHeader()
    {
      return "chrom\tstart\tend\tname\tscore\tstrand";
    }

    public string GetValue(T item)
    {
      return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
        item.Seqname,
        item.Start,
        item.End,
        item.Name,
        item.Score,
        item.Strand);
    }

    public void WriteToFile(string filename, IList<T> items)
    {
      using (var sw = new StreamWriter(filename))
      {
        foreach (var item in items)
        {
          sw.WriteLine(GetValue(item));
        }
      }
    }

    protected override Dictionary<int, Action<string, T>> GetIndexActionMap()
    {
      var result = new Dictionary<int, Action<string, T>>();

      result[0] = (m, n) => n.Seqname = m;
      result[1] = (m, n) => n.Start = long.Parse(m);
      result[2] = (m, n) => n.End = long.Parse(m);
      result[3] = (m, n) => n.Name = m;
      result[4] = (m, n) => n.Score = double.Parse(m);
      result[5] = (m, n) => n.Strand = m[0];
      result[6] = (m, n) => n.ThickStart = long.Parse(m);
      result[7] = (m, n) => n.ThickEnd = long.Parse(m);
      result[8] = (m, n) => n.ItemRgb = m;
      result[9] = (m, n) => n.BlockCount = int.Parse(m);
      result[10] = (m, n) => n.BlockSizes = m;
      result[11] = (m, n) => n.BlockStarts = m;

      if (this.MaxColumn != int.MaxValue)
      {
        var resultcount = result.Keys.Max();
        for (int i = MaxColumn; i <= resultcount; i++)
        {
          result.Remove(i);
        }
      }

      return result;
    }
  }
}
