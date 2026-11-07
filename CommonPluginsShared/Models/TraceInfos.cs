using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CommonPluginsShared.Models
{
    public class TraceInfos
    {
        public string InitialCaller { get; set; }
        public ParameterInfo[] CallerParams { get; set; }

        public string FileName { get; set; }
        public int LineNumber { get; set; }

        /// <summary>
        /// Builds caller metadata from an exception stack, or from the current stack when <paramref name="ex"/> is null.
        /// </summary>
        /// <param name="ex">Exception to inspect; may be null.</param>
        public TraceInfos(Exception ex)
        {
            // StackTrace(Exception) rejects null; fall back to the current call stack as a safety net.
            StackTrace Trace = ex != null ? new StackTrace(ex, true) : new StackTrace(true);
            StackFrame Frame = Trace.GetFrames()?.LastOrDefault();
            InitialCaller = Frame?.GetMethod()?.Name;
            CallerParams = Frame?.GetMethod()?.GetParameters();
            FileName = (Frame?.GetFileName()?.IsNullOrEmpty() ?? true) ? "???" : Path.GetFileName(Frame.GetFileName());
            LineNumber = Frame?.GetFileLineNumber() ?? 0;
        }
    }
}
