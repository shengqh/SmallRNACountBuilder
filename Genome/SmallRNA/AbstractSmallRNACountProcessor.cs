using CQS.Genome.Feature;
using CQS.Genome.Mapping;
using CQS.Genome.Sam;
using RCPA.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CQS.Genome.SmallRNA
{
  public class ReadSummary
  {
    public int TotalRead { get; set; }
    public int ExcludeRead { get; set; }
    public int FeatureRead { get; set; }
    public int GenomeRead { get; set; }
    public int TooShortRead { get; set; }
    public int MappedRead { get { return FeatureRead + GenomeRead; } }
    public int UnannotatedRead { get { return TotalRead - ExcludeRead - FeatureRead - GenomeRead - TooShortRead; } }
  }

  public abstract class AbstractSmallRNACountProcessor<T> : AbstractCountProcessor<T> where T : AbstractSmallRNACountProcessorOptions
  {
    public AbstractSmallRNACountProcessor(T options)
      : base(options)
    { }

    public void DrawPositionImageCategory(List<SAMAlignedItem> notNTAreads, List<FeatureLocation> featureLocations, string category, bool positionByPercentage = false, int minimumReadCount = 5)
    {
      var positionFile = Path.ChangeExtension(options.OutputFile, "." + category + ".position");
      DrawPositionImage(notNTAreads, featureLocations.Where(m => m.Category.Equals(category)).ToList(), category, positionFile, positionByPercentage, minimumReadCount);
    }

    public void DrawPositionImage(List<SAMAlignedItem> notNTAreads, List<FeatureLocation> features, string category, string positionFile, bool positionByPercentage = false, int minimumReadCount = 5)
    {
      if (features.Count > 0)
      {
        Progress.SetMessage("mapping reads overlapped to {0} {1} entries.", features.Count, category);

        //map reads to miRNA without considering NTA and offset.
        var map = BuildStrandMap(notNTAreads);

        new SmallRNAMapper(category, options, m => true).MapReadToFeature(features, map);

        SmallRNAMappedPositionBuilder.Build(features.GroupByName().GroupBySequence(), Path.GetFileNameWithoutExtension(options.OutputFile), positionFile, m => m[0].Name.StringAfter(":"), positionByPercentage, minimumReadCount);

        //clear all mapping information
        foreach (var read in notNTAreads)
        {
          foreach (var loc in read.Locations)
          {
            loc.Features.Clear();
          }
        }

        foreach (var feature in features)
        {
          feature.SamLocations.Clear();
        }
      }
    }

    protected void OrderFeatureItemGroup(List<FeatureItemGroup> mirnaGroups)
    {
      mirnaGroups.Sort((m1, m2) =>
      {
        var res = m1[0].Locations[0].Category.CompareTo(m2[0].Locations[0].Category);
        if (res == 0)
        {
          res = m2.GetEstimatedCount().CompareTo(m1.GetEstimatedCount());
        }
        if (res == 0)
        {
          res = m2.GetEstimatedCount(l => l.Offset == options.Offsets[0]).CompareTo(m1.GetEstimatedCount(l => l.Offset == options.Offsets[0]));
        }
        if (res == 0)
        {
          res = m1.Name.CompareTo(m2.Name);
        }
        return res;
      });
    }

    public Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> BuildStrandMap(List<SAMAlignedItem> reads)
    {
      //build chr/strand/samlist map
      Progress.ShowCurrentMemory ("Before building chr/strand/samlist map");

      Dictionary<string, int> plusCounts = new Dictionary<string, int>();
      Dictionary<string, int> minusCounts = new Dictionary<string, int>();

      foreach (var read in reads)
      {
          foreach (var loc in read.Locations)
          {
              if (loc.Strand == '+')
              {
                  plusCounts.TryGetValue(loc.Seqname, out int n);
                  plusCounts[loc.Seqname] = n + 1;
              }
              else
              {
                  minusCounts.TryGetValue(loc.Seqname, out int n);
                  minusCounts[loc.Seqname] = n + 1;
              }
          }
      }

      int count = 0;
      var chrStrandMatchedMap = new Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>>();
      foreach (var read in reads)
      {
        foreach (var loc in read.Locations)
        {
          count ++;
          if (count % 100000 == 0)
          {
            Progress.ShowCurrentMemory(string.Format("After processing {0} reads/locations", count));
          }

          Dictionary<char, List<SAMAlignedLocation>> map;
          if (!chrStrandMatchedMap.TryGetValue(loc.Seqname, out map))
          {
            map = new Dictionary<char, List<SAMAlignedLocation>>();

            int plus = plusCounts.TryGetValue(loc.Seqname, out int n1) ? n1 : 0;
            Progress.ShowCurrentMemory(string.Format("Allocate {0} for {1} strand {2}", plus, loc.Seqname, '+'));
            map['+'] = new List<SAMAlignedLocation>(plus);

            int minus = minusCounts.TryGetValue(loc.Seqname, out int n2) ? n2 : 0;
            Progress.ShowCurrentMemory(string.Format("Allocate {0} for {1} strand {2}", minus, loc.Seqname, '-'));
            map['-'] = new List<SAMAlignedLocation>(minus);

            chrStrandMatchedMap[loc.Seqname] = map;
          }
          map[loc.Strand].Add(loc);
        }
      }

      Progress.ShowCurrentMemory("After building chr/strand/samlist map");
      return chrStrandMatchedMap;
    }

    protected ReadSummary GetReadSummary(List<FeatureItemGroup> allmapped, HashSet<string> excludeQueries, List<SAMAlignedItem> reads, List<QueryInfo> totalQueries)
    {
      var result = new ReadSummary();

      if (File.Exists(options.CountFile))
      {
        result.TotalRead = Counts.GetTotalCount();
      }
      else
      {
        result.TotalRead = totalQueries.Count;
      }

      var featureQueries = new HashSet<string>(from fig in allmapped
                                               from fi in fig
                                               from loc in fi.Locations
                                               from sl in loc.SamLocations
                                               select sl.SamLocation.Parent.OriginalQname);
      result.FeatureRead = featureQueries.Sum(l => Counts.GetCount(l));

      result.ExcludeRead = excludeQueries.Sum(l => Counts.GetCount(l));

      result.GenomeRead = (from query in totalQueries
                           where (!query.Name.Contains(SmallRNAConsts.NTA_TAG) || query.Name.EndsWith(SmallRNAConsts.NTA_TAG))
                           let originalQname = query.Name.StringBefore(SmallRNAConsts.NTA_TAG)
                           where !featureQueries.Contains(originalQname) && query.Mismatch == 0 && query.Length >= options.TooShortReadLength
                           select originalQname).Distinct().Sum(m => Counts.GetCount(m));

      if (Counts.ItemMap != null)
      {
        result.TooShortRead = (from read in Counts.ItemMap.Values
                               where !featureQueries.Contains(read.Qname) && read.SequenceLength < 20
                               select read.Count).Sum();
      }
      else
      {
        result.TooShortRead = 0;
      }

      return result;
    }

    protected void WriteSummaryFile(string infoFile, ReadSummary readSummary, List<FeatureItemGroup> allmapped)
    {
      if (!File.Exists(infoFile) || !options.NotOverwrite)
      {
        Progress.SetMessage("summarizing ...");
        using (var sw = new StreamWriter(infoFile))
        {
          WriteOptions(sw);

          if (File.Exists(options.CountFile))
          {
            sw.WriteLine("#countFile\t{0}", options.CountFile);
          }

          sw.WriteLine("TotalReads\t{0}", readSummary.TotalRead);
          if (readSummary.ExcludeRead > 0)
          {
            sw.WriteLine("ExcludedReads\t{0}", readSummary.ExcludeRead);
          }
          sw.WriteLine("MappedReads\t{0}", readSummary.MappedRead);
          sw.WriteLine("FeatureReads\t{0}", readSummary.FeatureRead);
          sw.WriteLine("GenomeReads\t{0}", readSummary.GenomeRead);
          sw.WriteLine("TooShortReads\t{0}", readSummary.TooShortRead);
          sw.WriteLine("UnannotatedReads\t{0}", readSummary.UnannotatedRead);

          //Output individual category
          foreach (var cat in SmallRNAConsts.Biotypes)
          {
            var count = (from c in allmapped
                         from l in c
                         where l.Name.StartsWith(cat)
                         select l.GetEstimatedCount()).Sum();
            if (count > 0)
            {
              sw.WriteLine("{0}\t{1:0.#}", cat, count);
            }
          }
        }
      }
    }

    protected virtual void WriteOptions(StreamWriter sw)
    {
      sw.WriteLine("#file\t{0}", options.InputFiles.Merge(","));
      sw.WriteLine("#coordinate\t{0}", options.CoordinateFile);
      sw.WriteLine("#offsets\t{0}", options.OffsetStrings);
      sw.WriteLine("#minLength\t{0}", options.MinimumReadLength);
      sw.WriteLine("#minimumReadLengthForLongRNA\t{0}", options.MinimumReadLengthForLongRNA);
      sw.WriteLine("#maxMismatchCount\t{0}", options.MaximumMismatch);
      sw.WriteLine("#minimumOverlapPercentage\t{0}", options.MinimumOverlapPercentage);
      sw.WriteLine("#tooShortReadLength\t{0}", options.TooShortReadLength);
    }
  }
}