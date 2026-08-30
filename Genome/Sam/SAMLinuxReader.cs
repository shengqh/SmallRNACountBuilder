using System.Collections.Generic;

namespace CQS.Genome.Sam
{
  public class SAMLinuxReader : AbstractProcessReader, ISAMFile
  {
    private string rangeInBedFile;

    public SAMLinuxReader(string samtools)
      : base(samtools)
    { }

    public SAMLinuxReader(string samtools, string filename, string rangeInBedFile = null)
      : this(samtools)
    {
      this.rangeInBedFile = rangeInBedFile;
      OpenWithException(filename);
    }

    public override bool NeedProcess(string filename)
    {
      return SAMUtils.IsBAMFile(filename);
    }

    protected override string GetProcessArguments(string filename)
    {
      if (string.IsNullOrEmpty(this.rangeInBedFile))
      {
        return string.Format("view -h {0}", filename);
      }
      else
      {
        return string.Format("view -h {0} -L {1}", filename, this.rangeInBedFile);
      }
    }

    public List<string> ReadHeaders()
    {
      List<string> result = new List<string>();
      while (this.reader.Peek() == (int)'@')
      {
        result.Add(this.reader.ReadLine());
      }
      return result;
    }
  }
}
