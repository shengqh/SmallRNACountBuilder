using System;
using System.Collections.Generic;
using System.Linq;

namespace CQS.Genome.SmallRNA
{
  public class SmallRNAMapperBiotype : SmallRNAMapper
  {
    public SmallRNAMapperBiotype(SmallRNABiotype biotype, ISmallRNACountProcessorOptions options) : base(biotype.ToString(), options, feature => feature.Category.Equals(biotype.ToString())) { }
  }

  public class SmallRNAMapperBiotypes : SmallRNAMapper
  {
    private HashSet<string> _biotypes;
    public SmallRNAMapperBiotypes(SmallRNABiotype[] biotypes, ISmallRNACountProcessorOptions options) : base(options)
    {
      _biotypes = new HashSet<string>(from b in biotypes select b.ToString());
      MapperName = StringUtils.Merge(from b in biotypes select b.ToString(), "/");
      Accept = m => _biotypes.Contains(m.Category);
    }
  }
}
