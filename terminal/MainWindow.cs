using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            catch (Exception ex)
            {
                // Silent fail
                MessageBox.Show($"Log write failed: {ex.Message}");
            }
        }

        private Process _cmdProcess;
        private StreamWriter _processInput;
        private bool _isProcessRunning = false;
        private object _outputLock = new object();

        public MainWindow()
        {
            try
            {
                WriteLog("=== APP START ===");
                WriteLog("Constructor: Calling InitializeComponent");
                InitializeComponent();
                WriteLog("Constructor: InitializeComponent OK");
                
                WriteLog("Constructor: Attaching Loaded event");
                this.Loaded += MainWindow_Loaded;
                WriteLog("Constructor: Done");
            }
            catch (Exception ex)
            {
                WriteLog($"CONSTRUCTOR ERROR: {ex.Message}");
                WriteLog($"STACKTRACE: {ex.StackTrace}");
                MessageBox.Show($"Constructor Error:\n{ex.Message}\n\nLog: {LogPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WriteLog("MainWindow_Loaded: Started");
            try
            {
                WriteLog("MainWindow_Loaded: Calling StartCmdProcess");
                StartCmdProcess();
                WriteLog("MainWindow_Loaded: StartCmdProcess OK");
                
                InputBox.Focus();
                WriteLog("MainWindow_Loaded: Done");
            }
            catch (Exception ex)
            {
                WriteLog($"LOADED ERROR: {ex.Message}");
                WriteLog($"STACKTRACE: {ex.StackTrace}");
                MessageBox.Show($"Loaded Error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartCmdProcess()
        {
            WriteLog("StartCmdProcess: Creating Process");
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

                WriteLog("StartCmdProcess: Starting cmd.exe");
                _cmdProcess.Start();
                WriteLog("StartCmdProcess: cmd.exe started");
                
                _processInput = _cmdProcess.StandardInput;
                _isProcessRunning = true;

                WriteLog("StartCmdProcess: Starting reader threads");
                Task.Run(() => ReadStandardOutput());
                Task.Run(() => ReadStandardError());
                WriteLog("StartCmdProcess: OK");
            }
            catch (Exception ex)
            {
                WriteLog($"STARTCMD ERROR: {ex.Message}");
                WriteLog($"STACKTRACE: {ex.StackTrace}");
                _isProcessRunning = false;
                throw;
            }
        }

        private void ReadStandardOutput()
        {
            WriteLog("ReadStandardOutput: Thread started");
            try
            {
                while (_isProcessRunning && !_cmdProcess.StandardOutput.EndOfStream)
                {
                    string line = _cmdProcess.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        WriteLog($"OUTPUT: {line}");
                        Dispatcher.Invoke(() => 
                        {
                            OutputBox.Text += line + "\n";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"ReadOutput ERROR: {ex.Message}");
            }
        }

        private void ReadStandardError()
        {
            WriteLog("ReadStandardError: Thread started");
            try
            {
                while (_isProcessRunning && !_cmdProcess.StandardError.EndOfStream)
                {
                    string line = _cmdProcess.StandardError.ReadLine();
                    if (line != null)
                    {
                        WriteLog($"ERROR: {line}");
                        Dispatcher.Invoke(() => 
                        {
                            OutputBox.Text += "[ERROR] " + line + "\n";
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"ReadError ERROR: {ex.Message}");
            }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                string command = InputBox.Text;
                WriteLog($"USER INPUT: {command}");
                
                try
                {
                    _processInput.WriteLine(command);
                    _processInput.Flush();
                    InputBox.Clear();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    WriteLog($"INPUT ERROR: {ex.Message}");
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WriteLog("Window_Closing: Closing");
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
            WriteLog("Window_Closing: Done");
        }
    }
}
