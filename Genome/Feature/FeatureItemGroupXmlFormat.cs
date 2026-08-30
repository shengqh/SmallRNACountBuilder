using CQS.Genome.Sam;
using RCPA.Utils;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace CQS.Genome.Feature
{
  public class FeatureItemGroupXmlFormat : AbstractFeatureItemGroupXmlFormat
  {
    public FeatureItemGroupXmlFormat(bool exportPValue = false) : base(exportPValue)
    {
      //Console.WriteLine("Construction of FeatureItemGroupXmlFormatHand.");
    }

    public override void WriteToFile(string fileName, List<FeatureItemGroup> groups)
    {
      UTF8Encoding utf8 = new UTF8Encoding(false);
      Progress.SetMessage("Creating xml writer ... ");
      using (XmlTextWriter xw = new XmlTextWriter(fileName, utf8))
      {
        xw.Formatting = Formatting.Indented;
        Progress.SetMessage("Start writing ... ");
        xw.WriteStartDocument();

        xw.WriteStartElement("root");

        Progress.SetMessage("Getting queries ... ");
        var queries = groups.GetQueries();

        Progress.SetMessage("Writing {0} queries ...", queries.Count);
        SAMAlignedItemUtils.WriteTo(xw, queries);

        Progress.SetMessage("Writing {0} subjects ...", groups.Count);
        xw.WriteStartElement("subjectResult");
        foreach (var itemgroup in groups)
        {
          xw.WriteStartElement("subjectGroup");
          foreach (var item in itemgroup)
          {
            xw.WriteStartElement("subject");
            xw.WriteAttribute("name", item.Name);
            foreach (var region in item.Locations)
            {
              xw.WriteStartElement("region");
              xw.WriteAttribute("seqname", region.Seqname);
              xw.WriteAttribute("start", region.Start);
              xw.WriteAttribute("end", region.End);
              xw.WriteAttribute("strand", region.Strand);
              xw.WriteAttribute("sequence", XmlUtils.ToXml(region.Sequence));
              if (this.exportPValue)
              {
                xw.WriteAttribute("query_count_before_filter", region.QueryCountBeforeFilter);
                xw.WriteAttribute("query_count", region.QueryCount);
                xw.WriteAttribute("pvalue", region.PValue);
              }
              xw.WriteAttribute("size", region.Length);
              foreach (var sl in region.SamLocations)
              {
                var loc = sl.SamLocation;
                xw.WriteStartElement("query");
                xw.WriteAttribute("qname", loc.Parent.Qname);
                xw.WriteAttribute("loc", loc.GetLocation());
                xw.WriteAttribute("overlap", string.Format("{0:0.##}", sl.OverlapPercentage));
                xw.WriteAttribute("offset", sl.Offset);
                xw.WriteAttribute("query_count", loc.Parent.QueryCount);
                xw.WriteAttribute("seq_len", loc.Parent.Sequence.Length);
                xw.WriteAttribute("nmi", loc.NumberOfMismatch);
                xw.WriteAttribute("nnpm", loc.NumberOfNoPenaltyMutation);
                xw.WriteEndElement();
              }
              xw.WriteEndElement();
            }
            xw.WriteEndElement();
          }
          xw.WriteEndElement();
        }
        xw.WriteEndElement();
        xw.WriteEndElement();

        xw.WriteEndDocument();

        Progress.SetMessage("Writing xml file finished.");
      }
    }
  }
}