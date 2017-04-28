namespace DjvuNet.DjvuLibre
{
    public enum DjvuJobStatus : sbyte
    {
        NotStarted, /* operation was not even started */
        Started,    /* operation is in progress */
        OK,         /* operation terminated successfully */
        Failed,     /* operation failed because of an error */
        Stopped     /* operation was interrupted by user */
    }
}