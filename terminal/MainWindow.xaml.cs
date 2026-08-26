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
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RTLTerminal",
            "debug.log"
        );

        private static void WriteLog(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }
        private Process _cmdProcess;
        private StreamWriter _processInput;
        private bool _isProcessRunning = false;
        private object _outputLock = new object();

        public MainWindow()
        {
            try
            {
                WriteLog("=== RTLTerminal Started ===");
                WriteLog("MainWindow constructor started");
                
                InitializeComponent();
                WriteLog("InitializeComponent completed");
                
                this.Loaded += MainWindow_Loaded;
                WriteLog("Loaded event handler attached");
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR in constructor: {ex.Message}");
                WriteLog($"StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error in constructor: {ex.Message}\n\nCheck: %AppData%\\RTLTerminal\\debug.log", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                WriteLog("MainWindow_Loaded started");
                
                AppendOutput("RTL Terminal starting...\n", Brushes.Gray);
                WriteLog("Appended: RTL Terminal starting");
                
                AppendOutput("Launching cmd.exe...\n", Brushes.Gray);
                WriteLog("Appended: Launching cmd.exe");
                
                WriteLog("About to call StartCmdProcess");
                StartCmdProcess();
                WriteLog("StartCmdProcess completed");
                
                InputBox.Focus();
                WriteLog("Input focus set");
                
                AppendOutput("✓ RTL Terminal ready. Type commands and press Enter.\n", Brushes.Green);
                WriteLog("MainWindow_Loaded completed successfully");
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR in MainWindow_Loaded: {ex.Message}");
                WriteLog($"StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error in MainWindow_Loaded: {ex.Message}\n\nCheck: %AppData%\\RTLTerminal\\debug.log", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartCmdProcess()
        {
            try
            {
                WriteLog("StartCmdProcess: Creating Process object");
                _cmdProcess = new Process();
                
                WriteLog("StartCmdProcess: Setting up StartInfo");
                _cmdProcess.StartInfo.FileName = "cmd.exe";
                _cmdProcess.StartInfo.UseShellExecute = false;
                _cmdProcess.StartInfo.RedirectStandardInput = true;
                _cmdProcess.StartInfo.RedirectStandardOutput = true;
                _cmdProcess.StartInfo.RedirectStandardError = true;
                _cmdProcess.StartInfo.CreateNoWindow = true;
                _cmdProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                _cmdProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                WriteLog("StartCmdProcess: Starting cmd.exe");
                _cmdProcess.Start();
                WriteLog("StartCmdProcess: cmd.exe started successfully");
                
                _processInput = _cmdProcess.StandardInput;
                _isProcessRunning = true;

                WriteLog("StartCmdProcess: Starting output reader threads");
                // Start reading output in background threads
                Task.Run(() => ReadStandardOutput());
                Task.Run(() => ReadStandardError());
                WriteLog("StartCmdProcess: Reader threads started");
            }
            catch (Exception ex)
            {
                WriteLog($"ERROR in StartCmdProcess: {ex.Message}");
                WriteLog($"StackTrace: {ex.StackTrace}");
                _isProcessRunning = false;
                AppendOutput($"❌ Error starting cmd.exe: {ex.Message}\n", Brushes.Red);
                AppendOutput($"Details: {ex.StackTrace}\n", Brushes.Red);
                MessageBox.Show($"Failed to start cmd.exe:\n\n{ex.Message}\n\nCheck: %AppData%\\RTLTerminal\\debug.log", "Process Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
