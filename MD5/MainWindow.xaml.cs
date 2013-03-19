using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.IO;

namespace MD5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static string filePath;
        private static string checksum;
        // Triggering regex match with over 32 chars!
        private const string Hashvalidator = @"[a-zA-Z0-9]{32}";
        private void BtnOpen_OnClick(object sender, RoutedEventArgs e)
        {
            var file = new OpenFileDialog();

            if ((bool)file.ShowDialog())
            {
                filePath = file.FileName;
                TxtPath.Text = Regex.Replace(filePath, @"\\", @"\");
                BtnCalculate.IsEnabled = true;
            }
            else BtnCalculate.IsEnabled = false;
        }

        private void Calculate_OnClick(object sender, RoutedEventArgs e)
        {
            filePath = TxtPath.Text;
            if (filePath == null) return;
            if ((!File.Exists(filePath)) || (!File.Exists(TxtPath.Text)))
            {
                MessageBox.Show("File does not exist!");
                return;
            }
            Title = "Calculating (0%)";

            // Thread for calculating file checksum in the background:
            var checker = new BackgroundWorker();
            checker.DoWork += checker_DoWork;
            checker.ProgressChanged += checker_ProgressChanged;
            checker.RunWorkerCompleted += checker_RunWorkerCompleted;
            checker.WorkerReportsProgress = true;
            
            checker.RunWorkerAsync();

            BtnOpen.IsEnabled = false;
            BtnPaste.IsEnabled = false;
            BtnCalculate.IsEnabled = false;
        }

        private void checker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Test case:
            // File: Devil May Cry (Europe) (En,Fr,De,Es,It).7z
            // Hash: 5a52747ef4e4b87a0b6bddf93ac804f3

            Title = "Compute MD5";
            BtnOpen.IsEnabled = true;
            BtnPaste.IsEnabled = true;
            BtnCalculate.IsEnabled = true;

            var output = "Hash for " + filePath + ": \n" + checksum + "\n\n";
            if (!String.IsNullOrWhiteSpace(TxtHash.Text))
            {
                if (Regex.IsMatch(TxtHash.Text, Hashvalidator))
                {
                    if (TxtHash.Text == checksum)
                        output += "The hash " + TxtHash.Text +
                                  " is the same as " + checksum + "it appears to be a valid file";

                    else output += "No Match! " + TxtPath.Text + " is not the same";
                }
                else
                {
                    output += "Invalid format: Could not compare hash code to file.";
                }
            }
            MessageBox.Show(output, "Calculation complete!");
        }

        void checker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PrgProgress.Value = e.ProgressPercentage;
            Title = "Calculating (" + e.ProgressPercentage + "%)";
        }

        // Code for allowing progress updates when hashing files src:
        // http://www.infinitec.de/post.aspx?id=8a12082e-0c37-41c7-b04c-e980f6324316

        private static void checker_DoWork(object sender, DoWorkEventArgs e)
        {
            // ReSharper disable TooWideLocalVariableScope
            byte[] buffer;
            byte[] oldBuffer;
            int bytesRead;
            int oldBytesRead;
            long size;
            long totalBytesRead = 0;
            // ReSharper enable TooWideLocalVariableScope

            var thread = sender as BackgroundWorker;
            using (var fs = File.OpenRead(filePath))
            {
                //var tempMD5 = System.Security.Cryptography.MD5.Create();
                //var cs = new CryptoStream(fs, tempMD5, CryptoStreamMode.Write);

                //try { checksum = BitConverter.ToString(Md5.ComputeHash(fs)).Replace("-", string.Empty); }
                //catch (Exception ex) { Debug.WriteLine("Exception: \r\n\t" + ex.Message); }
                using (HashAlgorithm hashAlgorithm = System.Security.Cryptography.MD5.Create())
                {
                    size = fs.Length;
                    buffer = new byte[4096];
                    bytesRead = fs.Read(buffer, 0, buffer.Length);

                    do
                    {
                        oldBytesRead = bytesRead;
                        oldBuffer = buffer;

                        buffer = new byte[4096];
                        bytesRead = fs.Read(buffer, 0, buffer.Length);
                        totalBytesRead += bytesRead;

                        if (bytesRead == 0) hashAlgorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                        else hashAlgorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
                        if (thread != null) thread.ReportProgress((int)((double)totalBytesRead * 100 / size));
                    } while (bytesRead != 0);
                    checksum = FormatHash(hashAlgorithm.Hash);
                }
                
            }
            
        }

        private void BtnPaste_OnClick(object sender, RoutedEventArgs e)
        {
            if (Regex.IsMatch(Clipboard.GetText(), Hashvalidator)) TxtHash.Text = Clipboard.GetText().ToUpper();
        }

        private static string FormatHash(IEnumerable<byte> hash)
        {
            if (hash == null) throw new NullReferenceException();
            var output = new StringBuilder();

            foreach (var b in hash)
            {
                output.Append(b.ToString("x2"));
            }
            return output.ToString().ToUpper();
        }
    }
}
