namespace CQS.Genome.Pileup
{
  public class EventCount
  {
    public EventCount(string name, int count)
    {
      this.Event = name;
      this.Count = count;
    }
    public string Event { get; set; }
    public int Count { get; set; }
  }
}
