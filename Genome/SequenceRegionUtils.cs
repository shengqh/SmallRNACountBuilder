using CQS.Genome.Bed;
using CQS.Genome.Gtf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CQS.Genome
{
  public static class SequenceRegionUtils
  {
    private static readonly Regex Reg = new Regex("(.+?):(.+?)-(.+?):([+-])");
    private static readonly Regex Namereg = new Regex(@"\((.+?)\)(.+?):(.+?)-(.+?):([+-])");

    /// <summary>
    /// Check if two regions are container-containee. If one contains two, return 1. If two contains one, return -1. Otherwise return 0.
    /// </summary>
    /// <param name="one">first region</param>
    /// <param name="two">second region</param>
    /// <returns>result</returns>
    public static int Contains(this ISequenceRegion one, ISequenceRegion two)
    {
      if (one.Length >= two.Length)
      {
        if (one.Start <= two.Start && one.End >= two.End)
        {
          return 1;
        }
        return 0;
      }

      if (two.Start <= one.Start && two.End >= one.End)
      {
        return -1;
      }
      return 0;
    }

    public static double OverlapPercentage(this ISequenceRegion one, ISequenceRegion two)
    {
      if (!one.Seqname.Equals(two.Seqname))
      {
        return 0.0;
      }

      ISequenceRegion first, second;
      if (one.Start < two.Start || (one.Start == two.Start && one.End > two.End))
      {
        first = one;
        second = two;
      }
      else
      {
        first = two;
        second = one;
      }

      //no-overlap
      if (first.End < second.Start)
      {
        return 0.0;
      }

      //contain
      if (first.End >= second.End)
      {
        return 1.0;
      }

      //overlap
      var overlap = first.End - second.Start + 1.0;
      return overlap / Math.Min(first.Length, second.Length);
    }

    public static bool Overlap(this ISequenceRegion one, ISequenceRegion two, double minPercentage)
    {
      var perc = OverlapPercentage(one, two);
      return perc > 0 && perc >= minPercentage;
    }

    public static string GetLocationWithoutStrand(this ISequenceRegion sr)
    {
      return string.Format("{0}:{1}-{2}", sr.Seqname, sr.Start, sr.End);
    }

    /// <summary>
    /// Get location using chrom:start-end:strand format
    /// </summary>
    /// <param name="sr"></param>
    /// <returns></returns>
    public static string GetLocation(this ISequenceRegion sr)
    {
      return string.Format("{0}:{1}-{2}:{3}", sr.Seqname, sr.Start, sr.End, sr.Strand);
    }

    /// <summary>
    /// Get name and location using (name)chrom:start-end:strand format
    /// </summary>
    /// <param name="sr"></param>
    /// <returns></returns>
    public static string GetNameLocation(this ISequenceRegion sr)
    {
      return string.Format("({0}){1}", sr.Name, sr.GetLocation());
    }

    public static T ParseLocation<T>(string line) where T : ISequenceRegion, new()
    {
      var vm = Reg.Match(line);
      if (!vm.Success)
      {
        throw new ArgumentException("Cannot recognize location format (should be like 13:48633608-48633629:-) : " + line);
      }

      var result = new T
      {
        Seqname = vm.Groups[1].Value,
        Start = int.Parse(vm.Groups[2].Value),
        End = int.Parse(vm.Groups[3].Value),
        Strand = vm.Groups[4].Value[0]
      };
      return result;
    }

    public static string GetNameLocations<T>(this IEnumerable<T> items) where T : ISequenceRegion
    {
      return (from qg in items select qg.GetNameLocation()).Merge(",");
    }

    public static string GetLocations<T>(this IEnumerable<T> items) where T : ISequenceRegion
    {
      return (from qg in items select qg.GetLocation()).Merge(",");
    }

    public static List<T> ParseNameLocations<T>(string line) where T : ISequenceRegion, new()
    {
      if (string.IsNullOrEmpty(line))
      {
        throw new ArgumentNullException("line");
      }

      var vm = Namereg.Match(line);
      if (!vm.Success)
      {
        throw new ArgumentException(
          "Cannot recognize location format (should be like (hsa-let-7a-1-5p)9:96938244-96938265:+) : " + line);
      }

      var result = new List<T>();
      while (vm.Success)
      {
        var item = new T();
        item.Name = vm.Groups[1].Value;
        item.Seqname = vm.Groups[2].Value;
        item.Start = int.Parse(vm.Groups[3].Value);
        item.End = int.Parse(vm.Groups[4].Value);
        item.Strand = vm.Groups[5].Value[0];
        result.Add(item);
        vm = vm.NextMatch();
      }

      return result;
    }

    /// <summary>
    /// Get 1-based coordinate from file. Bed format will be automatically translated.
    /// </summary>
    /// <param name="coordinateFile">source file</param>
    /// <param name="gtfFeature">if it's gtf format, which feature name will be used as gene_id</param>
    /// <param name="bedAsGtf">if bed already be 1-based</param>
    /// <returns></returns>
    public static List<GtfItem> GetSequenceRegions(string coordinateFile, string gtfFeature = "", bool bedAsGtf = false)
    {
      bool isBedFormat = IsBedFormat(coordinateFile);

      List<GtfItem> result;
      if (isBedFormat)
      {
        //bed is zero-based, and the end is not included in the sequence region
        //https://genome.ucsc.edu/FAQ/FAQformat.html#format1
        //gtf is 1-based, and the end is included in the sequence region
        //http://useast.ensembl.org/info/website/upload/gff.html
        //since pos in sam format is 1-based, we need to convert beditem to gtfitem.
        //http://samtools.sourceforge.net/SAMv1.pdf
        var bedItems = new BedItemFile<BedItem>().ReadFromFile(coordinateFile);
        if (!bedAsGtf)
        {
          bedItems.ForEach(m => m.Start++);
        }
        result = bedItems.ConvertAll(m => new GtfItem(m));
      }
      else
      {
        result = GtfItemFile.ReadFromFile(coordinateFile).ToList();
        if (!string.IsNullOrEmpty(gtfFeature))
        {
          result.RemoveAll(m => !m.Feature.Equals(gtfFeature));
        }

        result.ForEach(m =>
        {
          if (m.Attributes.Contains("gene_id \""))
          {
            m.GeneId = m.Attributes.StringAfter("gene_id \"").StringBefore("\"");
          }
          else if (m.Attributes.Contains("ID="))
          {
            m.GeneId = m.Attributes.StringAfter("ID=").StringBefore(";");
          }

          if (m.Attributes.Contains("gene_name \""))
          {
            m.Name = m.Attributes.StringAfter("gene_name \"").StringBefore("\"");
          }
          else if (m.Attributes.Contains("Name="))
          {
            m.Name = m.Attributes.StringAfter("Name=").StringBefore(";");
          }

          if (string.IsNullOrEmpty(m.GeneId) && !string.IsNullOrEmpty(m.Name))
          {
            m.GeneId = m.Name;
          }

          if (!string.IsNullOrEmpty(m.GeneId) && string.IsNullOrEmpty(m.Name))
          {
            m.Name = m.GeneId;
          }

          if (string.IsNullOrEmpty(m.GeneId))
          {
            m.GeneId = m.Attributes;
            m.Name = m.Attributes;
          }
        });
      }

      return result;
    }

    public static bool IsBedFormat(string coordinateFile)
    {
      var isBedFormat = false;
      using (var sr = new StreamReader(coordinateFile))
      {
        string line;

        while ((line = sr.ReadLine()) != null)
        {
          if (line.StartsWith("#"))
          {
            continue;
          }

          var parts = line.Split('\t');
          long start, end;
          if (long.TryParse(parts[1], out start) && long.TryParse(parts[2], out end))
          {
            isBedFormat = true;
            break;
          }
        }
      }
      return isBedFormat;
    }
    public static string GetDisplayName(this ISequenceRegion sr)
    {
      if (!string.IsNullOrEmpty(sr.Sequence))
      {
        return sr.Name + ":" + sr.Sequence;
      }
      else
      {
        return sr.Name;
      }
    }

    public static void ParseLocation(this ISequenceRegion loc, XElement locEle)
    {
      loc.Seqname = locEle.Attribute("seqname").Value;
      loc.Start = long.Parse(locEle.Attribute("start").Value);
      loc.End = long.Parse(locEle.Attribute("end").Value);
      loc.Strand = locEle.Attribute("strand").Value[0];
    }

    public static void UnionWith(this ISequenceRegion loc, ISequenceRegion item)
    {
      loc.Start = Math.Min(loc.Start, item.Start);
      loc.End = Math.Max(loc.End, item.End);
    }

    public static long Offset(this ISequenceRegion actual, ISequenceRegion reference)
    {
      if (reference.Strand == '-')
      {
        return reference.End - actual.End;
      }
      else
      {
        return actual.Start - reference.Start;
      }
    }

    public static void Combine<T>(this List<T> regions, Action<T, T> unionAction = null, Func<T, T, bool> overlapPreRequired = null) where T : ISequenceRegion
    {
      regions.Sort((m1, m2) =>
      {
        var res = m1.Seqname.CompareTo(m2.Seqname);
        if (res != 0)
        {
          return res;
        }

        res = m1.Start.CompareTo(m2.Start);
        if (res != 0)
        {
          return res;
        }

        return m1.End.CompareTo(m2.End);
      });

      int i = 0;
      while (i < regions.Count - 1)
      {
        var iFeature = regions[i];
        var jFeature = regions[i + 1];

        if (iFeature.Seqname.Equals(jFeature.Seqname))
        {
          var iContain = iFeature.Contains(jFeature);
          if (iContain == 1) // i contains j
          {
            regions.RemoveAt(i + 1);
            continue;
          }

          if (iContain == -1)
          {
            regions.RemoveAt(i);

            continue;
          }

          if (iFeature.Contains(jFeature.Start))
          {
            if (overlapPreRequired == null || overlapPreRequired(iFeature, jFeature))
            {
              iFeature.UnionWith(jFeature);
              if (unionAction != null)
              {
                unionAction(iFeature, jFeature);
              }
              regions.RemoveAt(i + 1);
              continue;
            }
          }
        }

        i++;
      }
    }
  }
}