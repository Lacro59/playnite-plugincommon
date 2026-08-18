using Playnite.SDK.Data;
using System;

namespace CommonPluginsStores.Steam.Models
{
	/// <summary>
	/// Minimal GitHub Commits API item used to read the last commit date for a file path.
	/// </summary>
	public class GitHubCommitListItem
	{
		[SerializationPropertyName("commit")]
		public GitHubCommitDetail Commit { get; set; }
	}

	/// <summary>
	/// Nested commit payload from the GitHub Commits API.
	/// </summary>
	public class GitHubCommitDetail
	{
		[SerializationPropertyName("committer")]
		public GitHubCommitActor Committer { get; set; }

		[SerializationPropertyName("author")]
		public GitHubCommitActor Author { get; set; }
	}

	/// <summary>
	/// Author or committer block containing the commit timestamp.
	/// </summary>
	public class GitHubCommitActor
	{
		[SerializationPropertyName("date")]
		public DateTime Date { get; set; }
	}
}
