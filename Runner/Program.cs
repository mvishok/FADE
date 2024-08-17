
using FADE;
using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Runner;

namespace RUNNER
{
    internal class Runner
    {
        static async Task Main(string[] args)
        {
            Logger logger = new Logger();

            //when app is closed with ctrl+c
            Console.CancelKeyPress += (sender, e) =>
            {
                Cmd.KillChildProcesses(logger);
            };

            var (isAvailable, Version) = CheckForUpdates(logger);
            if (isAvailable == true)
            {
                logger.Warning("New version of FADE available: " + Version);
                logger.Warning("Update FADE using 'fade update'");
            }

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
            else if (args[0] == "autobase")
            {
                await RunAutobase(currentDirectory, logger, args);
            }
            else
            {
                logger.Error("Unknown package: " + args[0]);
            }
        }

        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void CurrentDomain_ProcessExit1(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
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
                        logger.Warning("Install the new version using 'fade fastre install'");
                    }
                }

                // Process args
                FastreArgs Args = new FastreArgs();
                args = await Args.Proc(args, currentDirectory + "\\fastre", logger);

                // Get remaining arguments as string and start Fastre
                if (args[0] == "exit")
                {
                    return;
                } 
                
                else if (args[0] == "run")
                {
                    string arguments = string.Join(" ", args.Skip(1));
                    int exitCode = await Cmd.FastreAsync($"cd \"{currentDirectory}/fastre\" && node fastre.js {arguments}");

                    if (exitCode != 0)
                    {
                        Console.Write("\n");
                        logger.Error("Fastre exited with code " + exitCode);
                    }

                } 
                
                else
                {
                    logger.Error("Failed to process Fastre arguments.");
                }
            }
            else
            {
                logger.Error("Fastre is not installed. Install it using 'fade fastre install'");
            }
        }

        static async Task RunAutobase(string currentDirectory, Logger logger, string[] args)
        {
            if (args.Length == 2)
            {
                //check if args[2] is either absolute or relative path. if it is relative path, convert it to absolute path with called directory
                //get current shell working directory
                string? cwd = Directory.GetCurrentDirectory();
                string? configPath = args[1];
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.GetFullPath(Path.Combine(cwd, configPath));
                } else
                {
                    configPath = Path.GetFullPath(configPath);
                }

                args[1] = configPath;
            } else
            {
                logger.Error("No config file provided.");
                return;
            }

            if (Directory.Exists(currentDirectory + "\\autobase") && File.Exists(currentDirectory + "\\autobase\\ver.txt"))
            {
                string fastreLatest = FetchLatestTagSync("autobase", logger);
                var lv = new Version(fastreLatest);

                // Check if the installed version is less than the latest version
                string version = File.ReadAllText(currentDirectory + "\\autobase\\ver.txt");
                if (version == null)
                {
                    logger.Error("Failed to read Autobase version from ver.txt");
                }
                else
                {
                    var cv = new Version(version);
                    if (cv.CompareTo(lv) < 0)
                    {
                        logger.Warning("New version of Autobase available: " + cv);
                        logger.Warning("Install the new version using 'fade autobase install'");
                    }
                }

                // Get remaining arguments as string
                string arguments = string.Join(" ", args.Skip(1));
                int exitCode = await Cmd.FastreAsync($"cd \"{currentDirectory}/autobase\" && autobase {arguments}");

                if (exitCode != 0)
                {
                    Console.Write("\n");
                    logger.Error("Autobase exited with code " + exitCode);
                }
            }
            else
            {
                logger.Error("Autobase is not installed. Install it using 'fade autobase install'");
            }
        }

        static string FetchLatestTagSync(string package, Logger logger)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FADE/1.0");

            try
            {
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
                    return "0.0.0";
                }
            }
            catch (Exception e)
            {
                logger.Error("Failed to fetch the latest tag: " + e.Message);
                return "0.0.0";
            }
        }

        static (bool, string) CheckForUpdates(Logger logger)
        {
            string fadeLatest = FetchLatestTagSync("fade", logger);
            var lv = new Version(fadeLatest);

            var cv = new Version("0.4.0"); // Current version of FADE

            if (cv.CompareTo(lv) < 0)
            {
                return (true, fadeLatest);
            }
            else
            {
                return (false, fadeLatest);
            }
        }
    }
}