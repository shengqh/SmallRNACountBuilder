using System;
using System.Diagnostics;
using System.IO;

namespace CQS
{
  public abstract class AbstractProcessReader : InputFileLineReader
  {
    private bool _disposed;
    private Process _proc;
    private string _tools;

    public AbstractProcessReader(string tools)
    {
      _tools = tools;
      _disposed = false;
      _proc = null;
    }

    public AbstractProcessReader(string tools, string filename)
      : this(tools)
    {
      OpenWithException(filename);
    }

    protected override TextReader OpenByProcess(string filename)
    {
      _proc = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = _tools,
          Arguments = GetProcessArguments(filename),
          UseShellExecute = false,
          RedirectStandardOutput = true,
          CreateNoWindow = true
        }
      };

      Console.Out.WriteLine("running command : " + _proc.StartInfo.FileName + " " + _proc.StartInfo.Arguments);
      try
      {
        if (!_proc.Start())
        {
          Console.Out.WriteLine("Cannot start {0} command!", _tools);
          return null;
        }
      }
      catch (Exception ex)
      {
        Console.Out.WriteLine("Cannot start {0} command : {1}", _tools, ex.Message);
        return null;
      }

      return _proc.StandardOutput;
    }

    protected override void Dispose(bool disposing)
    {
      // If you need thread safety, use a lock around these  
      // operations, as well as in your methods that use the resource. 
      if (!_disposed)
      {
        if (disposing)
        {
          if (_proc != null)
            _proc.Dispose();
        }

        // Indicate that the instance has been disposed.
        _proc = null;
        _disposed = true;
      }

      base.Dispose(disposing);
    }

    protected abstract string GetProcessArguments(string filename);
  }
}
