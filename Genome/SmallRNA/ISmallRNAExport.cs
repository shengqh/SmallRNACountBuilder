namespace CQS.Genome.SmallRNA
{
  public interface ISmallRNAExport
  {
    bool ExportYRNA { get; set; }
    bool ExportSnRNA { get; set; }
    bool ExportSnoRNA { get; set; }
    bool ExportERV { get; set; }
  }
}
