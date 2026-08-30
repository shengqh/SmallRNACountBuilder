using RCPA.Utils;
using System;
using System.Collections.Generic;

namespace CQS.Genome.Sam
{
  public static class SAMFactory
  {
    private static Dictionary<int, Func<ISAMFormat>> formats;

    static SAMFactory()
    {
      formats = new Dictionary<int, Func<ISAMFormat>>();
      //1:bowtie1, 2:bowtie2, 3:bwa, 4:gsnap, 5:star
      formats[1] = () => new Bowtie1Format();
      formats[2] = () => new Bowtie2Format();
      formats[3] = () => new BwaFormat();
      formats[4] = () => null;
      formats[5] = () => new StarFormat();
    }

    public static ISAMFile GetReader(string filename, bool skipHeaders = false, string rangeInBedFile = null)
    {
      ISAMFile result = null;
      if (SAMUtils.IsBAMFile(filename) && !SystemUtils.IsLinux)
      {
        result = new BAMWindowReader(filename);
      }
      else
      {
        result = new SAMLinuxReader("samtools", filename, rangeInBedFile);
      }

      if (skipHeaders)
      {
        result.ReadHeaders();
      }

      return result;
    }

    public static ISAMFormat GetFormat(int engineType)
    {
      if (formats.ContainsKey(engineType))
      {
        return formats[engineType]();
      }
      else
      {
        return null;
      }
    }

  }
}
