using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AltroMon_Launcher_Updater
{
    public class Downloader
    {
        private ulong totalDownload = 0;
        private ulong totalDownloaded = 0;
        private WebClient client = new WebClient();
        private const string siteUrl = "https://altromon.ob1lab.xyz";
        private List<PathWithSize> filesDownload = new List<PathWithSize>();
        private string altroMonPath;
        private string settingsPath;
        private string jdk17Path;
        private string jdk18Path;
        private string launcherPath;
        private ProgressBar currentProgressBar;
        private ProgressBar totalProgressBar;
        private ulong currentDownloadSize;
        private Label status;
        private string actualVersion;
        private Window userWindow;
        public Downloader(ProgressBar currentProgressBar, ProgressBar totalProgressBar, Label status, Window userWindow)
        {
            string appdata = Environment.GetEnvironmentVariable("appdata");
            this.altroMonPath = $"{appdata}\\AltroMon";
            this.settingsPath = $"{altroMonPath}\\settings.json";
            this.jdk17Path = $"{altroMonPath}\\jdk-17";
            this.jdk18Path = $"{altroMonPath}\\jdk-1.8";
            this.launcherPath = $"{altroMonPath}\\launcher";
            this.currentProgressBar = currentProgressBar;
            this.totalProgressBar = totalProgressBar;
            this.status = status;
            this.userWindow = userWindow;
            this.init();
        }
        private async void init()
        {
            await this.checkFiles();
            if (this.filesDownload.Count > 0) {
                this.userWindow.Visibility = Visibility.Visible;
            }
            client.DownloadProgressChanged += this.downloadFileProgress;
            await this.downloadFiles();
            this.currentProgressBar.Value = 100;
            this.totalProgressBar.Value = 100;
            this.status.Content = "Вы используете последнюю версию!";
            Process.Start($"{launcherPath}/AltroMon Launcher {actualVersion}.exe");
            System.Windows.Application.Current.Shutdown();
        }
        private async Task checkFiles()
        {
            if (!Directory.Exists(this.altroMonPath))
            {
                Directory.CreateDirectory(this.altroMonPath);
            }
            if (!File.Exists(this.settingsPath))
            {
                StreamWriter settingsFile = File.CreateText(settingsPath);
                await settingsFile.WriteAsync("{}");
                settingsFile.Close();
            }
            if (!Directory.Exists(this.jdk17Path))
            {
                await taskDownloadFolder("api/launcher/files/jdk-17");
            }
            if (!Directory.Exists(this.jdk18Path))
            {
                await taskDownloadFolder("api/launcher/files/jdk-1.8");
            }
            this.actualVersion = (await this.client.DownloadStringTaskAsync(new Uri($"{siteUrl}/api/launcher/version"))).Replace("\"", string.Empty);
            if (!Directory.Exists(this.launcherPath))
            {
                await taskDownloadFolder($"api/launcher/files/launcher");
            }
            else
            {
                string[] launcherFiles = Directory.GetFiles(this.launcherPath);
                foreach (string launcherFile in launcherFiles)
                {
                    if (launcherFile.EndsWith(".exe")) {
                        string[] launcherFileSplit = launcherFile.Split(' ');
                        if (launcherFileSplit.Length == 3) {
                            string clientVersion = launcherFileSplit[2].Replace(".exe", string.Empty);
                            if (clientVersion != actualVersion)
                            {
                                Directory.Delete(this.launcherPath, true);
                                await taskDownloadFolder($"api/launcher/files/launcher");
                            }
                        }
                    }
                }
            }
        }
        private async Task taskDownloadFolder(string apiPath)
        {
            string res = await this.client.DownloadStringTaskAsync(new Uri($"{siteUrl}/{apiPath}"));
            PathWithSize[] files = await Task.Run(() => JsonConvert.DeserializeObject<PathWithSize[]>(res));
            foreach (PathWithSize file in files)
            {
                this.filesDownload.Add(file);
                this.totalDownload += file.size;
            }
        }
        private async Task downloadFiles()
        {
            foreach (PathWithSize file in this.filesDownload)
            {
                string pathFile = string.Join("/", file.path.Split('/').Skip(2));
                string[] pathSplit = pathFile.Split('/');
                Directory.CreateDirectory(Path.GetDirectoryName($"{this.altroMonPath}/{pathFile}"));
                this.currentDownloadSize = file.size;
                this.status.Content = $"Скачиваем: {pathSplit[pathSplit.Length -1]}";
                await this.client.DownloadFileTaskAsync(new Uri($"https://altromon.ob1lab.xyz/{file.path}"), $"{this.altroMonPath}/{pathFile}");
                this.totalDownloaded += file.size;
            }
            if (this.filesDownload.Count > 0) {
                await Task.Delay(500);
            }
        }
        private void downloadFileProgress(object s, DownloadProgressChangedEventArgs e)
        {
            this.currentProgressBar.Value = e.ProgressPercentage;
            this.totalProgressBar.Value = (((this.totalDownloaded + (this.currentDownloadSize * (uint) e.ProgressPercentage)/100) * 100) / this.totalDownload);
        }
    }
    public class PathWithSize
    {
        public string path { get; set; }
        public uint size { get; set; }
    }
}
