using System;
using System.IO;

namespace CQS
{
  public class InputFileLineReader : LineFile, IInputFileLineReader
  {
    public InputFileLineReader()
    { }

    public InputFileLineReader(string filename)
      : base(filename)
    { }

    public override bool Open(string filename)
    {
      try
      {
        if (filename.Equals("-"))
        {
          return base.Open(Console.In);
        }
        else if (!NeedProcess(filename))
        {
          return base.Open(filename);
        }
        else
        {
          return base.Open(OpenByProcess(filename));
        }
      }
      catch (Exception ex)
      {
        throw new ArgumentException(string.Format("Cannot open file {0} : {1}", filename, ex.Message));
      }
    }

    public virtual bool NeedProcess(string filename)
    {
      return false;
    }

    protected virtual TextReader OpenByProcess(string filename)
    {
      throw new NotImplementedException();
    }
  }
}
