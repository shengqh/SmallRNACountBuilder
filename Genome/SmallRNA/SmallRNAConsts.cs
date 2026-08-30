using RCPA;

namespace CQS.Genome.SmallRNA
{
  //The feature name of smallRNA in ensembl gff file
  public enum SmallRNABiotype { miRNA, tRNA, mt_tRNA, yRNA, snoRNA, snRNA, rRNA, misc_RNA, lincRNA, lncRNA, ERV };

  public static class SmallRNAConsts
  {
    /// <summary>
    /// The feature name of miRNA in miRBase gff file. It will be the prefix of microRNA feature name.
    /// </summary>
    public static readonly string miRNA = SmallRNABiotype.miRNA.ToString();

    /// <summary>
    /// The feature name used in pipeline analysis. It will be the prefix of tRNA feature name.
    /// </summary>
    public static readonly string tRNA = SmallRNABiotype.tRNA.ToString();

    public static readonly string mt_tRNA = SmallRNABiotype.mt_tRNA.ToString();

    public static readonly string[] lncRNA = new[] { SmallRNABiotype.lncRNA.ToString(), SmallRNABiotype.lncRNA.ToString() };

    public static readonly string rRNA = SmallRNABiotype.rRNA.ToString();

    public const string NTA_TAG = ":CLIP_";

    public const string rRNADB_KEY = "rRNADB_";

    public static string[] Biotypes;

    static SmallRNAConsts()
    {
      Biotypes = EnumUtils.EnumToStringArray<SmallRNABiotype>();
    }
  }
}
