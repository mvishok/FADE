using System.Diagnostics;

namespace FADE
{
    internal class Cmd
    {
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
    }
}
