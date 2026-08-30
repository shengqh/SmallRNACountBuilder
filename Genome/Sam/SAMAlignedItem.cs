using CQS.Genome.Pileup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CQS.Genome.Sam
{
  public class SAMAlignedItem : ISAMItem
  {
    public SAMAlignedItem()
    {
      this._locations = new List<SAMAlignedLocation>();
    }

    /// <summary>
    /// The sample name that this mapped result from 
    /// </summary>
    public string Sample { get; set; }

    public string Qname { get; set; }

    public string Sequence { get; set; }

    public void InitializeDistinctSeqnameCount()
    {
      this.DistinctSeqnameCount = (from l in this.Locations
                                   select l.Seqname).Distinct().Count();
    }
    public int DistinctSeqnameCount { get; private set; }

    private string _clippedNTA;
    public string ClippedNTA
    {
      get
      {
        if (_clippedNTA == null)
        {
          var index = Qname.IndexOf("CLIP_");
          if (index == -1)
          {
            _clippedNTA = string.Empty;
          }
          else
          {
            _clippedNTA = Qname.Substring(index + 5);
          }
        }

        return _clippedNTA;
      }
      set
      {
        _clippedNTA = value;
      }
    }

    public void AddLocation(SAMAlignedLocation loc)
    {
      if (loc.Parent != this && loc.Parent != null)
      {
        loc.Parent.RemoveLocation(loc);
      }

      loc.Parent = this;
      if (!this._locations.Contains(loc))
      {
        this._locations.Add(loc);
      }
    }

    public void RemoveLocation(Func<SAMAlignedLocation, bool> func)
    {
      var locs = (from l in _locations
                  where func(l)
                  select l).ToList();
      locs.ForEach(l => RemoveLocation(l));
    }

    public void RemoveLocation(SAMAlignedLocation loc)
    {
      if (this._locations.Contains(loc))
      {
        if (this == loc.Parent)
        {
          loc.Parent = null;
        }
        this._locations.Remove(loc);
      }
    }

    public void AddLocations(IEnumerable<SAMAlignedLocation> locs)
    {
      var temps = new List<SAMAlignedLocation>(locs);
      foreach (var loc in temps)
      {
        AddLocation(loc);
      }
    }

    /// <summary>
    /// The locations that query sequence mapped to.
    /// </summary>
    private List<SAMAlignedLocation> _locations;
    public ReadOnlyCollection<SAMAlignedLocation> Locations
    {
      get
      {
        return _locations.AsReadOnly();
      }
    }

    public string Cigar
    {
      get
      {
        return Locations[0].Cigar;
      }
    }

    public long Pos
    {
      get
      {
        return Locations[0].Start;
      }
    }

    public long End
    {
      get
      {
        return Locations[0].End;
      }
    }

    public string Qual
    {
      get
      {
        return Locations[0].Qual;
      }
    }

    public int AlignmentScore
    {
      get
      {
        return Locations[0].AlignmentScore;
      }
    }

    public int MapQ
    {
      get
      {
        return Locations[0].MapQ;
      }
    }

    public int NumberOfMismatch
    {
      get
      {
        return Locations[0].NumberOfMismatch;
      }
    }

    public int QueryCount { get; set; }

    public int GetFeatureCount()
    {
      return Locations.Sum(l => l.Features.Count);
    }

    public double GetEstimatedCount()
    {
      return ((double)QueryCount) / GetFeatureCount();
    }

    private Regex cigarReg = new Regex(@"(\d+)([MIDNSHPX=])");

    public void GetSequences(out string alignmentSequence, out string referenceSequence)
    {
      StringBuilder align = new StringBuilder();
      var m = cigarReg.Match(this.Cigar);
      int mindex = 0;
      var matchseq = this.Locations[0].Sequence;

      while (m.Success)
      {
        var type = m.Groups[2].Value[0];
        var count = int.Parse(m.Groups[1].Value);
        if (type == 'M')
        {
          align.Append(matchseq.Substring(mindex, count));
          mindex += count;
        }
        else if (type == 'I')
        {
          mindex += count;
        }
        else if (type == 'D')
        {
          align.Append(new string(' ', count));
        }
        else if (type == 'S')
        {
          mindex += count;
        }
        else if (type == '=')
        {
          align.Append(matchseq.Substring(mindex, count));
          mindex += count;
        }
        else if (type == 'X')
        {
          align.Append(matchseq.Substring(mindex, count));
          mindex += count;
        }
        else
        {
          throw new Exception(string.Format("I don't know how to deal with {0}: {1}", type, this.Cigar));
        }

        m = m.NextMatch();
      }

      alignmentSequence = align.ToString();

      StringBuilder refer = new StringBuilder(alignmentSequence);
      int pos = 0;
      var ms = this.Locations[0].MismatchPositions;
      int seqpos = 0;
      while (pos < ms.Length)
      {
        if (char.IsNumber(ms[pos]))
        {
          var nextpos = pos + 1;
          while (nextpos < ms.Length && char.IsNumber(ms[nextpos]))
          {
            nextpos++;
          }
          var exactMatchedCount = int.Parse(ms.Substring(pos, nextpos - pos));
          seqpos += exactMatchedCount;
          pos = nextpos;
        }
        else if (ms[pos] == '^')
        { //deletion
          pos++;
          continue;
        }
        else
        {
          refer[seqpos++] = ms[pos];
          pos++;
          continue;
        }
      }

      referenceSequence = refer.ToString();
    }

    public void GetSequenceScore(out string alignmentSequence, out string score)
    {
      StringBuilder align = new StringBuilder();
      StringBuilder sbScore = new StringBuilder();
      var m = cigarReg.Match(this.Cigar);
      int mindex = 0;
      var matchseq = this.Locations[0].Sequence;
      var matchscore = this.Locations[0].Qual;

      while (m.Success)
      {
        var type = m.Groups[2].Value[0];
        var count = int.Parse(m.Groups[1].Value);
        if (type == 'M')
        {
          align.Append(matchseq.Substring(mindex, count));
          sbScore.Append(matchscore.Substring(mindex, count));
          mindex += count;
        }
        else if (type == 'I')
        {
          mindex += count;
        }
        else if (type == 'D')
        {
          align.Append(new string(' ', count));
          sbScore.Append(new string(' ', count));
        }
        else if (type == 'S')
        {
          mindex += count;
        }
        else if (type == '=')
        {
          align.Append(matchseq.Substring(mindex, count));
          sbScore.Append(matchscore.Substring(mindex, count));
          mindex += count;
        }
        else if (type == 'X')
        {
          align.Append(matchseq.Substring(mindex, count));
          sbScore.Append(matchscore.Substring(mindex, count));
          mindex += count;
        }
        else
        {
          throw new Exception(string.Format("I don't know how to deal with {0}: {1}", type, this.Cigar));
        }

        m = m.NextMatch();
      }

      alignmentSequence = align.ToString();
      score = sbScore.ToString();
    }

    public void ClearLocations()
    {
      this._locations.Clear();
    }

    public void SortLocations()
    {
      this._locations.Sort();
    }

    /// <summary>
    /// Get matched aligned positions only
    /// </summary>
    /// <returns></returns>
    public List<AlignedPosition> GetMatchedAlignedPositions()
    {
      var result = new List<AlignedPosition>();

      var m = cigarReg.Match(this.Cigar);
      int mindex = 0;
      var matchseq = this.Locations[0].Sequence;
      var matchscore = this.Locations[0].Qual;

      var position = this.Pos;

      while (m.Success)
      {
        var type = m.Groups[2].Value[0];
        var count = int.Parse(m.Groups[1].Value);
        if (type == 'M')
        {
          DealWithMatch(result, ref mindex, matchseq, matchscore, ref position, count);
        }
        else if (type == 'I')
        {
          //For insertion, change match index only
          mindex += count;
        }
        else if (type == 'D')
        {
          //For deletion, change position only
          position += count;
        }
        else if (type == 'S')
        {
          //For soft clipping (clipped sequences present in SEQ), change position and match index
          mindex += count;
          position += count;
        }
        else if (type == '=')
        {
          DealWithMatch(result, ref mindex, matchseq, matchscore, ref position, count);
        }
        else if (type == 'X')
        {
          DealWithMatch(result, ref mindex, matchseq, matchscore, ref position, count);
        }
        else
        {
          throw new Exception(string.Format("I don't know how to deal with {0}: {1}", type, this.Cigar));
        }

        m = m.NextMatch();
      }

      return result;
    }

    /// <summary>
    /// Get aligned positions including match/insertion/deletion
    /// </summary>
    /// <returns></returns>
    public List<AlignedPosition> GetAlignedPositions()
    {
      var result = new List<AlignedPosition>();

      var m = cigarReg.Match(this.Cigar);
      int mindex = 0;
      var matchseq = this.Locations[0].Sequence;
      var matchscore = this.Locations[0].Qual;

      var position = this.Pos;

      while (m.Success)
      {
        var type = m.Groups[2].Value[0];
        var count = int.Parse(m.Groups[1].Value);
        if (type == 'M')
        {
          DealWithMatch(result, ref mindex, matchseq, matchscore, ref position, count);
        }
        else if (type == 'I')
        {
          var insert = string.Format("{0}_{1}", AlignedEventType.INSERTION, matchseq.Substring(mindex, count));
          result.Add(new AlignedPosition(position, AlignedEventType.INSERTION, insert, matchscore[mindex], mindex, count));

          //For insertion, change match index only
          mindex += count;
        }
        else if (type == 'D')
        {
          result.Add(new AlignedPosition(position, AlignedEventType.DELETION, AlignedEventType.DELETION.ToString(), matchscore[mindex], mindex, count));

          //For deletion, change position only
          position += count;
        }
        else if (type == 'S')
        {
          //For soft clipping (clipped sequences present in SEQ), change position and match index
          mindex += count;
          position += count;
        }
        else if (type == '=')
        {
          DealWithMatch(result, ref mindex, matchseq, matchscore, ref position, count);
        }
        else if (type == 'X')
        {
          DealWithMatch(result, ref mindex, matchseq, matchscore, ref position, count);
        }
        else
        {
          throw new Exception(string.Format("I don't know how to deal with {0}: {1}", type, this.Cigar));
        }

        m = m.NextMatch();
      }

      return result;
    }

    private static void DealWithMatch(List<AlignedPosition> result, ref int mindex, string matchseq, string matchscore, ref long position, int count)
    {
      for (int i = 0; i < count; i++)
      {
        result.Add(new AlignedPosition(position + i, AlignedEventType.MATCH, matchseq[mindex + i].ToString(), matchscore[mindex + i], mindex + i));
      }
      //For alignment match, change position and match index
      mindex += count;
      position += count;
    }

    private string _originalQname;
    public string OriginalQname
    {
      get
      {
        if (string.IsNullOrWhiteSpace(_originalQname))
        {
          return Qname;
        }
        else
        {
          return _originalQname;
        }
      }
      set
      {
        _originalQname = value;
      }
    }

    private string _nta;
    public string NTA
    {
      get
      {
        if (string.IsNullOrWhiteSpace(_nta))
        {
          return string.Empty;
        }
        else
        {
          return _nta;
        }
      }
      set
      {
        _nta = value;
      }
    }
  }

  public static class SAMAlignedItemExtension
  {
    public static List<SAMAlignedItem> ToSAMAlignedItems(this XElement root)
    {
      var result = new List<SAMAlignedItem>();
      foreach (var queryEle in root.Element("queries").Elements("query"))
      {
        var query = new SAMAlignedItem();
        query.Qname = queryEle.Attribute("name").Value;
        query.Sequence = queryEle.Attribute("sequence").Value;
        query.QueryCount = int.Parse(queryEle.Attribute("count").Value);
        query.Sample = queryEle.GetAttributeValue("sample", null);
        result.Add(query);
        foreach (var locEle in queryEle.Elements("location"))
        {
          var loc = new SAMAlignedLocation(query);
          loc.ParseLocation(locEle);
          loc.Cigar = locEle.Attribute("cigar").Value;
          loc.AlignmentScore = int.Parse(locEle.Attribute("score").Value);
          loc.MismatchPositions = locEle.Attribute("mdz").Value;
          loc.NumberOfMismatch = int.Parse(locEle.Attribute("nmi").Value);
          var nnmpattr = locEle.Attribute("nnpm");
          if (nnmpattr != null)
          {
            loc.NumberOfNoPenaltyMutation = int.Parse(nnmpattr.Value);
          }
        }
      }
      return result;
    }

    public static XElement ToXElement(this IEnumerable<SAMAlignedItem> queries)
    {
      return new XElement("queries",
                from q in queries
                select new XElement("query",
                  new XAttribute("name", q.Qname),
                  new XAttribute("sequence", q.Sequence),
                  new XAttribute("count", q.QueryCount),
                  from l in q.Locations
                  select new XElement("location",
                    new XAttribute("seqname", l.Seqname),
                    new XAttribute("start", l.Start),
                    new XAttribute("end", l.End),
                    new XAttribute("strand", l.Strand),
                    new XAttribute("cigar", l.Cigar),
                    new XAttribute("score", l.AlignmentScore),
                    new XAttribute("mdz", l.MismatchPositions),
                    new XAttribute("nmi", l.NumberOfMismatch),
                    new XAttribute("nnpm", l.NumberOfNoPenaltyMutation))));
    }
  }
}
