using CQS.Genome.Feature;
using CQS.Genome.Mapping;
using CQS.Genome.Sam;
using RCPA.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNACountProcessor : AbstractSmallRNACountProcessor<SmallRNACountProcessorOptions>
  {
    protected Dictionary<char, char> noPaneltyMutations = new Dictionary<char, char>();

    public SmallRNACountProcessor(SmallRNACountProcessorOptions options)
      : base(options)
    {
      noPaneltyMutations['C'] = 'T';
      noPaneltyMutations['G'] = 'A';
    }

    protected override void FillReadCount(List<SAMAlignedItem> result)
    { 
      Progress.SetMessage("Reading count file ...");
      int nline = 0;
      using (var sr = new StreamReader(options.CountFile))
      {
        string line = sr.ReadLine();
        while ((line = sr.ReadLine()) != null)
        {
          nline++;
        }
      }

      Dictionary<string, int> count_map = new Dictionary<string, int>(nline);
      using (var sr = new StreamReader(options.CountFile))
      {
        string line = sr.ReadLine();
        while ((line = sr.ReadLine()) != null)
        {
          var parts = line.Split('\t');
          count_map[parts[0]] = int.Parse(parts[1]);
        }
      }

      result.ForEach(m =>
      {
        int count;
        if (count_map.TryGetValue(m.OriginalQname, out count)) 
        {
          m.QueryCount = count;  
        } 
        else if (count_map.TryGetValue(m.Qname, out count)) 
        {
          m.QueryCount = count;
        } 
        else
        {
          throw new Exception("Cannot find query " + m.OriginalQname + " in count file " + options.CountFile);
        }
      });

      Progress.ShowCurrentMemory("Before cleaning up");

      count_map.Clear();
      count_map=null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      Progress.ShowCurrentMemory("After cleaning up");
    }

    public override IEnumerable<string> Process()
    {
      var result = new List<string>();

      //read regions      
      var featureLocations = options.GetSequenceRegions();

      Progress.SetMessage("There are {0} coordinate entries", featureLocations.Count);
      if (featureLocations.Count == 0)
      {
        throw new Exception(string.Format("No coordinate found in file {0}", options.CoordinateFile));
      }

      var fGroups = featureLocations.GroupBy(l => l.Category).OrderByDescending(l => l.Count()).ToList();
      foreach (var fg in fGroups)
      {
        Console.WriteLine("{0} = {1}", fg.Key, fg.Count());
      }

      var featureChroms = new HashSet<string>(from feature in featureLocations
                                              select feature.Seqname);

      var resultFilename = options.OutputFile;
      result.Add(resultFilename);

      HashSet<string> cca = new HashSet<string>();
      if (File.Exists(options.CCAFile))
      {
        cca = new HashSet<string>(File.ReadAllLines(options.CCAFile));
      }

      //parsing reads
      List<QueryInfo> totalQueries;
      var reads = ParseCandidates(options.InputFiles, resultFilename, out totalQueries);
      if (reads.Count == 0)
      {
        throw new ArgumentException("No read found in file " + options.InputFiles.Merge(","));
      }

      HashSet<string> excludeQueries = new HashSet<string>();
      if (!string.IsNullOrEmpty(options.ExcludeXml))
      {
        Progress.SetMessage("Excluding queries in {0} ...", options.ExcludeXml);
        excludeQueries = new HashSet<string>(from q in MappedItemGroupXmlFileFormat.ReadQueries(options.ExcludeXml)
                                             select q.StringBefore(SmallRNAConsts.NTA_TAG));
        reads.RemoveAll(m => excludeQueries.Contains(m.Locations.First().Parent.Qname.StringBefore(SmallRNAConsts.NTA_TAG)));
        Progress.SetMessage("Total candidate {0} for mapping ...", reads.Count);
      }

      var hasMicroRnaNTA = reads.Any(l => l.NTA.Length > 0);

      var hasTrnaNTA = hasMicroRnaNTA || File.Exists(options.CCAFile);

      //var categories = new HashSet<string>(from fl in featureLocations select fl.Category);
      //Progress.SetMessage("Those categories were detected: {0}", string.Join(",", categories.ToArray()));

      //if (!options.NoCategory)
      //{
      //  //First of all, draw candidate mapping position graph
      //  var miRNAPositionFile = Path.ChangeExtension(options.OutputFile, SmallRNAConsts.miRNA + ".candidates.position");
      //  if (!options.NotOverwrite || !File.Exists(miRNAPositionFile))
      //  {
      //    Progress.SetMessage("Drawing microRNA candidates position pictures...");
      //    var notNTAreads = hasMicroRnaNTA ? reads.Where(m => m.NTA.Length == 0).ToList() : reads;
      //    DrawPositionImage(notNTAreads, featureLocations.Where(m => m.Category.Equals(SmallRNAConsts.miRNA)).ToList(), SmallRNABiotype.miRNA.ToString(), miRNAPositionFile);
      //  }
      //}

      var featureGroups = new List<FeatureItemGroup>();
      var mappedfile = resultFilename + ".mapped.xml";
      if (File.Exists(mappedfile) && options.NotOverwrite)
      {
        Progress.SetMessage("Reading mapped feature items...");
        featureGroups = new FeatureItemGroupXmlFormat().ReadFromFile(mappedfile);
      }
      else
      {
        Progress.SetMessage("Mapping feature items...");

        //mapping reads to features based on miRNA, tRNA, mt_tRNA and other smallRNA priority
        MapReadToSequenceRegion(featureLocations, reads, cca, hasMicroRnaNTA, hasTrnaNTA);

        var featureMapped = featureLocations.GroupByName();
        featureMapped.RemoveAll(m => m.GetEstimatedCount() == 0);
        featureMapped.ForEach(m => m.CombineLocations());

        if (options.NoCategory)
        {
          featureGroups = featureMapped.GroupByIdenticalQuery();
        }
        else
        {
          var mirnaGroups = featureMapped.Where(m => m.Name.StartsWith(SmallRNAConsts.miRNA)).GroupBySequence();
          if (mirnaGroups.Count > 0)
          {
            OrderFeatureItemGroup(mirnaGroups);

            Progress.SetMessage("writing miRNA count ...");

            var mirnaCountFile = Path.ChangeExtension(resultFilename, "." + SmallRNAConsts.miRNA + ".count");
            new SmallRNACountMicroRNAWriter(options.Offsets).WriteToFile(mirnaCountFile, mirnaGroups);
            result.Add(mirnaCountFile);
            featureGroups.AddRange(mirnaGroups);

            var positionFile = Path.ChangeExtension(options.OutputFile, SmallRNAConsts.miRNA + ".position");
            SmallRNAMappedPositionBuilder.Build(mirnaGroups, Path.GetFileNameWithoutExtension(options.OutputFile), positionFile, m => m[0].Name.StringAfter(":"));
          }
          mirnaGroups.Clear();

          var trnaCodeGroups = featureMapped.Where(m => m.Name.StartsWith(SmallRNAConsts.tRNA)).GroupByFunction(SmallRNAUtils.GetTrnaAnticodon, false);
          if (trnaCodeGroups.Count > 0)
          {
            OrderFeatureItemGroup(trnaCodeGroups);

            Progress.SetMessage("writing tRNA code count ...");
            var trnaCodeCountFile = Path.ChangeExtension(resultFilename, "." + SmallRNAConsts.tRNA + ".count");

            new FeatureItemGroupCountWriter(m => m.DisplayNameWithoutCategory).WriteToFile(trnaCodeCountFile, trnaCodeGroups);
            result.Add(trnaCodeCountFile);

            featureGroups.AddRange(trnaCodeGroups);

            var positionFile = Path.ChangeExtension(options.OutputFile, SmallRNAConsts.tRNA + ".position");
            SmallRNAMappedPositionBuilder.Build(trnaCodeGroups, Path.GetFileName(options.OutputFile), positionFile, m => m[0].Name.StringAfter(":"));
          }
          trnaCodeGroups.Clear();

          var otherFeatures = featureMapped.Where(m => !m.Name.StartsWith(SmallRNAConsts.miRNA) && !m.Name.StartsWith(SmallRNAConsts.tRNA)).ToList();
          var exportBiotypes = SmallRNAUtils.GetOutputBiotypes(options);
          foreach (var biotype in exportBiotypes)
          {
            WriteGroups(result, resultFilename, featureGroups, otherFeatures, biotype);
          }

          var leftFeatures = otherFeatures.Where(l => !exportBiotypes.Any(b => l.Name.StartsWith(b))).ToList();
          WriteGroups(result, resultFilename, featureGroups, leftFeatures, null);
        }

        Progress.SetMessage("writing all smallRNA count ...");
        new FeatureItemGroupCountWriter().WriteToFile(resultFilename, featureGroups);
        result.Add(resultFilename);

        Progress.SetMessage("writing mapping details...");
        new FeatureItemGroupXmlFormatHand().WriteToFile(mappedfile, featureGroups);
      }

      var readSummary = GetReadSummary(featureGroups, excludeQueries, reads, totalQueries);
      WriteInfoFile(result, resultFilename, readSummary, featureGroups);
      result.Add(mappedfile);
      Progress.End();

      return result;
    }

    private void WriteGroups(List<string> result, string resultFilename, List<FeatureItemGroup> allmapped, List<FeatureItem> otherFeatures, string biotype)
    {
      List<FeatureItemGroup> groups;
      FeatureItemGroupCountWriter writer;

      string displayBiotype;
      if (string.IsNullOrEmpty(biotype))
      {
        displayBiotype = "other";
        groups = otherFeatures.GroupByIdenticalQuery();
        writer = new FeatureItemGroupCountWriter(m => m.DisplayName);
      }
      else
      {
        displayBiotype = biotype;
        groups = otherFeatures.Where(m => m.Name.StartsWith(biotype)).GroupByIdenticalQuery();
        writer = new FeatureItemGroupCountWriter(m => m.DisplayNameWithoutCategory);
      }

      if (groups.Count > 0)
      {
        OrderFeatureItemGroup(groups);

        Progress.SetMessage("writing {0} count ...", displayBiotype);
        var file = Path.ChangeExtension(resultFilename, "." + displayBiotype + ".count");
        writer.WriteToFile(file, groups);
        result.Add(file);

        allmapped.AddRange(groups);

        //if (!string.IsNullOrEmpty(biotype))
        //{
        var positionFile = Path.ChangeExtension(options.OutputFile, displayBiotype + ".position");
        SmallRNAMappedPositionBuilder.Build(groups, Path.GetFileNameWithoutExtension(options.OutputFile), positionFile, m => m[0].Name.StringAfter(":"), true);
        var absolutePositionFile = Path.ChangeExtension(options.OutputFile, displayBiotype + ".absolutePosition");
        SmallRNAMappedPositionBuilder.Build(groups, Path.GetFileNameWithoutExtension(options.OutputFile), absolutePositionFile, m => m[0].Name.StringAfter(":"), false);
        //}
      }
      groups.Clear();
    }

    private void WriteInfoFile(List<string> result, string resultFilename, ReadSummary readSummary, List<FeatureItemGroup> allmapped)
    {
      var infoFile = Path.ChangeExtension(resultFilename, ".info");
      WriteSummaryFile(infoFile, readSummary, allmapped);
      result.Add(infoFile);
    }

    protected void MapReadToSequenceRegion(List<FeatureLocation> mapped, List<SAMAlignedItem> reads, HashSet<string> cca, bool hasMicroRnaNTA, bool hasTrnaNTA)
    {
      var chrStrandMatchedMap = BuildStrandMap(reads);

      List<IReadMapper> mappers = new List<IReadMapper>();
      if (!options.NoCategory)
      {
        //microRNA
        mappers.Add(new SmallRNAMapperMicroRNA(options, hasMicroRnaNTA) { Progress = this.Progress });
        //remove NTA except tRNA NTA
        mappers.Add(new SmallRNAMapperNTAFilter(hasMicroRnaNTA, hasTrnaNTA, false, cca));

        //tRNA
        mappers.Add(new SmallRNAMapperTRNA(options, hasTrnaNTA, cca) { Progress = this.Progress });
        //remove all NTA
        mappers.Add(new SmallRNAMapperNTAFilter(hasMicroRnaNTA, false, false, cca));

        //yRNA
        mappers.Add(new SmallRNAMapperBiotype(SmallRNABiotype.yRNA, options) { Progress = this.Progress });
        //snRNA
        mappers.Add(new SmallRNAMapperBiotype(SmallRNABiotype.snRNA, options) { Progress = this.Progress });
        //snoRNA
        mappers.Add(new SmallRNAMapperBiotype(SmallRNABiotype.snoRNA, options) { Progress = this.Progress });
        //rRNA from genome
        mappers.Add(new SmallRNAMapper("rRNA", options, feature => feature.Category.Equals(SmallRNABiotype.rRNA.ToString()) && !feature.Name.Contains("SILVA_") && !feature.Name.Contains(SmallRNAConsts.rRNADB_KEY)));
        //rRNA from ncbi
        mappers.Add(new SmallRNAMapperRRNADB(options));

        //other smallRNA except lincRNA
        mappers.Add(new SmallRNAMapper("otherSmallRNA", options, feature =>
          !feature.Category.Equals(SmallRNABiotype.miRNA.ToString()) &&
          !feature.Category.Equals(SmallRNABiotype.tRNA.ToString()) &&
          !feature.Category.Equals(SmallRNABiotype.yRNA.ToString()) &&
          !feature.Category.Equals(SmallRNABiotype.snRNA.ToString()) &&
          !feature.Category.Equals(SmallRNABiotype.snoRNA.ToString()) &&
          !feature.Category.Equals(SmallRNABiotype.rRNA.ToString()) &&
          !feature.Name.Contains("SILVA_") && !feature.Name.Contains(SmallRNAConsts.rRNADB_KEY) &&
          !feature.Category.Equals(SmallRNABiotype.lincRNA.ToString()) &&
          !feature.Category.Equals(SmallRNABiotype.lncRNA.ToString()) &&
          !feature.Category.Equals(SmallRNABiotype.ERV.ToString()))
        { Progress = this.Progress });

        //lncRNA
        mappers.Add(new SmallRNAMapperLincRNA(SmallRNABiotype.lincRNA.ToString(), options) { Progress = this.Progress });
        mappers.Add(new SmallRNAMapperLincRNA(SmallRNABiotype.lncRNA.ToString(), options) { Progress = this.Progress });

        //ERV
        mappers.Add(new SmallRNAMapperLincRNA(SmallRNABiotype.ERV.ToString(), options) { Progress = this.Progress });
      }
      else
      {
        mappers.Add(new SmallRNAMapperNTAFilter(true, false, false, cca));
        mappers.Add(new SmallRNAMapper("otherSmallRNA", options, feature => true)
        {
          Progress = this.Progress
        });
      }

      foreach (var mapper in mappers)
      {
        mapper.MapReadToFeatureAndRemoveFromMap(mapped, chrStrandMatchedMap);
      }
    }

    protected override void WriteOptions(StreamWriter sw)
    {
      base.WriteOptions(sw);
      if (!string.IsNullOrEmpty(options.CCAFile))
      {
        sw.WriteLine("#ccaFile\t{0}", options.CCAFile);
      }
      if (!string.IsNullOrEmpty(options.CountFile))
      {
        sw.WriteLine("#countFile\t{0}", options.CountFile);
      }

      sw.WriteLine("#noCategory\t{0}", options.NoCategory);
      sw.WriteLine("#exportYRNA\t{0}", options.ExportYRNA);
      sw.WriteLine("#exportSnRNA\t{0}", options.ExportSnRNA);
      sw.WriteLine("#exportSnoRNA\t{0}", options.ExportSnoRNA);
      sw.WriteLine("#exportERV\t{0}", options.ExportERV);
      sw.WriteLine("#newMethod\t{0}", options.NewMethod);
    }
  }
}