namespace CQS.Genome
{
  public class SingleNucleotidePolymorphism
  {
    public int Position { get; set; }

    public char RefAllele { get; set; }

    public char SampleAllele { get; set; }

    public SingleNucleotidePolymorphism(int pos, char refAllele, char sampleAllele)
    {
      this.Position = pos;
      this.RefAllele = refAllele;
      this.SampleAllele = sampleAllele;
    }

    public bool IsMutation(char from, char to)
    {
      return RefAllele == from && SampleAllele == to;
    }
  }
}
