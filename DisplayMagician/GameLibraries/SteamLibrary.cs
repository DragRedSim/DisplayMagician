﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ValveKeyValue;
using DisplayMagician.GameLibraries.SteamAppInfoParser;
using Microsoft.Win32;
using System.IO;
using System.Security;
using System.Diagnostics;

namespace DisplayMagician.GameLibraries
{
    public static class SteamLibrary
    {
        #region Class Variables
        // Common items to the class
        private static List<Game> _allSteamGames = new List<Game>();
        private static string steamAppIdRegex = @"/^[0-9A-F]{1,10}$";
        private static string _steamExe;
        private static string _steamPath;
        private static string _steamConfigVdfFile;
        private static string _registrySteamKey = @"SOFTWARE\\Valve\\Steam";
        private static string _registryAppsKey = $@"{_registrySteamKey}\\Apps";
        private static bool _isSteamInstalled = false;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        // Other constants that are useful
        #endregion

        private struct SteamAppInfo
        {
            public int GameID;
            public string GameName;
            public List<string> GameExes;
            public string GameInstallDir;
            public string GameSteamIconPath;
        }

        #region Class Constructors
        static SteamLibrary()
        {
            try
            {
                // Find the SteamExe location, and the SteamPath for later
                using (var key = Registry.CurrentUser.OpenSubKey(SteamLibrary.SteamRegistryKey, RegistryKeyPermissionCheck.ReadSubTree))
                {
                    _steamExe = (string)key?.GetValue(@"SteamExe", string.Empty) ?? string.Empty;
                    _steamExe = _steamExe.Replace('/', '\\');
                    _steamPath = (string)key?.GetValue(@"SteamPath", string.Empty) ?? string.Empty;
                    _steamPath = _steamPath.Replace('/', '\\');
                }
                if (File.Exists(_steamExe))
                    _isSteamInstalled = true;
            }
            catch (SecurityException ex)
            {
                logger.Warn("The user does not have the permissions required to read the Steam registry key.");
            }
            catch (ObjectDisposedException ex)
            {
                logger.Warn("The Microsoft.Win32.RegistryKey is closed when trying to access theSteam registry key (closed keys cannot be accessed).");
            }
            catch (IOException ex)
            {
                logger.Warn("The Steam registry key has been marked for deletion so we cannot access the value during the SteamLibrary check.");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Warn("The user does not have the necessary registry rights to check whether Steam is installed.");
            }
        }
        #endregion

        #region Class Properties
        public static List<Game> AllInstalledGames
        {
            get
            {
                // Load the Steam Games from Steam Client if needed
                if (_allSteamGames.Count == 0)
                    LoadInstalledGames();
                return _allSteamGames;
            }
        }


        public static int InstalledSteamGameCount
        {
            get
            {
                return _allSteamGames.Count;
            }
        }

        public static string SteamRegistryKey
        {
            get
            {
                return _registrySteamKey;
            }
        }

        public static string SteamAppsRegistryKey
        {
            get
            {
                return _registryAppsKey;
            }
        }

        public static string SteamExe
        {
            get
            {
                return _steamExe;
            }
        }

        public static string SteamPath
        {
            get
            {
                return _steamPath;
            }
        }

        public static bool IsSteamInstalled
        {
            get
            {
                return _isSteamInstalled;
            }

        }


        #endregion

        #region Class Methods
        public static bool AddSteamGame(SteamGame steamGame)
        {
            if (!(steamGame is SteamGame))
                return false;

            // Doublecheck if it already exists
            // Because then we just update the one that already exists
            if (ContainsSteamGame(steamGame))
            {
                // We update the existing Shortcut with the data over
                SteamGame steamGameToUpdate = GetSteamGame(steamGame.Id.ToString());
                steamGame.CopyInto(steamGameToUpdate);
            }
            else
            {
                // Add the steamGame to the list of steamGames
                _allSteamGames.Add(steamGame);
            }

            //Doublecheck it's been added
            if (ContainsSteamGame(steamGame))
            {
                return true;
            }
            else
                return false;

        }

        public static bool RemoveSteamGame(SteamGame steamGame)
        {
            if (!(steamGame is SteamGame))
                return false;

            // Remove the steamGame from the list.
            int numRemoved = _allSteamGames.RemoveAll(item => item.Id.Equals(steamGame.Id));

            if (numRemoved == 1)
            {
                return true;
            }
            else if (numRemoved == 0)
                return false;
            else
                throw new SteamLibraryException();
        }

        public static bool RemoveSteamGame(int steamGameId)
        {
            if (steamGameId<=0)
                return false;

            // Remove the steamGame from the list.
            int numRemoved = _allSteamGames.RemoveAll(item => item.Id.Equals(steamGameId));

            if (numRemoved == 1)
            {
                return true;
            }
            else if (numRemoved == 0)
                return false;
            else
                throw new SteamLibraryException();
        }


        public static bool RemoveSteamGame(string steamGameNameOrUuid)
        {
            if (String.IsNullOrWhiteSpace(steamGameNameOrUuid))
                return false;

            int numRemoved;
            Match match = Regex.Match(steamGameNameOrUuid, steamAppIdRegex, RegexOptions.IgnoreCase);
            if (match.Success)
                numRemoved = _allSteamGames.RemoveAll(item => steamGameNameOrUuid.Equals(Convert.ToUInt32(item.Id)));
            else
                numRemoved = _allSteamGames.RemoveAll(item => steamGameNameOrUuid.Equals(item.Name));

            if (numRemoved == 1)
                return true;
            else if (numRemoved == 0)
                return false;
            else
                throw new SteamLibraryException();

        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static bool ContainsSteamGame(SteamGame steamGame)
        {
            if (!(steamGame is SteamGame))
                return false;

            foreach (SteamGame testSteamGame in _allSteamGames)
            {
                if (testSteamGame.Id.Equals(steamGame.Id))
                    return true;
            }

            return false;
        }

        public static bool ContainsSteamGame(string steamGameNameOrUuid)
        {
            if (String.IsNullOrWhiteSpace(steamGameNameOrUuid))
                return false;


            Match match = Regex.Match(steamGameNameOrUuid, steamAppIdRegex, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                foreach (SteamGame testSteamGame in _allSteamGames)
                {
                    if (steamGameNameOrUuid.Equals(Convert.ToInt32(testSteamGame.Id)))
                        return true;
                }

            }
            else
            {
                foreach (SteamGame testSteamGame in _allSteamGames)
                {
                    if (steamGameNameOrUuid.Equals(testSteamGame.Name))
                        return true;
                }

            }

            return false;

        }

        public static bool ContainsSteamGame(int steamGameId)
        {
            foreach (SteamGame testSteamGame in _allSteamGames)
            {
                if (steamGameId == testSteamGame.Id)
                    return true;
            }

           
            return false;

        }


        public static SteamGame GetSteamGame(string steamGameNameOrUuid)
        {
            if (String.IsNullOrWhiteSpace(steamGameNameOrUuid))
                return null;

            Match match = Regex.Match(steamGameNameOrUuid, steamAppIdRegex, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                foreach (SteamGame testSteamGame in _allSteamGames)
                {
                    if (steamGameNameOrUuid.Equals(Convert.ToInt32(testSteamGame.Id)))
                        return testSteamGame;
                }

            }
            else
            {
                foreach (SteamGame testSteamGame in _allSteamGames)
                {
                    if (steamGameNameOrUuid.Equals(testSteamGame.Name))
                        return testSteamGame;
                }

            }

            return null;

        }

        public static SteamGame GetSteamGame(int steamGameId)
        {
            foreach (SteamGame testSteamGame in _allSteamGames)
            {
                if (steamGameId == testSteamGame.Id)
                    return testSteamGame;
            }

            return null;

        }

        public static bool LoadInstalledGames()
        {
            try
            {

                // Find the SteamExe location, and the SteamPath for later
                /*using (var key = Registry.CurrentUser.OpenSubKey(_registrySteamKey, RegistryKeyPermissionCheck.ReadSubTree))
                {
                    _steamExe = (string)key?.GetValue(@"SteamExe", string.Empty) ?? string.Empty;
                    _steamExe = _steamExe.Replace('/', '\\');
                    _steamPath = (string)key?.GetValue(@"SteamPath", string.Empty) ?? string.Empty;
                    _steamPath = _steamPath.Replace('/', '\\');
                }*/

                if (!_isSteamInstalled)
                {
                    // Steam isn't installed, so we return an empty list.
                    return false;
                }

                //Icon _steamIcon = Icon.ExtractAssociatedIcon(_steamExe);
                //IconExtractor steamIconExtractor = new IconExtractor(_steamExe);
                //Icon _steamIcon = steamIconExtractor.GetIcon(0);
                //MultiIcon _steamIcon = new MultiIcon();
                //_steamIcon.Load(_steamExe);

                List<int> steamAppIdsInstalled = new List<int>();
                // Now look for what games app id's are actually installed on this computer
                using (RegistryKey steamAppsKey = Registry.CurrentUser.OpenSubKey(_registryAppsKey, RegistryKeyPermissionCheck.ReadSubTree))
                {
                    if (steamAppsKey != null)
                    {
                        // Loop through the subKeys as they are the Steam Game IDs
                        foreach (string steamGameKeyName in steamAppsKey.GetSubKeyNames())
                        {
                            int steamAppId = 0;
                            if (int.TryParse(steamGameKeyName, out steamAppId))
                            {
                                string steamGameKeyFullName = $"{_registryAppsKey}\\{steamGameKeyName}";
                                using (RegistryKey steamGameKey = Registry.CurrentUser.OpenSubKey(steamGameKeyFullName, RegistryKeyPermissionCheck.ReadSubTree))
                                {
                                    // If the Installed Value is set to 1, then the game is installed
                                    // We want to keep track of that for later
                                    if ((int)steamGameKey.GetValue(@"Installed", 0) == 1)
                                    {
                                        // Add this Steam App ID to the list we're keeping for later
                                        steamAppIdsInstalled.Add(steamAppId);
                                    }

                                }

                            }
                        }
                    }
                }

                // Now we parse the steam appinfo.vdf to get access to things like:
                // - The game name
                // - THe game installation dir
                // - Sometimes the game icon
                // - Sometimes the game executable name (from which we can get the icon)
                Dictionary<int, SteamAppInfo> steamAppInfo = new Dictionary<int, SteamAppInfo>();

                string appInfoVdfFile = Path.Combine(_steamPath, "appcache", "appinfo.vdf");
                var newAppInfo = new AppInfo();
                newAppInfo.Read(appInfoVdfFile);

                Debug.WriteLine($"{newAppInfo.Apps.Count} apps");

                // Chec through all the apps we've extracted
                foreach (var app in newAppInfo.Apps)
                {
                    // We only care about the appIDs we have listed as actual games
                    // (The AppIds include all other DLC and Steam specific stuff too)
                    int detectedAppID = Convert.ToInt32(app.AppID);
                    if (steamAppIdsInstalled.Contains(detectedAppID))
                    {

                        try
                        {

                            SteamAppInfo steamGameAppInfo = new SteamAppInfo();
                            steamGameAppInfo.GameID = detectedAppID;
                            steamGameAppInfo.GameExes = new List<string>();

                            foreach (KVObject data in app.Data)
                            {
                                //Debug.WriteLine($"App: {app.AppID} - Data.Name: {data.Name}");

                                if (data.Name == "common")
                                {
                                    foreach (KVObject common in data.Children)
                                    {

                                        //Debug.WriteLine($"App: {app.AppID} - Common {common.Name}: {common.Value}");

                                        if (common.Name == "name")
                                        {
                                            Debug.WriteLine($"App: {app.AppID} - Common {common.Name}: {common.Value}");
                                            steamGameAppInfo.GameName = common.Value.ToString();
                                        }
                                        else if (common.Name == "clienticon")
                                        {
                                            Debug.WriteLine($"App: {app.AppID} - Common {common.Name}: {common.Value}");
                                            steamGameAppInfo.GameSteamIconPath = Path.Combine(_steamPath, @"steam", @"games", String.Concat(common.Value, @".ico"));
                                        }
                                        else if (common.Name == "type")
                                        {
                                            Debug.WriteLine($"App: {app.AppID} - Common {common.Name}: {common.Value}");
                                        }
                                    }
                                }
                                else if (data.Name == "config")
                                {
                                    foreach (KVObject config in data.Children)
                                    {
                                        //Console.WriteLine($"App: {app.AppID} - Config {config.Name}: {config.Value}");

                                        if (config.Name == "installdir")
                                        {
                                            Debug.WriteLine($"App: {detectedAppID} - Config {config.Name}: {config.Value}");
                                            steamGameAppInfo.GameInstallDir = config.Value.ToString();
                                        }
                                        else if (config.Name == "launch")
                                        {
                                            foreach (KVObject launch in config.Children)
                                            {
                                                foreach (KVObject launch_num in launch.Children)
                                                {
                                                    if (launch_num.Name == "executable")
                                                    {
                                                        Debug.WriteLine($"App: {detectedAppID} - Config - Launch {launch.Name} - {launch_num.Name}: {launch_num.Value}");
                                                        steamGameAppInfo.GameExes.Add(launch_num.Value.ToString());
                                                    }

                                                }
                                            }
                                        }
                                    }
                                }

                            }
                            steamAppInfo.Add(detectedAppID, steamGameAppInfo);
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"SteamGame/GetAllInstalledGames exception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                            //we just want to ignore it if we try to add it twice....
                        }

                        Debug.WriteLine($"App: {detectedAppID} - Token: {app.Token}");
                    }
                }



                // Now we access the config.vdf that lives in the Steam Config file, as that lists all 
                // the SteamLibraries. We need to find out where they areso we can interrogate them
                _steamConfigVdfFile = Path.Combine(_steamPath, "config", "config.vdf");
                string steamConfigVdfText = File.ReadAllText(_steamConfigVdfFile, Encoding.UTF8);

                List<string> steamLibrariesPaths = new List<string>();
                // Now we have to parse the config.vdf looking for the location of the SteamLibraries
                // We look for lines similar to this: "BaseInstallFolder_1"		"E:\\SteamLibrary"
                // There may be multiple so we need to check the whole file
                Regex steamLibrariesRegex = new Regex(@"""BaseInstallFolder_\d+""\s+""(.*)""", RegexOptions.IgnoreCase);
                // Try to match all lines against the Regex.
                MatchCollection steamLibrariesMatches = steamLibrariesRegex.Matches(steamConfigVdfText);
                // If at least one of them matched!
                foreach (Match steamLibraryMatch in steamLibrariesMatches)
                {
                    if (steamLibraryMatch.Success)
                    { 
                        string steamLibraryPath = Regex.Unescape(steamLibraryMatch.Groups[1].Value);
                        Debug.WriteLine($"Found steam library: {steamLibraryPath}");
                        steamLibrariesPaths.Add(steamLibraryPath);
                    }
                }
                // Now we go off and find the details for the games in each Steam Library
                foreach (string steamLibraryPath in steamLibrariesPaths)
                {
                    // Work out the path to the appmanifests for this steamLibrary
                    string steamLibraryAppManifestPath = Path.Combine(steamLibraryPath, @"steamapps");
                    // Get the names of the App Manifests for the games installed in this SteamLibrary
                    string[] steamLibraryAppManifestFilenames = Directory.GetFiles(steamLibraryAppManifestPath, "appmanifest_*.acf");
                    // Go through each app and extract it's details
                    foreach (string steamLibraryAppManifestFilename in steamLibraryAppManifestFilenames)
                    {
                        // Read in the contents of the file
                        string steamLibraryAppManifestText = File.ReadAllText(steamLibraryAppManifestFilename);
                        // Grab the appid from the file
                        Regex appidRegex = new Regex(@"""appid""\s+""(\d+)""", RegexOptions.IgnoreCase);
                        Match appidMatches = appidRegex.Match(steamLibraryAppManifestText);
                        if (appidMatches.Success)
                        {

                            int steamGameId = 0;
                            if (int.TryParse(appidMatches.Groups[1].Value, out steamGameId))
                            {
                                // Check if this game is one that was installed
                                if (steamAppInfo.ContainsKey(steamGameId))
                                {
                                    // This game is an installed game! so we start to populate it with data!
                                    string steamGameExe = "";

                                    string steamGameName = steamAppInfo[steamGameId].GameName;

                                    // Construct the full path to the game dir from the appInfo and libraryAppManifest data
                                    string steamGameInstallDir = Path.Combine(steamLibraryPath, @"steamapps", @"common", steamAppInfo[steamGameId].GameInstallDir);

                                    // And finally we try to populate the 'where', to see what gets run
                                    // And so we can extract the process name
                                    if (steamAppInfo[steamGameId].GameExes.Count > 0)
                                    {
                                        foreach (string gameExe in steamAppInfo[steamGameId].GameExes)
                                        {
                                            steamGameExe = Path.Combine(steamGameInstallDir, gameExe);
                                            // If the game executable exists, then we can proceed
                                            if (File.Exists(steamGameExe))
                                            {
                                                break;
                                            }
                                        }

                                    }

                                    // Next, we need to get the Icons we want to use, and make sure it's the latest one.
                                    string steamGameIconPath = "";
                                    // First of all, we attempt to use the Icon that Steam has cached, if it's available, as that will be updated to the latest
                                    if (File.Exists(steamAppInfo[steamGameId].GameSteamIconPath) && steamAppInfo[steamGameId].GameSteamIconPath.EndsWith(".ico"))
                                    {
                                        steamGameIconPath = steamAppInfo[steamGameId].GameSteamIconPath;
                                    }
                                    // If there isn't an icon for us to use, then we need to extract one from the Game Executables
                                    else if (!String.IsNullOrEmpty(steamGameExe))
                                    {
                                        steamGameIconPath = steamGameExe;
                                    }
                                    // The absolute worst case means we don't have an icon to use. SO we use the Steam one.
                                    else
                                    {
                                        // And we have to make do with a Steam Icon
                                        steamGameIconPath = _steamPath;
                                    }

                                    // And we add the Game to the list of games we have!
                                    _allSteamGames.Add(new SteamGame(steamGameId, steamGameName, steamGameExe, steamGameIconPath));

                                }
                            }
                        }
                    }
                }
            }
            catch (SecurityException ex)
            {
                Console.WriteLine($"SteamGame/GetAllInstalledGames securityexception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                if (ex.Source != null)
                    Console.WriteLine("SecurityException source: {0} - Message: {1}", ex.Source, ex.Message);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"SteamGame/GetAllInstalledGames unauthorizedaccessexception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                if (ex.Source != null)
                    Console.WriteLine("UnauthorizedAccessException  source: {0} - Message: {1}", ex.Source, ex.Message);
                throw;
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"SteamGame/GetAllInstalledGames objectdisposedexceptions: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                if (ex.Source != null)
                    Console.WriteLine("ObjectDisposedException  source: {0} - Message: {1}", ex.Source, ex.Message);
                throw;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"SteamGame/GetAllInstalledGames ioexception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                // Extract some information from this exception, and then
                // throw it to the parent method.
                if (ex.Source != null)
                    Console.WriteLine("IOException source: {0} - Message: {1}", ex.Source, ex.Message);
                throw;
            }

            return true;
        }

        #endregion

    }

    [global::System.Serializable]
    public class SteamLibraryException : Exception
    {
        public SteamLibraryException() { }
        public SteamLibraryException(string message) : base(message) { }
        public SteamLibraryException(string message, Exception inner) : base(message, inner) { }
        protected SteamLibraryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
