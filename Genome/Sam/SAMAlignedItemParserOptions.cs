using CommandLine;
using CQS.Genome.Sam;
using RCPA.Commandline;
using System;

namespace CQS.Genome.Mapping
{
  public class SAMAlignedItemParserOptions : AbstractOptions, ISAMAlignedItemParserOptions
  {
    private const int DEFAULT_EngineType = 1;
    private const int DEFAULT_MinimumReadLength = 16;
    private const int DEFAULT_MaximumMismatchCount = 1;
    public const int DEFAULT_MaximumNoPenaltyMutationCount = 1;

    public SAMAlignedItemParserOptions()
    {
      this.EngineType = DEFAULT_EngineType;
      this.MinimumReadLength = DEFAULT_MinimumReadLength;
      this.MaximumReadLength = int.MaxValue;
      this.MaximumMismatch = DEFAULT_MaximumMismatchCount;
      this.MaximumNoPenaltyMutationCount = DEFAULT_MaximumNoPenaltyMutationCount;
      this.IgnoreNTA = false;
      this.IgnoreNTAAndNoPenaltyMutation = false;
    }

    [Option('e', "engineType", DefaultValue = DEFAULT_EngineType, MetaValue = "INT", HelpText = "Engine type (1:bowtie1, 2:bowtie2, 3:bwa, 4:gsnap, 5:star)")]
    public virtual int EngineType { get; set; }

    [Option('l', "minlen", MetaValue = "INT", DefaultValue = DEFAULT_MinimumReadLength, HelpText = "Minimum read length")]
    public virtual int MinimumReadLength { get; set; }

    [Option("maxlen", MetaValue = "INT", DefaultValue = int.MaxValue, HelpText = "Maximum read length")]
    public virtual int MaximumReadLength { get; set; }

    [Option('m', "maxMismatch", MetaValue = "INT", DefaultValue = DEFAULT_MaximumMismatchCount, HelpText = "Maximum mismatch count")]
    public virtual int MaximumMismatch { get; set; }

    [Option("t2cAsNoPenaltyMutation", MetaValue = "INT", DefaultValue = false, HelpText = "Consider T2C as no penalty mutation")]
    public virtual bool T2cAsNoPenaltyMutation { get; set; }

    [Option("maxNoPenaltyMutation", MetaValue = "INT", DefaultValue = DEFAULT_MaximumNoPenaltyMutationCount, HelpText = "Maximum no penalty mutation count (such as T2C for Parclip, gsnap only)")]
    public virtual int MaximumNoPenaltyMutationCount { get; set; }

    [Option('s', "bestScore", HelpText = "Consider score difference between matches from same query")]
    public bool BestScore { get; set; }

    [Option("ignoreNTA", HelpText = "Ignore NTA reads when parsing")]
    public bool IgnoreNTA { get; set; }

    [Option("ignoreNTAAndNoPenaltyMutation", HelpText = "Ignore reads with both NTA and NoPenaltyMutation when parsing")]
    public bool IgnoreNTAAndNoPenaltyMutation { get; set; }

    public virtual ISAMFormat GetSAMFormat()
    {
      var result = SAMFactory.GetFormat(this.EngineType);
      if (result == null)
      {
        throw new Exception(string.Format("No SAM format defined for engine {0}", this.EngineType));
      }
      return result;
    }

    public override bool PrepareOptions()
    {
      return true;
    }
  }
}
