namespace CQS.Genome
{
  public interface ISequenceRegion
  {
    string Seqname { get; set; }

    long Start { get; set; }

    long End { get; set; }

    string Name { get; set; }

    char Strand { get; set; }

    long Length { get; }

    bool Contains(long position);

    string Sequence { get; set; }
  }
}
