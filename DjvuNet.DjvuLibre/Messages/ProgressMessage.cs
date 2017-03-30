using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// These messages are generated to indicate progress
    /// towards the completion of a print or save job.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class ProgressMessage
    {
        public AnyMassege Any;

        public DjvuJobStatus status;

        public int Percent;
    }
}