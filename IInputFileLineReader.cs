namespace CQS
{
  public interface IInputFileLineReader : ILineFile
  {
    /// <summary>
    /// Is the file need to use another program to open it and then use the standard output as source stream?
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    bool NeedProcess(string filename);
  }
}
