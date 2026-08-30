namespace CQS.Genome.SmallRNA
{
  public class SmallRNAMapperRRNADB : SmallRNAMapperLongRNA
  {
    public SmallRNAMapperRRNADB(ISmallRNACountProcessorOptions options) : base("rRNADB", options, feature => feature.Name.Contains(SmallRNAConsts.rRNADB_KEY) || feature.Name.Contains("SILVA_"))
    { }
  }
}
