using System;
using System.Threading;
using System.Threading.Tasks;

namespace CommonPluginsShared
{
	/// <summary>
	/// Enforces a minimum delay between consecutive operations (thread-safe, process-wide per instance).
	/// Used to reduce HTTP 429 / non-JSON gateway responses when calling store APIs in bulk.
	/// </summary>
	public sealed class RequestRateLimiter
	{
		private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
		private readonly TimeSpan _minInterval;
		private DateTime _lastRequestUtc = DateTime.MinValue;

		/// <summary>
		/// Creates a limiter that spaces requests by at least <paramref name="minInterval"/>.
		/// </summary>
		/// <param name="minInterval">Minimum elapsed time between the end of one wait and the start of the next allowed request.</param>
		public RequestRateLimiter(TimeSpan minInterval)
		{
			if (minInterval < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(nameof(minInterval), "Interval must be zero or positive.");
			}

			_minInterval = minInterval;
		}

		/// <summary>
		/// Waits until the next request is allowed, then records the request timestamp.
		/// </summary>
		/// <param name="cancellationToken">Optional cancellation while waiting between requests.</param>
		public async Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				DateTime now = DateTime.UtcNow;
				TimeSpan elapsed = now - _lastRequestUtc;
				if (_minInterval > TimeSpan.Zero && elapsed < _minInterval)
				{
					await Task.Delay(_minInterval - elapsed, cancellationToken).ConfigureAwait(false);
				}

				_lastRequestUtc = DateTime.UtcNow;
			}
			finally
			{
				_gate.Release();
			}
		}
	}
}
