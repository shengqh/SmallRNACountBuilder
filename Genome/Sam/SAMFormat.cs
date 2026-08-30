using System;
using System.Text.RegularExpressions;

namespace CQS.Genome.Sam
{
  public abstract class SAMFormat : ISAMFormat
  {
    public static readonly int OptionStartIndex = 11;
    public static readonly ISAMFormat Bowtie1 = new Bowtie1Format();
    public static readonly ISAMFormat Bowtie2 = new Bowtie2Format();
    public static readonly ISAMFormat Bwa = new BwaFormat();

    private readonly bool _hasAlternativeHits;
    private readonly string _name;
    private readonly string _numberOfMissmatchKey;
    private readonly string _scoreKey;

    protected SAMFormat(string name, bool hasAlternativeHits, string numberOfMissmatchName, string scoreName)
    {
      _name = name;
      _hasAlternativeHits = hasAlternativeHits;
      _numberOfMissmatchKey = numberOfMissmatchName + ":i:";
      _scoreKey = scoreName + ":i:";
    }

    public virtual string Name
    {
      get { return _name; }
    }

    public virtual bool HasAlternativeHits
    {
      get { return _hasAlternativeHits; }
    }

    public virtual string GetMismatchPositions(string[] parts)
    {
      return GetOptionValue(parts, "MD:Z:", false);
    }

    public virtual int GetNumberOfMismatch(string[] parts)
    {
      return int.Parse(GetOptionValue(parts, _numberOfMissmatchKey));
    }

    public virtual int GetAlignmentScore(string[] parts)
    {
      return int.Parse(GetOptionValue(parts, _scoreKey));
    }

    public virtual void ParseAlternativeHits(string[] parts, SAMAlignedItem target)
    {
    }

    public abstract int CompareScore(double score1, double score2);

    protected string GetOptionValue(string[] parts, string key, bool throwException = true)
    {
      for (var i = OptionStartIndex; i < parts.Length; i++)
      {
        if (parts[i].StartsWith(key))
        {
          return parts[i].Substring(key.Length);
        }
      }

      if (throwException)
      {
        throw new Exception(string.Format("data error, cannot find {0} value in query {1} by parser {2}",
          key,
          parts[0],
          Name));
      }

      return string.Empty;
    }
  }

  public class Bowtie1Format : SAMFormat
  {
    public Bowtie1Format()
      : base("Bowtie1", false, "NM", "NM")
    {
    }

    public override int CompareScore(double score1, double score2)
    {
      return score1.CompareTo(score2);
    }
  }

  public class Bowtie2Format : SAMFormat
  {
    public Bowtie2Format()
      : base("Bowtie2", false, "XM", "AS")
    {
    }

    public override int CompareScore(double score1, double score2)
    {
      return score2.CompareTo(score1);
    }
  }

  public class BwaFormat : SAMFormat
  {
    private readonly Regex _reg = new Regex(@"(.+?),([+-])(\d+?),(.+?),(\d+)");

    public BwaFormat()
      : base("Bwa", true, "XM", "AS")
    {
    }

    public override void ParseAlternativeHits(string[] parts, SAMAlignedItem item)
    {
      var countstr = GetOptionValue(parts, "X0:i:", false);
      if (string.IsNullOrEmpty(countstr))
      {
        return;
      }

      var count = int.Parse(countstr) - 1;
      if (count == 0)
      {
        return;
      }

      var xaz = GetOptionValue(parts, "XA:Z:", false);
      if (string.IsNullOrEmpty(xaz))
      {
        return;
      }

      var match = _reg.Match(xaz);
      for (var i = 0; i < count; i++)
      {
        var loc = new SAMAlignedLocation(item)
        {
          Seqname = match.Groups[1].Value,
          Strand = match.Groups[2].Value[0],
          Start = long.Parse(match.Groups[3].Value)
        };
        loc.End = loc.Start + item.Locations[0].Length - 1;
        loc.Cigar = match.Groups[4].Value;
        loc.NumberOfMismatch = int.Parse(match.Groups[5].Value);
        item.AddLocation(loc);
      }
    }

    public override int GetAlignmentScore(string[] parts)
    {
      string v = GetOptionValue(parts, "AS:i:", false);
      return string.IsNullOrEmpty(v) ? 0 : int.Parse(v);
    }

    public override int CompareScore(double score1, double score2)
    {
      return score2.CompareTo(score1);
    }
  }

  public class StarFormat : SAMFormat
  {
    public StarFormat() : base("Star", false, "nM", "AS") { }

    public override int CompareScore(double score1, double score2)
    {
      return score2.CompareTo(score1);
    }
  }
}