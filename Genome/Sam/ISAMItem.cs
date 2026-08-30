namespace CQS.Genome.Sam
{
  public interface ISAMItem
  {
    /// <summary>
    /// Query template NAME. Reads/segments having identical QNAME are regarded to come from the same template. 
    /// A QNAME `*' indicates the information is unavailable.
    /// </summary>
    string Qname { get; }

    /// <summary>
    /// 1-based leftmost mapping POSition of the rst matching base. The rst base in a reference
    /// sequence has coordinate 1. POS is set as 0 for an unmapped read without coordinate. If POS is
    /// 0, no assumptions can be made about RNAME and CIGAR.
    /// </summary>
    long Pos { get; }

    /// <summary>
    /// segment SEQuence. This eld can be a `*' when the sequence is not stored. If not a `*',
    /// the length of the sequence must equal the sum of lengths of M/I/S/=/X operations in CIGAR.
    /// An `=' denotes the base is identical to the reference base. No assumptions can be made on the
    /// letter cases.
    /// </summary>
    string Sequence { get; }

    /// <summary>
    /// ASCII of base QUALity plus 33 (same as the quality string in the Sanger FASTQ format).
    /// A base quality is the phred-scaled base error probability which equals 10 log10 Prfbase is wrongg.
    /// This eld can be a `*' when quality is not stored. If not a `*', SEQ must not be a `*' and the
    /// length of the quality string ought to equal the length of SEQ.
    /// </summary>
    string Qual { get; }

    int AlignmentScore { get; }
  }
}
