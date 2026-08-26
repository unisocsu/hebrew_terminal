using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace RTLTerminal
{
    public partial class MainWindow : Window
    {
        private Process _cmdProcess;
        private StreamWriter _processInput;
        private bool _isProcessRunning = false;
        private object _outputLock = new object();

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in constructor: {ex.Message}\n\n{ex.StackTrace}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                AppendOutput("RTL Terminal starting...\n", Brushes.Gray);
                AppendOutput("Launching cmd.exe...\n", Brushes.Gray);
                
                StartCmdProcess();
                
                InputBox.Focus();
                AppendOutput("✓ RTL Terminal ready. Type commands and press Enter.\n", Brushes.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in MainWindow_Loaded: {ex.Message}\n\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartCmdProcess()
        {
            try
            {
                _cmdProcess = new Process();
                _cmdProcess.StartInfo.FileName = "cmd.exe";
                _cmdProcess.StartInfo.UseShellExecute = false;
                _cmdProcess.StartInfo.RedirectStandardInput = true;
                _cmdProcess.StartInfo.RedirectStandardOutput = true;
                _cmdProcess.StartInfo.RedirectStandardError = true;
                _cmdProcess.StartInfo.CreateNoWindow = true;
                _cmdProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                _cmdProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                _cmdProcess.Start();
                _processInput = _cmdProcess.StandardInput;
                _isProcessRunning = true;

                // Start reading output in background threads
                Task.Run(() => ReadStandardOutput());
                Task.Run(() => ReadStandardError());
            }
            catch (Exception ex)
            {
                _isProcessRunning = false;
                AppendOutput($"❌ Error starting cmd.exe: {ex.Message}\n", Brushes.Red);
                AppendOutput($"Details: {ex.StackTrace}\n", Brushes.Red);
                MessageBox.Show($"Failed to start cmd.exe:\n\n{ex.Message}", "Process Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReadStandardOutput()
        {
            try
            {
                while (_isProcessRunning && !_cmdProcess.StandardOutput.EndOfStream)
                {
                    string line = _cmdProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        Dispatcher.Invoke(() => AppendOutput(line + "\n", Brushes.White));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendOutput($"Read error: {ex.Message}\n", Brushes.Red));
            }
        }

        private void ReadStandardError()
        {
            try
            {
                while (_isProcessRunning && !_cmdProcess.StandardError.EndOfStream)
                {
                    string line = _cmdProcess.StandardError.ReadLine();
                    if (line != null)
                    {
                        Dispatcher.Invoke(() => AppendOutput(line + "\n", Brushes.Yellow));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AppendOutput($"Error stream read error: {ex.Message}\n", Brushes.Red));
            }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                string command = InputBox.Text;
                AppendOutput($"> {command}\n", Brushes.Cyan);

                try
                {
                    _processInput.WriteLine(command);
                    _processInput.Flush();
                }
                catch (Exception ex)
                {
                    AppendOutput($"Error sending command: {ex.Message}\n", Brushes.Red);
                }

                InputBox.Clear();
                e.Handled = true;
            }
        }

        private void AppendOutput(string text, Brush color)
        {
            lock (_outputLock)
            {
                Paragraph paragraph = new Paragraph();
                Run run = new Run(text);
                run.Foreground = color;
                paragraph.Inlines.Add(run);
                
                // תמיכה בـ RTL - WPF יטפל באלגוריתם Bidi אוטומטית
                OutputBox.Document.Blocks.Add(paragraph);
                
                // Auto-scroll to bottom
                OutputBox.ScrollToEnd();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isProcessRunning = false;
            try
            {
                _processInput?.WriteLine("exit");
                _processInput?.Flush();
                _processInput?.Close();
                _cmdProcess?.WaitForExit(2000);
                _cmdProcess?.Kill();
            }
            catch { }
        }
    }
}
