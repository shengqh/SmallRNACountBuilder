using RCPA.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CQS.Genome.Feature
{
  public class FeatureItemGroupXmlFormatHand : AbstractFeatureItemGroupXmlFormat
  {
    public FeatureItemGroupXmlFormatHand(bool exportPValue = false) : base(exportPValue)
    {
      //Console.WriteLine("Construction of FeatureItemGroupXmlFormatHand.");
    }

    public override void WriteToFile(string fileName, List<FeatureItemGroup> groups)
    {
      UTF8Encoding utf8 = new UTF8Encoding(false);

      Progress.SetMessage("Ready for writing ...");

      using (var ft = new FileStream(fileName, FileMode.Create))
      using (var xw = new StreamWriter(ft, utf8))
      //using (var xw = new StreamWriter(ft))
      {
        //Console.WriteLine("Start writing ... ");
        Progress.SetMessage("Start writing ... ");
        //xw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xw.WriteLine("<?xml version=\"1.0\"?>");
        xw.WriteLine("<root>");

        Progress.SetMessage("Getting queries ... ");
        var queries = groups.GetQueries();

        Progress.SetMessage("Writing {0} queries ...", queries.Count);
        xw.WriteLine("  <queries>");

        queries.Sort((l1, l2) => l2.QueryCount.CompareTo(l1.QueryCount));
        foreach (var query in queries)
        {
          xw.WriteLine(@"    <query name=""{0}"" sequence=""{1}"" count=""{2}"">", query.Qname, query.Sequence, query.QueryCount);
          foreach (var loc in query.Locations)
          {
            xw.WriteLine(@"      <location seqname=""{0}"" start=""{1}"" end=""{2}"" strand=""{3}"" cigar=""{4}"" score=""{5}"" mdz=""{6}"" nmi=""{7}"" nnpm=""{8}"" />",
              loc.Seqname,
              loc.Start,
              loc.End,
              loc.Strand,
              loc.Cigar,
              loc.AlignmentScore,
              loc.MismatchPositions,
              loc.NumberOfMismatch,
              loc.NumberOfNoPenaltyMutation);
          }
          xw.WriteLine("    </query>");
        }
        xw.WriteLine("  </queries>");

        Progress.SetMessage("Writing {0} subjects ...", groups.Count);
        xw.WriteLine("  <subjectResult>");
        foreach (var itemgroup in groups)
        {
          xw.WriteLine("    <subjectGroup>");
          foreach (var item in itemgroup)
          {
            xw.WriteLine("      <subject name=\"{0}\">", item.Name);
            foreach (var region in item.Locations)
            {
              xw.Write("        <region seqname=\"{0}\" start=\"{1}\" end=\"{2}\" strand=\"{3}\" sequence=\"{4}\"",
                region.Seqname,
                region.Start,
                region.End,
                region.Strand,
                XmlUtils.ToXml(region.Sequence));
              if (this.exportPValue)
              {
                xw.Write(" query_count_before_filter=\"{0}\"", region.QueryCountBeforeFilter);
                xw.Write(" query_count=\"{0}\"", region.QueryCount);
                xw.Write(" pvalue=\"{0}\"", region.PValue);
              }
              xw.WriteLine(" size=\"{0}\">", region.Length);
              region.SamLocations.Sort((l1, l2) => l2.SamLocation.Parent.QueryCount.CompareTo(l1.SamLocation.Parent.QueryCount));
              foreach (var sl in region.SamLocations)
              {
                var loc = sl.SamLocation;
                xw.Write("          <query qname=\"{0}\"", loc.Parent.Qname);
                xw.Write(" loc=\"{0}\"", loc.GetLocation());
                xw.Write(" overlap=\"{0}\"", string.Format("{0:0.##}", sl.OverlapPercentage));
                xw.Write(" offset=\"{0}\"", sl.Offset);
                xw.Write(" query_count=\"{0}\"", loc.Parent.QueryCount);
                xw.Write(" seq_len=\"{0}\"", loc.Parent.Sequence.Length);
                xw.Write(" nmi=\"{0}\"", loc.NumberOfMismatch);
                xw.WriteLine(" nnpm=\"{0}\" />", loc.NumberOfNoPenaltyMutation);
              }
              xw.WriteLine("        </region>");
            }
            xw.WriteLine("      </subject>");
          }
          xw.WriteLine("    </subjectGroup>");
        }
        xw.WriteLine("  </subjectResult>");
        xw.Write("</root>");
      }
      Progress.SetMessage("Writing xml file finished.");
    }
  }
}