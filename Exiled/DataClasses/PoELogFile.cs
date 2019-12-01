using System;
using System.Collections.Generic;
using System.Text;

namespace Exiled.DataClasses
{
    public class PoELogFile
    {
        public List<PoELogLine> Lines { get; set; }
        public List<PoESession> Sessions { get; set; }
        public long LastStreamPosition { get; set; }
    }
}
