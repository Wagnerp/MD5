using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            StbChecksum.Content = new ProgressBar();
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
            Title = "Compute MD5";
            BtnOpen.IsEnabled = true;
            BtnPaste.IsEnabled = true;
            BtnCalculate.IsEnabled = true;
            ((ProgressBar) StbChecksum.Content).Width = String.IsNullOrWhiteSpace(TxtHash.Text) ? 200 : 330;
            if (!Regex.IsMatch(TxtHash.Text, Hashvalidator) && (!String.IsNullOrWhiteSpace(TxtHash.Text)))
            {
                TbkStatus.Text = "ERROR";
                TbkStatus.ToolTip = "The checksum provided is an invalid format";
            }
            else if (Regex.IsMatch(TxtHash.Text, Hashvalidator))
            {
                TbkStatus.Text = TxtHash.Text == checksum ? "MATCH" : "NOT A MATCH";
            }
            else TbkStatus.Text = String.Empty;

            var ntbk = new TextBlock
                {
                    Name = "TbkChecksum",
                    FontWeight = FontWeights.DemiBold,
                    Text = checksum,
                    ToolTip = "Click to copy checksum to clipboard",
                    HorizontalAlignment = HorizontalAlignment.Right
                };

            ntbk.MouseLeftButtonUp += TbkChecksum_OnMouseLeftButtonUp;
            StbChecksum.Content = ntbk;
        }

        void checker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ((ProgressBar)StbChecksum.Content).Value = e.ProgressPercentage;
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

        private static void TbkChecksum_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(((TextBlock)sender).Text);
        }
    }
}
