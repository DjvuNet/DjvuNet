namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// Various ways to render a page.
    /// </summary>
    public enum RenderMode
    {
        Color = 0,       /* color page or stencil */
        Black,           /* stencil or color page */
        ColorOnly,       /* color page or fail */
        MaskOnly,        /* stencil or fail */
        Background,      /* color background layer */
        Foreground,      /* color foreground layer */
    }
}