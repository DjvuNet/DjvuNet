using System;
using System.Runtime.InteropServices;

namespace DjvuNet.DjvuLibre
{

    public class DjvuMessage
    {
        private IntPtr _NativeMessage;

        public DjvuMessage(IntPtr messagePtr)
        {
            _NativeMessage = messagePtr;
        }

        public AnyMassege           Any { get { return GetMessage<AnyMassege>(); } }

        public ErrorMessage         Error { get { return ErrorMessage.GetMessage(_NativeMessage); } }

        public InfoMessage          Info { get { return InfoMessage.GetMessage(_NativeMessage); } }

        public NewStreamMessage     NewStream { get { return NewStreamMessage.GetMessage(_NativeMessage); } }

        public DocInfoMessage       DocInfo { get { return GetMessage<DocInfoMessage>(); } }

        public PageInfoMessage      PageInfo { get { return GetMessage<PageInfoMessage>(); } }

        public ChunkMessage         Chunk { get { return ChunkMessage.GetMessage(_NativeMessage); } }

        public LayoutMessage        Layout { get { return GetMessage<LayoutMessage>(); } }

        public DisplayMessage       Display { get { return GetMessage<DisplayMessage>(); } }

        public ThumbnailMessage     Thumbnail { get { return GetMessage<ThumbnailMessage>(); } }

        public ProgressMessage      Progress { get { return GetMessage<ProgressMessage>(); } }

        private T GetMessage<T>()
        {
            if (_NativeMessage != IntPtr.Zero)
                return Marshal.PtrToStructure<T>(_NativeMessage);
            else
                return default(T);
        }
    }
}