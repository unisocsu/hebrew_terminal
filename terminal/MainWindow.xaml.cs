using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RTLTerminal
{
    public partial class MainWindow : Window
    {
        private Process _process;
        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            StartProcess();
        }

        private void StartProcess()
        {
            try
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                _process.Start();
                _isRunning = true;

                OutputBox.Text = "RTL Terminal Ready\n";

                // Read output async
                Task.Run(() => ReadOutput());
                Task.Run(() => ReadError());
            }
            catch (Exception ex)
            {
                OutputBox.Text = $"Error: {ex.Message}";
            }
        }

        private void ReadOutput()
        {
            try
            {
                while (_isRunning && !_process.StandardOutput.EndOfStream)
                {
                    string line = _process.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputBox.Text += line + "\n";
                            OutputBox.ScrollToEnd();
                        });
                    }
                }
            }
            catch { }
        }

        private void ReadError()
        {
            try
            {
                while (_isRunning && !_process.StandardError.EndOfStream)
                {
                    string line = _process.StandardError.ReadLine();
                    if (line != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputBox.Text += "[ERROR] " + line + "\n";
                            OutputBox.ScrollToEnd();
                        });
                    }
                }
            }
            catch { }
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && _isRunning)
            {
                string cmd = InputBox.Text;
                _process.StandardInput.WriteLine(cmd);
                _process.StandardInput.Flush();
                InputBox.Clear();
                e.Handled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isRunning = false;
            try
            {
                _process?.StandardInput?.WriteLine("exit");
                _process?.StandardInput?.Flush();
                _process?.WaitForExit(2000);
                _process?.Kill();
            }
            catch { }
        }
    }
}
