using CQS.Genome.Mapping;
using System.Collections.Generic;

namespace CQS.Genome.SmallRNA
{
  public interface ISmallRNACountProcessorOptions : ICountProcessorOptions
  {
    IList<string> InputFiles { get; set; }

    string CoordinateFile { get; set; }

    string FastaFile { get; set; }

    string FastqFile { get; set; }

    List<long> Offsets { get; }

    double MinimumOverlapPercentage { get; set; }

    int MaximumMismatchForLongRNA { get; set; }

    int MinimumReadLengthForLongRNA { get; set; }

    bool NotOverwrite { get; set; }

    string ExcludeXml { get; set; }
  }
}
