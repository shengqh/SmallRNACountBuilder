namespace CQS.Genome.Sam
{
  public interface ISAMFormat
  {
    string Name { get; }

    bool HasAlternativeHits { get; }

    int GetAlignmentScore(string[] parts);

    string GetMismatchPositions(string[] parts);

    int GetNumberOfMismatch(string[] parts);

    void ParseAlternativeHits(string[] parts, SAMAlignedItem target);

    /// <summary>
    /// Compare two alignment score. 
    /// If score1 is better than score2, the result will be -1.
    /// If score1 is equals to score2, the result will be 0.
    /// If score1 is worse than score2, the result will be 1.
    /// </summary>
    /// <param name="score1">Alignment score 1</param>
    /// <param name="score2">Alignmenet score 2</param>
    /// <returns></returns>
    int CompareScore(double score1, double score2);
  }
}
