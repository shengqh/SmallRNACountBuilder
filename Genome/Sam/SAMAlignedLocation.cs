using Bio.IO.SAM;
using RCPA.Seq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CQS.Genome.Sam
{
  public class SAMAlignedLocation : SequenceRegion
  {
    public SAMAlignedLocation(SAMAlignedItem parent)
    {
      this.Features = new List<ISequenceRegion>();
      if (parent != null)
      {
        parent.AddLocation(this);
      }
    }

    public SAMAlignedItem Parent { get; set; }

    public SAMFlags Flag { get; set; }

    public int MapQ { get; set; }

    public string Cigar { get; set; }

    public string Qual { get; set; }

    /// <summary>
    /// Alignment score, parsed from option values
    /// </summary>
    public int AlignmentScore { get; set; }

    /// <summary>
    /// Number of mismatch, parsed from option values
    /// </summary>
    public int NumberOfMismatch { get; set; }

    /// <summary>
    /// Number of non-penalty mutation, such like T to C, or A to G, parsed from gsnap result
    /// </summary>
    public int NumberOfNoPenaltyMutation { get; set; }

    /// <summary>
    /// Mismatch positions, parsed from option values
    /// </summary>
    public string MismatchPositions { get; set; }

    /// <summary>
    /// The features (such as gene or miRNA) that query sequence mapped to.
    /// </summary>
    public List<ISequenceRegion> Features { get; private set; }

    public virtual void ParseEnd(string sequence)
    {
      this.End = this.Start + sequence.Length - 1;
    }

    public static string GetKey(string qname, string loc)
    {
      return string.Format("{0}-{1}", qname, loc);
    }

    public string GetKey()
    {
      return GetKey(Parent.Qname, this.GetLocation());
    }

    private static Regex mismatch = new Regex(@"(\d+)([^\d]+)");

    public SingleNucleotidePolymorphism GetNotGsnapMismatch(string querySequence)
    {
      if (this.NumberOfMismatch == 0)
      {
        return null;
      }

      var isPositiveStrand = this.Strand == '+';
      var m = mismatch.Match(this.MismatchPositions);
      if (!m.Success)
      {
        return null;
      }

      var seq = isPositiveStrand ? querySequence : SequenceUtils.GetReversedSequence(querySequence);
      var pos = int.Parse(m.Groups[1].Value);
      var detectedChr = seq[pos];

      var chr = m.Groups[2].Value.First();
      chr = isPositiveStrand ? chr : SequenceUtils.GetComplementAllele(chr);

      return new SingleNucleotidePolymorphism(pos, chr, detectedChr);
    }

    private List<SingleNucleotidePolymorphism> gsnapMismatches = null;

    public List<SingleNucleotidePolymorphism> GetGsnapMismatches()
    {
      if (this.gsnapMismatches != null)
      {
        return gsnapMismatches;
      }

      this.gsnapMismatches = new List<SingleNucleotidePolymorphism>();
      if (this.NumberOfMismatch == 0 && this.NumberOfNoPenaltyMutation == 0)
      {
        return this.gsnapMismatches;
      }

      var seq = Parent.Sequence;
      var mis = mismatch.Match(this.MismatchPositions);
      int pos = 0;
      while (mis.Success)
      {
        var curcount = int.Parse(mis.Groups[1].Value);
        var mismatches = mis.Groups[2].Value;
        pos += curcount;
        for (int i = 0; i < mismatches.Length; i++)
        {
          this.gsnapMismatches.Add(new SingleNucleotidePolymorphism(pos + i, mismatches[i], seq[pos + i]));
        }
        pos += mismatches.Length;
        mis = mis.NextMatch();
      }

      return this.gsnapMismatches;
    }
  }

  public static class SAMAlignedLocationExtension
  {
    public static Dictionary<string, SAMAlignedLocation> ToSAMAlignedLocationMap(this List<SAMAlignedItem> items)
    {
      var result = new Dictionary<string, SAMAlignedLocation>();
      foreach (var item in items)
      {
        foreach (var loc in item.Locations)
        {
          var key = loc.GetKey();
          if (result.ContainsKey(key))
          {
            Console.WriteLine("Duplicated key {0}", key);
          }
          else
          {
            result[key] = loc;
          }
        }
      }

      return result;
    }
  }
}
