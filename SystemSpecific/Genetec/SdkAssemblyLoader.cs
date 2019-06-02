using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace CameraImporter.SystemSpecific.Genetec
{
    public static class SdkAssemblyLoader
    {
        #region Private Fields

        private const string InstallationRegistryKey = @"SOFTWARE\Genetec\Security Center\";

        private const string InstallationRegistryKeyX32Bit = @"SOFTWARE\Wow6432Node\Genetec\Security Center\";

        private const string SCInstallPathName = "InstallDir";

        private const string SdkEnvironmentalVariable = "GSC_SDK";

        private const string SdkInstallPathName = "Installation Path";

        private static string SdkLocation = string.Empty;

        #endregion Private Fields

        #region Public Methods

        public static void Start()
        {
            SdkLocation = GetSdkLocation();
            ValidateAndTraceSdkLocation(SdkLocation);
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public static void Stop()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }

        #endregion Public Methods

        #region Private Methods

        private static string GetLatestSdkLocationFromRegistery()
        {
            string sdklocation = String.Empty;
            var locations = new List<string> { InstallationRegistryKey, InstallationRegistryKeyX32Bit };

            foreach (var registryKeyName in locations)
            {
                using (var registryKey = Registry.LocalMachine.OpenSubKey(registryKeyName))
                {
                    if (registryKey == null)
                    {
                        continue;
                    }
                    var subKeyName = registryKey.GetSubKeyNames()
                        .Select(s =>
                        {
                            if (Version.TryParse(s, out Version v))
                            {
                                using (var registryKeyValue = registryKey.OpenSubKey(s))
                                {
                                    if (registryKeyValue == null)
                                    {
                                        return null;
                                    }

                                    //try to locate SDK installation path first. If the SDK isn't installed, still try to locate the SC folder
                                    var location = registryKeyValue.GetValue(SdkInstallPathName)?.ToString();
                                    if (string.IsNullOrWhiteSpace(location) || !Directory.Exists(location))
                                    {
                                        location = registryKeyValue.GetValue(SCInstallPathName)?.ToString();
                                        if (string.IsNullOrWhiteSpace(location) || !Directory.Exists(location))
                                        {
                                            return null;
                                        }
                                    }

                                    return new
                                    {
                                        Version = v,
                                        InstallationPath = location
                                    };
                                }
                            }
                            return null;
                        })
                        .Where(v => v != null)
                        .OrderByDescending(v => v?.Version)
                        .FirstOrDefault();

                    if (subKeyName == null)
                    {
                        continue;
                    }

                    sdklocation = subKeyName.InstallationPath;
                    break;
                }
            }

            return sdklocation;
        }

        /// <summary>
        /// Gets the SDK location using the following strategies.
        /// 1) Load the SDK folder location using the .exe.config file
        /// 2) Load the Sdk folder location using the Environmental Variable GSC_SDK
        /// 3) Load the latest Sdk or SC folder location using the Registry.
        /// </summary>
        /// <returns></returns>
        private static string GetSdkLocation()
        {
            // The Sdk location is determined by the .exe.config file first.
            // The location of the SDK installation folder is determined by the environmental variable second; if it is not set, then
            // the location will be obtained from the registry. In the case there is more than one SDK version installed, the location
            // of the latest version will be used to resolve.
            var firstLocation = System.Configuration.ConfigurationManager.AppSettings["SdkLocation"];
            if (!string.IsNullOrEmpty(firstLocation))
            {
                return firstLocation;
            }

            var secondLocation = Environment.GetEnvironmentVariable(SdkEnvironmentalVariable);
            if (secondLocation != null)
            {
                return secondLocation;
            }

            string location = GetLatestSdkLocationFromRegistery();
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ApplicationException("Could not locate Security Center installation folder.");
            }
            return location;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;
            AssemblyName assemblyName = new AssemblyName(args.Name);

            if (string.IsNullOrEmpty(SdkLocation) ||
                assemblyName.Name.ToLower().EndsWith(".resources") ||
                assemblyName.Name.ToLower().EndsWith(".xmlserializers"))
            {
                return null;
            }

            string assemblyPath = Path.Combine(SdkLocation, assemblyName.Name + ".dll");
            try
            {
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    assembly = File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
                    if (assembly == null)
                    {
                        string message =
                            string.Format("The assembly resolver was unable to locate the specified assembly: {0}" +
                            " in the location: {1}", assemblyPath, SdkLocation);
                        // Different logic can be used to handle this case, this is just an example.
                        Console.WriteLine(message);
                    }
                }
            }
            catch (Exception e)
            {
                // Different logic can be used to handle this case, this is just an example.
                Console.WriteLine("An exception was caught trying to locate a specified assembly: {0}. Description {1}", args.Name, e.Message);
                assembly = null;
            }

            return assembly;
        }

        private static void ValidateAndTraceSdkLocation(string sdkLocation)
        {
            if (string.IsNullOrEmpty(sdkLocation))
            {
                string message =
                    "An Error Occured. The Sdk location found is empty. Ensure the Genetec Security Center SDK is installed.";
                throw new DirectoryNotFoundException(message);
            }
            if (!Directory.Exists(sdkLocation))
            {
                string message =
                    "An Error Occured. The Sdk folder on the computer does not exist.  Ensure the Genetec Security Center SDK is installed.";
                throw new DirectoryNotFoundException(message);
            }
        }

        #endregion Private Methods
    }
}