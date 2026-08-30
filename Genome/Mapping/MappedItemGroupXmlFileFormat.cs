using CQS.Genome.Sam;
using RCPA;
using RCPA.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CQS.Genome.Mapping
{
  public class MappedItemGroupXmlFileFormat : IFileFormat<List<MappedItemGroup>>
  {
    private bool exportPValue;

    public MappedItemGroupXmlFileFormat(bool exportPValue = false)
    {
      this.exportPValue = exportPValue;
    }

    public static List<string> ReadQueries(string fileName)
    {
      List<string> result = new List<string>();
      XElement root = XElement.Load(fileName);
      foreach (var queryEle in root.Element("queries").Elements("query"))
      {
        result.Add(queryEle.Attribute("name").Value);
      }
      return result;
    }

    public List<MappedItemGroup> ReadFromFile(string fileName)
    {
      var result = new List<MappedItemGroup>();

      XElement root = XElement.Load(fileName);

      //Console.WriteLine("read locations ...");
      Dictionary<string, SAMAlignedLocation> qmmap = root.ToSAMAlignedItems().ToSAMAlignedLocationMap();

      //Console.WriteLine("read mapped items ...");
      foreach (XElement groupEle in root.Element("subjectResult").Elements("subjectGroup"))
      {
        var group = new MappedItemGroup();
        result.Add(group);

        foreach (XElement mirnaEle in groupEle.Elements("subject"))
        {
          var mirna = new MappedItem();
          group.Add(mirna);
          mirna.Name = mirnaEle.Attribute("name").Value;

          foreach (XElement regionEle in mirnaEle.Elements("region"))
          {
            var region = new SequenceRegionMapped();
            mirna.MappedRegions.Add(region);

            region.Region.Name = mirna.Name;
            region.Region.ParseLocation(regionEle);

            if (regionEle.Attribute("sequence") != null)
            {
              region.Region.Sequence = regionEle.Attribute("sequence").Value;
            }

            if (regionEle.Attribute("query_count_before_filter") != null)
            {
              region.QueryCountBeforeFilter = int.Parse(regionEle.Attribute("query_count_before_filter").Value);
            }

            if (regionEle.Attribute("pvalue") != null)
            {
              region.PValue = double.Parse(regionEle.Attribute("pvalue").Value);
            }

            foreach (XElement queryEle in regionEle.Elements("query"))
            {
              string qname = queryEle.Attribute("qname").Value;
              string loc = queryEle.Attribute("loc").Value;
              string key = SAMAlignedLocation.GetKey(qname, loc);
              SAMAlignedLocation query = qmmap[key];
              region.AlignedLocations.Add(query);
              query.Features.Add(region.Region);
            }
          }
        }
      }
      qmmap.Clear();

      return result;
    }

    public void WriteToFile(string fileName, List<MappedItemGroup> groups)
    {
      List<SAMAlignedItem> queries = groups.GetQueries();

      var xml = new XElement("root",
        queries.ToXElement(),
        new XElement("subjectResult",
          from itemgroup in groups
          select new XElement("subjectGroup",
            from item in itemgroup
            select new XElement("subject",
              new XAttribute("name", item.Name),
              from region in item.MappedRegions
              select new XElement("region",
                new XAttribute("seqname", region.Region.Seqname),
                new XAttribute("start", region.Region.Start),
                new XAttribute("end", region.Region.End),
                new XAttribute("strand", region.Region.Strand),
                new XAttribute("sequence", XmlUtils.ToXml(region.Region.Sequence)),
                this.exportPValue ? new XAttribute("query_count_before_filter", region.QueryCountBeforeFilter) : null,
                this.exportPValue ? new XAttribute("query_count", region.QueryCount) : null,
                this.exportPValue ? new XAttribute("pvalue", region.PValue) : null,
                from loc in region.AlignedLocations
                select new XElement("query",
                  new XAttribute("qname", loc.Parent.Qname),
                  new XAttribute("loc", loc.GetLocation()),
                  this.exportPValue ? new XAttribute("query_count", loc.Parent.QueryCount) : null
                  ))))));
      xml.Save(fileName);
    }
  }
}