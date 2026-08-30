using CQS.Genome.Sam;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome
{
  public class SequenceRegionMapped
  {
    public SequenceRegionMapped()
    {
      this.AlignedLocations = new List<SAMAlignedLocation>();
      this.Region = new SequenceRegion();
    }

    public ISequenceRegion Region { get; set; }

    public int Offset { get; set; }

    public int _queryCountBeforeFilter;

    public int QueryCountBeforeFilter
    {
      get
      {
        if (_queryCountBeforeFilter == 0)
        {
          return QueryCount;
        }
        return _queryCountBeforeFilter;
      }
      set
      {
        _queryCountBeforeFilter = value;
      }
    }

    public int QueryCount
    {
      get
      {
        return this.AlignedLocations.Sum(m => m.Parent.QueryCount);
      }
    }

    public List<SAMAlignedLocation> AlignedLocations { get; private set; }

    public double GetEstimatedCount()
    {
      return this.AlignedLocations.Sum(m => m.Parent.GetEstimatedCount());
    }

    public double GetEstimatedCount(Func<SAMAlignedLocation, bool> accept)
    {
      return (from loc in this.AlignedLocations
              where accept(loc)
              select loc.Parent.GetEstimatedCount()).Sum();
    }

    public double PValue { get; set; }
  }
}
