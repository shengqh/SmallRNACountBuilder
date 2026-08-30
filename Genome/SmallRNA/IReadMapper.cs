using CQS.Genome.Feature;
using CQS.Genome.Sam;
using System.Collections.Generic;

namespace CQS.Genome.SmallRNA
{
  public interface IReadMapper
  {
    string MapperName { get; }

    void MapReadToFeature(List<FeatureLocation> features, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap);

    void MapReadToFeatureAndRemoveFromMap(List<FeatureLocation> features, Dictionary<string, Dictionary<char, List<SAMAlignedLocation>>> chrStrandReadMap);
  }
}
