﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ShellProgressBar;
using Newtonsoft.Json.Linq;
using System.IO;
using static System.Net.WebRequestMethods;


namespace FADE
{
    internal class Fsde
    {
        static void Main(string[] args)
        {
            Logger logger = new Logger();

            if (args.Length == 0)
            {
                logger.Error("No arguments provided");
                logger.Info("To install FADE, run 'fade install'");
                return;
            }

            if (args[0] == "help")
            {
                Console.WriteLine("Usage: fade <command> [options]");
                Console.WriteLine("Commands:");
                Console.WriteLine("help - Display this help message");
                Console.WriteLine("install - Installs FADE Package");
            }

            if (args[0] == "install")
            {
                List<string> available = new List<string> { "fastre", "autobase" };
                List<string> packages = new List<string>();
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        //check if package is available in the list
                        if (!available.Contains(args[i].ToLower()))
                        {
                            logger.Error("Unknown package: " + args[i]);
                            return;
                        }
                        packages.Add(args[i].ToLower());
                    }
                }
                else
                {
                    packages.Add("fastre");
                    packages.Add("autobase");
                }
                logger.Info("FADE will install " + packages.Count + " packages");

                foreach (string package in packages)
                {
                    InstallAsync(package, logger).Wait();
                }
            }

            if (args[0] == "update")
            {
                string fadeLatest = FetchLatestTag("fade", logger).Result;

                var lv = new Version(fadeLatest);
                var cv = new Version("0.2.0");

                if (cv.CompareTo(lv) >= 0)
                {
                    logger.Info("FADE is up to date (v" + fadeLatest + ")");
                }
                else
                {
                    //download latest release of fade
                    logger.Info("Downloading FADE v" + fadeLatest);
                    string fade = $"https://github.com/mvishok/FADE/releases/download/{fadeLatest}/FADE.exe";
                    logger.Info(fade);
                    bool downloadedFade = Downloader.DownloadFileWithProgress(fade, "FADEInstaller.exe").Result;

                    if (!downloadedFade)
                    {
                        logger.Error("Failed to download FADE v" + fadeLatest);
                        return;
                    }
                    else
                    {
                        logger.Success("FADE v" + fadeLatest + " downloaded successfully");
                    }

                    //launch exe
                    logger.Info("Launching FADE v" + fadeLatest);
                    Process.Start("FADEInstaller.exe");
                    logger.Success("FADE Updater launched successfully. Please continue on the launched instance.");
                    return;
                }
            }
        }

        static async Task InstallAsync(string package, Logger logger)
        {
            string? currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (currentDirectory == null)
            {
                logger.Error("Failed to get installantion directory");
                return;
            }

            if (package == "fastre")
            {

                //check if node is installed
                Process nodeExists = Cmd.cmd("node -v");

                if (nodeExists.ExitCode != 0)
                {
                    logger.Info("Node not installed");
                    logger.Info("Downloading Node.js v22.4.1");
                    string node = $"https://nodejs.org/dist/latest/node-v22.4.1-{(Environment.Is64BitOperatingSystem ? "x64" : "x86")}.msi";
                    bool downloaded = await Downloader.DownloadFileWithProgress(node, currentDirectory + "\\node.msi");

                    if (!downloaded)
                    {
                        logger.Error("Failed to download Node.js v22.4.1");
                        return;
                    }
                    else
                    {
                        logger.Success("Node.js v22.4.1 downloaded successfully");
                    }

                    logger.Info("Installing Node.js v22.4.1");
                    Process process = Cmd.admin($"msiexec /qn /i \"{currentDirectory}\\node.msi\"", logger);
                    if (process.ExitCode != 0)
                    {
                        logger.Error("Failed to install Node.js v22.4.1");
                        return;
                    }
                    else
                    {
                        logger.Success("Node.js v22.4.1 installed successfully");
                        System.IO.File.Delete(currentDirectory + "\\node.msi");
                    }
                }

                //download latest version of fastre
                logger.Info("Fetching latest tag of Fastre");
                string fastreLatest = await FetchLatestTag("fastre", logger);
                var lv = new Version(fastreLatest);

                //check if fastre is already installed in ./fastre directory
                logger.Info("Checking existing Fastre installation");
                if (Directory.Exists(currentDirectory + "\\fastre"))
                {
                    //load package.json if exists
                    if (System.IO.File.Exists(currentDirectory + "\\fastre\\package.json"))
                    {
                        var packageJson = System.IO.File.ReadAllText(currentDirectory + "\\fastre\\package.json");
                        var jsonObj = JObject.Parse(packageJson);
                        string? version = jsonObj["version"]?.ToString();
                        if (version == null)
                        {
                            logger.Error("Failed to read Fastre version from package.json");
                            return;
                        }
                        var cv = new Version(version);
                        if (cv.CompareTo(lv) >= 0)
                        {
                            logger.Info("Fastre " + version + " is already installed");
                            return;
                        }
                        else
                        {
                            logger.Info("Fastre " + version + " is outdated");
                            logger.Warning("Fastre " + version + " will be updated to " + fastreLatest);
                            logger.Warning("This operation will overwrite the existing installation.");
                            logger.Warning("Directory to be overwritten: " + currentDirectory + "\\fastre");
                            logger.Warning("Do you want to continue? (y/n)");
                            string? response = Console.ReadLine();
                            if (response?.ToLower() != "y")
                            {
                                logger.Info("Installation aborted");
                                return;
                            }

                            //delete existing fastre directory
                            logger.Info("Deleting existing Fastre installation");
                            Directory.Delete(currentDirectory + "\\fastre", true);
                        }
                    }

                }

                //download
                logger.Info("Downloading Fastre " + fastreLatest);
                string fastre = $"https://github.com/mvishok/fastre/archive/refs/tags/{fastreLatest}.zip";
                bool downloadedFastre = await Downloader.DownloadFileWithProgress(fastre, currentDirectory + "\\fastre.zip");

                if (!downloadedFastre)
                {
                    logger.Error("Failed to download Fastre " + fastreLatest);
                    return;
                }
                else
                {
                    logger.Success("Fastre " + fastreLatest + " downloaded successfully");
                }

                //extract to ./ (current directory)
                logger.Info("Extracting Fastre " + fastreLatest);
                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(currentDirectory + "\\fastre.zip", currentDirectory);
                    Directory.Move(currentDirectory + "\\fastre-" + fastreLatest, currentDirectory + "\\fastre");
                    logger.Success("Fastre " + fastreLatest + " extracted successfully");
                    System.IO.File.Delete(currentDirectory + "\\fastre.zip");
                }
                catch (Exception ex)
                {
                    logger.Error("Failed to extract Fastre " + fastreLatest + ": " + ex.Message);
                    return;
                }

                //install dependencies after cd to fastre
                logger.Info("Installing Fastre dependencies");
                Process processFastre = Cmd.cmd($"cd \"{currentDirectory}\\fastre\" && \"C:\\Program Files\\nodejs\\npm\" install");
                if (processFastre.ExitCode != 0)
                {
                    logger.Error("Failed to install Fastre dependencies");
                    return;
                }
                else
                {
                    logger.Success("Fastre dependencies installed successfully");
                }

                logger.Info("Creating Fastre runner");
                string batContent = "@echo off" + Environment.NewLine + "set ARGS=%*" + Environment.NewLine + "runner.exe \"fastre\" %ARGS%";
                try
                {
                    System.IO.File.WriteAllText(currentDirectory + "/fastre.bat", batContent);
                    logger.Success("Fastre runner created successfully");
                }
                catch (Exception ex)
                {
                    logger.Error("Failed to create Fastre runner: " + ex.Message);
                    return;
                }


                logger.Success("Fastre " + fastreLatest + " installed successfully");
            }
            else if (package == "autobase")
            {

                //get latest version of autobase
                logger.Info("Fetching latest tag of Autobase");
                string autobaseLatest = await FetchLatestTag("autobase", logger);
                var lv = new Version(autobaseLatest);

                //Check if autobase is already downloaded to currentDirectory/autobase/
                if (Directory.Exists(currentDirectory + "\\autobase"))
                {
                    if (System.IO.File.Exists(currentDirectory + "\\autobase\\autobase.exe") && System.IO.File.Exists(currentDirectory + "\\autobase\\ver.txt"))
                    {
                        //check version of existing installation at /autobase/ver.txt
                        string version = System.IO.File.ReadAllText(currentDirectory + "\\autobase\\ver.txt");
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
                            logger.Warning("Directory to be overwritten: " + currentDirectory + "\\autobase");
                            logger.Warning("Do you want to continue? (y/n)");
                            string? response = Console.ReadLine();
                            if (response?.ToLower() != "y")
                            {
                                logger.Info("Installation aborted");
                                return;
                            }

                            //delete existing autobase directory
                            logger.Info("Deleting existing Autobase installation");
                            Directory.Delete(currentDirectory + "\\autobase", true);
                        }
                    }
                }

                Directory.CreateDirectory(currentDirectory + "\\autobase");

                //download
                logger.Info("Downloading Autobase " + autobaseLatest);
                string autobase = $"https://github.com/mvishok/autobase/releases/download/{autobaseLatest}/autobase.exe";
                bool downloadAutobase = await Downloader.DownloadFileWithProgress(autobase, currentDirectory + "\\autobase\\autobase.exe");

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
                System.IO.File.WriteAllText(currentDirectory + "\\autobase\\ver.txt", autobaseLatest);

                //create runner
                logger.Info("Creating Autobase runner");
                string batContent = "@echo off" + Environment.NewLine + "set ARGS=%*" + Environment.NewLine + "runner.exe \"autobase\" %ARGS%";
                try
                {
                    System.IO.File.WriteAllText(currentDirectory + "/autobase.bat", batContent);
                    logger.Success("Autobase runner created successfully");
                }
                catch (Exception ex)
                {
                    logger.Error("Failed to create Autobase runner: " + ex.Message);
                    return;
                }

                logger.Success("Autobase " + autobaseLatest + " installed successfully");
            }

            return;
        }

        static async Task<string> FetchLatestTag(string package, Logger logger)
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
}