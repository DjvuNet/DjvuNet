using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjvuNet.Diagnostics
{
    [EventSource(Name = _Name, Guid = _Guid)]
    public partial class DjvuEventSource : EventSource
    {
        const string _Name = "DjvuNet-Library";
        const String _Guid = "{B1C56657-E6FE-4348-BDCC-0B271A77A06E}";
        const byte _Version = 0x01;

        protected static DjvuEventSource _Instance;

        public static DjvuEventSource Log
        {
            get
            {
                if (_Instance == null)
                    _Instance = new DjvuEventSource();
                return _Instance;
            }
        }

        protected DjvuEventSource() : base(_Name) { }

        [Event(1, Level = EventLevel.LogAlways, Opcode = EventOpcode.Start, Task = EventTask.None, Version = _Version)]
        public void OnDocumentOpen()
        {
            base.WriteEvent(1);
        }

        [Event(2, Level = EventLevel.LogAlways, Opcode = EventOpcode.Stop, Version = _Version)]
        public void OnDocumentClose()
        {
            base.WriteEvent(2);
        }

        [Event(3, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnRootFormCreated()
        {
            base.WriteEvent(3);
        }

        [Event(4, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnRootFormInitialized()
        {
            base.WriteEvent(4);
        }

        [Event(5, Level = EventLevel.Informational, Opcode = EventOpcode.Suspend, Version = _Version)]
        public void OnRootChildCreated()
        {
            base.WriteEvent(5);
        }

        [Event(6, Level = EventLevel.Informational, Opcode = EventOpcode.Start, Version = _Version)]
        public void OnDocumentParsed()
        {
            base.WriteEvent(6);
        }

        [Event(7, Level = EventLevel.Informational, Opcode = EventOpcode.Stop, Version = _Version)]
        public void OnPageCreated()
        {
            base.WriteEvent(7);
        }

        [Event(8, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnNavigating()
        {
            base.WriteEvent(8);
        }

        [Event(9, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnNavigated()
        {
            base.WriteEvent(9);
        }

        [Event(10, Level = EventLevel.Informational, Opcode = EventOpcode.Resume, Version = _Version)]
        public void OnPageImageCreated()
        {
            base.WriteEvent(10);
        }

        [Event(11, Level = EventLevel.Error, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnException(Exception ex)
        {
            base.WriteEvent(11, UnwindExceptions(ex));
        }

        [Event(12, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnPageBackgroundCreated()
        {
            base.WriteEvent(12);
        }

        [Event(13, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnPageThumbnailCreated()
        {
            base.WriteEvent(13);
        }

        [Event(14, Level = EventLevel.Error, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnPageParsingError()
        {
            base.WriteEvent(14);
        }

        [Event(15, Level = EventLevel.Error, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnChunkParsingError()
        {
            base.WriteEvent(15);
        }

        [Event(16, Level = EventLevel.Critical, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnUnrecoverableParsingError()
        {
            base.WriteEvent(16);
        }

        [Event(17, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnUnsupportedChunkParsed()
        {
            base.WriteEvent(17);
        }

        [Event(18, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnUnknownChunkParsed()
        {
            base.WriteEvent(18);
        }

        [Event(19, Level = EventLevel.Informational, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnSecureDjvuDocumentLoad()
        {
            base.WriteEvent(19);
        }

        [Event(20, Level = EventLevel.LogAlways, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnDocumentSaved()
        {
            base.WriteEvent(20);
        }

        [Event(21, Level = EventLevel.LogAlways, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnSettingsChanged()
        {
            base.WriteEvent(21);
        }

        [Event(22, Level = EventLevel.Critical, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnCriticalError(Exception ex)
        {
            String message = "Critical error !!!\n" + UnwindExceptions(ex);
            base.WriteEvent(22, message);
        }

        [Event(23, Level = EventLevel.Critical, Opcode = EventOpcode.Info, Version = _Version)]
        public void OnUnhadledException(Exception ex)
        {
            String message = "Unhandled exception !!!\n" + UnwindExceptions(ex);
            base.WriteEvent(23, message);
        }

        [NonEvent]
        private string UnwindExceptions(Exception ex)
        {
            String result = String.Empty;

            if (ex != null)
            {
                int level = 0;
                result += "Unwinding exceptions ............................................\n";
                result += $"Exception level {level}\n";
                result += ".................................................................\n";
                result += $"{ex.ToString()}\n";
                result += ".................................................................\n";

                Exception innerException = ex.InnerException;
                while (innerException != null)
                {
                    level++;
                    result += $"Exception level {level}\n";
                    result += ".................................................................\n";
                    result += $"{innerException.ToString()}\n";
                    result += ".................................................................\n";
                    innerException = innerException.InnerException;
                }

                result += "End of unwinded exception .......................................\n";
            }

            return result;
        }

    }

}
