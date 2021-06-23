using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WatchAndDo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Main();
        }

        private void Main()
        {
            using var watcher = new FileSystemWatcher(@"C:\Test\TestFolder");

            watcher.NotifyFilter = NotifyFilters.Attributes;
            watcher.NotifyFilter = NotifyFilters.CreationTime;
            watcher.NotifyFilter = NotifyFilters.DirectoryName;
            watcher.NotifyFilter = NotifyFilters.FileName;
            watcher.NotifyFilter = NotifyFilters.LastAccess;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.NotifyFilter = NotifyFilters.Security;
            watcher.NotifyFilter = NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.Filter = " *.*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            PrintException(e.GetException());
        }

        private void PrintException(Exception ex)
        {
            if(ex != null)
            {
                OutputText.Text = $"Message: {ex.Message}";
                OutputText.Text = "Stacktrace";
                OutputText.Text = ex.StackTrace;
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            OutputText.Text = $"Renamed: ";
            OutputText.Text = $"    Old: {e.OldFullPath}";
            OutputText.Text = $"    New: {e.FullPath}";
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            OutputText.Text = $"Deleted: {e.FullPath}";
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            string value = $"Created: {e.FullPath}";
            OutputText.Text = value;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if(e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            OutputText.Text = ($"Changed: {e.FullPath}");
        }
    }
}
