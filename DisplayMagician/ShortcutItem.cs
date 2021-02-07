﻿using DisplayMagician.GameLibraries;
using DisplayMagician.Resources;
using DisplayMagicianShared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.IconLib;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using IWshRuntimeLibrary;
using AudioSwitcher.AudioApi.CoreAudio;

namespace DisplayMagician
{
    public enum ShortcutPermanence
    {
        Permanent,
        Temporary,
    }

    public enum ShortcutCategory
    {
        Application,
        Game,
        NoGame,
    }

    public struct StartProgram
    {
        public int Priority;
        public bool Enabled;
        public string Executable;
        public string Arguments;
        public bool ExecutableArgumentsRequired;
        public bool CloseOnFinish;
    }

    public struct Executable
    {
        public string DifferentExecutableToMonitor;
        public string ExecutableNameAndPath;
        public int ExecutableTimeout;
        public string ExecutableArguments;
        public bool ExecutableArgumentsRequired;
        public bool ProcessNameToMonitorUsesExecutable;
    }

    public struct GameStruct
    {
        public Game GameToPlay;
        public int StartTimeout;
        public string GameArguments;
        public bool GameArgumentsRequired;
    }


    public class ShortcutItem : IComparable
    {
        
        private string _profileUuid = "";
        private ProfileItem _profileToUse;
        private string _uuid = "";
        private string _name = "";
        private ShortcutCategory _category = ShortcutCategory.Game;
        private string _differentExecutableToMonitor;
        private string _executableNameAndPath = "";
        private string _executableArguments;
        private bool _executableArgumentsRequired = false;
        private bool _processNameToMonitorUsesExecutable = true;
        private int _gameAppId;
        private string _gameName;
        private SupportedGameLibrary _gameLibrary;
        private int _startTimeout = 20;
        private string _gameArguments;
        private bool _gameArgumentsRequired;
        private string _audioDevice;
        private bool _changeAudioDevice;
        private bool _setAudioVolume = false;
        private decimal _audioVolume = -1;
        private string _captureDevice;
        private bool _changeCaptureDevice;
        private bool _setCaptureVolume = false;
        private decimal _captureVolume = -1;
        private ShortcutPermanence _displayPermanence = ShortcutPermanence.Temporary;
        private ShortcutPermanence _audioPermanence = ShortcutPermanence.Temporary;
        private ShortcutPermanence _capturePermanence = ShortcutPermanence.Temporary;
        private bool _autoName = true;
        private bool _isPossible;
        private List<StartProgram> _startPrograms;
        private Bitmap _shortcutBitmap, _originalLargeBitmap, _originalSmallBitmap;
        [JsonIgnore]
#pragma warning disable CS3008 // Identifier is not CLS-compliant
        public string _originalIconPath;
        [JsonIgnore]
        public string _savedShortcutIconCacheFilename;
#pragma warning restore CS3008 // Identifier is not CLS-compliant
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ShortcutItem()
        {
            // Create a new UUID for the shortcut if one wasn't created already
            if (String.IsNullOrWhiteSpace(_uuid))
                _uuid = Guid.NewGuid().ToString("D");

            // If there are no GameLibraries then choose executable instead
            if (!(UplayLibrary.IsUplayInstalled && SteamLibrary.IsSteamInstalled))
            {
                _gameLibrary = SupportedGameLibrary.Unknown;
                _gameName = "";
                _gameArguments = "";
                _category = ShortcutCategory.Application;
            }
            // Autocreate a name for the shortcut if AutoName is on
            // (and if we have a profile to use)
            if (AutoName && _profileToUse is ProfileItem)
            {
                // If Autoname is on, and then lets autoname it!
                // That populates all the right things
                AutoSuggestShortcutName();
            }

        }


        public ShortcutItem(
            string name,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile,
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence,
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null,
            bool autoName = true,
            string uuid = ""
            ) : this()
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _category = ShortcutCategory.NoGame;
            _profileToUse = profile;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalLargeBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);
            _originalSmallBitmap = ToSmallBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

        }

        public ShortcutItem(
            string name, 
            string profileUuid, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false, 
            string audioDevice = "", 
            bool setAudioVolume = false, 
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false, 
            decimal captureVolume = -1, 
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            ) : this()
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileUuid = profileUuid;
            _category = ShortcutCategory.NoGame;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileToUse
            foreach (ProfileItem profileToTest in ProfileRepository.AllProfiles)
            {
                if (profileToTest.UUID.Equals(_profileUuid, StringComparison.OrdinalIgnoreCase))
                {
                    _profileToUse = profileToTest;
                    break;
                }

            }

            if (_profileToUse == null)
            {
                throw new Exception($"Trying to create a ShortcutItem and cannot find a loaded profile with UUID {uuid}.");
            }

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
        }



        public ShortcutItem(
            string name,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile, 
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            int gameAppId,
            string gameName,
            SupportedGameLibrary gameLibrary,
            int gameTimeout,
            string gameArguments,
            bool gameArgumentsRequired,
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence,
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null,
            bool autoName = true,
            string uuid = ""
            ) : this()
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name; 
            _profileToUse = profile;
            _category = ShortcutCategory.Game;
            _gameAppId = gameAppId;
            _gameName = gameName;
            _gameLibrary = gameLibrary;
            _startTimeout = gameTimeout;
            _gameArguments = gameArguments;
            _gameArgumentsRequired = gameArgumentsRequired;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalLargeBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);
            _originalSmallBitmap = ToSmallBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

        }

        public ShortcutItem(
            string name, 
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile, 
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            GameStruct game, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            ) : this()
        {
            // Create a new UUID for the shortcut if one wasn't created already
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileToUse = profile;
            _category = ShortcutCategory.Game;
            _gameAppId = game.GameToPlay.Id;
            _gameName = game.GameToPlay.Name;
            _gameLibrary = game.GameToPlay.GameLibrary;
            _startTimeout = game.StartTimeout;
            _gameArguments = game.GameArguments;
            _gameArgumentsRequired = game.GameArgumentsRequired;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
        }



        public ShortcutItem(
            string name, 
            string profileUuid, 
            GameStruct game, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            ) : this()
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileUuid = profileUuid;
            _category = ShortcutCategory.Game;
            _gameAppId = game.GameToPlay.Id;
            _gameName = game.GameToPlay.Name;
            _gameLibrary = game.GameToPlay.GameLibrary;
            _startTimeout = game.StartTimeout;
            _gameArguments = game.GameArguments;
            _gameArgumentsRequired = game.GameArgumentsRequired;
            _gameArgumentsRequired = false;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileToUse
            foreach (ProfileItem profileToTest in ProfileRepository.AllProfiles)
            {
                if (profileToTest.UUID.Equals(_profileUuid,StringComparison.OrdinalIgnoreCase))
                {
                    _profileToUse = profileToTest;
                    break;
                }

            }

            if (_profileToUse == null)
            {
                throw new Exception($"Trying to create a ShortcutItem and cannot find a loaded profile with UUID {uuid}.");
            }

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
        }

        public ShortcutItem(
            string name,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile,
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            string differentExecutableToMonitor,
            string executableNameAndPath,
            int executableTimeout,
            string executableArguments,
            bool executableArgumentsRequired,
            bool processNameToMonitorUsesExecutable,
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence,
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null,
            bool autoName = true,
            string uuid = ""
            ) : this()
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileToUse = profile;
            _category = ShortcutCategory.Application;
            _differentExecutableToMonitor = differentExecutableToMonitor;
            _executableNameAndPath = executableNameAndPath;
            _startTimeout = executableTimeout;
            _executableArguments = executableArguments;
            _executableArgumentsRequired = executableArgumentsRequired;
            _processNameToMonitorUsesExecutable = processNameToMonitorUsesExecutable;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            //if (_profileToUse is ProfileItem)
            //     _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
            _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

        }

        public ShortcutItem(
            string name, 
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile, 
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            Executable executable, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            ) : this()
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name; 
            _profileToUse = profile;
            _category = ShortcutCategory.Application;
            _differentExecutableToMonitor = executable.DifferentExecutableToMonitor;
            _executableNameAndPath = executable.ExecutableNameAndPath;
            _startTimeout = executable.ExecutableTimeout;
            _executableArguments = executable.ExecutableArguments;
            _executableArgumentsRequired = executable.ExecutableArgumentsRequired;
            _processNameToMonitorUsesExecutable = executable.ProcessNameToMonitorUsesExecutable;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            //if (_profileToUse is ProfileItem)
            //    _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
            _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
        }

        public ShortcutItem(
            string name, 
            string profileUuid, 
            Executable executable, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            ) : this()
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileUuid = profileUuid;
            _category = ShortcutCategory.Application;
            _differentExecutableToMonitor = executable.DifferentExecutableToMonitor;
            _executableNameAndPath = executable.ExecutableNameAndPath;
            _startTimeout = executable.ExecutableTimeout;
            _executableArguments = executable.ExecutableArguments;
            _executableArgumentsRequired = executable.ExecutableArgumentsRequired;
            _processNameToMonitorUsesExecutable = executable.ProcessNameToMonitorUsesExecutable;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileToUse
            foreach (ProfileItem profileToTest in ProfileRepository.AllProfiles)
            {
                if (profileToTest.UUID.Equals(_profileUuid, StringComparison.OrdinalIgnoreCase))
                {
                    _profileToUse = profileToTest;
                    break;
                }

            }

            if (_profileToUse == null)
            {
                throw new Exception($"Trying to create a ShortcutItem and cannot find a loaded profile with UUID {uuid}.");
            }

            // We create the OriginalBitmap from the IconPath
            if (_originalIconPath.EndsWith(".ico"))
            {
                Icon icoIcon = new Icon(_originalIconPath, 256, 256);
                //_originalBitmap = ExtractVistaIcon(biggestIcon);
                _originalLargeBitmap = icoIcon.ToBitmap();
                icoIcon.Dispose();
            }
            else
            {
                _originalLargeBitmap = ToLargeBitmap(_originalIconPath);
            }
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            //if (_profileToUse is ProfileItem)
            //    _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
            _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
        }


        public static Version Version
        {
            get => new Version(1, 0);
        }

        public string UUID
        {
            get
            {
                return _uuid;
            }
            set
            {
                string uuidV4Regex = @"[0-9A-F]{8}-[0-9A-F]{4}-4[0-9A-F]{3}-[89AB][0-9A-F]{3}-[0-9A-F]{12}";
                Match match = Regex.Match(value, uuidV4Regex, RegexOptions.IgnoreCase);
                if (match.Success)
                    _uuid = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public bool AutoName
        {
            get
            {
                return _autoName;
            }
            set
            {
                _autoName = value;
            }
        }   


        [JsonIgnore]
#pragma warning disable CS3003 // Type is not CLS-compliant
        public ProfileItem ProfileToUse {
#pragma warning restore CS3003 // Type is not CLS-compliant
            get
            {
                return _profileToUse;
            }
            set
            {
                if (value is ProfileItem)
                {
                    _profileToUse = value;
                    _profileUuid = _profileToUse.UUID;
                    // We should try to set the Profile
                    // And we rename the shortcut if the AutoName is on
                    if (AutoName)
                        AutoSuggestShortcutName();
                }
            }
        }

        public string ProfileUUID { 
            get 
            {
                return _profileUuid;
            }
            set
            {
                _profileUuid = value;

                // We try to find and set the ProfileTouse
                foreach (ProfileItem profileToTest in ProfileRepository.AllProfiles)
                {
                    if (profileToTest.UUID.Equals(_profileUuid, StringComparison.OrdinalIgnoreCase))
                        _profileToUse = profileToTest;
                }
            }
        }

        public ShortcutPermanence DisplayPermanence 
        { 
            get 
            {
                return _displayPermanence;
            }

            set 
            {
                _displayPermanence = value;
            }
        }

        public ShortcutPermanence AudioPermanence
        {
            get
            {
                return _audioPermanence;
            }

            set
            {
                _audioPermanence = value;
            }
        }

        public ShortcutPermanence CapturePermanence
        {
            get
            {
                return _capturePermanence;
            }

            set
            {
                _capturePermanence = value;
            }
        }

        public ShortcutCategory Category
        {
            get
            {
                return _category;
            }

            set
            {
                _category = value;
            }
        }

        public string DifferentExecutableToMonitor
        {
            get
            {
                return _differentExecutableToMonitor;
            }

            set
            {
                _differentExecutableToMonitor = value;
            }
        }

        public string ExecutableNameAndPath
        {
            get
            {
                return _executableNameAndPath;
            }

            set
            {
                _executableNameAndPath = value;

                // If the executableNameandPath is set then we also want to update the originalIconPath
                // so it's the path to the application. This will kick of the icon grabbing processes
                if (Category.Equals(ShortcutCategory.Application))
                    _originalIconPath = value;

            }
        }

        public string ExecutableArguments
        {
            get
            {
                return _executableArguments;
            }

            set
            {
                _executableArguments = value;
            }
        }

        public bool ExecutableArgumentsRequired
        {
            get
            {
                return _executableArgumentsRequired;
            }

            set
            {
                _executableArgumentsRequired = value;
            }
        }

        public bool ProcessNameToMonitorUsesExecutable
        {
            get
            {
                return _processNameToMonitorUsesExecutable;
            }

            set
            {
                _processNameToMonitorUsesExecutable = value;
            }
        }

        public int GameAppId
        {
            get
            {
                return _gameAppId;
            }

            set
            {
                _gameAppId = value;
            }
        }

        public string GameName
        {
            get
            {
                return _gameName;
            }

            set
            {
                _gameName = value;
            }
        }

        public SupportedGameLibrary GameLibrary
        {
            get
            {
                return _gameLibrary;
            }

            set
            {
                _gameLibrary = value;
            }
        }

        public int StartTimeout
        {
            get
            {
                return _startTimeout;
            }

            set
            {
                _startTimeout = value;
            }
        }

        public string GameArguments
        {
            get
            {
                return _gameArguments;
            }

            set
            {
                _gameArguments = value;
            }
        }

        public bool GameArgumentsRequired
        {
            get
            {
                return _gameArgumentsRequired;
            }

            set
            {
                _gameArgumentsRequired = value;
            }
        }

        public string AudioDevice
        {
            get
            {
                return _audioDevice;
            }

            set
            {
                _audioDevice = value;
            }
        }

        public bool ChangeAudioDevice
        {
            get
            {
                return _changeAudioDevice;
            }

            set
            {
                _changeAudioDevice = value;
            }
        }

        public decimal AudioVolume
        {
            get
            {
                return _audioVolume;
            }

            set
            {
                _audioVolume = value;
            }
        }

        public bool SetAudioVolume
        {
            get
            {
                return _setAudioVolume;
            }

            set
            {
                _setAudioVolume = value;
            }
        }

        public string CaptureDevice
        {
            get
            {
                return _captureDevice;
            }

            set
            {
                _captureDevice = value;
            }
        }

        public bool ChangeCaptureDevice
        {
            get
            {
                return _changeCaptureDevice;
            }

            set
            {
                _changeCaptureDevice = value;
            }
        }

        public decimal CaptureVolume
        {
            get
            {
                return _captureVolume;
            }

            set
            {
                _captureVolume = value;
            }
        }

        public bool SetCaptureVolume
        {
            get
            {
                return _setCaptureVolume;
            }

            set
            {
                _setCaptureVolume = value;
            }
        }

        public List<StartProgram> StartPrograms
        {
            get
            {
                return _startPrograms;
            }

            set
            {
                _startPrograms = value;
            }
        }

        public string OriginalIconPath {
            get
            {
                return _originalIconPath;
            }

            set
            {
                _originalIconPath = value;

                // And we do the same for the OriginalBitmap 
                //_originalLargeBitmap = ToLargeBitmap(_originalIconPath);                
            }
        }

        [JsonConverter(typeof(CustomBitmapConverter))]
        public Bitmap OriginalLargeBitmap
        {
            get
            {
                return _originalLargeBitmap;
            }

            set
            {
                _originalLargeBitmap = value;

                // And we do the same for the Bitmap overlay, but only if the ProfileToUse is set
                //if (_profileToUse is ProfileItem)
                //    _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            }
        }

        [JsonConverter(typeof(CustomBitmapConverter))]
        public Bitmap ShortcutBitmap
        {
            get
            {
                return _shortcutBitmap;
            }

            set
            {
                _shortcutBitmap = value;
            }
        }

        public string SavedShortcutIconCacheFilename
        {
            get
            {
                return _savedShortcutIconCacheFilename;
            }
            set
            {
                _savedShortcutIconCacheFilename = value;
            }
        }

        [JsonIgnore]
        public bool IsPossible
        {
            get
            {
                return _isPossible;
            }
            set
            {
                _isPossible = value;
            }
        }

        public void UpdateNoGameShortcut(
            string name,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile,
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence,
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null,
            bool autoName = true,
            string uuid = ""
            )
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _category = ShortcutCategory.NoGame;
            _profileToUse = profile;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalLargeBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);
            _originalSmallBitmap = ToSmallBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }

        public void UpdateNoGameShortcut(
            string name, 
            string profileUuid, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            )
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileUuid = profileUuid;
            _category = ShortcutCategory.NoGame;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileToUse
            foreach (ProfileItem profileToTest in ProfileRepository.AllProfiles)
            {
                if (profileToTest.UUID.Equals(_profileUuid, StringComparison.OrdinalIgnoreCase))
                {
                    _profileToUse = profileToTest;
                    break;
                }

            }

            if (_profileToUse == null)
            {
                throw new Exception($"Trying to create a ShortcutItem and cannot find a loaded profile with UUID {uuid}.");
            }

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }



        public void UpdateGameShortcut(
            string name,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile,
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            int gameAppId,
            string gameName,
            SupportedGameLibrary gameLibrary,
            int gameTimeout,
            string gameArguments,
            bool gameArgumentsRequired,
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence,
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null,
            bool autoName = true,
            string uuid = ""
            )
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileToUse = profile;
            _category = ShortcutCategory.Game;
            _gameAppId = gameAppId;
            _gameName = gameName;
            _gameLibrary = gameLibrary;
            _startTimeout = gameTimeout;
            _gameArguments = gameArguments;
            _gameArgumentsRequired = gameArgumentsRequired;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalLargeBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);
            _originalSmallBitmap = ToSmallBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }

        public void UpdateGameShortcut(
            string name, 
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile, 
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            GameStruct game, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = "")
        {
            // Create a new UUID for the shortcut if one wasn't created already
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileToUse = profile;
            _category = ShortcutCategory.Game;
            _gameAppId = game.GameToPlay.Id;
            _gameName = game.GameToPlay.Name;
            _gameLibrary = game.GameToPlay.GameLibrary;
            _startTimeout = game.StartTimeout;
            _gameArguments = game.GameArguments;
            _gameArgumentsRequired = game.GameArgumentsRequired;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }



        public void UpdateGameShortcut(
            string name, 
            string profileUuid, 
            GameStruct game, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            )
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileUuid = profileUuid;
            _category = ShortcutCategory.Game;
            _gameAppId = game.GameToPlay.Id;
            _gameName = game.GameToPlay.Name;
            _gameLibrary = game.GameToPlay.GameLibrary;
            _startTimeout = game.StartTimeout;
            _gameArguments = game.GameArguments;
            _gameArgumentsRequired = game.GameArgumentsRequired;
            _gameArgumentsRequired = false;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileToUse
            foreach (ProfileItem profileToTest in ProfileRepository.AllProfiles)
            {
                if (profileToTest.UUID.Equals(_profileUuid, StringComparison.OrdinalIgnoreCase))
                {
                    _profileToUse = profileToTest;
                    break;
                }

            }

            if (_profileToUse == null)
            {
                throw new Exception($"Trying to create a ShortcutItem and cannot find a loaded profile with UUID {uuid}.");
            }

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            if (_profileToUse is ProfileItem)
                _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }

        public void UpdateExecutableShortcut(
            string name,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile,
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            string differentExecutableToMonitor,
            string executableNameAndPath,
            int executableTimeout,
            string executableArguments,
            bool executableArgumentsRequired,
            bool processNameToMonitorUsesExecutable,
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence,
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null,
            bool autoName = true,
            string uuid = ""
            )
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileToUse = profile;
            _category = ShortcutCategory.Application;
            _differentExecutableToMonitor = differentExecutableToMonitor;
            _executableNameAndPath = executableNameAndPath;
            _startTimeout = executableTimeout;
            _executableArguments = executableArguments;
            _executableArgumentsRequired = executableArgumentsRequired;
            _processNameToMonitorUsesExecutable = processNameToMonitorUsesExecutable;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            //if (_profileToUse is ProfileItem)
            //     _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
            _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }

        public void UpdateExecutableShortcut(
            string name, 
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            ProfileItem profile, 
#pragma warning restore CS3001 // Argument type is not CLS-compliant
            Executable executable, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            )
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileToUse = profile;
            _category = ShortcutCategory.Application;
            _differentExecutableToMonitor = executable.DifferentExecutableToMonitor;
            _executableNameAndPath = executable.ExecutableNameAndPath;
            _startTimeout = executable.ExecutableTimeout;
            _executableArguments = executable.ExecutableArguments;
            _executableArgumentsRequired = executable.ExecutableArgumentsRequired;
            _processNameToMonitorUsesExecutable = executable.ProcessNameToMonitorUsesExecutable;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileUuid
            _profileUuid = profile.UUID;

            // We create the OriginalBitmap from the IconPath
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            //if (_profileToUse is ProfileItem)
            //    _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
            _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }

        public void UpdateExecutableShortcut(
            string name, 
            string profileUuid, 
            Executable executable, 
            ShortcutPermanence displayPermanence,
            ShortcutPermanence audioPermanence, 
            ShortcutPermanence capturePermanence,
            string originalIconPath,
            bool changeAudioDevice = false,
            string audioDevice = "",
            bool setAudioVolume = false,
            decimal audioVolume = -1,
            bool changeCaptureDevice = false,
            string captureDevice = "",
            bool setCaptureVolume = false,
            decimal captureVolume = -1,
            List<StartProgram> startPrograms = null, 
            bool autoName = true, 
            string uuid = ""
            )
        {
            if (!String.IsNullOrWhiteSpace(uuid))
                _uuid = uuid;
            _name = name;
            _profileUuid = profileUuid;
            _category = ShortcutCategory.Application;
            _differentExecutableToMonitor = executable.DifferentExecutableToMonitor;
            _executableNameAndPath = executable.ExecutableNameAndPath;
            _startTimeout = executable.ExecutableTimeout;
            _executableArguments = executable.ExecutableArguments;
            _executableArgumentsRequired = executable.ExecutableArgumentsRequired;
            _processNameToMonitorUsesExecutable = executable.ProcessNameToMonitorUsesExecutable;
            _changeAudioDevice = changeAudioDevice;
            _audioDevice = audioDevice;
            _setAudioVolume = setAudioVolume;
            _audioVolume = audioVolume;
            _changeCaptureDevice = changeCaptureDevice;
            _captureDevice = captureDevice;
            _setCaptureVolume = setCaptureVolume;
            _captureVolume = captureVolume;
            _displayPermanence = displayPermanence;
            _audioPermanence = audioPermanence;
            _capturePermanence = capturePermanence;
            _autoName = autoName;
            _startPrograms = startPrograms;
            _originalIconPath = originalIconPath;

            // Now we need to find and populate the profileToUse
            foreach (ProfileItem profileToTest in ProfileRepository.AllProfiles)
            {
                if (profileToTest.UUID.Equals(_profileUuid, StringComparison.OrdinalIgnoreCase))
                {
                    _profileToUse = profileToTest;
                    break;
                }

            }

            if (_profileToUse == null)
            {
                throw new Exception($"Trying to create a ShortcutItem and cannot find a loaded profile with UUID {uuid}.");
            }

            // We create the OriginalBitmap from the IconPath
            if (_originalIconPath.EndsWith(".ico"))
            {
                Icon icoIcon = new Icon(_originalIconPath, 256, 256);
                //_originalBitmap = ExtractVistaIcon(biggestIcon);
                _originalLargeBitmap = icoIcon.ToBitmap();
                icoIcon.Dispose();
            }
            else
            {
                _originalLargeBitmap = ToLargeBitmap(_originalIconPath);
            }
            _originalLargeBitmap = ToLargeBitmap(_originalIconPath);

            // We create the ShortcutBitmap from the OriginalBitmap 
            // (We only do it if there is a valid profile)
            //if (_profileToUse is ProfileItem)
            //    _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);
            _shortcutBitmap = ToBitmapOverlay(_originalLargeBitmap, _profileToUse.ProfileTightestBitmap, 256, 256);

            ReplaceShortcutIconInCache();
        }


        public bool CopyTo (ShortcutItem shortcut, bool overwriteUUID = true)
        {
            if (!(shortcut is ShortcutItem))
                return false;

            if (overwriteUUID)
                shortcut.UUID = UUID;

            // Copy all the shortcut data over to the other Shortcut
            shortcut.Name = Name;
            shortcut.ProfileToUse = ProfileToUse;
            shortcut.ProfileUUID = ProfileUUID;
            shortcut.DisplayPermanence = DisplayPermanence;
            shortcut.AudioPermanence = AudioPermanence;
            shortcut.CapturePermanence = CapturePermanence;
            shortcut.Category = Category;
            shortcut.DifferentExecutableToMonitor = DifferentExecutableToMonitor;
            shortcut.ExecutableNameAndPath = ExecutableNameAndPath;
            shortcut.ExecutableArguments = ExecutableArguments;
            shortcut.ExecutableArgumentsRequired = ExecutableArgumentsRequired;
            shortcut.ProcessNameToMonitorUsesExecutable = ProcessNameToMonitorUsesExecutable;
            shortcut.GameAppId = GameAppId;
            shortcut.GameName = GameName;
            shortcut.GameLibrary = GameLibrary;
            shortcut.StartTimeout = StartTimeout;
            shortcut.GameArguments = GameArguments;
            shortcut.GameArgumentsRequired = GameArgumentsRequired;
            shortcut.OriginalIconPath = OriginalIconPath;
            shortcut.OriginalLargeBitmap = OriginalLargeBitmap;
            shortcut.ShortcutBitmap = ShortcutBitmap;
            shortcut.SavedShortcutIconCacheFilename = SavedShortcutIconCacheFilename;
            shortcut.IsPossible = IsPossible;
            shortcut.StartPrograms = StartPrograms;
            shortcut.ChangeAudioDevice = ChangeAudioDevice;
            shortcut.AudioDevice = AudioDevice;
            shortcut.SetAudioVolume = SetAudioVolume;
            shortcut.AudioVolume = AudioVolume;
            shortcut.ChangeCaptureDevice = ChangeCaptureDevice;
            shortcut.CaptureDevice = CaptureDevice;
            shortcut.SetCaptureVolume = SetCaptureVolume;
            shortcut.CaptureVolume = CaptureVolume;

            // Save the shortcut incon to the icon cache
            shortcut.ReplaceShortcutIconInCache();

            return true;
        }

        public void ReplaceShortcutIconInCache()
        {
            string newShortcutIconFilename;
            if (_category == ShortcutCategory.Application)
            {
                // Work out the name of the shortcut we'll save.
                newShortcutIconFilename = Path.Combine(Program.AppShortcutPath, String.Concat(@"executable-", _profileToUse.UUID, "-", Path.GetFileNameWithoutExtension(_executableNameAndPath), @".ico"));

            }
            else
            {
                // Work out the name of the shortcut we'll save.
                newShortcutIconFilename = Path.Combine(Program.AppShortcutPath, String.Concat(_gameLibrary.ToString().ToLower(CultureInfo.InvariantCulture), @"-", _profileToUse.UUID, "-", _gameAppId.ToString(), @".ico"));
            }

            // If the new shortcut icon should be named differently
            // then change the name of it
            if (!newShortcutIconFilename.Equals(_savedShortcutIconCacheFilename))
            {
                if (System.IO.File.Exists(_savedShortcutIconCacheFilename))
                    System.IO.File.Delete(_savedShortcutIconCacheFilename);

                SaveShortcutIconToCache();
            }

        }


        public void SaveShortcutIconToCache()
        {

            // Only add this set of options if the shortcut is to an standalone application
            if (_category == ShortcutCategory.Application)
            {
                // Work out the name of the shortcut we'll save.
                _savedShortcutIconCacheFilename = Path.Combine(Program.AppShortcutPath, String.Concat(@"executable-", _profileToUse.UUID, "-", Path.GetFileNameWithoutExtension(_executableNameAndPath), @".ico"));

            }
            else 
            {
                // Work out the name of the shortcut we'll save.
                _savedShortcutIconCacheFilename = Path.Combine(Program.AppShortcutPath, String.Concat(_gameLibrary.ToString().ToLower(CultureInfo.InvariantCulture),@"-", _profileToUse.UUID, "-", _gameAppId.ToString(), @".ico"));
            }

            MultiIcon shortcutIcon;
            try
            {
                //shortcutIcon = new ProfileIcon(shortcut.ProfileToUse).ToIconOverlay(shortcut.OriginalIconPath);
                shortcutIcon = ToIconOverlay();
                shortcutIcon.Save(_savedShortcutIconCacheFilename, MultiIconFormat.ICO);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ShortcutRepository/SaveShortcutIconToCache exception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");

                // If we fail to create an icon based on the original executable or game
                // Then we use the standard DisplayMagician profile one.
                shortcutIcon = _profileToUse.ProfileIcon.ToIcon();
                shortcutIcon.Save(_savedShortcutIconCacheFilename, MultiIconFormat.ICO);
            }

        }

        public static Bitmap ToLargeBitmap(string fileNameAndPath)
        {
            if (String.IsNullOrWhiteSpace(fileNameAndPath))
                return null;

            Bitmap bm = null
;            if (fileNameAndPath.EndsWith(".ico"))
            {
                Icon icoIcon = new Icon(fileNameAndPath, 256, 256);
                bm = icoIcon.ToBitmap();
                icoIcon.Dispose();
            }
            else
            {
                bm = IconFromFile.GetLargeBitmapFromFile(fileNameAndPath, true, false);
            }           
            return bm;
        }

        public static Bitmap ToSmallBitmap(string fileNameAndPath)
        {
            if (String.IsNullOrWhiteSpace(fileNameAndPath))
                return null;

            Bitmap bm = null; 
            if (fileNameAndPath.EndsWith(".ico"))
            {
                Icon icoIcon = new Icon(fileNameAndPath, 128, 128);
                bm = icoIcon.ToBitmap();
                icoIcon.Dispose();
            }
            else
            {
                bm = IconFromFile.GetSmallBitmapFromFile(fileNameAndPath, false, false, false);
            }
            return bm;
        }

        public Bitmap ToBitmapOverlay(Bitmap originalBitmap, Bitmap overlayBitmap, int width, int height, PixelFormat format = PixelFormat.Format32bppArgb)
        {

            if (width <= 0)
                return null;

            if (height <= 0)
                return null;

            // Figure out sizes and positions
            Size targetSize = new Size(width,height);
            Size originalBitmapCurrentSize = new Size(originalBitmap.Width, originalBitmap.Height);
            Size overlaylBitmapCurrentSize = new Size(overlayBitmap.Width, overlayBitmap.Height);

            // Make a new empty bitmap of the wanted size
            var combinedBitmap = new Bitmap(targetSize.Width, targetSize.Height, format);
            combinedBitmap.MakeTransparent();

            using (var g = Graphics.FromImage(combinedBitmap))
            {

                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.AssumeLinear;

                // Resize the originalBitmap if needed then draw it
                Size originalBitmapNewSize = ResizeDrawing.FitWithin(originalBitmapCurrentSize, targetSize);
                Point originalBitmapNewLocation = ResizeDrawing.AlignCenter(originalBitmapNewSize, targetSize);
                g.DrawImage(originalBitmap, originalBitmapNewLocation.X, originalBitmapNewLocation.Y, originalBitmapNewSize.Width, originalBitmapNewSize.Height);

                // Resize the overlayBitmap if needed then draw it in the bottom-right corner
                Size overlayBitmapMaxSize = ResizeDrawing.FitWithin(overlaylBitmapCurrentSize, targetSize);
                Size overlayBitmapNewSize = ResizeDrawing.MakeSmaller(overlayBitmapMaxSize,70);
                Point overlayBitmapNewLocation = ResizeDrawing.AlignBottomRight(overlayBitmapNewSize, targetSize);

                g.DrawImage(overlayBitmap, overlayBitmapNewLocation.X, overlayBitmapNewLocation.Y, overlayBitmapNewSize.Width, overlayBitmapNewSize.Height);

            }
            return combinedBitmap;
        }

#pragma warning disable CS3002 // Return type is not CLS-compliant
        public MultiIcon ToIconOverlay()
#pragma warning restore CS3002 // Return type is not CLS-compliant
        {
            Size[] iconSizes = new[]
            {
                new Size(256, 256),
                new Size(64, 64),
                new Size(48, 48),
                new Size(32, 32),
                new Size(24, 24),
                new Size(16, 16)
            };
            MultiIcon multiIcon = new MultiIcon();
            SingleIcon icon = multiIcon.Add("Icon1");

            foreach (Size size in iconSizes)
            {
                Bitmap bitmapOverlay = ToBitmapOverlay(_originalLargeBitmap, ProfileToUse.ProfileTightestBitmap, size.Width, size.Height);
                icon.Add(bitmapOverlay);

                if (size.Width >= 256 && size.Height >= 256)
                {
                    icon[icon.Count - 1].IconImageFormat = IconImageFormat.PNG;
                }

                bitmapOverlay.Dispose();
            }

            multiIcon.SelectedIndex = 0;

            return multiIcon;
        }


        public (bool,string) IsValid()
        {
            // Do some validation checks to make sure the shortcut is sensible
            // And that we have enough to try and action within the shortcut
            // (in other words check everything in the shortcut is still valid)

            // Does the profile we want to Use still exist?
            // Is the profile still valid right now? i.e. are all the screens available?
            if (!ProfileToUse.IsPossible)
            {
                return (false,string.Format("The profile '{0}' is not valid right now and cannot be used.",ProfileToUse.Name));
            }
            // Is the main application still installed?
            if (Category.Equals(ShortcutCategory.Application))
            {
                // We need to check if the Application still exists
                if (!System.IO.File.Exists(ExecutableNameAndPath))
                {
                    return (false, string.Format("The application executable '{0}' does not exist, or cannot be accessed by DisplayMagician.", ExecutableNameAndPath));
                }

            } else if (Category.Equals(ShortcutCategory.Game))
            {
                // If the game is a Steam Game we check for that
                if (GameLibrary.Equals(SupportedGameLibrary.Steam))
                {

                    // First check if Steam is installed
                    // Check if Steam is installed and error if it isn't
                    if (!SteamLibrary.IsSteamInstalled)
                    {
                        return (false, Language.Steam_executable_file_not_found);
                    }

                    // We need to look up details about the game
                    if (!SteamLibrary.ContainsSteamGame(GameAppId))
                    {
                        return (false, string.Format("The Steam Game with AppID '{0}' is not installed on this computer.", GameAppId));
                    }
                }
                // If the game is a Uplay Game we check for that
                else if (GameLibrary.Equals(SupportedGameLibrary.Uplay))
                {
                    // First check if Steam is installed
                    // Check if Steam is installed and error if it isn't
                    if (!UplayLibrary.IsUplayInstalled)
                    {
                        return (false, "Cannot find the Uplay executable! Uplay doesn't appear to be installed");
                    }

                    // We need to look up details about the game
                    if (!UplayLibrary.ContainsUplayGame(GameAppId))
                    {
                        return (false, string.Format("The Uplay Game with AppID '{0}' is not installed on this computer.", GameAppId));
                    }

                }


            }
            // Check the Audio Device is still valid (if one is specified)
            if (ChangeAudioDevice)
            {
                CoreAudioController audioController = ShortcutRepository.AudioController;
                IEnumerable<CoreAudioDevice> audioDevices = audioController.GetPlaybackDevices();
                foreach (CoreAudioDevice audioDevice in audioDevices)
                {
                    if (audioDevice.FullName.Equals(AudioDevice))
                    {
                        if (audioDevice.State == AudioSwitcher.AudioApi.DeviceState.Disabled)
                            return (false, $"The Audio Device {AudioDevice} is disabled, so the shortcut '{Name}' cannot be used. You need to enable the audio device to use this shortcut, or edit the shortcut to change the audio device.");
                        if (audioDevice.State == AudioSwitcher.AudioApi.DeviceState.NotPresent)
                            return (false, $"The Audio Device {AudioDevice} is not present, so the shortcut '{Name}' cannot be used.");
                        if (audioDevice.State == AudioSwitcher.AudioApi.DeviceState.Unplugged)
                            return (false, $"The Audio Device {AudioDevice} is unplugged, so the shortcut '{Name}' cannot be used. You need to plug in the audio device to use this shortcut, or edit the shortcut to change the audio device.");
                    }
                }
            }
            // Check the Capture Device is still valid (if one is specified)
            if (ChangeCaptureDevice)
            {
                CoreAudioController audioController = ShortcutRepository.AudioController;
                IEnumerable<CoreAudioDevice> captureDevices = audioController.GetCaptureDevices();
                foreach (CoreAudioDevice captureDevice in captureDevices)
                {
                    if (captureDevice.FullName.Equals(CaptureDevice))
                    {
                        if (captureDevice.State == AudioSwitcher.AudioApi.DeviceState.Disabled)
                            return (false, $"The Capture Device {CaptureDevice} is disabled, so the shortcut '{Name}' cannot be used. You need to enable the capture device to use this shortcut, or edit the shortcut to change the capture device.");
                        if (captureDevice.State == AudioSwitcher.AudioApi.DeviceState.NotPresent)
                            return (false, $"The Capture Device {CaptureDevice} is not present, so the shortcut '{Name}' cannot be used.");
                        if (captureDevice.State == AudioSwitcher.AudioApi.DeviceState.Unplugged)
                            return (false, $"The Capture Device {CaptureDevice} is unplugged, so the shortcut '{Name}' cannot be used. You need to plug in the capture device to use this shortcut, or edit the shortcut to change the capture device.");
                    }
                }
            }

            // TODO Do all the specified pre-start apps still exist?

            return (true, "Shortcut is valid");

        }

        // ReSharper disable once FunctionComplexityOverflow
        // ReSharper disable once CyclomaticComplexity
        public bool CreateShortcut(string shortcutFileName)
        {
            string programName = Path.GetFileNameWithoutExtension(ExecutableNameAndPath);
            string shortcutDescription = string.Empty;
            string shortcutIconFileName;

            var shortcutArgs = new List<string>
            {
                // Add the SwitchProfile command as the first argument to start to switch to another profile
                $"{DisplayMagicianStartupAction.RunShortcut}",
                $"\"{UUID}\""
            };

            // Only add the rest of the options if the permanence is temporary
            if (DisplayPermanence == ShortcutPermanence.Temporary)
            {
                // Only add this set of options if the shortcut is to an standalone application
                if (Category == ShortcutCategory.Application)
                {
                    // Prepare text for the shortcut description field
                    shortcutDescription = string.Format(Language.Execute_application_with_profile, programName, ProfileToUse.Name);

                }
                // Only add the rest of the options if the temporary switch radio button is set
                // and if the game launching radio button is set
                else if (DisplayPermanence == ShortcutPermanence.Temporary)
                {
                    // Prepare text for the shortcut description field
                    shortcutDescription = string.Format(Language.Execute_application_with_profile, GameName, ProfileToUse.Name);
                }

            }
            // Only add the rest of the options if the permanent switch radio button is set
            else
            {
                // Prepare text for the shortcut description field
                shortcutDescription = string.Format(Language.Switching_display_profile_to_profile, ProfileToUse.Name);
            }

            // Now we are ready to create a shortcut based on the filename the user gave us
            shortcutFileName = Path.ChangeExtension(shortcutFileName, @"lnk");

            // And we use the Icon from the shortcutIconCache
            shortcutIconFileName = SavedShortcutIconCacheFilename;

            // If the user supplied a file
            if (shortcutFileName != null)
            {
                try
                {
                    // Remove the old file if it exists to replace it
                    if (System.IO.File.Exists(shortcutFileName))
                    {
                        System.IO.File.Delete(shortcutFileName);
                    }

                    // Actually create the shortcut!
                    //var wshShellType = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
                    //dynamic wshShell = Activator.CreateInstance(wshShellType);
                    

                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutFileName);

                    shortcut.TargetPath = Application.ExecutablePath;
                    shortcut.Arguments = string.Join(" ", shortcutArgs);
                    shortcut.Description = shortcutDescription;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath) ??
                                                string.Empty;

                    shortcut.IconLocation = shortcutIconFileName;
                    shortcut.Save();
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, $"ShortcutItem/CreateShortcut: Execption while creating desktop shortcut!");

                    // Clean up a failed attempt
                    if (System.IO.File.Exists(shortcutFileName))
                    {
                        System.IO.File.Delete(shortcutFileName);
                    }
                }
            }

            // Return a status on how it went
            // true if it was a success or false if it was not
            return shortcutFileName != null && System.IO.File.Exists(shortcutFileName);
        }


        

        public void AutoSuggestShortcutName()
        {
            if (AutoName && _profileToUse is ProfileItem)
            {
                if (Category.Equals(ShortcutCategory.NoGame))
                {
                    if (DisplayPermanence.Equals(ShortcutPermanence.Permanent))
                        _name = $"{_profileToUse.Name}";
                    else if (DisplayPermanence.Equals(ShortcutPermanence.Temporary))
                        _name = $"{_profileToUse.Name} (Temporary)";
                }
                else if (Category.Equals(ShortcutCategory.Game) && GameName.Length > 0)
                {
                    _name = $"{GameName} ({_profileToUse.Name})";
                }
                else if (Category.Equals(ShortcutCategory.Application) && ExecutableNameAndPath.Length > 0)
                {
                    string baseName = Path.GetFileNameWithoutExtension(ExecutableNameAndPath);
                    _name = $"{baseName} ({_profileToUse.Name})";
                }
            }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is ShortcutItem)) throw new ArgumentException("Object to CompareTo is not a Shortcut"); ;

            ShortcutItem otherShortcut = (ShortcutItem) obj;
            return this.Name.CompareTo(otherShortcut.Name);                
        }

    }


    #region JsonConverterBitmap
    internal class CustomBitmapConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        //convert from byte to bitmap (deserialize)

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string image = (string)reader.Value;

            byte[] byteBuffer = Convert.FromBase64String(image);
            MemoryStream memoryStream = new MemoryStream(byteBuffer)
            {
                Position = 0
            };

            return (Bitmap)Bitmap.FromStream(memoryStream);
        }

        //convert bitmap to byte (serialize)
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Bitmap bitmap = (Bitmap)value;

            ImageConverter converter = new ImageConverter();
            writer.WriteValue((byte[])converter.ConvertTo(bitmap, typeof(byte[])));
        }

        public static System.Drawing.Imaging.ImageFormat GetImageFormat(Bitmap bitmap)
        {
            ImageFormat img = bitmap.RawFormat;

            if (img.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
                return System.Drawing.Imaging.ImageFormat.Jpeg;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.Bmp))
                return System.Drawing.Imaging.ImageFormat.Bmp;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.Png))
                return System.Drawing.Imaging.ImageFormat.Png;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.Emf))
                return System.Drawing.Imaging.ImageFormat.Emf;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.Exif))
                return System.Drawing.Imaging.ImageFormat.Exif;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                return System.Drawing.Imaging.ImageFormat.Gif;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.Icon))
                return System.Drawing.Imaging.ImageFormat.Icon;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.MemoryBmp))
                return System.Drawing.Imaging.ImageFormat.MemoryBmp;
            if (img.Equals(System.Drawing.Imaging.ImageFormat.Tiff))
                return System.Drawing.Imaging.ImageFormat.Tiff;
            else
                return System.Drawing.Imaging.ImageFormat.Wmf;
        }

    }

    #endregion
}