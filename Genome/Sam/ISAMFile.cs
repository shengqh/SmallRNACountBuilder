using System;
using System.Collections.Generic;

namespace CQS.Genome.Sam
{
  public interface ISAMFile : ILineFile, IDisposable
  {
    List<string> ReadHeaders();
  }
}
