using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CQS.Genome.Gtf
{
  public class GtfItemFile : LineFile
  {
    public GtfItemFile()
    {
    }

    public GtfItemFile(string filename)
      : base(filename)
    {
    }

    public GtfItem Next()
    {
      if (reader == null)
      {
        throw new FileNotFoundException("Open file first.");
      }

      string line;
      while ((line = reader.ReadLine()) != null)
      {
        var parts = line.Split('\t');
        if (parts.Length >= 9)
        {
          return ParseItem(parts);
        }
      }

      return null;
    }

    private static GtfItem ParseItem(string[] parts)
    {
      return new GtfItem
      {
        Seqname = parts[0],
        Source = parts[1],
        Feature = parts[2],
        Start = long.Parse(parts[3]),
        End = long.Parse(parts[4]),
        Score = parts[5],
        Strand = parts[6][0],
        Frame = parts[7][0],
        Attributes = parts.Length == 9 ? parts[8] : parts.Skip(8).Merge(" ")

      };
    }

    public GtfItem NextExon()
    {
      return Next("exon");
    }

    public GtfItem Next(string featureName)
    {
      string line;
      while ((line = reader.ReadLine()) != null)
      {
        var parts = line.Split('\t');
        if (parts.Length >= 9 && parts[2].Equals(featureName))
        {
          return ParseItem(parts);
        }
      }
      return null;
    }

    public static List<GtfItem> ReadFromFile(string filename)
    {
      var result = new List<GtfItem>();
      using (var f = new GtfItemFile(filename))
      {
        GtfItem item;
        while ((item = f.Next()) != null)
        {
          result.Add(item);
        }
      }
      return result;
    }

    public static List<GtfItem> ReadFromFile(string filename, string featureName)
    {
      var result = new List<GtfItem>();
      using (var f = new GtfItemFile(filename))
      {
        GtfItem item;
        while ((item = f.Next(featureName)) != null)
        {
          result.Add(item);
        }
      }
      return result;
    }

    public static void WriteToFile(string fileName, List<GtfItem> items)
    {
      using (var sw = new StreamWriter(fileName))
      {
        foreach (var item in items)
        {
          sw.WriteLine(item.ToString());
        }
      }
    }
  }
}