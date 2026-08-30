using System;

namespace CQS.Genome
{
  public class SequenceRegion : ISequenceRegion, IComparable<SequenceRegion>
  {
    public SequenceRegion()
    {
      this.Seqname = string.Empty;
      this.Start = 0;
      this.End = 0;
      this.Name = string.Empty;
      this.Strand = '+';
      this.Sequence = string.Empty;
    }

    public SequenceRegion(ISequenceRegion source)
    {
      this.Seqname = source.Seqname;
      this.Start = source.Start;
      this.End = source.End;
      this.Name = source.Name;
      this.Strand = source.Strand;
      this.Sequence = source.Sequence;
    }

    public string Seqname { get; set; }

    public long Start { get; set; }

    public long End { get; set; }

    public string Name { get; set; }

    public char Strand { get; set; }

    public virtual long Length
    {
      get
      {
        return this.End - this.Start + 1;
      }
    }

    public int CompareTo(SequenceRegion other)
    {
      var result = this.Seqname.CompareTo(other.Seqname);
      if (0 == result)
      {
        result = this.Start.CompareTo(other.Start);
      }
      return result;
    }

    public bool Contains(long position)
    {
      return position >= this.Start && position <= this.End;
    }

    public bool HasOverlap(ISequenceRegion loc)
    {
      if (loc == null)
      {
        return false;
      }

      return this.Contains(loc.Start) || loc.Contains(this.Start);
    }

    public void Union(ISequenceRegion loc)
    {
      this.Start = Math.Min(this.Start, loc.Start);
      this.End = Math.Max(this.End, loc.End);
    }

    public string Sequence { get; set; }

    public override string ToString()
    {
      return string.Format("{0}:{1}-{2}:{3}", Seqname, Start, End, Strand);
    }
  }
}
