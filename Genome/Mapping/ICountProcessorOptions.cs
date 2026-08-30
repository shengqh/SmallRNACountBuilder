using CQS.Genome.Sam;
using CQS.Genome.SmallRNA;

namespace CQS.Genome.Mapping
{
  public interface ICountProcessorOptions : ISAMAlignedItemParserOptions
  {
    string OutputFile { get; set; }

    string CountFile { get; set; }

    SmallRNACountMap GetCountMap();

    ICandidateBuilder GetCandidateBuilder();
  }
}
