using CQS.Genome.Mapping;
using CQS.Genome.Sam;
using CQS.Genome.SmallRNA;
using RCPA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CQS.Genome.Gsnap
{
  public class SAMAlignedItemCandidateGsnapBuilder : AbstractSAMAlignedItemCandidateBuilder
  {
    private Dictionary<char, char> mutations;
    private HashSet<char> validMutations;

    /// <summary>
    /// Constructor of SAMAlignedItemCandidateBuilder
    /// </summary>
    public SAMAlignedItemCandidateGsnapBuilder(ISAMAlignedItemParserOptions options, Dictionary<char, char> mutations = null, char[] validMutations = null)
      : base(options)
    {
      if (mutations == null)
      {
        this.mutations = new Dictionary<char, char>();
        this.mutations['C'] = 'T';
        this.mutations['G'] = 'A';
      }
      else
      {
        this.mutations = mutations;
      }

      if (validMutations == null)
      {
        if (mutations == null)
        {
          this.validMutations = new HashSet<char>(new char[] { 'C' });
        }
        else
        {
          this.validMutations = new HashSet<char>(mutations.Keys);
        }
      }
      else
      {
        this.validMutations = new HashSet<char>(validMutations);
      }
    }

    private static Regex locReg = new Regex(@"([+-])(.+):(\d+)\.\.(\d+)");
    protected override List<T> DoBuild<T>(string fileName, out List<QueryInfo> totalQueries)
    {
      var result = new List<T>();

      totalQueries = new List<QueryInfo>();

      using (var sr = StreamUtils.GetReader(fileName))
      {
        int count = 0;
        int waitingcount = 0;
        string line;
        while ((line = sr.ReadLine()) != null)
        {
          if (!line.StartsWith(">"))
          {
            continue;
          }

          if (count % 10000 == 0)
          {
            if (Progress.IsCancellationPending())
            {
              throw new UserTerminatedException();
            }
          }

          if (count % 1000000 == 0 && count > 0)
          {
            Progress.SetMessage("{0} candidates from {1} reads", waitingcount, count);
          }

          count++;

          //just for test
          //if (waitingcount == 10000)
          //{
          //  break;
          //}

          var parts = line.Split('\t');
          var qname = parts[3];
          bool hasNTATag = qname.HasNTATag();
          bool hasNTA = qname.HasNTA();

          if (_options.IgnoreNTA)
          {
            if (hasNTA)
            {
              continue;
            }
          }

          var qi = new QueryInfo(qname);

          totalQueries.Add(qi);

          int matchCount = int.Parse(parts[1]);
          if (matchCount == 0)
          {
            continue;
          }

          var seq = parts[0].Substring(1);
          qi.Length = seq.Length;

          //contains 'N'
          if (seq.Contains('N'))
          {
            continue;
          }

          //too short
          if (seq.Length < _options.MinimumReadLength)
          {
            continue;
          }

          //too long
          if (seq.Length > _options.MaximumReadLength)
          {
            continue;
          }

          var sam = new T()
          {
            Qname = qname,
            Sequence = seq
          };

          for (int i = 0; i < matchCount; i++)
          {
            string matchline = sr.ReadLine();

            if (string.IsNullOrWhiteSpace(matchline))
            {
              sam.ClearLocations();
              break;
            }

            var matchparts = matchline.Split('\t');
            var matchgenome = matchparts[0].Trim();

            if (matchgenome.Contains('-'))//insertion or deletion, not allowed now
            {
              continue;
            }

            if (matchgenome.Contains('*'))//soft clip, not allowed now
            {
              continue;
            }

            string mismatchPosition = string.Empty;
            string cigar = string.Empty;
            int mismatch;
            int mutation;
            GetMismatchPositions(seq, matchgenome, ref mismatchPosition, ref cigar, out mutation, out mismatch);
            qi.Mismatch = mismatch;
            if (mismatch > _options.MaximumMismatch)
            {
              continue;
            }
            if (mutation > _options.MaximumNoPenaltyMutationCount)
            {
              continue;
            }

            if (_options.IgnoreNTAAndNoPenaltyMutation)
            {
              if (mutation > 0)
              {
                if (hasNTA)
                {
                  continue;
                }

                if (hasNTATag)
                {
                  var pos = cigar.LastIndexOf('.');
                  if (pos >= cigar.Length - 3)
                  {
                    continue;
                  }
                }
              }
            }

            var match = locReg.Match(matchparts[2]);
            var strand = match.Groups[1].Value[0];
            var chr = match.Groups[2].Value;
            var start = int.Parse(match.Groups[3].Value);
            var end = int.Parse(match.Groups[4].Value);

            var loc = new SAMAlignedLocation(sam)
            {
              Seqname = chr,
              Start = strand == '+' ? start : end,
              End = strand == '+' ? end : start,
              Strand = strand,
              NumberOfMismatch = mismatch,
              NumberOfNoPenaltyMutation = mutation,
              Cigar = cigar,
              MismatchPositions = mismatchPosition
            };

            sam.AddLocation(loc);
          }

          if (sam.Locations.Count > 0)
          {
            if (sam.Locations.Count > 1)
            {
              var minNNPM = sam.Locations.Min(m => m.NumberOfNoPenaltyMutation);
              sam.RemoveLocation(m => m.NumberOfNoPenaltyMutation > minNNPM);
            }

            result.Add(sam);
            waitingcount++;
          }
        }

        Progress.SetMessage("Finally, there are {0} candidates from {1} reads", waitingcount, count);
      }

      return result;
    }

    public void GetMismatchPositions(string seq, string matchgenome, ref string mismatchPosition, ref string cigar, out int nnmpCount, out int mismatchCount)
    {
      var cigarStr = new StringBuilder();
      var misp = new StringBuilder();

      nnmpCount = 0;
      mismatchCount = 0;

      int match = 0;
      for (int i = 0; i < seq.Length; i++)
      {
        if (matchgenome[i] == seq[i])
        {
          match++;
          cigarStr.Append(seq[i]);
          continue;
        }
        else
        {
          char mutation = ' ';
          if (matchgenome[i] == '.')
          {
            if (this.mutations.ContainsKey(seq[i]))
            {
              mutation = this.mutations[seq[i]];
            }
            else
            {
              throw new Exception(string.Format("Failed to parse {0} in {1} position of {2}", matchgenome[i], i, matchgenome));
            }

            if (this.validMutations.Contains(seq[i]))
            {
              cigarStr.Append('.');
              nnmpCount++;
            }
            else
            {
              cigarStr.Append(Char.ToLower(seq[i]));
              mismatchCount++;
            }
          }
          else if (char.IsLower(matchgenome[i]))
          {
            cigarStr.Append(matchgenome[i]);
            mutation = Char.ToUpper(matchgenome[i]);
          }
          else
          {
            throw new Exception(string.Format("Failed to parse {0} in {1} position of {2}", matchgenome[i], i, matchgenome));
          }
          if (match > 0 || misp.Length == 0)
          {
            misp.Append(match);
          }
          match = 0;
          misp.Append(mutation);
        }
      }

      if (match > 0)
      {
        misp.Append(match);
      }

      mismatchPosition = misp.ToString();
      cigar = cigarStr.ToString();
    }
  }
}
