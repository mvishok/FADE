using Spectre.Console;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace FADE
{
    internal class Cmd
    {
        private static List<Process> _childProcesses = new List<Process>();
        public static async Task<int> AutobaseAsync(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.EnvironmentVariables["FORCE_COLOR"] = "1"; // Force color output

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AnsiConsole.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AnsiConsole.Markup("[red]" + e.Data + "[/]");
                }
            };

            _childProcesses.Add(process);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            return process.ExitCode;
        }

        public static async Task<int> FastreAsync(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.EnvironmentVariables["FORCE_COLOR"] = "1"; // Force color output

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AnsiConsole.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AnsiConsole.Markup("[red]" + e.Data + "[/]");
                }
            };

            _childProcesses.Add(process);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
            return process.ExitCode;
        }

        public static void KillChildProcesses(Logger logger)
        {
            foreach (var process in _childProcesses)
            {
                logger.Info("Killing child process: " + process.Id);
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true); // Forcefully terminate the process
                        process.WaitForExit(); // Wait for the process to exit
                        logger.Info("Successfully killed process: " + process.Id);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to kill process {process.Id}: {ex.Message}");
                }
                finally
                {
                    process.Dispose(); // Clean up resources
                }
            }
            _childProcesses.Clear();
            Environment.Exit(0);
        }
    }
}
