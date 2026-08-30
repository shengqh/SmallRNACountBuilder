namespace CQS.Genome.Pileup
{
  /// <summary>
  /// Aligned information for specific position from one read
  /// </summary>
  public class AlignedPosition
  {
    /// <summary>
    /// Position in chromosome
    /// </summary>
    public long Position { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    public AlignedEventType EventType { get; set; }

    /// <summary>
    /// Allele/Insertion/Deletion at that position
    /// </summary>
    public string AlignedEvent { get; set; }

    /// <summary>
    /// Matched score
    /// </summary>
    public char Score { get; set; }

    /// <summary>
    /// The distance to the 3' terminal of read
    /// </summary>
    public int Distance { get; set; }

    /// <summary>
    /// Event length for insertion/deletion
    /// </summary>
    public int EventLength { get; set; }

    public AlignedPosition()
    {
      this.Position = -1;
      this.EventType = Pileup.AlignedEventType.UNKNOWN;
      this.AlignedEvent = string.Empty;
      this.Score = (char)0;
      this.Distance = -1;
    }

    public AlignedPosition(long position, AlignedEventType at, string alignEvent, char score, int distance, int eventLength = 1)
    {
      this.Position = position;
      this.EventType = at;
      this.AlignedEvent = alignEvent;
      this.Score = score;
      this.Distance = distance;
      this.EventLength = eventLength;
    }
  }
}
