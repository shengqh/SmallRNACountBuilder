using System.Linq;

namespace Bio
{
  public static class ISequenceExtension
  {
    public static string GetSequenceString(this ISequence seq)
    {
      return new string(seq.Select(a => (char)a).ToArray());
    }
  }
}
