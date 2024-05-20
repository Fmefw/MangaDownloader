using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DownloaderGUI.ViewModels;
using DownloaderGUI.Views;
using System;

namespace DownloaderGUI
{
    public partial class App : Application
    {
        private TrayIcon _trayIcon;
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {

                desktop.MainWindow = new MainWindow()
                {
                    DataContext = new MainWindowViewModel(),
                };

                desktop.Exit += OnExit;

                _trayIcon = new TrayIcon
                {
                    ToolTipText = "Dynasty Scans Manga Downloader",
                    Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://DownloaderGUI/Assets/avalonia-logo.ico")))
                };

                var showMenuItem = new NativeMenuItem { Header = "Show" };
                showMenuItem.Click += RestoreClick;

                var exitMenuItem = new NativeMenuItem { Header = "Exit" };
                exitMenuItem.Click += ExitClick;

                _trayIcon.Menu = new NativeMenu();
                _trayIcon.Menu.Items.Add(showMenuItem);
                _trayIcon.Menu.Items.Add(new NativeMenuItemSeparator());
                _trayIcon.Menu.Items.Add(exitMenuItem);

                _trayIcon.IsVisible = true;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void RestoreClick(object sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.WindowState = WindowState.Normal;
            }
        }

        private void ExitClick(object sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _trayIcon.Dispose();
        }
    }
}