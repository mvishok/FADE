using FSDE;
using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace RUNNER
{
    internal class Runner
    {
        static async Task Main(string[] args)
        {
            Logger logger = new Logger();
            string? currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (currentDirectory == null)
            {
                logger.Error("Failed to get the installation directory.");
                return;
            }

            if (args.Length == 0)
            {
                logger.Error("No arguments provided.");
                return;
            }

            if (args[0] == "fastre")
            {
                await RunFastre(currentDirectory, logger, args);
            }
            else if (args[0] == "syncengin")
            {
                logger.Error("Syncengin is not available.");
            }
            else
            {
                logger.Error("Unknown package: " + args[0]);
            }
        }

        static async Task RunFastre(string currentDirectory, Logger logger, string[] args)
        {
            if (Directory.Exists(currentDirectory + "\\fastre"))
            {
                string fastreLatest = FetchLatestTagSync("fastre", logger);
                var lv = new Version(fastreLatest);

                // Check if the installed version is less than the latest version
                var packageJson = File.ReadAllText(currentDirectory + "/fastre/package.json");
                var jsonObj = JObject.Parse(packageJson);
                string? version = jsonObj["version"]?.ToString();
                if (version == null)
                {
                    logger.Error("Failed to read Fastre version from package.json");
                }
                else
                {
                    var cv = new Version(version);
                    if (cv.CompareTo(lv) < 0)
                    {
                        logger.Warning("New version of Fastre available: " + cv);
                        logger.Warning("Install the new version using 'fsde fastre install'");
                        return;
                    }
                }

                // Get remaining arguments as string
                string arguments = string.Join(" ", args.Skip(1));
                int exitCode = await Cmd.CmdAsync($"cd \"{currentDirectory}/fastre\" && node fastre.js {arguments}");

                if (exitCode != 0)
                {
                    Console.Write("\n");
                    logger.Error("Fastre exited with code " + exitCode);
                }
            }
            else
            {
                logger.Error("Fastre is not installed. Install it using 'fsde fastre install'");
            }
        }

        static string FetchLatestTagSync(string package, Logger logger)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FSDE/1.0");

            var response = httpClient.GetAsync($"https://api.github.com/repos/mvishok/{package}/releases/latest").Result;
            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().Result;

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
    }
}