using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    /// <summary>
    /// This message is generated when a DjVu viewer
    /// should recompute the layout of the page viewer
    /// because the page size and resolution information 
    /// has been updated.
    /// </summary>
    /// <remarks>
    /// Both the <m_relayout> and <m_redisplay> messages are derived from the
    /// <m_chunk> message.  They are intended for driving a djvu image viewer.
    /// When receiving <m_relayout>, the viewer should get the image size, decide
    /// zoom factors, and place the image area, scrollbars, toolbars, and other gui
    /// objects.  When receiving <m_redisplay>, the viewer should invalidate the
    /// image area so that the gui toolkit calls the repaint event handler. This
    /// handler should call ddjvu_page_render() and paint the part of the
    /// image that needs repainting.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public class LayoutMessage
    {
        public AnyMassege Any;
    };
}