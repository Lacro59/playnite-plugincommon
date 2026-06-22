using System;

namespace CommonPluginsShared.Collections
{
	/// <summary>
	/// Event data raised when a multi-game batch refresh completes in
	/// <see cref="PluginDatabaseObject{TSettings, TItem, T}.Refresh(System.Collections.Generic.IEnumerable{System.Guid}, string)"/>.
	/// </summary>
	public sealed class BatchRefreshCompletedEventArgs : EventArgs
	{
		/// <summary>Gets the number of games processed before completion or cancellation.</summary>
		public int ProcessedCount { get; }

		/// <summary>Gets the total number of games in the batch request.</summary>
		public int TotalCount { get; }

		/// <summary>Gets a value indicating whether the user canceled the progress dialog.</summary>
		public bool Canceled { get; }

		/// <summary>Initializes a new instance of the <see cref="BatchRefreshCompletedEventArgs"/> class.</summary>
		/// <param name="processedCount">Number of games processed.</param>
		/// <param name="totalCount">Total games in the batch.</param>
		/// <param name="canceled">Whether the operation was canceled.</param>
		public BatchRefreshCompletedEventArgs(int processedCount, int totalCount, bool canceled)
		{
			ProcessedCount = processedCount;
			TotalCount = totalCount;
			Canceled = canceled;
		}
	}
}
