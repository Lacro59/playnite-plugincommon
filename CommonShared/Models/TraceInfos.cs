using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonShared.Models
{
    public class TraceInfos
    {
        public string InitialCaller { get; set; }
        public string FileName { get; set; }
        public int LineNumber { get; set; }

        public TraceInfos(Exception ex)
        {
            StackTrace Trace = new StackTrace(ex, true);
            StackFrame Frame = Trace.GetFrames()?.LastOrDefault();

            InitialCaller = Frame?.GetMethod()?.Name;
            FileName = Frame.GetFileName().IsNullOrEmpty() ? "???" : Frame.GetFileName();
            LineNumber = Frame.GetFileLineNumber();
        }
    }
}
