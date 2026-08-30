using System;
using System.IO;

namespace CQS
{
  public class LineFile : ILineFile, IDisposable
  {
    private bool _disposed;

    private bool _closeNeeded;

    protected TextReader reader = null;

    // The stream passed to the constructor  
    // must be readable and not null. 
    public LineFile()
    {
      _disposed = false;
      _closeNeeded = false;
      reader = null;
    }

    public LineFile(string filename)
      : this()
    {
      if (!Open(filename))
      {
        throw new ArgumentException(string.Format("Cannot open file {0}", filename));
      }
    }

    protected virtual void DoAfterOpen() { }

    protected void OpenWithException(string filename)
    {
      if (!Open(filename))
      {
        throw new ArgumentException(string.Format("Cannot open file {0}", filename));
      }
    }

    public virtual bool Open(string filename)
    {
      Close();
      try
      {
        reader = new StreamReader(filename);
        _closeNeeded = true;
        DoAfterOpen();
        return true;
      }
      catch (Exception)
      {
        reader = null;
        return false;
      }
    }

    public virtual bool Open(Stream source)
    {
      Close();
      try
      {
        reader = new StreamReader(source);
        _closeNeeded = false;
        DoAfterOpen();
        return true;
      }
      catch (Exception)
      {
        reader = null;
        return false;
      }
    }

    public virtual bool Open(TextReader source)
    {
      Close();
      if (source == null)
      {
        return false;
      }

      reader = source;
      _closeNeeded = false;
      DoAfterOpen();
      return true;
    }

    public virtual void Close()
    {
      if (reader != null && _closeNeeded)
      {
        reader.Close();
        reader = null;
        _closeNeeded = false;
      }
    }

    public virtual void Reset()
    {
      if (reader != null && (reader is StreamReader))
      {
        (reader as StreamReader).SetCharpos(0);
      }
    }

    public void Dispose()
    {
      Dispose(true);

      // Use SupressFinalize in case a subclass 
      // of this type implements a finalizer.
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      // If you need thread safety, use a lock around these  
      // operations, as well as in your methods that use the resource. 
      if (!_disposed)
      {
        if (disposing)
        {
          if (reader != null && _closeNeeded)
            reader.Dispose();
        }

        // Indicate that the instance has been disposed.
        reader = null;
        _disposed = true;
        _closeNeeded = false;
      }
    }

    ~LineFile()
    {
      Dispose(false);
    }

    public TextReader Reader
    {
      get { return reader; }
    }

    public virtual string ReadLine()
    {
      return reader.ReadLine();
    }
  }
}
