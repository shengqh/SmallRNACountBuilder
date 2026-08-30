using CommandLine;
using CQS.Genome.Mapping;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNACountProcessorOptions : AbstractSmallRNACountProcessorOptions, ICountProcessorOptions, ISmallRNAExport
  {
    [Option("noCategory", HelpText = "No category in coordindates file, treat all as one category")]
    public bool NoCategory { get; set; }

    [Option("ccaFile", Required = false, MetaValue = "FILE", HelpText = "NTA with 3' CC from original CCA file")]
    public string CCAFile { get; set; }

    [Option("exportYRNA", HelpText = "Export yRNA individually")]
    public bool ExportYRNA { get; set; }

    [Option("exportSnRNA", HelpText = "Export snRNA individually")]
    public bool ExportSnRNA { get; set; }

    [Option("exportSnoRNA", HelpText = "Export snoRNA individually")]
    public bool ExportSnoRNA { get; set; }

    [Option("exportERV", HelpText = "Export ERV individually")]
    public bool ExportERV { get; set; }

    [Option("newMethod", HelpText = "Using fast method to parse result (Deprecated)")]
    public bool NewMethod { get; set; }

    public SmallRNACountProcessorOptions()
    {
    }

    public override ICandidateBuilder GetCandidateBuilder()
    {
      if (this.EngineType != 4) //not gsnap
      {
        return new SmallRNACandidateBuilder(this);
      }
      else
      {
        return base.GetCandidateBuilder();
      }
    }
  }
}
