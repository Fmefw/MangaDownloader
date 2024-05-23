using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using DownloaderGUI.ViewModels;
using DownloaderGUI.Views;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using Mono.Unix;
using Mono.Unix.Native;

namespace DownloaderGUI
{
    public partial class App : Application
    {
        private TrayIcon _trayIcon;
        private const string AppName = "Dynasty Scans Manga Downloader";
        private const string PipeName = "DynastyScansMangaDownloader";
        private const string SocketPath = "/tmp/DynastyScansMangaDownloader";
        private static FileStream lockFileStream;
        private static Socket socketListener;
        private static Thread ipcThread;
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                
                if (IsSingleInstance())
                {
                    
                    ipcThread = new Thread(StartPipeServer);
                    ipcThread.IsBackground = true;
                    ipcThread.Start();
                    
                    desktop.Startup += OnStartup;
                    desktop.Exit += OnExit;
                    desktop.MainWindow = new MainWindow()
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                    desktop.MainWindow.Show();
                }
                else
                {
                    SendMessageToFirstInstance();
                    Environment.Exit(0);
                }
               
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
        
        private bool IsSingleInstance()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                bool createdNew;
                var mutex = new Mutex(true, AppName, out createdNew);
                return createdNew;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    string lockFilePath = Path.Combine(Path.GetTempPath(), AppName + ".lock");
                    lockFileStream = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return true;
                }
                catch (IOException)
                {
                    // File is already locked
                    return false;
                }
            }

            return false;
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

        private void OnStartup(object sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            //
        }
        
        private void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            _trayIcon.Dispose();
            lockFileStream?.Close();
            if (File.Exists(SocketPath))
            {
                File.Delete(SocketPath);
            }
            socketListener?.Close();
            ipcThread?.Join();
        }
        
        private void StartPipeServer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Thread pipeServerThread = new Thread(() =>
                {
                    using (var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In))
                    {
                        while (true)
                        {
                            pipeServer.WaitForConnection();
                            using (var reader = new StreamReader(pipeServer))
                            {
                                var message = reader.ReadLine();
                                if (message == "Show")
                                {
                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                                        if (desktop?.MainWindow != null)
                                        {
                                            if (desktop.MainWindow.WindowState == WindowState.Minimized)
                                            {
                                                desktop.MainWindow.WindowState = WindowState.Normal;
                                            }
                                            desktop.MainWindow.Activate();
                                        }
                                    });
                                }
                            }
                            pipeServer.Disconnect();
                        }
                    }
                });

                pipeServerThread.IsBackground = true;
                pipeServerThread.Start();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Thread socketServerThread = new Thread(() =>
                {
                    if (File.Exists(SocketPath))
                    {
                        File.Delete(SocketPath);
                    }
                    
                    socketListener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                    socketListener.Bind(new UnixDomainSocketEndPoint(SocketPath));
                    socketListener.Listen(1);

                    while (true)
                    {
                        try
                        {
                            using (var handler = socketListener.Accept())    
                            {
                                byte[] buffer = new byte[256];
                                int bytesRead = handler.Receive(buffer);
                                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                if (message == "Show")
                                {
                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                                        if (desktop?.MainWindow != null)
                                        {
                                            if (desktop.MainWindow.WindowState == WindowState.Minimized)
                                            {
                                                desktop.MainWindow.WindowState = WindowState.Normal;
                                            }
                                            desktop.MainWindow.Activate();
                                            desktop.MainWindow.Show();
                                        }
                                    });
                                }
                            }
                        }
                        catch (SocketException)
                        {
                            //
                        }
                    }
                });

                socketServerThread.IsBackground = true;
                socketServerThread.Start();
            }
        }

        private void SendMessageToFirstInstance()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    pipeClient.Connect(1000); // Wait for 1 second to connect
                    using (var writer = new StreamWriter(pipeClient) { AutoFlush = true })
                    {
                        writer.WriteLine("Show");
                    }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                using (var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    client.Connect(new UnixDomainSocketEndPoint(SocketPath));
                    byte[] messageBytes = Encoding.UTF8.GetBytes("Show");
                    client.Send(messageBytes);
                }
            }
        }
    }
}