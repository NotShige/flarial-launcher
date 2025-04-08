﻿using Flarial.Launcher.Functions;
using Flarial.Launcher.Managers;
using Flarial.Launcher.Pages;
using Flarial.Launcher.Animations;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Application;
using File = System.IO.File;

namespace Flarial.Launcher
{
    public partial class MainWindow
    {
        public int version = 200;

        public static int progressPercentage;

        public static bool isDownloadingVersion = false;

        public static long progressBytesReceived;

        public static long progressBytesTotal;

        public static string progressType;

        public static bool isPremium;

        public static bool isLoggedIn;

        public static ImageBrush PFP;

        public static bool Reverse;

        public static TextBlock StatusLabel;

        public static TextBlock versionLabel;

        public static TextBlock Username;

        private static StackPanel mbGrid;

        private static readonly Stopwatch speed = new();

        public static SDK.Catalog VersionCatalog;

        public bool IsLaunchEnabled
        {
            get { return (bool)GetValue(IsLaunchEnabledProperty); }
            set { SetValue(IsLaunchEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsLaunchEnabledProperty =
            DependencyProperty.Register("IsLaunchEnabled", typeof(bool),
                typeof(MainWindow), new PropertyMetadata(true));

        public bool updateTextEnabled
        {
            get { return (bool)GetValue(updateTextEnabledProperty); }
            set { SetValue(updateTextEnabledProperty, value); }
        }

        public static readonly DependencyProperty updateTextEnabledProperty =
            DependencyProperty.Register("updateTextEnabled", typeof(bool),
                typeof(MainWindow), new PropertyMetadata(true));

        public int updateProgress
        {
            get { return (int)GetValue(updateProgressProperty); }
            set { SetValue(updateProgressProperty, value); }
        }

        public static readonly DependencyProperty updateProgressProperty =
            DependencyProperty.Register("updateProgress", typeof(int),
                typeof(MainWindow), new PropertyMetadata(0));


        public static UnhandledExceptionEventHandler unhandledExceptionHandler = (sender, args) =>
        {
            Exception ex = (Exception)args.ExceptionObject;
            string errorMessage = $"Unhandled exception: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";

            Trace.WriteLine(errorMessage);
            try
            {
                MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                Trace.WriteLine("Failed to show error in MessageBox.");
            }
        };


        private const int WM_CLOSE = 0x0010;
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SnapsToDevicePixels = UseLayoutRounding = true;

            MouseLeftButtonDown += (_, _) => DragMove();
            ContentRendered += MainWindow_ContentRendered;

            CreateDirectoriesAndFiles();

            Stopwatch stopwatch = new Stopwatch();
            speed.Start();
            stopwatch.Start();

            Trace.WriteLine("Debug 0 " + stopwatch.Elapsed.Milliseconds.ToString());

            string today = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string filePath = $"{VersionManagement.launcherPath}\\{today}.txt";

            var outResultsFile = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            );

            var textListener = new AutoFlushTextWriterTraceListener(outResultsFile);
            Trace.Listeners.Add(textListener);

            Trace.WriteLine("Debug 0.5 " + stopwatch.Elapsed.Milliseconds.ToString());

            AppDomain.CurrentDomain.UnhandledException += unhandledExceptionHandler;

            Trace.WriteLine("Debug 1 " + stopwatch.Elapsed.Milliseconds.ToString());

            LauncherVersion.Text = "v" + Assembly.GetExecutingAssembly().GetName().Version;

            Trace.WriteLine("Debug 2 " + stopwatch.Elapsed.Milliseconds.ToString());

            Dispatcher.InvokeAsync(Minecraft.Init);
            Trace.WriteLine("Debug 3 " + stopwatch.Elapsed.Milliseconds.ToString());

            StatusLabel = statusLabel;
            versionLabel = VersionLabel;
            Username = username;
            mbGrid = MbGrid;
            PFP = pfp;
            SettingsPage.MainGrid = MainGrid;
            SettingsPage.b1 = MainBorder;

            Environment.CurrentDirectory = VersionManagement.launcherPath;

            Dispatcher.InvokeAsync(RPCManager.Initialize);

            Trace.WriteLine("Debug 9 " + stopwatch.Elapsed.Milliseconds.ToString());
            Application.Current.MainWindow = this;

            SetGreetingLabel();
            Trace.WriteLine("Debug 10 " + stopwatch.Elapsed.Milliseconds.ToString());

            stopwatch.Stop();
            CreateMessageBox("Join our discord! https://flarial.xyz/discord");

            IsLaunchEnabled = false;

            StartRefreshTimer();

        }
        private System.Timers.Timer refreshTimer;
        private void StartRefreshTimer()
        {
            refreshTimer = new System.Timers.Timer(1.5 * 60 * 1000);
            refreshTimer.Elapsed += (sender, e) => RefreshWebView();
            refreshTimer.AutoReset = true;
            refreshTimer.Enabled = true;
        }

        private void RefreshWebView()
        {
        }

        private async void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            if (await SDK.Launcher.AvailableAsync())
            {
                updateTextEnabled = true;

                Dispatcher.Invoke(() =>
                {
                    MainGrid.IsEnabled = false;
                    MainGrid.Visibility = Visibility.Hidden;
                    mbGrid.Visibility = Visibility.Hidden;
                    LolGrid.Visibility = Visibility.Visible;
                    LolGrid.IsEnabled = true;
                }, DispatcherPriority.ApplicationIdle);

                await SDK.Launcher.UpdateAsync(DownloadProgressCallback2);
            }

            await Config.loadConfig();
            await Task.Run(() => Dispatcher.Invoke(() => VersionLabel.Text = SDK.Minecraft.Version));
            VersionCatalog = await SDK.Catalog.GetAsync();
            IsLaunchEnabled = HomePage.IsEnabled = true;
        }

        public static void CreateMessageBox(string text)
        {
            mbGrid.Children.Add(new Flarial.Launcher.Styles.MessageBox { Text = text });
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e) => DragMove();
        private void Minimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Close(object sender, RoutedEventArgs e)
        {
            if (!isDownloadingVersion)
            {
                AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionHandler;
                Trace.Close();
                Close();
            }
            else
            {
                CreateMessageBox("Flarial is currently downloading a version. You cannot close.");
            }
        }



        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) =>
            SettingsPageTransition.SettingsEnterAnimation(MainBorder, MainGrid);
        private void UIElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
            NewsPageTransition.Animation(Reverse, MainBorder, NewsBorder, NewsArrow);


        private void CreateDirectoriesAndFiles()
        {
            Trace.WriteLine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flarial"));

            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flarial")))
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Flarial"));

            if (!Directory.Exists(VersionManagement.launcherPath))
                Directory.CreateDirectory(VersionManagement.launcherPath);

            if (!Directory.Exists(BackupManager.backupDirectory))
                Directory.CreateDirectory(BackupManager.backupDirectory);

            if (!Directory.Exists(VersionManagement.launcherPath + "\\Versions\\"))
                Directory.CreateDirectory(VersionManagement.launcherPath + "\\Versions\\");

            if (!File.Exists($"{VersionManagement.launcherPath}\\cachedToken.txt"))
                File.Create($"{VersionManagement.launcherPath}\\cachedToken.txt");
        }


        private void SetGreetingLabel()
        {
            int Time = Int32.Parse(DateTime.Now.ToString("HH", System.Globalization.DateTimeFormatInfo.InvariantInfo));

            if (Time >= 0 && Time < 12)
                GreetingLabel.Text = "Good Morning!";
            else if (Time >= 12 && Time < 18)
                GreetingLabel.Text = "Good Afternoon!";
            else if (Time >= 18 && Time <= 24)
                GreetingLabel.Text = "Good Evening!";
        }

        private async void Inject_Click(object sender, RoutedEventArgs e)
        {
            IsLaunchEnabled = false;

            if (!SDK.Minecraft.Installed)
            {
                CreateMessageBox("Minecraft isn't installed, please install it!");
                IsLaunchEnabled = true; return;
            }

            bool compatible = await VersionCatalog.CompatibleAsync();
            if (!compatible)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CreateMessageBox("Flarial does not support this version of Minecraft.");
                    if (Config.UseCustomDLL) CreateMessageBox("Custom DLL will be used.");
                });
                IsLaunchEnabled = true; return;
            }


            if (!Config.UseCustomDLL)
            {
                if (compatible)
                {
                    await SDK.Client.DownloadAsync(Config.UseBetaDLL, (value) => DownloadProgressCallback(value));
                    await SDK.Client.LaunchAsync(Config.UseBetaDLL);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusLabel.Text = "Launched! Enjoy.";
                        IsLaunchEnabled = true;
                    });

                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Config.CustomDLLPath))
                {
                    await SDK.Minecraft.LaunchAsync(Config.CustomDLLPath);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusLabel.Text = "Launched Custom DLL! Enjoy.";
                        IsLaunchEnabled = true;
                    });
                }
            }

            IsLaunchEnabled = true;
        }



        public void DownloadProgressCallback(int value)
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusLabel.Text = $"Downloaded {value}% of client";
            });
        }

        public void DownloadProgressCallback2(int value)
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                updateProgress = value;
            });
        }

        private async void SaveConfig(object sender, RoutedEventArgs e)
        {
            await Config.saveConfig();
        }

        private void Window_OnClosing(object sender, CancelEventArgs e)
        {
            Trace.Close();
            AppDomain.CurrentDomain.UnhandledException -= unhandledExceptionHandler;
            Environment.Exit(0);
        }

    }
}

public class AutoFlushTextWriterTraceListener(Stream stream) : TextWriterTraceListener(stream)
{
    public override async void Write(string message)
    {
        await Writer.WriteAsync(message).ConfigureAwait(false);
        Writer.Flush();
    }

    public override async void WriteLine(string message)
    {
        await Writer.WriteLineAsync(message).ConfigureAwait(false);
        Writer.Flush();
    }
}


public class FileTraceListener : TraceListener
{
    private readonly StreamWriter _writer;

    public FileTraceListener(string filePath)
    {
        _writer = new StreamWriter(filePath, true)
        {
            AutoFlush = true // Enable AutoFlush if needed
        };
    }

    public override async void Write(string message)
    {
        await _writer.WriteAsync(message).ConfigureAwait(false);
    }

    public override async void WriteLine(string message)
    {
        await _writer.WriteLineAsync(message).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _writer?.Flush();
            _writer?.Close();
            _writer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
