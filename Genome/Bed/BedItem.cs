using CQS.Genome.Gtf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.Bed
{
  public class BedItem : ISequenceRegion
  {
    public class Block
    {
      private BedItem parent;

      public Block(BedItem parent)
      {
        this.parent = parent;
      }

      public long Size { get; set; }

      public long Start { get; set; }

      public long ChromStart
      {
        get
        {
          return parent.Start + this.Start;
        }
      }

      public long ChromEnd
      {
        get
        {
          return this.ChromStart + this.Size - 1;
        }
      }
    }

    /// <summary>
    /// Required field.
    /// The name of the chromosome (e.g. chr3, chrY, chr2_random) or scaffold (e.g. scaffold10671).
    /// </summary>
    public string Seqname { get; set; }

    /// <summary>
    /// Required field.
    /// The starting position of the feature in the chromosome or scaffold. The first base in a chromosome is numbered 0.
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// Required field.
    /// The ending position of the feature in the chromosome or scaffold. The chromEnd base is not included in the display 
    /// of the feature. For example, the first 100 bases of a chromosome are defined as chromStart=0, chromEnd=100, and span the bases numbered 0-99.
    /// </summary>
    public long End { get; set; }

    /// <summary>
    /// Optional field.
    /// Defines the name of the BED line. This label is displayed to the left of the BED line in the Genome Browser 
    /// window when the track is open to full display mode or directly to the left of the item in pack mode.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Optional field.
    /// A score between 0 and 1000. If the track line useScore attribute is set to 1 for this annotation data set, 
    /// the score value will determine the level of gray in which this feature is displayed (higher numbers = darker gray). 
    /// This table shows the Genome Browser's translation of BED score values into shades of gray:
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Optional field.
    /// Defines the strand - either '+' or '-'.
    /// </summary>
    public char Strand { get; set; }

    /// <summary>
    /// Optional field.
    /// The starting position at which the feature is drawn thickly (for example, the start codon in gene displays).
    /// </summary>
    public long ThickStart { get; set; }

    /// <summary>
    /// Optional field.
    /// The ending position at which the feature is drawn thickly (for example, the stop codon in gene displays).
    /// </summary>
    public long ThickEnd { get; set; }

    /// <summary>
    /// Optional field.
    /// An RGB value of the form R,G,B (e.g. 255,0,0). If the track line itemRgb attribute is set to "On", 
    /// this RBG value will determine the display color of the data contained in this BED line. 
    /// NOTE: It is recommended that a simple color scheme (eight colors or less) be used with this attribute 
    /// to avoid overwhelming the color resources of the Genome Browser and your Internet browser.
    /// </summary>
    public string ItemRgb { get; set; }

    public List<Block> Blocks { get; private set; }

    /// <summary>
    /// Optional field.
    /// The number of blocks (exons) in the BED line.
    /// </summary>
    public int BlockCount
    {
      get
      {
        return Blocks.Count;
      }
      set
      {
        Blocks.Clear();
        for (int i = 0; i < value; i++)
        {
          Blocks.Add(new Block(this));
        }
      }
    }

    private void SetBlockValue(string value, Action<string, Block> setValue)
    {
      if (!string.IsNullOrWhiteSpace(value))
      {
        var parts = value.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
          while (i >= Blocks.Count)
          {
            Blocks.Add(new Block(this));
          }
          setValue(parts[i], Blocks[i]);
        }
      }
      else
      {
        foreach (var b in Blocks)
        {
          b.Size = 0;
        }
      }
    }

    private string GetBlockValue(Func<Block, string> getValue)
    {
      return (from b in Blocks
              select getValue(b)).Merge(',');
    }

    /// <summary>
    /// Optional field.
    /// A comma-separated list of the block sizes. The number of items in this list should correspond to blockCount.
    /// </summary>
    public string BlockSizes
    {
      get
      {
        return GetBlockValue(m => m.Size.ToString());
      }
      set
      {
        SetBlockValue(value, (m, n) => n.Size = long.Parse(m));
      }
    }

    /// <summary>
    /// Optional field.
    /// A comma-separated list of block starts. All of the blockStart positions should be calculated relative to chromStart. 
    /// The number of items in this list should correspond to blockCount.
    /// </summary>
    public string BlockStarts
    {
      get
      {
        return GetBlockValue(m => m.Start.ToString());
      }
      set
      {
        SetBlockValue(value, (m, n) => n.Start = long.Parse(m));
      }
    }

    public BedItem()
    {
      this.Seqname = String.Empty;
      this.Start = -1;
      this.End = -1;
      this.Name = String.Empty;
      this.Score = 0;
      this.Strand = '.';
      this.ThickStart = -1;
      this.ThickEnd = -1;
      this.ItemRgb = String.Empty;
      this.Blocks = new List<Block>();
      this.BlockCount = 0;
      this.BlockSizes = String.Empty;
      this.BlockStarts = String.Empty;
      this.Sequence = string.Empty;
    }

    public long Length
    {
      get
      {
        if (this.Start == -1 || this.End == -1)
        {
          return 0;
        }

        return Math.Abs(this.End - this.Start);
      }
    }

    public bool Contains(long position)
    {
      return position >= this.Start && position < this.End;
    }

    public string Sequence { get; set; }

    public GtfItem ToGtfItem()
    {
      var result = new GtfItem(this);
      result.Start = result.Start + 1;
      return result;
    }
  }
}
