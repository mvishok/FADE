using CommandDotNet;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using FADE;

public class Runner
{
    Logger logger = new Logger();
    string? exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    string? fastrePath;
    
    static int Main(string[] args)
    {
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
        Process installed = Cmd.cmd("fastre -v");
        if (installed.ExitCode != 0)
        {
            logger.Info("fastre is not installed");
            logger.Info("Install fastre by running 'fastre install fastre'");
            return Task.CompletedTask;
        }
        else
        {
            String fastreP = Cmd.cmdString("which fastre");

            if (fastreP.StartsWith("ERROR:"))
            {
                logger.Error("An error while trying to locate fastre");
                logger.Error(fastreP);
                return Task.CompletedTask;
            }
            else
            {
                fastrePath = fastreP.Replace("/", "\\").Substring(1).Insert(1, ":").Trim();
                fastrePath = string.Join("\\", fastrePath.Split('\\').Take(fastrePath.Split('\\').Length - 1)) + "\\node_modules\\fastre";
            }
        }

        string[] args = arg.Split(' ');

        switch (args[0])
        {
            case "add":
                if (await AddFastrePackageAsync(args)) return Task.CompletedTask;
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

    static async Task<string> FetchLatestTag(string author, string package, Logger logger)
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
