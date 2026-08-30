using CommandLine;
using CQS.Genome.Gsnap;
using CQS.Genome.SmallRNA;
using System.IO;

namespace CQS.Genome.Mapping
{
  public class AbstractCountProcessorOptions : SAMAlignedItemParserOptions, ICountProcessorOptions
  {
    public AbstractCountProcessorOptions()
    {
      IgnoreNTA = false;
    }

    [Option('o', "outputFile", Required = true, MetaValue = "FILE", HelpText = "Output count file")]
    public string OutputFile { get; set; }

    [Option('c', "countFile", Required = false, MetaValue = "FILE", HelpText = "Sequence/count file")]
    public string CountFile { get; set; }

    public override bool PrepareOptions()
    {
      base.PrepareOptions();

      if (!string.IsNullOrEmpty(this.CountFile) && !File.Exists(this.CountFile))
      {
        ParsingErrors.Add(string.Format("Count file not exists {0}.", this.CountFile));
      }

      return ParsingErrors.Count == 0;
    }

    public virtual ICandidateBuilder GetCandidateBuilder()
    {
      if (this.EngineType == 4)
      {
        return new SAMAlignedItemCandidateGsnapBuilder(this);
      }
      else if (this.BestScore)
      {
        return new SAMAlignedItemBestScoreCandidateBuilder(this);
      }
      else
      {
        return new SAMAlignedItemCandidateBuilder(this);
      }
    }

    private SmallRNACountMap cm;

    public virtual SmallRNACountMap GetCountMap()
    {
      if (cm == null)
      {
        cm = new SmallRNACountMap(this.CountFile);
      }
      return cm;
    }
  }
}
