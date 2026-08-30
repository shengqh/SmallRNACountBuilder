using Bio.IO.BAM;
using Bio.IO.SAM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CQS.Genome.Sam
{
  public class BAMWindowReader : ISAMFile
  {
    private BAMParser _parser;
    private FileStream _stream;
    private bool _disposed;

    public BAMWindowReader(string filename)
    {
      _disposed = false;
      _stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      _parser = new BAMParser();
    }

    public List<string> ReadHeaders()
    {
      var header = _parser.GetHeader(_stream);

      var rfs = (from h in header.RecordFields
                 select string.Format("@{0}\t{1}", h.Typecode, (from t in h.Tags
                                                                select string.Format("{0}:{1}", t.Tag, t.Value)).Merge("\t"))).ToList();

      var sqs = (from s in header.ReferenceSequences
                 select string.Format("@SQ\tSN:{0}\tLN:{1}", s.Name, s.Length)).ToList();

      return rfs.Union(sqs).Union(header.Comments).ToList();
    }

    public SAMAlignedSequence ReadSAMAlignedSequence()
    {
      if (_parser.IsEOF())
      {
        return null;
      }
      return _parser.GetAlignedSequence(true);
    }

    public string SAMToString(SAMAlignedSequence sam)
    {
      if (sam == null)
      {
        return null;
      }

      return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}",
        sam.QName,
        (int)sam.Flag,
        sam.RName,
        sam.Pos,
        sam.MapQ,
        sam.CIGAR,
        sam.MRNM,
        sam.MPos,
        sam.ISize,
        sam.GetQuerySequenceString(),
        sam.GetQualityScoresString(),
        (from of in sam.OptionalFields
         select string.Format("{0}:{1}:{2}", of.Tag, of.VType, of.Value)).Merge("\t"));
    }

    public string ReadLine()
    {
      var sam = ReadSAMAlignedSequence();
      return SAMToString(sam);
    }

    public void Dispose()
    {
      Dispose(true);

      // Use SupressFinalize in case a subclass 
      // of this type implements a finalizer.
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      // If you need thread safety, use a lock around these  
      // operations, as well as in your methods that use the resource. 
      if (!_disposed)
      {
        if (disposing)
        {
          if (_stream != null)
            _stream.Dispose();
        }

        // Indicate that the instance has been disposed.
        _stream = null;
        _disposed = true;
      }
    }

    ~BAMWindowReader()
    {
      Dispose(false);
    }
  }
}
