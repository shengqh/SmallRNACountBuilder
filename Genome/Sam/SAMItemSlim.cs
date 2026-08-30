namespace CQS.Genome.Sam
{
  public class SAMItemSlim : ISAMItem
  {
    private long? _end;

    /// <summary>
    ///   Reference sequence NAME of the alignment. If @SQ header lines are present, RNAME (if not `*')
    ///   must be present in one of the SQ-SN tag. An unmapped segment without coordinate
    ///   has a `*' at this eld. However, an unmapped segment may also have an ordinary coordinate
    ///   such that it can be placed at a desired position after sorting. If RNAME is `*', no assumptions
    ///   can be made about POS and CIGAR.
    /// </summary>
    public string Rname { get; set; }

    public string MatchString { get; set; }

    public long End
    {
      get
      {
        if (!_end.HasValue)
        {
          _end = Pos + Sequence.Length - 1;
        }
        return _end.Value;
      }
      set { _end = value; }
    }

    public int SeqLength
    {
      get
      {
        if (!string.IsNullOrEmpty(Sequence))
        {
          return Sequence.Length;
        }
        return (int)(End - Pos + 1);
      }
    }

    public char Strand { get; set; }

    public string Location
    {
      get { return string.Format("{0}:{1}-{2}", Rname, Pos, End); }
    }

    public string Qname { get; set; }

    private string _pairName;
    public string PairName
    {
      get
      {
        return string.IsNullOrEmpty(_pairName) ? Qname : _pairName;
      }
      set
      {
        _pairName = value;
      }
    }

    public long Pos { get; set; }

    public string Sequence { get; set; }

    public string Qual { get; set; }

    public int AlignmentScore { get; set; }
  }
}