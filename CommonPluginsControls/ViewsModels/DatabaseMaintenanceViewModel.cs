using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CommonPluginsControls.ViewModels
{
	public class DatabaseMaintenanceViewModel : ObservableObject
	{
		private readonly IPluginDatabase _pluginDatabase;
		private long _savedBackupMaxCount;
		public event Action RequestClose;

		private DatabaseBackupInfo _currentDatabase;
		public DatabaseBackupInfo CurrentDatabase
		{
			get => _currentDatabase;
			set => SetValue(ref _currentDatabase, value);
		}

		public ObservableCollection<DatabaseBackupInfo> Backups { get; } = new ObservableCollection<DatabaseBackupInfo>();

		private long _backupMaxCount;
		public long BackupMaxCount
		{
			get => _backupMaxCount;
			set
			{
				if (_backupMaxCount == value)
				{
					return;
				}

				long normalized = value < 3 ? 3 : value;
				SetValue(ref _backupMaxCount, normalized);
				OnPropertyChanged(nameof(HasPendingBackupMaxCountChanges));
			}
		}

		public bool HasPendingBackupMaxCountChanges => BackupMaxCount != _savedBackupMaxCount;

		private DatabaseBackupInfo _selectedBackup;
		public DatabaseBackupInfo SelectedBackup
		{
			get => _selectedBackup;
			set
			{
				SetValue(ref _selectedBackup, value);
			}
		}

		public RelayCommand RefreshCommand { get; private set; }
		public RelayCommand CreateBackupCommand { get; private set; }
		public RelayCommand RestoreBackupCommand { get; private set; }
		public RelayCommand DeleteBackupCommand { get; private set; }
		public RelayCommand ClearDatabaseCommand { get; private set; }
		public RelayCommand SaveSettingsCommand { get; private set; }
		public RelayCommand CancelSettingsCommand { get; private set; }

		public DatabaseMaintenanceViewModel(IPluginDatabase pluginDatabase)
		{
			_pluginDatabase = pluginDatabase;

			RefreshCommand = new RelayCommand(RefreshData);
			CreateBackupCommand = new RelayCommand(CreateBackup);
			RestoreBackupCommand = new RelayCommand(RestoreBackup, () => SelectedBackup != null);
			DeleteBackupCommand = new RelayCommand(DeleteBackup, () => SelectedBackup != null);
			ClearDatabaseCommand = new RelayCommand(ClearDatabase);
			SaveSettingsCommand = new RelayCommand(SaveSettings);
			CancelSettingsCommand = new RelayCommand(CancelSettings);

			RefreshData();
		}

		private void RefreshData()
		{
			BackupMaxCount = _pluginDatabase.GetDatabaseBackupMaxCount();
			_savedBackupMaxCount = BackupMaxCount;
			OnPropertyChanged(nameof(HasPendingBackupMaxCountChanges));
			CurrentDatabase = _pluginDatabase.GetCurrentDatabaseInfo();

			Backups.Clear();
			_pluginDatabase
				.GetDatabaseBackups()
				.OrderByDescending(x => x.FileDate)
				.ToList()
				.ForEach(Backups.Add);

			if (SelectedBackup != null)
			{
				SelectedBackup = Backups.FirstOrDefault(x => string.Equals(x.FilePath, SelectedBackup.FilePath, StringComparison.OrdinalIgnoreCase));
			}
		}

		private void SaveSettings()
		{
			if (HasPendingBackupMaxCountChanges)
			{
				_pluginDatabase.SetDatabaseBackupMaxCount((int)BackupMaxCount);
				_savedBackupMaxCount = BackupMaxCount;
				OnPropertyChanged(nameof(HasPendingBackupMaxCountChanges));
			}

			RequestClose?.Invoke();
		}

		private void CancelSettings()
		{
			BackupMaxCount = _savedBackupMaxCount;
			RequestClose?.Invoke();
		}

		private void CreateBackup()
		{
			string path = _pluginDatabase.CreateDatabaseBackup();
			if (!string.IsNullOrEmpty(path))
			{
				API.Instance.Dialogs.ShowMessage(
					string.Format("Backup created:\n{0}", path),
					_pluginDatabase.PluginName);
			}

			RefreshData();
		}

		private void RestoreBackup()
		{
			if (SelectedBackup == null)
			{
				return;
			}

			var result = API.Instance.Dialogs.ShowMessage(
				string.Format("Restore database from this backup?\n\n{0}", SelectedBackup.FileName),
				_pluginDatabase.PluginName,
				System.Windows.MessageBoxButton.YesNo,
				System.Windows.MessageBoxImage.Warning);

			if (result != System.Windows.MessageBoxResult.Yes)
			{
				return;
			}

			bool restored = _pluginDatabase.RestoreDatabaseBackup(SelectedBackup.FilePath);
			API.Instance.Dialogs.ShowMessage(
				restored ? "Database restored successfully." : "Database restore failed.",
				_pluginDatabase.PluginName);

			RefreshData();
		}

		private void DeleteBackup()
		{
			if (SelectedBackup == null)
			{
				return;
			}

			var result = API.Instance.Dialogs.ShowMessage(
				string.Format("Delete this backup file?\n\n{0}", SelectedBackup.FileName),
				_pluginDatabase.PluginName,
				System.Windows.MessageBoxButton.YesNo,
				System.Windows.MessageBoxImage.Warning);

			if (result != System.Windows.MessageBoxResult.Yes)
			{
				return;
			}

			bool deleted = _pluginDatabase.DeleteDatabaseBackup(SelectedBackup.FilePath);
			if (!deleted)
			{
				API.Instance.Dialogs.ShowMessage("Unable to delete selected backup.", _pluginDatabase.PluginName);
			}

			RefreshData();
		}

		private void ClearDatabase()
		{
			var result = API.Instance.Dialogs.ShowMessage(
				ResourceProvider.GetString("LOCCommonClearAllConfirm"),
				_pluginDatabase.PluginName,
				System.Windows.MessageBoxButton.YesNo,
				System.Windows.MessageBoxImage.Warning);

			if (result == System.Windows.MessageBoxResult.Yes)
			{
				_pluginDatabase.ClearDatabase();
				RefreshData();
			}
		}
	}
}
