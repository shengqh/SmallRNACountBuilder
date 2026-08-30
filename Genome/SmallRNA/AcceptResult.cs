namespace CQS.Genome.SmallRNA
{
  public class AcceptResult
  {
    public bool Accepted { get; set; }
    public int NumberOfMismatch { get; set; }
    public int NumberOfNoPenaltyMutation { get; set; }
    public double OverlapPercentage { get; set; }
  }
}
