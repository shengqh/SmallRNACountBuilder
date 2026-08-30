using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.Pileup
{
  public enum StrandType { UNKNOWN, FORWARD, REVERSE };
  public enum AlignedEventType { UNKNOWN, MATCH, MISMATCH, INSERTION, DELETION };
  public enum PositionType { UNKNOWN, START, MIDDLE, END };

  public class PileupBase
  {
    public PileupBase()
    {
      this.Strand = StrandType.UNKNOWN;
      this.EventType = AlignedEventType.UNKNOWN;
      this.Position = PositionType.UNKNOWN;
      this.Event = string.Empty;
      this.Score = 0;
      this.ReadMappingQuality = 0;
    }

    /// <summary>
    /// StrandType { UNKNOWN, FORWARD, REVERSE }
    /// </summary>
    public StrandType Strand { get; set; }

    /// <summary>
    /// EventType { UNKNOWN, MATCH, MISMATCH, INSERTION, DELETION }
    /// </summary>
    public AlignedEventType EventType { get; set; }

    /// <summary>
    /// PositionType { UNKNOWN, START, MIDDLE, END }
    /// </summary>
    public PositionType Position { get; set; }

    /// <summary>
    /// The position of base in the read
    /// </summary>
    public string PositionInRead { get; set; }

    public string Event { get; set; }

    public int Score { get; set; }

    public int ReadMappingQuality { get; set; }

    public bool PassScoreFilter(int minimumMappingQuality)
    {
      return this.Score >= minimumMappingQuality ||
        this.EventType == Pileup.AlignedEventType.INSERTION ||
        this.EventType == Pileup.AlignedEventType.DELETION;
    }
  }

  public class PileupBaseList : List<PileupBase>
  {
    public string SampleName { get; set; }

    private List<EventCount> _eventCountList;
    public List<EventCount> EventCountList
    {
      get
      {
        if (_eventCountList == null)
        {
          InitEventCountList();
        }
        return _eventCountList;
      }
      set
      {
        _eventCountList = value;
      }
    }

    public void InitEventCountList(bool orderByCount = true)
    {
      var map = new Dictionary<string, EventCount>();
      foreach (var e in this)
      {
        EventCount ec;
        if (map.TryGetValue(e.Event, out ec))
        {
          ec.Count++;
        }
        else
        {
          ec = new EventCount(e.Event, 1);
          map[e.Event] = ec;
        }
      }

      if (orderByCount)
      {
        _eventCountList = (from ec in map.Values
                           orderby ec.Count descending
                           select ec).ToList();
      }
      else
      {
        _eventCountList = (from ec in map.Values
                           orderby ec.Event
                           select ec).ToList();
      }
    }

    public void InitEventCountList(int minimumMappingQuality, bool orderByCount = true)
    {
      var map = new Dictionary<string, EventCount>();
      foreach (var e in this)
      {
        if (!e.PassScoreFilter(minimumMappingQuality))
        {
          continue;
        }

        EventCount ec;
        if (map.TryGetValue(e.Event, out ec))
        {
          ec.Count++;
        }
        else
        {
          ec = new EventCount(e.Event, 1);
          map[e.Event] = ec;
        }
      }

      if (orderByCount)
      {
        _eventCountList = (from ec in map.Values
                           orderby ec.Count descending
                           select ec).ToList();
      }
      else
      {
        _eventCountList = (from ec in map.Values
                           orderby ec.Event
                           select ec).ToList();
      }
    }
  }
}
