using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome
{
  public static class GenomeUtils
  {
    public static string GetKey(string chr, object pos)
    {
      return string.Format("{0}_{1}", chr, pos);
    }

    public static string GetKey<T>(T obj, Func<T, string> getChr, Func<T, object> getPos)
    {
      return GetKey(getChr(obj), getPos(obj));
    }

    public static int CompareChromosome(string chr1, string chr2)
    {
      int chrNumber1, chrNumber2;

      var n1 = int.TryParse(chr1, out chrNumber1);
      var n2 = int.TryParse(chr2, out chrNumber2);

      if (n1)
      {
        if (n2)
        {
          return chrNumber1.CompareTo(chrNumber2);
        }
        else
        {
          return -1;
        }
      }
      else if (n2)
      {
        return 1;
      }
      else if (chr1.Equals(chr2))
      {
        return 0;
      }
      else if (chr1.Equals("M") || chr1.Equals("MT"))
      {
        return 1;
      }
      else if (chr2.Equals("M") || chr2.Equals("MT"))
      {
        return -1;
      }
      else
      {
        return chr1.CompareTo(chr2);
      }
    }

    public static void SortChromosome<T>(List<T> items, Func<T, string> getChromosome, Func<T, long> getPosition)
    {
      var trychr = -1;

      var map = (from item in items
                 let chr = getChromosome(item)
                 let chrIsNumber = int.TryParse(chr, out trychr)
                 let chrNumber = chrIsNumber ? int.Parse(chr) : -1
                 let position = getPosition(item)
                 select new
                 {
                   Item = item,
                   Chr = chr,
                   ChrIsNumber = chrIsNumber,
                   ChrNumber = chrNumber,
                   Position = position
                 }).ToDictionary(m => m.Item);

      items.Sort((i1, i2) =>
      {
        var m1 = map[i1];
        var m2 = map[i2];

        int result;
        if (m1.ChrIsNumber && m2.ChrIsNumber)
        {
          result = m1.ChrNumber.CompareTo(m2.ChrNumber);
        }
        else if (!m1.ChrIsNumber && !m2.ChrIsNumber)
        {
          result = m1.Chr.CompareTo(m2.Chr);
        }
        else if (m1.ChrIsNumber)
        {
          result = -1;
        }
        else
        {
          result = 1;
        }

        if (result == 0)
        {
          result = m1.Position.CompareTo(m2.Position);
        }

        return result;
      });
    }
  }
}