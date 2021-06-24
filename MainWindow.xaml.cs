using System;
using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using Timer = System.Windows.Forms.Timer;

namespace WatchAndDo
{
    public partial class MainWindow : Window
    {
        FileSystemWatcher watcher;
        private readonly Timer timer = new();
        private bool isWatching;
        private bool canChange;
        private string filePath = string.Empty;

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

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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
            foreach (var directory in directoryInfo.GetDirectories())
            {
                directoryItem.Items.Add(CreateDirectoryItems(directory));
            }
            foreach (var file in directoryInfo.GetFiles())
            {
                directoryItem.Items.Add(new TreeViewItem { Header = file.Name, Tag = file.FullName });
            }

            return directoryItem;
        }

        private void BtnListen_Click(object sender, RoutedEventArgs e)
        {
            //Check to see if filewatcher is on or not and display usefull info
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

            watcher = new FileSystemWatcher
            {
                Path = TxtDir.Text,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes,
                Filter = "*.*"
            };
            watcher.Created += Watcher_Created;
            watcher.Renamed += Watcher_Renamed;
            watcher.Changed += Watcher_Changed;
            watcher.Deleted += Watcher_Deleted;
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

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            AppendListViewCalls("File: \"" + e.FullPath + "\"- " + DateTime.Now + " - Processed the changes successfully");
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            AppendListViewCalls("File: \"" + e.FullPath + "\"- " + DateTime.Now + " - created successfully");
            SendMail(e);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            AppendListViewCalls("Renamed successfully:");
            AppendListViewCalls("Old File: \"" + e.OldFullPath + "\"- " + DateTime.Now);
            AppendListViewCalls("New File: \"" + e.FullPath + "\"- " + DateTime.Now);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            AppendListViewCalls("File: \"" + e.FullPath + "\"- " + DateTime.Now + " - deleted successfully");
        }

        private void AppendListViewCalls(object input)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                ListResults.Items.Add(input);

            }));
        }

        private void TreeFiles_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
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

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                MainWindow appendListViewsCalls = new();
                appendListViewsCalls.AppendListViewCalls("-----------------------------------");
                appendListViewsCalls.AppendListViewCalls($"Message: {ex.Message}");
                appendListViewsCalls.AppendListViewCalls("Stacktrace:");
                appendListViewsCalls.AppendListViewCalls(ex.StackTrace);
                PrintException(ex.InnerException);
                appendListViewsCalls.AppendListViewCalls("-----------------------------------");
            }
        }

        private static void SendMail(FileSystemEventArgs e)
        {
            //using MailMessage mail = new();
            //mail.From = new MailAddress("**********@***********.com");
            //mail.To.Add("**********@***********.com");

            //var path = e.FullPath;
            //string readText = File.ReadAllText(path);
            //mail.Body = readText;

            //using SmtpClient smtpClient = new(); //SMTP port(TLS): 587 | SMTP port(SSL): 465
            //smtpClient.UseDefaultCredentials = false;
            //smtpClient.Credentials = new System.Net.NetworkCredential("**********@***********.com", "***********");
            //smtpClient.Port = 587;
            //smtpClient.Host = "smtp-mail.outlook.com";
            //smtpClient.EnableSsl = true;
            //smtpClient.Send(mail);

            smtpClient.SendCompleted += SmtpClient_SendCompleted;
        }

        private static void SmtpClient_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            string token = (string)e.UserState;
            MainWindow appendListViewsCalls = new();

            if (e.Cancelled)
            {
                appendListViewsCalls.AppendListViewCalls($"[{0}] Send canceled: " + token);
            }
            else if (e.Error != null)
            {
                appendListViewsCalls.AppendListViewCalls($"[{0}] {1}" + token + " " + e.Error.ToString());
            }
            else
            {
                appendListViewsCalls.AppendListViewCalls("Email successfully sent");
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
