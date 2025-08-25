using System;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace ScudioControlPanel.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _autoRefreshTimer;
        private bool _isShuttingDown;
        private readonly HttpClient _httpClient = new();

        private string _latestVersion = "Unknown";
        public string LatestVersion
        {
            get => _latestVersion;
            set => SetProperty(ref _latestVersion, value);
        }

        public MainWindowViewModel()
        {
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            _autoRefreshTimer.Tick += (_, _) => Refresh();

            IsAutoRefreshEnabled = true;
            Refresh();
            _ = FetchLatestVersionAsync(); 
        }

        public string AppVersion => GetAppVersion();

        public DateTime LastRefreshedAt { get; private set; }

        public bool IsAutoRefreshEnabled
        {
            get => _autoRefreshTimer.IsEnabled;
            set
            {
                if (value == _autoRefreshTimer.IsEnabled) return;
                if (value)
                {
                    _autoRefreshTimer.Start();
                }
                else
                {
                    _autoRefreshTimer.Stop();
                }
                OnPropertyChanged();
            }
        }

        private int _refreshIntervalSeconds = 60;
        public int RefreshIntervalSeconds
        {
            get => _refreshIntervalSeconds;
            set
            {
                if (value < 1) value = 1;
                if (_refreshIntervalSeconds == value) return;
                _refreshIntervalSeconds = value;
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(_refreshIntervalSeconds);
                OnPropertyChanged();
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            LastRefreshedAt = DateTime.Now;
            OnPropertyChanged(nameof(LastRefreshedAt));
            _ = FetchLatestVersionAsync(); 
        }

        [RelayCommand]
        public void BeginShutdown()
        {
            _isShuttingDown = true;
        }

        public bool IsShuttingDown => _isShuttingDown;

        private static string GetAppVersion()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            var version = asm.GetName().Version?.ToString();
            return informational ?? fileVersion ?? version ?? "0.0.0";
        }

        [RelayCommand]
        private void SetInterval(int seconds)
        {
            RefreshIntervalSeconds = seconds;
        }

        [RelayCommand]
        public async Task FetchLatestVersionAsync()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("AvaloniaApp", "1.0"));

                var url = "https://raw.githubusercontent.com/maheshjaiswal271/Work-Log-Assistant/refs/heads/main/src/version.json";
                var json = await _httpClient.GetStringAsync(url);

                using var doc = JsonDocument.Parse(json);
                var tag = doc.RootElement.GetProperty("version").GetString();

                LatestVersion = tag ?? "No Version found";
            }
            catch(Exception ex)
            {
                LatestVersion = $"Error: {ex.Message}";
            }
        }
    }
}
