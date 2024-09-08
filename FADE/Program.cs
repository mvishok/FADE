using CommandDotNet;
using FADE;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class Program
{
    string fadeVersion = "0.1.0";
    FADE.Logger logger = new FADE.Logger();
    string? exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    // this is the entry point of your application
    static int Main(string[] args)
    {
        // AppRunner<T> where T is the class defining your commands
        // You can use Program or create commands in another class
        return new AppRunner<Program>().Run(args);
    }

    //Install command
    public async Task install(string package)
    {
        if (package == "fastre")
        {
            // Check if Node.js is installed and install it if it's not
            Process nodeExists = Cmd.cmd("node -v");
            if (nodeExists.ExitCode != 0)
            {
                logger.Info("Node not installed");

                logger.Info("Fetching latest version of Node.js");
                string? latestNode;
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FADE/1.0");
                try
                {
                    var response = await httpClient.GetAsync("https://nodejs.org/download/release/index.json");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var jsonObj = JArray.Parse(json);
                    latestNode = jsonObj[0]["version"]?.ToString();

                }
                catch (Exception e)
                {
                    logger.Error("Failed to fetch the latest version of Node.js: " + e.Message);
                    return;
                }


                logger.Info("Downloading Node.js " + latestNode);

                string node = $"https://nodejs.org/download/release/{latestNode}/node-{latestNode}-{(Environment.Is64BitOperatingSystem ? "x64" : "x86")}.msi";
                logger.Info(node);
                bool downloaded = await Downloader.DownloadFileWithProgress(node, exeDir + "\\node.msi");

                if (!downloaded)
                {
                    logger.Error("Failed to download Node.js v22.4.1");
                    Environment.Exit(1);
                    return;
                }
                else
                {
                    logger.Success("Node.js v22.4.1 downloaded successfully");
                }

                logger.Info("Installing Node.js v22.4.1");
                Process process = Cmd.admin($"msiexec /qn /i \"{exeDir}\\node.msi\"", logger);
                if (process.ExitCode != 0)
                {
                    logger.Error("Failed to install Node.js v22.4.1");
                    return;
                }
                else
                {
                    logger.Success("Node.js v22.4.1 installed successfully");
                    System.IO.File.Delete(exeDir + "\\node.msi");
                }
            }

            // Check if fastre is already installed to avoid reinstallation
            Process fastreExists = Cmd.cmd("which fastre");
            if (fastreExists.ExitCode == 0)
            {
                logger.Info("FASTRE is already installed");
                return;
            }

            //install fastre with npm globally
            logger.Info("Installing FASTRE");
            Process fastre = Cmd.cmd("npm install -g fastre");
            if (fastre.ExitCode != 0)
            {
                logger.Error("Failed to install FASTRE");
                return;
            }
            else
            {
                logger.Success("FASTRE installed successfully");
            }

            //create runner
            logger.Info("Creating FASTRE runner");
            string batContent = "@echo off" + Environment.NewLine + "set ARGS=%*" + Environment.NewLine + "Run.exe fastre \"%ARGS%\"";
            try
            {
                System.IO.File.WriteAllText(exeDir + "\\fastre.bat", batContent);
                logger.Success("FASTRE runner created successfully");
            }
            catch (Exception e)
            {
                logger.Error("Failed to create FASTRE runner: " + e.Message);
                return;
            }

            logger.Info("FASTRE installed successfully");
        }
        else if (package == "autobase")
        {

            //get latest version of autobase
            logger.Info("Fetching latest tag of Autobase");
            string autobaseLatest = await FetchLatestTag("autobase", logger);
            var lv = new Version(autobaseLatest);

            if (Directory.Exists(exeDir + "\\autobase"))
            {
                if (System.IO.File.Exists(exeDir + "\\autobase\\autobase.exe") && System.IO.File.Exists(exeDir + "\\autobase\\ver.txt"))
                {
                    //check version of existing installation at /autobase/ver.txt
                    string version = System.IO.File.ReadAllText(exeDir + "\\autobase\\ver.txt");
                    var cv = new Version(version);

                    if (cv.CompareTo(lv) >= 0)
                    {
                        logger.Info("Autobase " + version + " is already installed");
                        return;
                    }
                    else
                    {
                        logger.Info("Autobase " + version + " is outdated");
                        logger.Warning("Autobase " + version + " will be updated to " + autobaseLatest);
                        logger.Warning("This operation will overwrite the existing installation.");
                        logger.Warning("Directory to be overwritten: " + exeDir + "\\autobase");
                        logger.Warning("Do you want to continue? (y/n)");
                        string? response = Console.ReadLine();
                        if (response?.ToLower() != "y")
                        {
                            logger.Info("Installation aborted");
                            return;
                        }

                        //delete existing autobase directory
                        logger.Info("Deleting existing Autobase installation");
                        Directory.Delete(exeDir + "\\autobase", true);
                    }
                }
            }

            Directory.CreateDirectory(exeDir + "\\autobase");

            //download
            logger.Info("Downloading Autobase " + autobaseLatest);
            string autobase = $"https://github.com/mvishok/autobase/releases/download/{autobaseLatest}/autobase.exe";
            bool downloadAutobase = await Downloader.DownloadFileWithProgress(autobase, exeDir + "\\autobase\\autobase.exe");

            if (!downloadAutobase)
            {
                logger.Error("Failed to download Fastre " + autobaseLatest);
                return;
            }
            else
            {
                logger.Success("Autobase " + autobaseLatest + " downloaded successfully");
            }

            //create ver.txt
            System.IO.File.WriteAllText(exeDir + "\\autobase\\ver.txt", autobaseLatest);

            //create runner
            logger.Info("Creating Autobase runner");
            string batContent = "@echo off" + Environment.NewLine + "set ARGS=%*" + Environment.NewLine + "Run.exe autobase \"%ARGS%\"";
            try
            {
                System.IO.File.WriteAllText(exeDir + "/autobase.bat", batContent);
                logger.Success("Autobase runner created successfully");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to create Autobase runner: " + ex.Message);
                return;
            }

            logger.Success("Autobase " + autobaseLatest + " installed successfully");
        }
        else
        {
            logger.Error("Unknown package: " + package);
        }
    }

    //Update command
    public async Task update(string? package)
    {
        //if no package given update FADE alone
        if (package == null)
        {

            logger.Info("Fetching latest tag of FADE");
            string fadeLatest = await FetchLatestTag("fade", logger);

            //get current version of fade
            var cv = new Version(fadeVersion);
            var lv = new Version(fadeLatest);

            if (cv.CompareTo(lv) >= 0)
            {
                logger.Info("FADE is already up to date at version " + fadeVersion);
                return;
            }
            
            //download fade
            logger.Info("Downloading FADE " + fadeLatest);
            string fade = $"https://github.com/mvishok/fade/releases/download/{fadeLatest}/fade.exe";
            bool downloaded = await Downloader.DownloadFileWithProgress(fade, exeDir + "\\update.exe");

            if (!downloaded)
            {
                logger.Error("Failed to download FADE " + fadeLatest);
                return;
            }
            else
            {
                logger.Success("FADE " + fadeLatest + " downloaded successfully");
            }

            //start "update.exe /VERYSILENT"
            logger.Info("Installing FADE " + fadeLatest);
            Process fadeProcess = Cmd.admin($"\"{exeDir}\\update.exe\" /VERYSILENT", logger);
            if (fadeProcess.ExitCode != 0)
            {
                logger.Error("Failed to install FADE " + fadeLatest);
                logger.Error("Installer exit code: " + fadeProcess.ExitCode);
                logger.Info("Please try running the installer manually");
                logger.Info("The installer is located at " + exeDir + "\\update.exe");
                return;
            }
            else
            {
                logger.Info("Cleaning up");
                try
                {
                    File.Delete(exeDir + "\\update.exe");
                }
                catch (Exception e)
                {
                    logger.Error("Failed to clean up: " + e.Message);
                }

                logger.Success("FADE " + fadeLatest + " installed successfully");
            }
        }
        else if (package == "fastre")
        {
            string fastrePath;
            //Check if fastre is installed
            Process installed = Cmd.cmd("which fastre");
            if (installed.ExitCode != 0)
            {
                logger.Info("fastre is not installed");
                logger.Info("Install fastre by running 'fastre install fastre'");
                return;
            }
            else
            {
                String fastreP = Cmd.cmdString("which fastre");
                fastrePath = fastreP.Replace("/", "\\").Substring(1).Insert(1, ":").Trim();
                fastrePath = string.Join("\\", fastrePath.Split('\\').Take(fastrePath.Split('\\').Length - 1)) + "\\node_modules\\fastre";
            }

            //get current version of fastre
            string pkgJson = File.ReadAllText(fastrePath + "\\package.json");
            dynamic pkg = JsonConvert.DeserializeObject(pkgJson) ?? new JObject();
            Version cv = new Version(pkg["version"].ToString());

            //get latest version of fastre
            logger.Info("Fetching latest tag of Fastre");
            string fastreLatest = await FetchLatestTag("fastre", logger);
            Version lv = new Version(fastreLatest);

            if (cv.CompareTo(lv) >= 0)
            {
                logger.Info("Fastre is already up to date at version " + pkg["version"]);
                return;
            }
            else
            {
                //install fastre with npm globally
                logger.Info("Updating Fastre");
                Process fastre = Cmd.cmd("npm install -g fastre");
                if (fastre.ExitCode != 0)
                {
                    logger.Error("Failed to update Fastre");
                    return;
                }
                else
                {
                    logger.Success("Fastre updated successfully");
                }
            }
        }
        else if (package == "autobase")
        {
            // Check if Autobase is installed
            if (!Directory.Exists(exeDir + "\\autobase") || !System.IO.File.Exists(exeDir + "\\autobase\\autobase.exe"))
            {
                logger.Info("Autobase is not installed");
                logger.Info("Install Autobase by running 'fastre install autobase'");
                return;
            }

            //get current version of autobase
            if (!System.IO.File.Exists(exeDir + "\\autobase\\ver.txt"))
            {
                logger.Error("Failed to read the version of Autobase. Assuming version 0.0.0");
                System.IO.File.WriteAllText(exeDir + "\\autobase\\ver.txt", "0.0.0");
            }

            string ver = System.IO.File.ReadAllText(exeDir + "\\autobase\\ver.txt");
            Version cv = new Version(ver);

            //get latest version of autobase
            logger.Info("Fetching latest tag of Autobase");
            string autobaseLatest = await FetchLatestTag("autobase", logger);
            Version lv = new Version(autobaseLatest);

            if (cv.CompareTo(lv) >= 0)
            {
                logger.Info("Autobase is already up to date at version " + ver);
                return;
            }
            else
            {
                // delete existing autobase installation
                logger.Info("Deleting existing Autobase installation");
                Directory.Delete(exeDir + "\\autobase", true);

                // install autobase
                logger.Info("Installing Autobase");
                install("autobase").Wait();

                logger.Success("Autobase updated successfully");
            }
        }
        else
        {
            logger.Error("Unknown package: " + package);
        }
    }

    private async Task<string> FetchLatestTag(string package, Logger logger)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FADE/1.0");

        try
        {
            var response = await httpClient.GetAsync($"https://api.github.com/repos/mvishok/{package}/releases/latest");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            // Parse the JSON and get the release tag
            var jsonObj = JObject.Parse(json);
            string? releaseTag = jsonObj["tag_name"]?.ToString();

            if (!string.IsNullOrEmpty(releaseTag))
            {
                return releaseTag;
            }
            else
            {
                logger.Error("Failed to retrieve the release tag.");
                return "";
            }
        }
        catch (Exception e)
        {
            logger.Error("Failed to fetch the latest tag: " + e.Message);
            return "0.0.0";
        }
    }
}