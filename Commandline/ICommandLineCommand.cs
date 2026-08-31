using CommandLine;
using CommandLine.Text;
using System;
using System.IO;
using System.Linq;

namespace RCPA.Commandline
{
  public interface ICommandLineCommand
  {
    string Name { get; }

    string Description { get; }

    bool Process(string[] args);

    bool LinuxSupported { get; }
  }

  public abstract class AbstractCommandLineCommand<T> : ICommandLineCommand where T : AbstractOptions, new()
  {
    #region ICommandLineCommand Members

    public abstract string Name { get; }

    public abstract string Description { get; }

    public virtual bool LinuxSupported { get { return true; } }

    public virtual bool Process(string[] args)
    {
      var result = true;
      if (System.Diagnostics.Debugger.IsAttached)
      {
        result = DoProcess(args, result);
      }
      else
      {
        try
        {
          result = DoProcess(args, result);
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine("Error: " + ex.Message);
          Console.Error.WriteLine("Trace: " + ex.StackTrace);
          result = false;
        }
      }

      return result;
    }

    private bool DoProcess(string[] args, bool result)
    {
      var parserResult = new Parser(with => { with.HelpWriter = null; }).ParseArguments<T>(args);

      return parserResult.MapResult(
        options =>
        {
          if (!options.PrepareOptions())
          {
            Console.Out.WriteLine(BuildUsage(parserResult, options));
            return false;
          }

          options.ResetDefaultValue(args);

          var files = GetProcessor(options).Process();
          if (files != null && files.Count() > 0)
          {
            if (files.All(File.Exists))
            {
              Console.WriteLine("File saved to :\n" + files.Merge("\n"));
            }
            else
            {
              Console.WriteLine(files.Merge("\n"));
            }
          }
          return true;
        },
        errors =>
        {
          Console.Out.WriteLine(BuildUsage(parserResult, null));
          return false;
        });
    }

    private static string BuildUsage(ParserResult<T> parserResult, T options)
    {
      var helpText = HelpText.AutoBuild(parserResult, h => HelpText.DefaultParsingErrorsHandler(parserResult, h), e => e);

      if (options == null || options.ParsingErrors.Count == 0)
      {
        return helpText.ToString();
      }

      var sb = new System.Text.StringBuilder();
      sb.AppendLine("ERROR(S):");
      foreach (var line in options.ParsingErrors)
      {
        sb.AppendLine("  " + line);
      }
      sb.AppendLine();
      sb.Append(helpText.ToString());
      return sb.ToString();
    }

    public abstract IProcessor GetProcessor(T options);

    #endregion
  }
}