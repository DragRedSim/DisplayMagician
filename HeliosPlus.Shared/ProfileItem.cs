using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using WindowsDisplayAPI.DisplayConfig;
using HeliosPlus.Shared.Resources;
using Newtonsoft.Json;
using NvAPIWrapper.Mosaic;
using NvAPIWrapper.Native.Mosaic;
using HeliosPlus.Shared.Topology;
using System.Drawing;
using System.Drawing.Imaging;
using WindowsDisplayAPI;
using System.Text.RegularExpressions;
using HeliosPlus.Shared.DisplayIdentification;

namespace HeliosPlus.Shared
{
    public class ProfileItem
    {
        private static List<ProfileItem> _allSavedProfiles = new List<ProfileItem>();
        private ProfileIcon _profileIcon;
        private Bitmap _profileBitmap, _profileShortcutBitmap;
        private List<string> _profileDisplayIdentifiers = new List<string>();
        private static List<Display> _availableDisplays;
        private static List<UnAttachedDisplay> _unavailableDisplays;

        internal static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HeliosPlus");

        private string _uuid = "";
        private Version _version;
        private bool _isActive = false;
        private bool _isPossible = false;


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
                MemoryStream memoryStream = new MemoryStream(byteBuffer);
                memoryStream.Position = 0;

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
        public ProfileItem()
        {
            /*try
            {
                // Generate the DeviceIdentifiers ready to be used
                ProfileDisplayIdentifiers = DisplayIdentifier.GetDisplayIdentification();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ShortcutItem/Instansiation exception: {ex.Message}: {ex.StackTrace} - {ex.InnerException}");
                // ignored
            }*/

        }

        public static Version Version = new Version(2, 1);

        #region Instance Properties

        public string UUID
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_uuid))
                    _uuid = Guid.NewGuid().ToString("D");
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

        [JsonIgnore]
        public bool IsPossible
        {
            get
            {

                //List<string> DisplayInfo = DisplayIdentifier.GetDisplayIdentification();
                //DisplayManager displayManager;
                Console.WriteLine($"*** Physical GPU looping ***"); 
                NvAPIWrapper.GPU.PhysicalGPU[] myPhysicalGPUs =  NvAPIWrapper.GPU.PhysicalGPU.GetPhysicalGPUs();
                foreach (NvAPIWrapper.GPU.PhysicalGPU myGPU in myPhysicalGPUs)
                {
                    Console.WriteLine($"PhysicalGPU: {myGPU.FullName}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.ActiveOutputs}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.ArchitectInformation}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.Board}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.Foundry}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.GPUId}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.GPUType}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.Handle}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.IsQuadro}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.SystemType}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.UsageInformation}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.ActiveOutputs}");
                    Console.WriteLine($"PhysicalGPU: {myGPU}");
                    Console.WriteLine($"PhysicalGPU: {myGPU}");
                    Console.WriteLine($"PhysicalGPU: {myGPU}");
                    Console.WriteLine($"PhysicalGPU: {myGPU}");
                    Console.WriteLine($"PhysicalGPU: {myGPU}");
                    Console.WriteLine($"PhysicalGPU: {myGPU}");
                  
                    // get a list of all physical displayDevices attached to the GPUs
                    NvAPIWrapper.Display.DisplayDevice[] connectedDisplayDevices = myGPU.GetDisplayDevices();
                    foreach (NvAPIWrapper.Display.DisplayDevice aConnectedDisplayDevice in connectedDisplayDevices)
                    {
                        Console.WriteLine($"DisplayID: {aConnectedDisplayDevice.DisplayId}");
                        Console.WriteLine($"ConnectionType: {aConnectedDisplayDevice.ConnectionType}");
                        Console.WriteLine($"IsActive: {aConnectedDisplayDevice.IsActive}");
                        Console.WriteLine($"IsAvailble: {aConnectedDisplayDevice.IsAvailable}");
                        Console.WriteLine($"IsCluster: {aConnectedDisplayDevice.IsCluster}");
                        Console.WriteLine($"IsConnected: {aConnectedDisplayDevice.IsConnected}");
                        Console.WriteLine($"IsDynamic: {aConnectedDisplayDevice.IsDynamic}");
                        Console.WriteLine($"IsMultistreamrootnaode: {aConnectedDisplayDevice.IsMultiStreamRootNode}");
                        Console.WriteLine($"IsOSVisible: {aConnectedDisplayDevice.IsOSVisible}");
                        Console.WriteLine($"IsPhysicallyConnected: {aConnectedDisplayDevice.IsPhysicallyConnected}");
                        Console.WriteLine($"IsWFD: {aConnectedDisplayDevice.IsWFD}");
                        Console.WriteLine($"Output: {aConnectedDisplayDevice.Output}");
                        Console.WriteLine($"PhysicalGPU: {aConnectedDisplayDevice.PhysicalGPU}");
                        Console.WriteLine($"ScanOutInformation: {aConnectedDisplayDevice.ScanOutInformation}");
                    }
                }

                Console.WriteLine($"*** Physical GPU looping using outputs ***");
                foreach (NvAPIWrapper.GPU.PhysicalGPU myGPU in myPhysicalGPUs)
                {
                    Console.WriteLine($"PhysicalGPU ToString: {myGPU.ToString()}");
                    Console.WriteLine($"PhysicalGPU Fullname: {myGPU.FullName}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.ArchitectInformation}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.Board}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.Foundry}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.GPUId}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.GPUType}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.Handle}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.IsQuadro}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.SystemType}");
                    Console.WriteLine($"PhysicalGPU: {myGPU.UsageInformation}");

                    // get a list of all physical outputs attached to the GPUs
                    NvAPIWrapper.GPU.GPUOutput[] myGPUOutputs = myGPU.ActiveOutputs;
                    foreach (NvAPIWrapper.GPU.GPUOutput aGPUOutput in myGPUOutputs)
                    {
                        Console.WriteLine($"Device DigitalVibrance control: {aGPUOutput.ToString()}");
                        Console.WriteLine($"Device OutputID : {aGPUOutput.OutputId}");
                        Console.WriteLine($"Device OutputType: {aGPUOutput.OutputType}");
                        Console.WriteLine($"Device DigitalVibranceControl: {aGPUOutput.DigitalVibranceControl}");

                        // Figure out the displaydevice attached to the output
                        NvAPIWrapper.Display.DisplayDevice aConnectedDisplayDevice = myGPU.GetDisplayDeviceByOutput(aGPUOutput);
                        Console.WriteLine($"DisplayID: {aConnectedDisplayDevice.DisplayId}");
                        Console.WriteLine($"ConnectionType: {aConnectedDisplayDevice.ConnectionType}");
                        Console.WriteLine($"IsActive: {aConnectedDisplayDevice.IsActive}");
                        Console.WriteLine($"IsAvailble: {aConnectedDisplayDevice.IsAvailable}");
                        Console.WriteLine($"IsCluster: {aConnectedDisplayDevice.IsCluster}");
                        Console.WriteLine($"IsConnected: {aConnectedDisplayDevice.IsConnected}");
                        Console.WriteLine($"IsDynamic: {aConnectedDisplayDevice.IsDynamic}");
                        Console.WriteLine($"IsMultistreamrootnaode: {aConnectedDisplayDevice.IsMultiStreamRootNode}");
                        Console.WriteLine($"IsOSVisible: {aConnectedDisplayDevice.IsOSVisible}");
                        Console.WriteLine($"IsPhysicallyConnected: {aConnectedDisplayDevice.IsPhysicallyConnected}");
                        Console.WriteLine($"IsWFD: {aConnectedDisplayDevice.IsWFD}");
                        Console.WriteLine($"Output: {aConnectedDisplayDevice.Output}");
                        Console.WriteLine($"PhysicalGPU: {aConnectedDisplayDevice.PhysicalGPU}");
                        Console.WriteLine($"ScanOutInformation: {aConnectedDisplayDevice.ScanOutInformation}");

                    }
                }

                Console.WriteLine($"*** A giant list of displaydevices ***");
                List<NvAPIWrapper.Display.Display> allConnectedDisplays = NvAPIWrapper.Display.Display.GetDisplays().ToList();
                foreach (NvAPIWrapper.Display.Display availableDisplay in allConnectedDisplays)
                {
                    Console.WriteLine($"DisplayID: {availableDisplay.DisplayDevice.DisplayId}");
                    Console.WriteLine($"ConnectionType: {availableDisplay.DisplayDevice.ConnectionType}");
                    Console.WriteLine($"IsActive: {availableDisplay.DisplayDevice.IsActive}");
                    Console.WriteLine($"IsAvailble: {availableDisplay.DisplayDevice.IsAvailable}");
                    Console.WriteLine($"IsCluster: {availableDisplay.DisplayDevice.IsCluster}");
                    Console.WriteLine($"IsConnected: {availableDisplay.DisplayDevice.IsConnected}");
                    Console.WriteLine($"IsDynamic: {availableDisplay.DisplayDevice.IsDynamic}");
                    Console.WriteLine($"IsMultistreamrootnaode: {availableDisplay.DisplayDevice.IsMultiStreamRootNode}");
                    Console.WriteLine($"IsOSVisible: {availableDisplay.DisplayDevice.IsOSVisible}");
                    Console.WriteLine($"IsPhysicallyConnected: {availableDisplay.DisplayDevice.IsPhysicallyConnected}");
                    Console.WriteLine($"IsWFD: {availableDisplay.DisplayDevice.IsWFD}");
                    Console.WriteLine($"Output: {availableDisplay.DisplayDevice.Output}");
                    Console.WriteLine($"PhysicalGPU: {availableDisplay.DisplayDevice.PhysicalGPU}");
                    Console.WriteLine($"ScanOutInformation: {availableDisplay.DisplayDevice.ScanOutInformation}");
                }



/*                var bytes = display.DisplayDevice.PhysicalGPU.ReadEDIDData(display.DisplayDevice.Output);
                DisplayName = new EDID(bytes).Descriptors
                    .Where(descriptor => descriptor is StringDescriptor)
                    .Cast<StringDescriptor>()
                    .FirstOrDefault(descriptor => descriptor.Type == StringDescriptorType.MonitorName)?.Value;
*/

                Console.WriteLine($"### All Unavailable Displays ###");
                List<UnAttachedDisplay> allDisconnectedDisplays = UnAttachedDisplay.GetUnAttachedDisplays().ToList();
                foreach (UnAttachedDisplay unavailableDisplay in allDisconnectedDisplays)
                {
                    Console.WriteLine($"DevicePath: {unavailableDisplay.DeviceKey}");
                    Console.WriteLine($"DeviceKey: {unavailableDisplay.DeviceKey}");
                    Console.WriteLine($"DeviceName: {unavailableDisplay.Adapter.DeviceName}");
                    Console.WriteLine($"DisplayFullName: {unavailableDisplay.DisplayFullName}");
                    Console.WriteLine($"DisplayName: {unavailableDisplay.DisplayName}");
                    Console.WriteLine($"IsAvail: {unavailableDisplay.IsAvailable}");
                    Console.WriteLine($"IsValid: {unavailableDisplay.IsValid}");
                    if (unavailableDisplay.IsAvailable)
                        Console.WriteLine($"");
                }

                Console.WriteLine($"### All Available Displays ###");
                List<Display> allWindowsConnectedDisplays = Display.GetDisplays().ToList();
                foreach (Display availableDisplay in allWindowsConnectedDisplays)
                {
                    Console.WriteLine($"DevicePath: {availableDisplay.DeviceKey}");
                    Console.WriteLine($"DeviceKey: {availableDisplay.DeviceKey}");
                    Console.WriteLine($"DeviceName: {availableDisplay.Adapter.DeviceName}");
                    Console.WriteLine($"DisplayFullName: {availableDisplay.DisplayFullName}");
                    Console.WriteLine($"DisplayName: {availableDisplay.DisplayName}");
                    Console.WriteLine($"IsAvail: {availableDisplay.IsAvailable}");
                    Console.WriteLine($"IsValid: {availableDisplay.IsValid}");
                    if (availableDisplay.IsAvailable)
                        Console.WriteLine($"");
                }



                IEnumerable<Display> currentDisplays = Display.GetDisplays();
                foreach (Display availableDisplay in currentDisplays)
                {
                    Console.WriteLine($"DsiplayName: {availableDisplay.DisplayName}");
                    if (availableDisplay.IsAvailable)
                        Console.WriteLine($"");
                }

                // Find the list of TargetDisplays we currently have from the currentprofile
                List<string> availableDevicePaths = new List<string>();
                ProfileViewport[] availableViewports = ProfileRepository.CurrentProfile.Viewports;

                foreach (ProfileViewport availableViewport in availableViewports)
                {
                    PathInfo pathInfo = availableViewport.ToPathInfo();
                    //pathInfo.TargetsInfo;
                    foreach (ProfileViewportTargetDisplay realTD in availableViewport.TargetDisplays)
                    {
                        string devicePath = realTD.DevicePath;
                        availableDevicePaths.Add(devicePath);
                    }
                }

                // If there are no viewports, then return false
                if (Viewports.Length == 0)
                    return false;


                Console.WriteLine($"-----Getting the possible mosiac Grid topologies ");
                NvAPIWrapper.Mosaic.GridTopology[] possibleMosaicTopologies = NvAPIWrapper.Mosaic.GridTopology.GetGridTopologies();
                foreach (NvAPIWrapper.Mosaic.GridTopology mosaicTopology in possibleMosaicTopologies)
                {
                    Console.WriteLine($"Mosaic AcceleratePrimaryDisplay: {mosaicTopology.AcceleratePrimaryDisplay}");
                    Console.WriteLine($"Mosaic ApplyWithBezelCorrectedResolution: {mosaicTopology.ApplyWithBezelCorrectedResolution}");
                    Console.WriteLine($"Mosaic BaseMosaicPanoramic: {mosaicTopology.BaseMosaicPanoramic}");
                    Console.WriteLine($"Mosaic Columns: {mosaicTopology.Columns}");
                    Console.WriteLine($"Mosaic ImmersiveGaming: {mosaicTopology.ImmersiveGaming}");
                    Console.WriteLine($"Mosaic Resolution: {mosaicTopology.Resolution}");
                    Console.WriteLine($"Mosaic Rows: {mosaicTopology.Rows}");

                    foreach (NvAPIWrapper.Mosaic.GridTopologyDisplay possibleGridTopologyDisplay in mosaicTopology.Displays)
                    {
                        Console.WriteLine($"Mosaic DisplayId: {possibleGridTopologyDisplay.DisplayDevice.DisplayId}");
                        Console.WriteLine($"Mosaic ConnectionType: {possibleGridTopologyDisplay.DisplayDevice.ConnectionType}");
                        Console.WriteLine($"Mosaic IsActive: {possibleGridTopologyDisplay.DisplayDevice.IsActive}");
                        Console.WriteLine($"Mosaic IsConnected: {possibleGridTopologyDisplay.DisplayDevice.IsConnected}");
                        Console.WriteLine($"Mosaic IsCluster: {possibleGridTopologyDisplay.DisplayDevice.IsCluster}");
                        Console.WriteLine($"Mosaic IsDynamic: {possibleGridTopologyDisplay.DisplayDevice.IsDynamic}");
                        Console.WriteLine($"Mosaic IsOSVisible: {possibleGridTopologyDisplay.DisplayDevice.IsOSVisible}");
                        Console.WriteLine($"Mosaic IsMultiStreamRootNode: {possibleGridTopologyDisplay.DisplayDevice.IsMultiStreamRootNode}");
                        Console.WriteLine($"Mosaic IsPhysicallyConnected: {possibleGridTopologyDisplay.DisplayDevice.IsPhysicallyConnected}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.CloneImportance}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.IsDisplayWarped}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.IsIntensityModified}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.SourceDesktopRectangle}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.SourceToTargetRotation}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.SourceViewPortRectangle}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.TargetDisplayHeight}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.TargetDisplayWidth}");
                        Console.WriteLine($"Mosaic ScanOutInformation CloneImportance: {possibleGridTopologyDisplay.DisplayDevice.ScanOutInformation.TargetViewPortRectangle}");
                    }
                }

                // Then go through the displays in the profile and check they are made of displays
                // that currently are available.
                foreach (ProfileViewport profileViewport in Viewports)
                {
                    // If there are no TargetDisplays in a viewport, then return false
                    // cause thats invalid
                    if (profileViewport.TargetDisplays.Length == 0)
                        return false;

                    // For each profile, we want to make sure all TargetDisplays.DevicePath are in the list of 
                    // availableDevicePaths
                    foreach (ProfileViewportTargetDisplay profileViewportTargetDisplay in profileViewport.TargetDisplays)
                    {

                        // Check if the profiles are NVIDIA Mosaic Surround profiles
                        if (profileViewportTargetDisplay.SurroundTopology != null)
                        {
                            Console.WriteLine($"ProfileViewportTargetDisplay {profileViewportTargetDisplay.DisplayName} within Profile {Name} is a Mosaic profile");

                            bool validMosaicTopology = false;
                            // Ask Nvidia driver if the Mosaic SurroundTopology is possible
                            foreach (NvAPIWrapper.Mosaic.GridTopology mosaicTopology in possibleMosaicTopologies)
                            {
                                // we loop through the list of possible mosaic grid topologies to see if this profile
                                // is in there
                                if (mosaicTopology.Equals(profileViewportTargetDisplay.SurroundTopology.ToGridTopology()))
                                {
                                    validMosaicTopology = true;
                                    break;
                                }
                                
                            }
                            if (!validMosaicTopology)
                            {
                                Console.WriteLine($"ProfileViewportTargetDisplay {profileViewportTargetDisplay.DisplayName} is NOT a VALID mosaic display");
                                Console.WriteLine($"Profile {Name} is NOT a VALID profile and can be used! It IS NOT possible.");
                                return false;
                            }
                            
                            Console.WriteLine($"ProfileViewportTargetDisplay {profileViewportTargetDisplay.DisplayName} is a VALID mosaic profile");
                        }
                        else
                        {
                            Console.WriteLine($"ProfileViewportTargetDisplay {profileViewportTargetDisplay.DisplayName} within Profile {Name} is a normal windows profile");

                            // Check this DevicePath is in the list of availableTargetDisplays
                            if (!availableDevicePaths.Contains(profileViewportTargetDisplay.DevicePath))
                            {
                                // profileViewportTargetDisplay is a display that isn't available right now
                                // This means that this profile is currently now possible
                                // So we return that fact to the calling function.
                                Console.WriteLine($"ProfileViewportTargetDisplay {profileViewportTargetDisplay.DisplayName} is NOT a VALID windows display");
                                Console.WriteLine($"Profile {Name} is NOT a VALID profile and can be used! It IS NOT possible.");
                                return false;
                            }

                            Console.WriteLine($"ProfileViewportTargetDisplay {profileViewportTargetDisplay.DisplayName} is a VALID windows display");

                        }
                    }
                }

                Console.WriteLine($"Profile {Name} is a VALID profile and can be used! It IS possible.");

                return true;

            }
        }

        public string Name { get; set; }

        public ProfileViewport[] Viewports { get; set; } = new ProfileViewport[0];

        [JsonIgnore]
        public ProfileIcon ProfileIcon
        {
            get
            {
                if (_profileIcon != null)
                    return _profileIcon;
                else
                {
                    _profileIcon = new ProfileIcon(this);
                    return _profileIcon;
                }
            }
            set
            {
                _profileIcon = value;
            }

        }

        public string SavedProfileIconCacheFilename { get; set; }

        public List<string>  ProfileDisplayIdentifiers
        {
            get
            {
                if (_profileDisplayIdentifiers.Count == 0)
                {
                    _profileDisplayIdentifiers = DisplayIdentifier.GetCurrentDisplayIdentification();
                }
                return _profileDisplayIdentifiers;
            }
            set
            {
                if (value is List<string>)
                    _profileDisplayIdentifiers = value;
            }
        }

        [JsonConverter(typeof(CustomBitmapConverter))]
        public Bitmap ProfileBitmap
        {
            get
            {
                if (_profileBitmap != null)
                    return _profileBitmap;
                else
                {
                    _profileBitmap = this.ProfileIcon.ToBitmap(256, 256);
                    return _profileBitmap;
                }
            }
            set
            {
                _profileBitmap = value;
            }

        }

        [JsonConverter(typeof(CustomBitmapConverter))]
        public Bitmap ProfileTightestBitmap
        {
            get
            {
                if (_profileShortcutBitmap != null)
                    return _profileShortcutBitmap;
                else
                {
                    _profileShortcutBitmap = this.ProfileIcon.ToTightestBitmap();
                    return _profileShortcutBitmap;
                }
            }
            set
            {
                _profileShortcutBitmap = value;
            }

        }

        #endregion

        public static bool IsValidName(string testName)
        {
            foreach (ProfileItem loadedProfile in _allSavedProfiles)
            {
                if (loadedProfile.Name == testName)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidUUID(string testId)
        {
            string uuidV4Regex = @"/^[0-9A-F]{8}-[0-9A-F]{4}-4[0-9A-F]{3}-[89AB][0-9A-F]{3}-[0-9A-F]{12}$/i";
            Match match = Regex.Match(testId, uuidV4Regex, RegexOptions.IgnoreCase);
            if (match.Success)
                return true;
            else
                return false;
        }

        public bool IsValid()
        {

            if (Viewports != null &&
                ProfileIcon is Bitmap &&
                File.Exists(SavedProfileIconCacheFilename) &&
                ProfileBitmap is Bitmap &&
                ProfileTightestBitmap is Bitmap &&
                ProfileDisplayIdentifiers.Count > 0)
                return true;
            else 
                return false;
        }

        public bool CopyTo(ProfileItem profile, bool overwriteId = true)
        {
            if (!(profile is ProfileItem))
                return false;

            if (overwriteId == true)
                profile.UUID = UUID;

            // Copy all our profile data over to the other profile
            profile.Name = Name;
            profile.Viewports = Viewports;
            profile.ProfileIcon = ProfileIcon;
            profile.SavedProfileIconCacheFilename = SavedProfileIconCacheFilename;
            profile.ProfileBitmap = ProfileBitmap;
            profile.ProfileTightestBitmap = ProfileTightestBitmap;
            profile.ProfileDisplayIdentifiers = ProfileDisplayIdentifiers;
            return true;
        }

        public bool PreSave()
        {
            // Prepare our profile data for saving
            if (_profileDisplayIdentifiers.Count == 0)
            {
                _profileDisplayIdentifiers = DisplayIdentifier.GetCurrentDisplayIdentification();
            }

            // Return if it is valid and we should continue
            return IsValid();
        }


        // The public override for the Object.Equals
        public override bool Equals(object obj)
        {
            return this.Equals(obj as ProfileItem);
        }

        // Profiles are equal if their Viewports are equal
        public bool Equals(ProfileItem other)
        {

            // If parameter is null, return false.
            if (Object.ReferenceEquals(other, null))
                return false;

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, other))
                return true;

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != other.GetType())
                return false;
            
            // Check whether the profiles' properties are equal
            // We need to exclude the name as the name is solely for saving to disk
            // and displaying to the user. 
            // Two profiles are equal only when they have the same viewport data
            if (Viewports.SequenceEqual(other.Viewports))
                return true;
            else
                return false;
        }

        // If Equals() returns true for this object compared to  another
        // then GetHashCode() must return the same value for these objects.
        public override int GetHashCode()
        {

            // Get hash code for the Viewports field if it is not null.
            int hashViewports = Viewports == null ? 0 : Viewports.GetHashCode();

            //Calculate the hash code for the product.
            return hashViewports;

        }


        public override string ToString()
        {
            return (Name ?? Language.UN_TITLED_PROFILE);
        }

        private static string GetValidFilename(string uncheckedFilename)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {
                uncheckedFilename = uncheckedFilename.Replace(c.ToString(), "");
            }
            return uncheckedFilename;
        }
        
    }

    // Custom comparer for the Profile class
    // Allows us to use 'Contains'
    class ProfileComparer : IEqualityComparer<ProfileItem>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(ProfileItem x, ProfileItem y)
        {

            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            // Check whether the profiles' properties are equal
            // We need to exclude the name as the name is solely for saving to disk
            // and displaying to the user. 
            // Two profiles are equal only when they have the same viewport data
            if (x.Viewports.Equals(y.Viewports))
                return true;
            else
                return false;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.
        public int GetHashCode(ProfileItem profile)
        {

            // Check whether the object is null
            if (Object.ReferenceEquals(profile, null)) return 0;

            // Get hash code for the Viewports field if it is not null.
            int hashViewports = profile.Viewports == null ? 0 : profile.Viewports.GetHashCode();

            //Calculate the hash code for the product.
            return hashViewports;

        }

    }
}