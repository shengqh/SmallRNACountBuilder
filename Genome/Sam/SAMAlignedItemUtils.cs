using System.Collections.Generic;
using System.Xml;

namespace CQS.Genome.Sam
{
  public class SAMAlignedItemUtils
  {
    public static List<SAMAlignedItem> ReadFrom(XmlReader source)
    {
      var result = new List<SAMAlignedItem>();

      source.ReadToFollowing("queries");
      if (source.ReadToDescendant("query"))
      {
        do
        {
          var query = new SAMAlignedItem();
          result.Add(query);

          query.Qname = source.GetAttribute("name");
          query.Sequence = source.GetAttribute("sequence");
          query.QueryCount = int.Parse(source.GetAttribute("count"));
          query.Sample = source.GetAttribute("sample");
          if (source.ReadToDescendant("location"))
          {
            do
            {
              var loc = new SAMAlignedLocation(query);

              loc.Seqname = source.GetAttribute("seqname");
              loc.Start = long.Parse(source.GetAttribute("start"));
              loc.End = long.Parse(source.GetAttribute("end"));
              loc.Strand = source.GetAttribute("strand")[0];
              loc.Cigar = source.GetAttribute("cigar");
              loc.AlignmentScore = int.Parse(source.GetAttribute("score"));
              loc.MismatchPositions = source.GetAttribute("mdz");
              loc.NumberOfMismatch = int.Parse(source.GetAttribute("nmi"));
              var nnmpattr = source.GetAttribute("nnpm");
              if (nnmpattr != null)
              {
                loc.NumberOfNoPenaltyMutation = int.Parse(nnmpattr);
              }
            } while (source.ReadToNextSibling("location"));
          }
        } while (source.ReadToNextSibling("query"));
      }

      return result;
    }

    public static void WriteTo(XmlWriter xw, IEnumerable<SAMAlignedItem> queries)
    {
      xw.WriteStartElement("queries");
      foreach (var q in queries)
      {
        xw.WriteStartElement("query");
        xw.WriteAttributeString("name", q.Qname);
        xw.WriteAttributeString("sequence", q.Sequence);
        xw.WriteAttributeString("count", q.QueryCount.ToString());
        if (!string.IsNullOrEmpty(q.Sample))
        {
          xw.WriteAttributeString("sample", q.Sample);
        }
        foreach (var l in q.Locations)
        {
          xw.WriteStartElement("location");
          xw.WriteAttributeString("seqname", l.Seqname);
          xw.WriteAttributeString("start", l.Start.ToString());
          xw.WriteAttributeString("end", l.End.ToString());
          xw.WriteAttributeString("strand", l.Strand.ToString());
          xw.WriteAttributeString("cigar", l.Cigar);
          xw.WriteAttributeString("score", l.AlignmentScore.ToString());
          xw.WriteAttributeString("mdz", l.MismatchPositions);
          xw.WriteAttributeString("nmi", l.NumberOfMismatch.ToString());
          xw.WriteAttributeString("nnpm", l.NumberOfNoPenaltyMutation.ToString());
          xw.WriteEndElement();
        }
        xw.WriteEndElement();
      }
      xw.WriteEndElement();
    }
  }
}
