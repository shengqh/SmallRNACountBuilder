using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CQS.Genome.Gtf
{
  /// <summary>
  /// http://mblab.wustl.edu/GTF22.html
  /// <seqname> <source> <feature> <start> <end> <score> <strand> <frame> [attributes] [comments]
  /// Here is a simple example with 3 translated exons. Order of rows is not important.
  /// 381 Twinscan  CDS          380   401   .   +   0  gene_id "001"; transcript_id "001.1";
  /// 381 Twinscan  CDS          501   650   .   +   2  gene_id "001"; transcript_id "001.1";
  /// 381 Twinscan  CDS          700   707   .   +   2  gene_id "001"; transcript_id "001.1";
  /// 381 Twinscan  start_codon  380   382   .   +   0  gene_id "001"; transcript_id "001.1";
  /// 381 Twinscan  stop_codon   708   710   .   +   0  gene_id "001"; transcript_id "001.1";
  /// The whitespace in this example is provided only for readability. In GTF, fields must be separated by a single TAB and no white space.
  /// </summary>
  public class GtfItem : ISequenceRegion
  {
    private static Regex geneIdReg = new Regex("gene_id\\s\"(.+?)\"");
    private static Regex transcriptIdReg = new Regex("transcript_id\\s\"(.+?)\"");
    private static Regex exonNumberReg = new Regex("exon_number\\s\"(.+?)\"");
    private static Regex geneNameReg = new Regex("gene_name\\s\"(.+?)\"");
    private static Regex geneBiotypeReg = new Regex("gene_biotype\\s\"(.+?)\"");
    private static Regex geneTypeReg = new Regex("gene_type\\s\"(.+?)\"");
    private static Dictionary<string, Regex> regMap = new Dictionary<string, Regex>();

    /// <summary>
    /// The name of the sequence. Commonly, this is the chromosome ID or contig ID. 
    /// Note that the coordinates used must be unique within each sequence name in all GTFs for an annotation set.
    /// </summary>
    public string Seqname { get; set; }

    /// <summary>
    /// The source column should be a unique label indicating where the annotations came from --- typically the 
    /// name of either a prediction program or a public database.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// The following feature types are required: "CDS", "start_codon", "stop_codon". The features "5UTR", "3UTR", "inter", 
    /// "inter_CNS", "intron_CNS" and "exon" are optional. All other features will be ignored. The types must have the correct 
    /// capitalization shown here.
    /// 
    /// CDS represents the coding sequence starting with the first translated codon and proceeding to the last translated codon. 
    /// Unlike Genbank annotation, the stop codon is not included in the CDS for the terminal exon. 
    /// 
    /// The optional feature "5UTR" represents regions from the transcription start site or beginning of the known 5' UTR to the base before the start codon 
    /// of the transcript. If this region is interrupted by introns then each exon or partial exon is annotated as a separate 5UTR 
    /// feature. 
    /// 
    /// Similarly, "3UTR" represents regions after the stop codon and before the polyadenylation site or end of the known 
    /// 3' untranslated region. Note that the UTR features can only be used to annotate portions of mRNA genes, not non-coding RNA genes.
    /// 
    /// The feature "exon" more generically describes any transcribed exon. Therefore, exon boundaries will be the transcription 
    /// start site, splice donor, splice acceptor and poly-adenylation site. The start or stop codon will not necessarily lie on an exon boundary.
    /// 
    /// The "start_codon" feature is up to 3bp long in total and is included in the coordinates for the "CDS" features. 
    /// The "stop_codon" feature similarly is up to 3bp long and is excluded from the coordinates for the "3UTR" features, if used.
    /// The "start_codon" and "stop_codon" features are not required to be atomic; they may be interrupted by valid splice sites. 
    /// A split start or stop codon appears as two distinct features. All "start_codon" and "stop_codon" features must have a 0,1,2 
    /// in the <frame> field indicating which part of the codon is represented by this feature. Contiguous start and stop codons will always have frame 0.
    /// 
    /// The "inter" feature describes an intergenic region, one which is by almost all accounts not transcribed. 
    /// The "inter_CNS" feature describes an intergenic conserved noncoding sequence region. 
    /// All of these should have an empty transcript_id attribute, since they are not transcribed and do not belong to any transcript. 
    /// 
    /// The "intron_CNS" feature describes a conserved noncoding sequence region within an intron of a transcript, and should have a 
    /// transcript_id associated with it.
    /// </summary>
    public string Feature { get; set; }

    /// <summary>
    /// Integer start and end coordinates of the feature relative to the beginning of the sequence named in <Seqname>.  
    /// <Start> must be less than or equal to <End>. SEQUENCE NUMBERING STARTS AT 1. Values of <Start> and <End> that 
    /// extend outside the reference sequence are technically acceptable, but they are discouraged.
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// Integer start and end coordinates of the feature relative to the beginning of the sequence named in <seqname>.  
    /// <start> must be less than or equal to <end>. Sequence numbering starts at 1. Values of <start> and <end> that 
    /// extend outside the reference sequence are technically acceptable, but they are discouraged.
    /// </summary>
    public long End { get; set; }

    /// <summary>
    /// The score field indicates a degree of confidence in the feature's existence and coordinates. The value of this 
    /// field has no global scale but may have relative significance when the <source> field indicates the prediction 
    /// program used to create this annotation. It may be a floating point number or integer, and not necessary and may 
    /// be replaced with a dot.
    /// </summary>
    public string Score { get; set; }

    public char Strand { get; set; }

    /// <summary>
    /// 0 indicates that the feature begins with a whole codon at the 5' most base. 
    /// 1 means that there is one extra base (the third base of a codon) before the first whole codon and 
    /// 2 means that there are two extra bases (the second and third bases of the codon) before the first codon. 
    /// Note that for reverse strand features, the 5' most base is the end coordinate.
    /// </summary>
    public char Frame { get; set; }

    /// <summary>
    /// parsing from [Attributes]
    /// gene_id value;     A globally unique identifier for the genomic locus of the transcript. If empty, no gene is associated with this feature.
    /// </summary>
    public string GeneId { get; set; }

    /// <summary>
    /// parsing from [Attributes]
    /// gene_name value
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// parsing from [Attributes]
    /// transcript_id value;     A globally unique identifier for the predicted transcript. If empty, no transcript is associated with this feature.
    /// </summary>
    public string TranscriptId { get; set; }

    /// <summary>
    /// parsing from [Attributes]
    /// </summary>
    public int ExonNumber { get; set; }

    /// <summary>
    /// All nine features have the same two mandatory attributes at the end of the record:
    ///   gene_id value;     A globally unique identifier for the genomic locus of the transcript. If empty, no gene is associated with this feature.
    ///   transcript_id value;     A globally unique identifier for the predicted transcript. If empty, no transcript is associated with this feature.
    /// These attributes are designed for handling multiple transcripts from the same genomic region. Any other attributes or comments must appear after 
    /// these two and will be ignored.
    /// Attributes must end in a semicolon which must then be separated from the start of any subsequent attribute by exactly one space character (NOT a tab character).
    /// Textual attributes should be surrounded by doublequotes.
    /// These attributes are required even for non-mRNA transcribed regions such as "inter" and "inter_CNS" features.
    /// </summary>
    private string _attributes;
    public string Attributes
    {
      get
      {
        return _attributes;
      }
      set
      {
        _attributes = value;
        GeneId = GetRegexValue(geneIdReg, value);
        TranscriptId = GetRegexValue(transcriptIdReg, value);
        ExonNumber = int.Parse(GetRegexValue(exonNumberReg, value, "0"));
        Name = GetRegexValue(geneNameReg, value);
      }
    }

    public string GetAttribute(string key)
    {
      Regex keyReg;
      if (!regMap.TryGetValue(key, out keyReg))
      {
        keyReg = new Regex(key + "\\s\"(.+?)\"");
        regMap[key] = keyReg;
      }

      return GetRegexValue(keyReg, Attributes);
    }

    public GtfItem()
    {
      this.Seqname = string.Empty;
      this.Source = ".";
      this.Feature = string.Empty;
      this.Start = -1;
      this.End = -1;
      this.Score = ".";
      this.Strand = '.';
      this.Frame = '.';
      this._attributes = string.Empty;
      this.GeneId = string.Empty;
      this.TranscriptId = string.Empty;
      this.ExonNumber = -1;
      this.Sequence = string.Empty;
      this.Name = string.Empty;
    }

    public GtfItem(ISequenceRegion source)
      : this()
    {
      this.Name = source.Name;
      this.Seqname = source.Seqname;
      this.Start = source.Start;
      this.End = source.End;
      this.Strand = source.Strand;
      this.Sequence = source.Sequence;
    }

    public long Length
    {
      get
      {
        if (this.End < 0 || this.Start < 0)
        {
          return 0;
        }

        if (this.End > this.Start)
        {
          return this.End - this.Start + 1;
        }

        return this.Start - this.End + 1;
      }
    }

    public bool InRange(long position)
    {
      return this.Start <= position && position <= this.End;
    }

    private static string GetRegexValue(Regex reg, string value, string defaultValue = "")
    {
      var m = reg.Match(value);
      if (m.Success)
      {
        return m.Groups[1].Value;
      }
      else
      {
        return defaultValue;
      }
    }

    public bool IsSameTranscript(GtfItem another)
    {
      if (this.ExonNumber == -1 && another.ExonNumber == -1)
      {
        return false;
      }

      return (this.TranscriptId == another.TranscriptId);
    }

    public void SetToZeroBased()
    {
      this.Start -= 1;
      this.End -= 1;
    }

    public bool Contains(long position)
    {
      return position >= this.Start && position <= this.End;
    }

    public string Sequence { get; set; }

    public override string ToString()
    {
      return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
        Seqname,
        Source,
        Feature,
        Start,
        End,
        Score,
        Strand,
        Frame,
        Attributes);
    }

    public string GetBiotype()
    {
      var result = GetRegexValue(geneBiotypeReg, this.Attributes);
      if (string.IsNullOrEmpty(result))
      {
        result = GetRegexValue(geneTypeReg, this.Attributes);
      }
      return result;
    }

    public string GetNameExon()
    {
      if (this.ExonNumber == 0)
      {
        return this.Name;
      }
      else
      {
        return this.Name + ":exon" + this.ExonNumber.ToString();
      }
    }
  }
}
