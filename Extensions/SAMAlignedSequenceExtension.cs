using System;
using System.IO;
using System.Linq;

namespace Bio.IO.SAM
{
  public static class SAMAlignedSequenceExtension
  {
    public static string GetQuerySequenceString(this SAMAlignedSequence sam)
    {
      if (sam.Flag.HasFlag(SAMFlags.QueryOnReverseStrand))
      {
        return sam.QuerySequence.GetReverseComplementedSequence().GetSequenceString();
      }
      else
      {
        return sam.QuerySequence.GetSequenceString();
      }
    }

    public static string GetQualityScoresString(this SAMAlignedSequence sam)
    {
      if (sam.Flag.HasFlag(SAMFlags.QueryOnReverseStrand))
      {
        return new string(sam.GetEncodedQualityScores().Reverse().Select(a => (char)a).ToArray());
      }
      else
      {
        return new string(sam.GetEncodedQualityScores().Select(a => (char)a).ToArray());
      }
    }

    public static string GetOptionValue(this SAMAlignedSequence sam, string tag, string vtype, string defaultValue)
    {
      var field = sam.OptionalFields.FirstOrDefault(m => m.Tag.Equals(tag) && m.VType.Equals(vtype));
      if (null == field)
      {
        return defaultValue;
      }
      return field.Value;
    }

    public static string GetOptionValue(this SAMAlignedSequence sam, string tag, string vtype, bool throwException = true, string parserName = null)
    {
      var field = sam.OptionalFields.FirstOrDefault(m => m.Tag.Equals(tag) && m.VType.Equals(vtype));
      if (null == field)
      {
        if (throwException)
        {
          throw new Exception(string.Format("data error, cannot find {0}:{1}:XXX value in query {2}{3}.",
            tag,
            vtype,
           sam.QName,
          parserName == null ? "" : " by parser " + parserName));
        }
        else
        {
          return null;
        }
      }
      return field.Value;
    }

    //public static string GetZezValue(this SAMAlignedSequence sam)
    //{
    //  return GetOptionValue(sam, "ZE", "Z");
    //}

    public static void WriteFastq(this SAMAlignedSequence sam, StreamWriter sw, bool posAsPaired = false)
    {
      if (posAsPaired)
      {
        sw.WriteLine(string.Format("@{0} {1}", sam.QName, sam.Pos));
      }
      else
      {
        sw.WriteLine("@" + sam.QName);
      }
      sw.WriteLine(sam.GetQuerySequenceString());
      sw.WriteLine("+");
      sw.WriteLine(sam.GetQualityScoresString());
    }

    public static void WriteFasta(this SAMAlignedSequence sam, StreamWriter sw)
    {
      sw.WriteLine(">" + sam.QName);
      sw.WriteLine(sam.GetQuerySequenceString());
    }
  }
}

