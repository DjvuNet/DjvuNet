namespace DjvuNet.Configuration
{
    public static class DjvuOptions
    {
        /// <summary>
        /// Controls whether the DjvuLibre Bezier gamma correction is applied.
        /// Default is false to strictly match standard DjvuLibre C++ behavior.
        /// </summary>
        /// TODO: Temporary solution during development and testing expansion
        public static readonly bool BEZIERGAMMA = false;
    }
}
