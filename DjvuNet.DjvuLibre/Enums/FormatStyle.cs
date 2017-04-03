namespace DjvuNet.DjvuLibre
{
    public enum FormatStyle
    {
        BGR24,           /* truecolor 24 bits in BGR order */
        RGB24,           /* truecolor 24 bits in RGB order */
        RGBMASK16,       /* truecolor 16 bits with masks */
        RGBMASK32,       /* truecolor 32 bits with masks */
        GREY8,           /* greylevel 8 bits */
        PALETTE8,        /* paletized 8 bits (6x6x6 color cube) */
        MSBTOLSB,        /* packed bits, msb on the left */
        LSBTOMSB,        /* packed bits, lsb on the left */
    }
}
