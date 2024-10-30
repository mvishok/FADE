using CommandDotNet;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using FADE;
using System.Runtime.ConstrainedExecution;
using System;
using System.Runtime.InteropServices;

public class Runner
{
    const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

    Logger logger = new Logger();
    static readonly string? exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    string? fastrePath;

    public static async Task<int> cmdRunInput(string command)
    {
        // Enable ANSI colors in Windows console
        IntPtr handle = GetStdHandle(-11);  // Get the console's standard output handle
        GetConsoleMode(handle, out int mode);
        SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);

        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = "/c " + command;
        process.StartInfo.UseShellExecute = false;  // Attach to current console
        process.StartInfo.RedirectStandardInput = false;  // No input/output redirection
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.StartInfo.CreateNoWindow = false;  // Use current terminal

        process.Start();

        await process.WaitForExitAsync();  // Wait for the process to exit
        return process.ExitCode;  // Return the exit code
    }

    static int Main(string[] args)
    {
        if (string.IsNullOrEmpty(exeDir)){
            Console.WriteLine("Failed to determine the executable directory.");
            return 1;
        }
        Console.CancelKeyPress += (sender, e) =>
        {
            Cmd.KillChildProcesses();
            Environment.Exit(1);
        };
        // AppRunner<T> where T is the class defining your commands
        // You can use Program or create commands in another class
        return new AppRunner<Runner>().Run(args);
    }

    public async Task<Task> fastre(string arg)
    {
        //Check if fastre is installed
        Process installed = Cmd.cmd("which fastre");
        if (installed.ExitCode != 0)
        {
            logger.Info("fastre is not installed");
            logger.Info("Install fastre by running 'fastre install fastre'");
            return Task.CompletedTask;
        }
        else
        {
            String fastreP = Cmd.cmdString("which fastre");
            fastrePath = fastreP.Replace("/", "\\").Substring(1).Insert(1, ":").Trim();
            fastrePath = string.Join("\\", fastrePath.Split('\\').Take(fastrePath.Split('\\').Length - 1)) + "\\node_modules\\fastre";    
        }

        //check for updates to fastre silently from fastrePath/package.json
        string pkgJson = File.ReadAllText(fastrePath + "\\package.json");
        dynamic pkg = JsonConvert.DeserializeObject(pkgJson) ?? new JObject();
        Version cv = new Version(pkg["version"].ToString());
        Version lv = new Version(await FetchLatestTag("mvishok", "fastre", logger));
        if (lv > cv)
        {
            logger.Warning("A new version of fastre is available. Run 'fade update fastre' to update.\n");
        }

        string[]? args = arg.Split(' ');

        switch (args[0])
        {
            case "add":
                if (await AddFastrePackageAsync(args)) return Task.CompletedTask;
                else Environment.Exit(1);
                break;

            case "update":
                if (await UpdateFastrePackageAsync(args)) return Task.CompletedTask;
                else Environment.Exit(1);
                break;

            case "remove":
                if (RemoveFastePackage(args)) return Task.CompletedTask;
                else Environment.Exit(1);
                break;

            default:

                //delete one hpyhen '-' if there are three hyphens consecutively in arg string
                arg = arg.Replace("$", "--");
                //start "npx fastre" with the arguments
                int exitCode = await Cmd.cmdRun("npx fastre " + arg);
                if (exitCode != 0)
                {
                    Console.Write("\n");
                    logger.Error("Fastre exited with code " + exitCode);
                }
                break;
        }

        return Task.CompletedTask;
    }

    public async Task<Task> autobase(string arg)
    {
        //Check if autobase is installed in exeDir/autobase
        if (!Directory.Exists(exeDir + "\\autobase") || !File.Exists(exeDir + "\\autobase\\autobase.exe"))
        {
            logger.Info("autobase is not installed");
            logger.Info("Install autobase by running 'fade install autobase'");
            return Task.CompletedTask;
        }

        if (!File.Exists(exeDir + "\\autobase\\ver.txt")){ 
            logger.Error("Version file not found. Assuming 0.0.0");
            System.IO.File.WriteAllText(exeDir + "\\autobase\\ver.txt", "0.0.0");
        }

        // Check for updates to autobase silently from exeDir/autobase/ver.txt
        Version cv = new Version(System.IO.File.ReadAllText(exeDir + "\\autobase\\ver.txt"));
        Version lv = new Version(await FetchLatestTag("mvishok", "autobase", logger));
        if (lv > cv)
        {
            logger.Warning("A new version of autobase is available. Run 'fade update autobase' to update.\n");
        }

        //delete one hpyhen '-' if there are three hyphens consecutively in arg string
        arg = arg.Replace("$", "--");

        //start "exeDir/autobase/autobase.exe" with the arguments
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        int exitCode = await Cmd.cmdRun($"cd {exeDir}\\autobase && {exeDir[..2]} && autobase.exe {arg}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        if (exitCode != 0)
        {
            Console.Write("\n");
            logger.Error("Autobase exited with code " + exitCode);
        }

        return Task.CompletedTask;
    }

    public async Task<Task> minter(string? arg)
    {
        //Check if minter is installed in exeDir/minter
        if (!Directory.Exists(exeDir + "\\minter") || !File.Exists(exeDir + "\\minter\\minter.jar"))
        {
            logger.Info("minter is not installed");
            logger.Info("Install minter by running 'fade install minter'");
            return Task.CompletedTask;
        }

        if (!File.Exists(exeDir + "\\minter\\ver.txt")){ 
            logger.Error("Version file not found. Assuming 0.0.0");
            System.IO.File.WriteAllText(exeDir + "\\minter\\ver.txt", "0.0.0");
        }

        // Check for updates to minter silently from exeDir/minter/ver.txt
        Version cv = new Version(System.IO.File.ReadAllText(exeDir + "\\minter\\ver.txt"));
        Version lv = new Version(await FetchLatestTag("mvishok", "minter", logger));
        if (lv > cv)
        {
            logger.Warning("A new version of minter is available. Run 'fade update minter' to update.\n");
        }

        //split the arg string by space
        string[] args = new string[0];
        if (arg != null)
        {
            args = arg.Split(' ');
        }

        if (args.Length > 0 && args[0] != "")
        {
            //the first argument is the path to the file. convert it to absolute path if it is relative to the caller's directory
            if (!Path.IsPathRooted(args[0]))
            {
                //get the cwd
                string callerDir = Cmd.cmdString("cd");
                callerDir = callerDir.Substring(0, callerDir.Length - 1);
                args[0] = Path.Combine(callerDir, args[0]);
            }
        }

        arg = string.Join(" ", args);

        //start "exeDir/minter/minter.jar" with the arguments
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        int exitCode = await cmdRunInput($"cd \"{exeDir}\\minter\" && {exeDir[..2]} && java -jar minter.jar {arg}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        if (exitCode != 0) {
            Console.Write("\n");
            logger.Error("Minter exited with code " + exitCode);
        }
        return Task.CompletedTask;
    }

    private async Task<bool> AddFastrePackageAsync(String[] args)
    {

        if (args.Length < 2)
        {
            logger.Error("Invalid number of arguments");
            return false;
        }

        string[] repo = args[1].Split('/');
        if (repo.Length != 2)
        {
            logger.Error("Invalid repo name. Use 'username/repo'");
            return false;
        }
        // fetch latest tag and check if it is valid
        logger.Info("Fetching the latest tag for " + repo[0] + "/" + repo[1]);
        string latestTag = await FetchLatestTag(repo[0], repo[1], logger);
        if (latestTag == "")
        {
            logger.Error("Failed to fetch the latest tag. Exiting.");
            return false;
        }

        logger.Info("Latest tag: " + latestTag);

        // check if packages folder exists
        if (!Directory.Exists(fastrePath + "\\packages"))
        {
            Directory.CreateDirectory(fastrePath + "\\packages");
        }

        // check if packages.json exists
        if (!File.Exists(fastrePath + "\\packages\\packages.json"))
        {
            File.WriteAllText(fastrePath + "\\packages\\packages.json", "{}");
        }

        // check if package.json already contains the package
        logger.Info("Reading packages.json");
        string packagesJson = File.ReadAllText(fastrePath + "\\packages\\packages.json");
        dynamic json = JsonConvert.DeserializeObject(packagesJson) ?? new JObject();

        // check if username folder exists
        if (!Directory.Exists(fastrePath + "\\packages\\" + repo[0]))
        {
            Directory.CreateDirectory(fastrePath + "\\packages\\" + repo[0]);
        }

        // check if repo folder exists
        logger.Info("Checking if package already exists");
        if (Directory.Exists(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1]))
        {
            logger.Warning("This package already exists in disk. Overwrite? (y/n): ");
            string overwrite = Console.ReadLine() ?? "n";

            if (overwrite == "n")
            {
                return false;
            }

            // delete the folder
            try
            {
                Directory.Delete(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1], true);
            }
            catch (Exception e)
            {
                logger.Error("Failed to delete the package folder: " + e.Message);
                return false;
            }
        }

        // download the package from github
        string github = $"https://github.com/{repo[0]}/{repo[1]}/archive/refs/tags/{latestTag}.zip";
        logger.Info("Downloading package from " + github);
        bool downloadPakcage = await Downloader.DownloadFileWithProgress(github, fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

        if (!downloadPakcage)
        {
            logger.Error("Failed to download the package. Exiting.");
            return false;
        }

        // extract the package
        logger.Info("Extracting package");
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip", fastrePath + "\\packages\\" + repo[0] + "\\");
        }
        catch (Exception e)
        {
            logger.Error("Failed to extract the package: " + e.Message);
            return false;
        }

        // Rename package-tag to package
        logger.Info("Renaming package folder");
        Directory.Move(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + "-" + latestTag, fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1]);

        // Remove the zip file
        logger.Info("Cleaning up");
        File.Delete(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

        // fetch entrypoint from pkg.json
        logger.Info("Reading pkg.json");
        string pkgJson = File.ReadAllText(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + "\\pkg.json");
        dynamic pkg = JsonConvert.DeserializeObject(pkgJson) ?? new JObject();

        // check if entry point exists
        logger.Info("Checking if entry point exists");
        if (pkg["entry"] == null)
        {
            logger.Error("Entry point not found in pkg.json. Exiting.");
            return false;
        }

        // Add package to packages.json
        logger.Info("Adding package to packages.json");
        string pname = repo[1] + "@" + latestTag;
        json[pname] = repo[0] + "\\" + repo[1] + "\\" + pkg["entry"];
        string updatedPackagesJson = JsonConvert.SerializeObject(json, Formatting.Indented);
        File.WriteAllText(fastrePath + "\\packages\\packages.json", updatedPackagesJson);

        // success
        logger.Success("Package " + repo[1].Split('@')[0] + " added successfully.");

        return true;
    }

    private async Task<bool> UpdateFastrePackageAsync(String[] args)
    {
        if (args.Length < 2)
        {
            logger.Error("Invalid number of arguments");
            return false;
        }

        string[] repo = args[1].Split('/');
        if (repo.Length != 2)
        {
            logger.Error("Invalid repo name. Use 'username/repo'");
            return false;
        }

        // check if packages folder exists
        if (!Directory.Exists(fastrePath + "\\packages"))
        {
            logger.Error("Packages folder not found. Exiting.");
            return false;
        }

        // check if packages.json exists
        if (!File.Exists(fastrePath + "\\packages\\packages.json"))
        {
            logger.Error("packages.json not found. Exiting.");
            return false;
        }

        //read packages.json
        logger.Info("Reading packages.json");
        string packagesJson = File.ReadAllText(fastrePath + "\\packages\\packages.json");
        dynamic json = JsonConvert.DeserializeObject(packagesJson) ?? new JObject();

        //read pkg.json of package (dir / username / repo / pkg.json)
        var version = new Version();
        String ver;
        if (!Directory.Exists(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1]))
        {
            logger.Error("Package not found. Exiting.");
            return false;
        }
        else
        {
            string pkgJson = File.ReadAllText(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + "\\pkg.json");
            dynamic pkg = JsonConvert.DeserializeObject(pkgJson) ?? new JObject();
            ver = pkg["version"].ToString();
            version = new Version(ver);
        }

        // fetch latest tag and compare with current version
        logger.Info("Fetching the latest tag for " + repo[0] + "/" + repo[1]);
        string latestTag = await FetchLatestTag(repo[0], repo[1], logger);
        if (latestTag == "")
        {
            logger.Error("Failed to fetch the latest tag. Exiting.");
            return false;
        }

        var latestVersion = new Version(latestTag);
        if (latestVersion <= version)
        {
            logger.Info("Package is already up to date.");
            return true;
        }

        // delete the package folder
        logger.Info("Deleting the package folder");
        try
        {
            Directory.Delete(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1], true);
        }
        catch (Exception e)
        {
            logger.Error("Failed to delete the package folder: " + e.Message);
            return false;
        }

        // download the package from github
        string github = $"https://github.com/{repo[0]}/{repo[1]}/archive/refs/tags/{latestTag}.zip";
        logger.Info("Downloading package from " + github);
        bool downloadPakcage = await Downloader.DownloadFileWithProgress(github, fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

        if (!downloadPakcage)
        {
            logger.Error("Failed to download the package. Exiting.");
            return false;
        }

        //extract the package
        logger.Info("Extracting package");
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip", fastrePath + "\\packages\\" + repo[0] + "\\");
        }
        catch (Exception e)
        {
            logger.Error("Failed to extract the package: " + e.Message);
            return false;
        }

        // Rename package-tag to package
        logger.Info("Renaming package folder");
        Directory.Move(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + "-" + latestTag, fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1]);

        // Remove the zip file
        logger.Info("Cleaning up");
        File.Delete(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

        // update the version in packages.json
        logger.Info("Updating version in packages.json");
        string oldname = repo[1] + "@" + ver;
        string newname = repo[1] + "@" + latestTag;
        string p = json[oldname];
        json.Remove(oldname);
        json[newname] = p;

        string updatedPackagesJson = JsonConvert.SerializeObject(json, Formatting.Indented);
        File.WriteAllText(fastrePath + "\\packages\\packages.json", updatedPackagesJson);

        // success
        logger.Success("Package " + repo[1].Split('@')[0] + " updated successfully.");
        return true;
    }

    private bool RemoveFastePackage(String[] args)
    {
        if (args.Length < 2)
        {
            logger.Error("Invalid number of arguments");
            return false;
        }

        string[] repo = args[1].Split('/');
        if (repo.Length != 2)
        {
            logger.Error("Invalid repo name. Use 'username/repo'");
            return false;
        }

        // check if packages folder exists
        if (!Directory.Exists(fastrePath + "\\packages"))
        {
            logger.Error("Packages folder not found. Exiting.");
            return false;
        }

        // check if packages.json exists
        if (!File.Exists(fastrePath + "\\packages\\packages.json"))
        {
            logger.Error("packages.json not found. Exiting.");
            return false;
        }

        //read packages.json
        logger.Info("Reading packages.json");
        string packagesJson = File.ReadAllText(fastrePath + "\\packages\\packages.json");
        dynamic json = JsonConvert.DeserializeObject(packagesJson) ?? new JObject();

        // check if username folder exists
        if (!Directory.Exists(fastrePath + "\\packages\\" + repo[0]))
        {
            logger.Error("Package not found. Exiting.");
            return false;
        }

        string ver;

        // check if repo folder exists
        if (!Directory.Exists(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1]))
        {
            logger.Error("Package not found. Exiting.");
            return false;
        }
        else
        {
            string pkgJson = File.ReadAllText(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1] + "\\pkg.json");
            dynamic pkg = JsonConvert.DeserializeObject(pkgJson) ?? new JObject();
            ver = pkg["version"].ToString();
        }

        // delete the package folder
        logger.Info("Deleting the package folder");
        try
        {
            Directory.Delete(fastrePath + "\\packages\\" + repo[0] + "\\" + repo[1], true);
        }
        catch (Exception e)
        {
            logger.Error("Failed to delete the package folder: " + e.Message);
            return false;
        }

        // remove the package from packages.json
        logger.Info("Removing package from packages.json");
        string pname = repo[1] + "@" + ver;
        json.Remove(pname);
        string updatedPackagesJson = JsonConvert.SerializeObject(json, Formatting.Indented);
        File.WriteAllText(fastrePath + "\\packages\\packages.json", updatedPackagesJson);

        // success
        logger.Success("Package " + repo[1] + " removed successfully.");
        return true;
    }

    private static async Task<string> FetchLatestTag(string author, string package, Logger logger)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FADE/1.0");

        try
        {
            var response = await httpClient.GetAsync($"https://api.github.com/repos/{author}/{package}/releases/latest");
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
            return "";
        }
    }
}
