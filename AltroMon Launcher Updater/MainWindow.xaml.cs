using System.Windows;

namespace AltroMon_Launcher_Updater
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            new Downloader(progress_current, progress_total, label_status, mainUserWindow);
        }
    }
}
