using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ScudioControlPanel.ViewModels;
using ScudioControlPanel.Views;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Win32;
using System.Reactive.Linq;

namespace ScudioControlPanel
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                var viewModel = new MainWindowViewModel();
                var mainWindow = new MainWindow
                {
                    DataContext = viewModel,
                    ShowInTaskbar = false,
                    WindowState = WindowState.Minimized,
                };

                // Handle close-to-tray in code-behind event
                mainWindow.Closing += (sender, e) =>
                {
                    if (viewModel.IsShuttingDown)
                    {
                        return;
                    }

                    // Prevent closing; hide to tray
                    e.Cancel = true;
                    mainWindow.Hide();
                };

                desktop.MainWindow = mainWindow;

                // Create system tray icon with menu
                var tray = new TrayIcon
                {
                    ToolTipText = "Utility Service"
                };
                using var iconStream = AssetLoader.Open(new Uri("avares://ScudioControlPanel/Assets/avalonia-logo.ico"));
                tray.Icon = new WindowIcon(iconStream);

                mainWindow.GetObservable(Window.WindowStateProperty).Subscribe( state =>
                {
                    if (state == WindowState.Minimized)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            mainWindow.Hide();
                            mainWindow.ShowInTaskbar = false;
                        });
                    }
                });

                var menu = new NativeMenu();
                var showItem = new NativeMenuItem("Show");
                showItem.Click += (_, _) => Dispatcher.UIThread.Post(() =>
                {
                    if (!mainWindow.IsVisible)
                    {
                        mainWindow.ShowInTaskbar = true;
                        mainWindow.Show();
                    }
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                });
                var refreshItem = new NativeMenuItem("Refresh Now");
                refreshItem.Click += (_, _) => viewModel.RefreshCommand.Execute(null);
                var exitItem = new NativeMenuItem("Exit");
                exitItem.Click += (_, _) =>
                {
                    viewModel.BeginShutdown();
                    desktop.Shutdown();
                };
                menu.Items.Add(showItem);
                menu.Items.Add(refreshItem);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(exitItem);
                tray.Menu = menu;

                tray.Clicked += (_, _) => Dispatcher.UIThread.Post(() =>
                {
                    if (!mainWindow.IsVisible)
                    {
                        mainWindow.ShowInTaskbar = true;
                        mainWindow.Show();
                    }
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                });

                tray.IsVisible = true;

                EnsureWindowsAutostart();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }

        private void EnsureWindowsAutostart()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    return;
                }

                using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
                if (runKey is null)
                {
                    return;
                }

                runKey.SetValue("ScudioControlPanel", $"\"{exePath}\"");
            }
            catch
            {
            }
        }
    }
}