using FADE;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Runner
{
    internal class FastreArgs
    {
        public async Task<string[]> Proc(String[] args, String dir, Logger logger)
        {
            // config
            if (args.Length > 2 && args[1] == "--config")
            {
                //check if args[2] is either absolute or relative path. if it is relative path, convert it to absolute path with called directory
                //get current shell working directory
                string? cwd = Directory.GetCurrentDirectory();
                string? configPath = args[2];
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.GetFullPath(Path.Combine(cwd, configPath));
                }
                else
                {
                    configPath = Path.GetFullPath(configPath);
                }

                args[2] = configPath;
                args[0] = "run";
            }

            // create
            if (args.Length > 2 && args[1] == "create")
            {
                // create package
                if (args[2] == "package")
                {
                    // if dir /package does not exist, create it
                    if (!Directory.Exists(dir + "\\packages"))
                    {
                        Directory.CreateDirectory(dir + "\\packages");
                    }

                    // Ask for package name, author, version, entry point
                    Console.Write("Package name (Tag name): ");
                    string packageName = Console.ReadLine() ?? "packages";

                    Console.Write("Author: ");
                    string author = Console.ReadLine() ?? "author";

                    Console.Write("Version: ");
                    string version = Console.ReadLine() ?? "1.0.0";

                    Console.Write("Entry point: ");
                    string entryPoint = Console.ReadLine() ?? "index.js";

                    // Create folder with author name if not exists
                    if (!Directory.Exists(dir + "\\packages\\" + author))
                    {
                        Directory.CreateDirectory(dir + "\\packages\\" + author);
                    }

                    // if author\packageName exists, ask for overwrite
                    if (Directory.Exists(dir + "\\packages\\" + author + "\\" + packageName))
                    {
                        Console.Write("Package already exists. Overwrite? (y/n): ");
                        string overwrite = Console.ReadLine() ?? "n";

                        if (overwrite == "n")
                        {
                            args[0] = "exit";
                            return args;
                        }

                        // delete the folder
                        try
                        {
                            Directory.Delete(dir + "\\packages\\" + author + "\\" + packageName, true);
                        }
                        catch (Exception e)
                        {
                            logger.Error("Failed to delete the package folder: " + e.Message);
                            return args;
                        }
                    }

                    // Create package folder
                    logger.Info("Creating package " + packageName + " by " + author);
                    Directory.CreateDirectory(dir + "\\packages\\" + author + "\\" + packageName);

                    // Create entry point file
                    logger.Info("Creating entry point " + entryPoint);
                    File.WriteAllText(dir + "\\packages\\" + author + "\\" + packageName + "\\" + entryPoint, "");

                    // Now, append to packages\packages.json
                    logger.Info("Reading packages.json");
                    string packagesJson = File.ReadAllText(dir + "\\packages\\packages.json");
                    dynamic json = JsonConvert.DeserializeObject(packagesJson) ?? new JObject();

                    // Check if package already exists and warn user
                    if (json[packageName + "@" + version] != null)
                    {
                        logger.Warning("Package with the same name and version already exists. The packages.json file will be overwritten. Proceed? (y/n): ");
                        string overwrite = Console.ReadLine() ?? "n";

                        if (overwrite == "n")
                        {
                            args[0] = "exit";
                            return args;
                        }
                    }

                    // Add package to json: { "packageName@version": "entryPoint" }
                    logger.Info("Adding package to packages.json");
                    json[packageName + "@" + version] = author + "\\" + packageName + "\\" + entryPoint;
                    string updatedPackagesJson = JsonConvert.SerializeObject(json, Formatting.Indented);
                    File.WriteAllText(dir + "\\packages\\packages.json", updatedPackagesJson);

                    // Finally create package.json with all the details
                    logger.Info("Creating pkg.json");
                    dynamic packageJson = new JObject();
                    packageJson["name"] = packageName;
                    packageJson["author"] = author;
                    packageJson["version"] = version;
                    packageJson["entry"] = entryPoint;
                    string packageJsonString = JsonConvert.SerializeObject(packageJson, Formatting.Indented);
                    File.WriteAllText(dir + "\\packages\\" + author + "\\" + packageName + "\\pkg.json", packageJsonString);

                    // Start file explorer 
                    Process.Start("explorer.exe", dir + "\\packages\\" + author + "\\" + packageName + "\\");

                    logger.Success("Package " + packageName + " created successfully.");
                }
                args[0] = "exit";
            }

            // add package from github
            if (args.Length > 2 && args[1] == "add")
            {
                // args[2] is username/repo. check if it is valid
                string[] repo = args[2].Split('/');
                if (repo.Length != 2)
                {
                    logger.Error("Invalid repo name. Use 'username/repo'");
                    args[0] = "exit";
                    return args;
                }

                // fetch latest tag and check if it is valid
                logger.Info("Fetching the latest tag for " + repo[0] + "/" + repo[1]);
                string latestTag = await FetchLatestTag(repo[0], repo[1], logger);
                if (latestTag == "")
                {
                    logger.Error("Failed to fetch the latest tag. Exiting.");
                    args[0] = "exit";
                    return args;
                }

                // check if packages folder exists
                if (!Directory.Exists(dir + "\\packages"))
                {
                    Directory.CreateDirectory(dir + "\\packages");
                }

                // check if packages.json exists
                if (!File.Exists(dir + "\\packages\\packages.json"))
                {
                    File.WriteAllText(dir + "\\packages\\packages.json", "{}");
                }

                // check if package.json already contains the package
                logger.Info("Reading packages.json");
                string packagesJson = File.ReadAllText(dir + "\\packages\\packages.json");
                dynamic json = JsonConvert.DeserializeObject(packagesJson) ?? new JObject();

                // check if username folder exists
                if (!Directory.Exists(dir + "\\packages\\" + repo[0]))
                {
                    Directory.CreateDirectory(dir + "\\packages\\" + repo[0]);
                }

                // check if repo folder exists
                logger.Info("Checking if package already exists");
                if (Directory.Exists(dir + "\\packages\\" + repo[0] + "\\" + repo[1]))
                {
                    logger.Warning("This package already exists in disk. Overwrite? (y/n): ");
                    string overwrite = Console.ReadLine() ?? "n";

                    if (overwrite == "n")
                    {
                        args[0] = "exit";
                        return args;
                    }

                    // delete the folder
                    try
                    {
                        Directory.Delete(dir + "\\packages\\" + repo[0] + "\\" + repo[1], true);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Failed to delete the package folder: " + e.Message);
                        return args;
                    }
                }

                // download the package from github
                string github = $"https://github.com/{repo[0]}/{repo[1]}/archive/refs/tags/{latestTag}.zip";
                logger.Info("Downloading package from " + github);
                bool downloadPakcage = await Downloader.DownloadFileWithProgress(github, dir + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

                if (!downloadPakcage)
                {
                    logger.Error("Failed to download the package. Exiting.");
                    args[0] = "exit";
                    return args;
                }

                // extract the package
                logger.Info("Extracting package");
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip", dir + "\\packages\\" + repo[0] + "\\");
                }
                catch (Exception e)
                {
                    logger.Error("Failed to extract the package: " + e.Message);
                    args[0] = "exit";
                    return args;
                }

                // Rename package-tag to package
                logger.Info("Renaming package folder");
                Directory.Move(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + "-" + latestTag, dir + "\\packages\\" + repo[0] + "\\" + repo[1]);

                // Remove the zip file
                logger.Info("Cleaning up");
                File.Delete(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

                // fetch entrypoint from pkg.json
                logger.Info("Reading pkg.json");
                string pkgJson = File.ReadAllText(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + "\\pkg.json");
                dynamic pkg = JsonConvert.DeserializeObject(pkgJson) ?? new JObject();

                // check if entry point exists
                logger.Info("Checking if entry point exists");
                if (pkg["entry"] == null)
                {
                    logger.Error("Entry point not found in pkg.json. Exiting.");
                    args[0] = "exit";
                    return args;
                }

                // Add package to packages.json
                logger.Info("Adding package to packages.json");
                string pname = repo[1] + "@" + latestTag;
                json[pname] = repo[0] + "\\" + repo[1] + "\\" + pkg["entry"];
                string updatedPackagesJson = JsonConvert.SerializeObject(json, Formatting.Indented);
                File.WriteAllText(dir + "\\packages\\packages.json", updatedPackagesJson);

                // success
                logger.Success("Package " + repo[1].Split('@')[0] + " added successfully.");

                args[0] = "exit";
            }

            // update package from github
            if (args.Length > 2 && args[1] == "update")
            {
                // args[2] is username/repo. check if it is valid
                string[] repo = args[2].Split('/');
                if (repo.Length != 2)
                {
                    logger.Error("Invalid repo name. Use 'username/repo'");
                    args[0] = "exit";
                    return args;
                }

                // check if packages folder exists
                if (!Directory.Exists(dir + "\\packages"))
                {
                    Directory.CreateDirectory(dir + "\\packages");
                }

                // check if packages.json exists
                if (!File.Exists(dir + "\\packages\\packages.json"))
                {
                    File.WriteAllText(dir + "\\packages\\packages.json", "{}");
                }

                //read packages.json
                logger.Info("Reading packages.json");
                string packagesJson = File.ReadAllText(dir + "\\packages\\packages.json");
                dynamic json = JsonConvert.DeserializeObject(packagesJson) ?? new JObject();

                //read pkg.json of package (dir / username / repo / pkg.json)
                var version = new Version();
                String ver;
                if (!Directory.Exists(dir + "\\packages\\" + repo[0] + "\\" + repo[1]))
                {
                    logger.Error("Package does not exist. Exiting.");
                    args[0] = "exit";
                    return args;
                } else
                {
                    string pkgJson = File.ReadAllText(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + "\\pkg.json");
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
                    args[0] = "exit";
                    return args;
                }

                var latestVersion = new Version(latestTag);

                if (latestVersion <= version)
                {
                    logger.Success("Package is already up to date.");
                    args[0] = "exit";
                    return args;
                }

                // warn user about overwriting
                logger.Warning("Package will be updated to version " + latestTag + ". Proceed? (y/n): ");
                string overwrite = Console.ReadLine() ?? "n";

                if (overwrite == "n")
                {
                    args[0] = "exit";
                    return args;
                }
                // delete the package folder
                try
                {
                    Directory.Delete(dir + "\\packages\\" + repo[0] + "\\" + repo[1], true);
                }
                catch (Exception e)
                {
                    logger.Error("Failed to delete the package folder: " + e.Message);
                    return args;
                }

                // download the package from github
                string github = $"https://github.com/{repo[0]}/{repo[1]}/archive/refs/tags/{latestTag}.zip";
                logger.Info("Downloading package from " + github);
                bool downloadPakcage = await Downloader.DownloadFileWithProgress(github, dir + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

                if (!downloadPakcage)
                {
                    logger.Error("Failed to download the package. Exiting.");
                    args[0] = "exit";
                    return args;
                }

                // extract the package
                logger.Info("Extracting package");
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip", dir + "\\packages\\" + repo[0] + "\\");
                }
                catch (Exception e)
                {
                    logger.Error("Failed to extract the package: " + e.Message);
                    args[0] = "exit";
                    return args;
                }

                // Rename package-tag to package
                logger.Info("Renaming package folder");
                Directory.Move(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + "-" + latestTag, dir + "\\packages\\" + repo[0] + "\\" + repo[1]);

                // Remove the zip file
                logger.Info("Cleaning up");
                File.Delete(dir + "\\packages\\" + repo[0] + "\\" + repo[1] + ".zip");

                // update the version in packages.json
                logger.Info("Updating version in packages.json");
                string oldname = repo[1] + "@" + ver;
                string newname = repo[1] + "@" + latestTag;
                string p = json[oldname];
                json.Remove(oldname);
                json[newname] = p;
                
                string updatedPackagesJson = JsonConvert.SerializeObject(json, Formatting.Indented);
                File.WriteAllText(dir + "\\packages\\packages.json", updatedPackagesJson);

                // success
                logger.Success("Package " + repo[1].Split('@')[0] + " updated successfully.");
                args[0] = "exit";
            }
            return args;
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
}
