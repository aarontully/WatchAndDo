using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using Timer = System.Windows.Forms.Timer;

namespace WatchAndDo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FileSystemWatcher watcher;
        static readonly object locker = new();
        private readonly Timer timer = new();
        private bool isWatching;
        private bool canChange;
        private string filePath = string.Empty;
        DateTime lastRead = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(Settings.Default.PathSetting))
            {
                TxtDir.Text = Settings.Default.PathSetting; //gets the saved path
                ListDirectory(treeFiles, TxtDir.Text);
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();

            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtDir.Text = dialog.SelectedPath;
            }
            ListDirectory(treeFiles, TxtDir.Text);
        }

        //This method will list files to our treeview
        private void ListDirectory(System.Windows.Controls.TreeView treeView, string path)
        {
            try
            {
                treeView.Items.Clear();
                var rootDirectoryInfo = new DirectoryInfo(path);
                treeView.Items.Add(CreateDirectoryItems(rootDirectoryInfo));
            }
            catch (Exception ex)
            {
                AppendListViewCalls(ex.Message);
            }
        }

        private static TreeViewItem CreateDirectoryItems(DirectoryInfo directoryInfo)
        {
            var directoryItem = new TreeViewItem { Header = directoryInfo.Name };
            foreach(var directory in directoryInfo.GetDirectories())
            {
                directoryItem.Items.Add(CreateDirectoryItems(directory));
            }
            foreach(var file in directoryInfo.GetFiles())
            {
                directoryItem.Items.Add(new TreeViewItem { Header = file.Name, Tag = file.FullName });
            }

            return directoryItem;
        }

        private void BtnListen_Click(object sender, RoutedEventArgs e)
        {
            //Check to see if filewatchewr is on or not and display usefull info
            if (isWatching)
            {
                BtnListen.Content = "Start Watching";
                StopWatching();
            }
            else
            {
                BtnListen.Content = "Stop Watching";
                StartWatching();
            }
        }

        private void StartWatching()
        {
            if (!IsDirectoryValid(TxtDir.Text))
            {
                AppendListViewCalls(DateTime.Now + " - Watch Directory Invalid");
                return;
            }
            isWatching = true;
            timer.Enabled = true;
            timer.Start();
            timer.Interval = 500;
            AppendListViewCalls(DateTime.Now + " - Watcher Started");

            watcher = new FileSystemWatcher();
            watcher.Path = TxtDir.Text;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnChanged);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
        }

        private void StopWatching()
        {
            isWatching = false;
            timer.Enabled = false;
            timer.Stop();
            AppendListViewCalls(DateTime.Now + " - Watcher Stopped");
        }

        private static bool IsDirectoryValid(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            //Specify what to do when a file is changed, created, or deleted  

            //filter file types  
            if (Regex.IsMatch(System.IO.Path.GetExtension(e.FullPath), @"\.txt", RegexOptions.IgnoreCase))
            {
                try
                {
                    while (IsFileLocked(e.FullPath))
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    lock (locker)
                    {
                        //Process file  
                        //Do further activities  
                        DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);
                        if (lastWriteTime != lastRead)
                        {
                            AppendListViewCalls("File: \"" + e.FullPath + "\"- " + DateTime.Now + " - Processed the changes successfully");
                            lastRead = lastWriteTime;
                        }

                    }
                }
                catch (FileNotFoundException)
                {
                    //Stop processing  
                }
                catch (Exception ex)
                {
                    AppendListViewCalls("File: \"" + e.FullPath + "\" ERROR processing file (" + ex.Message + ")");
                }
            }

            else
                AppendListViewCalls("File: \"" + e.FullPath + "\" has been ignored");
        }

        private static bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = new FileInfo(file).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (IOException)
            {
                //the file is unavailable because it is:  
                //still being written to  
                //or being processed by another thread  
                //or does not exist (has already been processed)  
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked  
            return false;
        }

        private void AppendListViewCalls(object input)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                this.ListResults.Items.Add(input);

            }));
        }

        private void treeFiles_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                var item = (TreeViewItem)e.NewValue;
                filePath = item.Tag.ToString();

                //Check changes for .txt files only  
                if (Regex.IsMatch(System.IO.Path.GetExtension(filePath), @"\.txt", RegexOptions.IgnoreCase))
                {
                    canChange = false;
                    TxtEditor.Clear();
                    string contents = File.ReadAllText(filePath);
                    TxtEditor.Text = contents;
                    canChange = true;
                }
            }
            catch (Exception ex)
            {
                AppendListViewCalls(ex.Message);
            }
        }

        private void TxtEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (canChange)
                {
                    System.IO.File.WriteAllText(filePath, TxtEditor.Text);
                }

            }
            catch (Exception ex)
            {
                AppendListViewCalls(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Save Path on closure  
            Settings.Default.PathSetting = TxtDir.Text;
            Settings.Default.Save();
        }
    }
}
