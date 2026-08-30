using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS
{
  public abstract class AbstractTableFile<T> : LineFile where T : new()
  {
    private int minLength = 0;

    protected List<int> actionIndecies = new List<int>();

    private Dictionary<int, Action<string, T>> indexActionMap = null;

    public AbstractTableFile() : base() { }

    public AbstractTableFile(string filename) : base(filename) { }

    /// <summary>
    /// Initialize index header map after file is opened.
    /// </summary>
    protected override void DoAfterOpen()
    {
      base.DoAfterOpen();

      this.indexActionMap = GetIndexActionMap();
      this.actionIndecies = (from key in indexActionMap.Keys
                             orderby key
                             select key).ToList();
      this.minLength = this.actionIndecies.Last() + 1;
    }

    /// <summary>
    /// Initialize index action map
    /// </summary>
    /// <returns>max</returns>
    protected abstract Dictionary<int, Action<string, T>> GetIndexActionMap();

    protected virtual int MinLength
    {
      get
      {
        return this.minLength;
      }
    }

    /// <summary>
    /// get next item
    /// </summary>
    /// <returns>next item</returns>
    public virtual T Next()
    {
      string line;
      while ((line = reader.ReadLine()) != null)
      {
        if (!AcceptItem(line))
        {
          continue;
        }

        var parts = line.Split('\t');
        if (parts.Length < this.MinLength)
        {
          continue;
        }

        var item = new T();
        for (int i = 0; i < actionIndecies.Count; i++)
        {
          var index = actionIndecies[i];
          if (index >= parts.Length)
          {
            break;
          }

          indexActionMap[index](parts[index], item);
        }

        return item;
      }

      return default(T);
    }

    /// <summary>
    /// If line should be parsed.
    /// </summary>
    protected virtual bool AcceptItem(string line)
    {
      return true;
    }

    /// <summary>
    /// Read list from file
    /// </summary>
    /// <param name="fileName">data file name</param>
    /// <returns>list of item</returns>
    public virtual List<T> ReadFromFile(string fileName)
    {
      List<T> result = new List<T>();
      if (!Open(fileName))
      {
        throw new Exception("Cannot open file " + fileName);
      }

      T item;
      while ((item = Next()) != null)
      {
        result.Add(item);
      }

      return result;
    }
  }
}
