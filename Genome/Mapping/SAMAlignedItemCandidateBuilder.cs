using Bio.IO.SAM;
using CQS.Genome.Sam;
using RCPA;
using RCPA.Seq;
using System;
using System.Collections.Generic;

namespace CQS.Genome.Mapping
{
  public class SAMAlignedItemCandidateBuilder : AbstractSAMAlignedItemCandidateBuilder
  {
    /// <summary>
    /// Constructor of SAMAlignedItemCandidateBuilder
    /// </summary>
    /// <param name="engineType">1:bowtie1, 2:bowtie2, 3:bwa</param>
    public SAMAlignedItemCandidateBuilder(int engineType)
      : base(engineType)
    { }

    public SAMAlignedItemCandidateBuilder(ISAMAlignedItemParserOptions options)
      : base(options)
    { }

    protected virtual bool AcceptQueryName(string qname)
    {
      return true;
    }

    protected override List<T> DoBuild<T>(string fileName, out List<QueryInfo> totalQueries)
    {
      var result = new List<T>();

      _format = _options.GetSAMFormat();

      totalQueries = new List<QueryInfo>();

      using (var sr = SAMFactory.GetReader(fileName, true))
      {
        int count = 0;
        int waitingcount = 0;
        string line;
        while ((line = sr.ReadLine()) != null)
        {
          if (count % 1000 == 0)
          {
            if (Progress.IsCancellationPending())
            {
              throw new UserTerminatedException();
            }
          }

          if (count % 1000000 == 0 && count > 0)
          {
            Progress.ShowCurrentMemory(string.Format("{0} candidates from {1} reads", waitingcount, count));
          }

          count++;
          var qname = line.StringBefore("\t");
          //Console.WriteLine("line = {0}", line);
          //Console.WriteLine("query = {0}", qname);

          var qi = new QueryInfo(qname);
          totalQueries.Add(qi);

          var parts = line.Split('\t');
          SAMFlags flag = (SAMFlags)int.Parse(parts[SAMFormatConst.FLAG_INDEX]);
          //unmatched
          if (flag.HasFlag(SAMFlags.UnmappedQuery))
          {
            continue;
          }

          //too many mismatchs
          var mismatchCount = _format.GetNumberOfMismatch(parts);
          var seq = parts[SAMFormatConst.SEQ_INDEX];

          qi.Mismatch = mismatchCount;
          qi.Length = seq.Length;
          qi.NoPenaltyMutation = 0;

          if (_options.T2cAsNoPenaltyMutation)
          {

          }

          if (mismatchCount > _options.MaximumMismatch)
          {
            continue;
          }

          if (!AcceptQueryName(qname))
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

          var cigar = parts[SAMFormatConst.CIGAR_INDEX];
          ////insertion/deletion
          //if (cigar.Any(m => m == 'I' || m == 'D'))
          //{
          //  continue;
          //}

          bool isReversed = flag.HasFlag(SAMFlags.QueryOnReverseStrand);
          char strand;
          if (isReversed)
          {
            strand = '-';
            seq = SequenceUtils.GetReverseComplementedSequence(seq);
          }
          else
          {
            strand = '+';
          }

          var score = _format.GetAlignmentScore(parts);

          var sam = new T()
          {
            Qname = qname,
            Sequence = seq
          };

          var seqname = parts[SAMFormatConst.RNAME_INDEX];
          var loc = new SAMAlignedLocation(sam)
          {
            Seqname = seqname,
            Start = int.Parse(parts[SAMFormatConst.POS_INDEX]),
            Strand = strand,
            Cigar = cigar,
            NumberOfMismatch = mismatchCount,
            AlignmentScore = score,
            MismatchPositions = _format.GetMismatchPositions(parts)
          };

          loc.ParseEnd(sam.Sequence);
          sam.AddLocation(loc);

          if (_format.HasAlternativeHits)
          {
            _format.ParseAlternativeHits(parts, sam);
          }

          result.Add(sam);

          waitingcount++;
        }

        Progress.ShowCurrentMemory(string.Format("Finally, there are {0} candidates from {1} reads", waitingcount, count));
      }

      return result;
    }
  }
}
