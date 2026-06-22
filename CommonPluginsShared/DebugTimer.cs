using System;
using System.Diagnostics;

namespace CommonPluginsShared
{
	/// <summary>
	/// Lightweight timing utility for debug instrumentation.
	/// Must always be consumed inside <c>#if DEBUG</c> blocks at the call site
	/// to guarantee zero overhead in Release builds.
	/// </summary>
	public sealed class DebugTimer : IDisposable
	{
		private readonly Stopwatch _sw;
		private readonly string _context;
		private bool _stopped;

		/// <summary>
		/// Starts the timer and logs the start of the scope.
		/// </summary>
		/// <param name="context">Scope identifier (e.g. "PluginButton.ctor").</param>
		public DebugTimer(string context)
		{
			_context = context;
			_sw = Stopwatch.StartNew();
			Common.LogDebug(true, string.Format("[{0}] start", _context));
		}

		/// <summary>
		/// Logs an intermediate step with elapsed time since construction.
		/// </summary>
		/// <param name="label">Step description.</param>
		public void Step(string label)
		{
			Common.LogDebug(true, string.Format("[{0}] {1} [{2}ms]", _context, label, _sw.ElapsedMilliseconds));
		}

		/// <summary>
		/// Stops the timer and logs total elapsed time.
		/// Safe to call multiple times — no-op after the first call.
		/// </summary>
		/// <param name="label">Final log label. Defaults to "end".</param>
		public void Stop(string label = "end")
		{
			if (_stopped)
			{
				return;
			}

			_sw.Stop();
			_stopped = true;
			Common.LogDebug(true, string.Format("[{0}] {1} [{2}ms total]", _context, label, _sw.ElapsedMilliseconds));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Stop();
		}
	}
}