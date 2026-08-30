using CQS.Genome.Sam;

namespace CQS.Genome.Feature
{
  public class FeatureSamLocation
  {
    public FeatureSamLocation(FeatureLocation featureLocation)
    {
      this.FeatureLocation = featureLocation;
      this.FeatureLocation.SamLocations.Add(this);
    }

    private SAMAlignedLocation _samLocation;
    public SAMAlignedLocation SamLocation
    {
      get
      {
        return _samLocation;
      }
      set
      {
        _samLocation = value;
        if (value != null && !value.Features.Contains(this.FeatureLocation))
        {
          value.Features.Add(this.FeatureLocation);
        }
      }
    }

    public FeatureLocation FeatureLocation { get; private set; }

    private long _offset = int.MaxValue;
    public long Offset
    {
      get
      {
        if (_offset == int.MaxValue)
        {
          CalculateOffset();
        }
        return _offset;
      }
      set
      {
        _offset = value;
      }
    }

    public int NumberOfMismatch { get; set; }

    public int NumberOfNoPenaltyMutation { get; set; }

    public double OverlapPercentage { get; set; }

    private void CalculateOffset()
    {
      if (FeatureLocation == null || _samLocation == null)
      {
        _offset = int.MaxValue;
      }
      else
      {
        _offset = _samLocation.Offset(FeatureLocation);
      }
    }
  }
}
