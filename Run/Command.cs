using Spectre.Console;
using System.Diagnostics;
using System.Text;

namespace FADE
{
    internal class Cmd
    {
        private static List<Process> _childProcesses = new List<Process>();

        public static Process cmd(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();

            return process;
        }

        public static String cmdString(string command)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();

            if (process.ExitCode != 0)
            {
                return "ERROR:" + process.StandardError.ReadToEnd();
            }
            else
            {
                return output;
            }
        }

        public static async Task<int> cmdRun(string command)
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

        public static Process admin(string command, Logger logger)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + command;
                process.StartInfo.UseShellExecute = true; // This must be true for runas
                process.StartInfo.Verb = "runas"; // Run as administrator
                process.Start();

                process.WaitForExit();
                return process;
            }
            catch (Exception ex)
            {
                logger.Error("Error running command as administrator: " + ex.Message);
                Environment.Exit(1);
                return null;

            }
        }

        public static void KillChildProcesses()
        {
            Logger logger = new Logger();
            foreach (var process in _childProcesses)
            {
                logger.Info("Killing child process: " + process.Id);
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true); // Forcefully terminate the process
                        process.WaitForExit(1);
                        if (!process.HasExited)  logger.Error("Failed to kill process: " + process.Id);
                        else logger.Info("Successfully killed process: " + process.Id);
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
