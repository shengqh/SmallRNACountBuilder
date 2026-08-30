using CQS.Genome.SmallRNA;
using System;

namespace CQS
{
  internal static class Program
  {
    private static int Main(string[] args)
    {
      var command = new SmallRNACountProcessorCommand();

      if (args.Length == 0)
      {
        Console.WriteLine(command.Name + "\t" + command.Description);
      }

      return command.Process(args) ? 0 : 1;
    }
  }
}
