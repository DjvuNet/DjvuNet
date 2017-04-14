using System.Drawing;

namespace DjvuNet.DataChunks
{
    public interface IFGjpChunk : IDjvuNode
    {
        Image ForegroundImage { get; }
    }
}