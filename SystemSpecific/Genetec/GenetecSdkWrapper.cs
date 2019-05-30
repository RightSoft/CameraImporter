﻿using CameraImporter.Extensions;
using CameraImporter.Model.Genetec;
using CameraImporter.Shared;
using CameraImporter.Shared.Interface;
using CameraImporter.SystemSpecific.Genetec.Interface;
using CameraImporter.ViewModel;
using Genetec.Sdk;
using Genetec.Sdk.Entities;
using Genetec.Sdk.Entities.Video;
using Genetec.Sdk.EventsArgs;
using Genetec.Sdk.Queries;
using Genetec.Sdk.Workflows.UnitManager;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading.Tasks;

namespace CameraImporter.SystemSpecific.Genetec
{
    public class GenetecSdkWrapper : IGenetecSdkWrapper
    {
        private Engine _mSdkEngine;

        private readonly List<EntityModel> _existingCameras = new List<EntityModel>();
        private readonly List<EntityModel> _availableArchivers = new List<EntityModel>();
        private List<string> _allCameraGuidsAfterAddingNewCameras = new List<string>();

        public event EventHandler<IsLoggedInEventArgs> IsLoggedIn;
        public event EventHandler<AvailableArchiversFoundEventArgs> AvailableArchiversFound;
        public event EventHandler<EntityModel> AddingCameraCompleted;
        public event EventHandler<ExistingCameraListFoundEventArgs> ExistingCameraListFound;

        public void Init()
        {
            _mSdkEngine = new Engine
            {
                ClientCertificate = "KxsD11z743Hf5Gq9mv3+5ekxzemlCiUXkTFY5ba1NOGcLCmGstt2n0zYE9NsNimv"
            };

            _mSdkEngine.LoggedOn += SdkEngine_LoggedOn;
            _mSdkEngine.LoggedOff += SdkEngine_LoggedOff;
            _mSdkEngine.LogonFailed += SdkEngine_LogonFailed;
        }

        public void Dispose()
        {
            SdkAssemblyLoader.Stop();
        }

        public void FetchAvailableCameras()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(_existingCameras.Clear);

            var camerasQuery = _mSdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
            camerasQuery?.EntityTypeFilter.Add(EntityType.Camera);

            camerasQuery?.BeginQuery(OnCameraQueryReceived, camerasQuery);
        }

        private void OnCameraQueryReceived(IAsyncResult ar)
        {
            var cameraQuery = ar.AsyncState as EntityConfigurationQuery;
            var results = cameraQuery?.EndQuery(ar);

            foreach (DataRow dataRow in results.Data.Rows)
            {
                Guid cameraguid = (Guid)dataRow[0];
                if (_existingCameras.Any(o => o.EntityGuid == cameraguid))
                {
                    continue;
                }

                var cameraEntity = _mSdkEngine.GetEntity<Camera>(cameraguid);
                if (cameraEntity == null)
                {
                    continue;
                }

                _existingCameras.Add(new EntityModel
                {
                    EntityGuid = cameraguid,
                    EntityName = cameraEntity.Name,
                });
            }

            ExistingCameraListFound?.Invoke(this, new ExistingCameraListFoundEventArgs
            (
                _existingCameras.Any(),
                _existingCameras
            ));
        }

        public void FetchAvailableArchivers()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(_availableArchivers.Clear);

            var archiversQuery = _mSdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
            archiversQuery?.EntityTypeFilter.Add(EntityType.Role, (byte)RoleType.Archiver);
            archiversQuery?.BeginQuery(OnArchiverQueryReceived, archiversQuery);
        }

        private void VideoUnitManager_EnrollmentStatusChanged(object sender, UnitEnrolledEventArgs e)
        {
            if (e.EnrollmentResult == EnrollmentResult.Added)
            {
                AddingCameraCompleted?.Invoke(this, new EntityModel { EntityName = e.Address, EntityGuid = e.Unit });
            }
        }

        private void OnArchiverQueryReceived(IAsyncResult ar)
        {
            var archiversQuery = ar.AsyncState as EntityConfigurationQuery;
            var results = archiversQuery?.EndQuery(ar);

            if (results == null)
            {
                return;
            }

            foreach (DataRow dataRow in results.Data.Rows)
            {
                Guid archiverGuid = (Guid)dataRow[0];
                var archiverEntity = _mSdkEngine.GetEntity<Role>(archiverGuid);
                if (archiverEntity == null)
                {
                    continue;
                }

                _availableArchivers.Add(new EntityModel
                {
                    EntityGuid = archiverGuid,
                    EntityName = archiverEntity.Name,
                });
            }

            AvailableArchiversFound?.Invoke(this, new AvailableArchiversFoundEventArgs
            (
                _availableArchivers.Any(),
                _availableArchivers
            ));
        }

        private void SdkEngine_LoggedOn(object sender, LoggedOnEventArgs e)
        {
            IsLoggedIn?.Invoke(this, new IsLoggedInEventArgs("SDK Logged on", true));
            _mSdkEngine.VideoUnitManager.EnrollmentStatusChanged += VideoUnitManager_EnrollmentStatusChanged;
        }

        private void SdkEngine_LoggedOff(object sender, LoggedOffEventArgs e)
        {
            IsLoggedIn?.Invoke(this, new IsLoggedInEventArgs("SDK Logged off", false));
            _mSdkEngine.VideoUnitManager.EnrollmentStatusChanged -= VideoUnitManager_EnrollmentStatusChanged;
        }

        private void SdkEngine_LogonFailed(object sender, LogonFailedEventArgs e)
        {
            IsLoggedIn?.Invoke(this, new IsLoggedInEventArgs($"SDK Logon Failed. Reason: {e.FormattedErrorMessage}", false));
        }

        public async Task<bool> AddCamera(GenetecCamera cameraData, ILogger logger, SettingsData settingsData)
        {
            var videoUnitProductInfo =
                _mSdkEngine.VideoUnitManager
                    .FindProductsByManufacturer(cameraData.Manufacturer)
                    .FirstOrDefault(x => x.ProductType == cameraData.ProductType);

            if (!IPAddress.TryParse(cameraData.Ip, out IPAddress unitAddress))
            {
                logger.Log($"Unable to parse Ip Address for {cameraData.CameraName}", LogLevel.Error);
                return false;
            }

            var ip = new IPEndPoint(unitAddress, cameraData.Port);
            var addVideoUnitInfos = new AddVideoUnitInfo(videoUnitProductInfo, ip, false)
            {
                UserName = cameraData.UserName,
                Password = CreateSecureString(cameraData.Password)
            };

            AddUnitResponse response =
                await _mSdkEngine.VideoUnitManager.AddVideoUnit(addVideoUnitInfos, settingsData.Archiver.EntityGuid);

            if (response != null)
            {
                if (!response.Error.Equals(Error.None))
                {
                    logger.Log(
                        $"Response{Environment.NewLine}Error: {response.Error} {Environment.NewLine}Missing Information: {response.MissingInformation}", LogLevel.Error);
                }
            }
            else
            {
                logger.Log($"There was no response from the server for the adding {cameraData.CameraName} task", LogLevel.Info);
            }

            return true;
        }

        public bool UpdateAddedCameraSettings(GenetecCamera cameraData, ILogger logger)
        {
            logger.Log(
                $"\nWill now attpemt to add settings for: {cameraData.CameraName}",
                LogLevel.Warning);

            var currentCameraGuid = _existingCameras.Select(p => p.EntityGuid.ToString()).ToList().Where(p => p.Contains(cameraData.Guid)).FirstOrDefault();

            Camera cameraToBeUpdated = null;
            if (currentCameraGuid != null && !string.IsNullOrEmpty(currentCameraGuid))
            {
                cameraToBeUpdated = (Camera)_mSdkEngine.GetEntity(Guid.Parse(currentCameraGuid));
            }

            if (cameraToBeUpdated != null)
            {
                cameraToBeUpdated.RecordingConfiguration.RetentionPeriod =
                    new TimeSpan(TryToGetRetentionValue(cameraData.Stream1Retention, logger), 0, 0, 0);

                List<VideoStream> videoStreams = new List<VideoStream>();

                var stream1AlgorithmType = MapImportedCamAlgorithmType(cameraData.Stream1Codec);
                var stream2AlgorithmType = MapImportedCamAlgorithmType(cameraData.Stream2Codec);

                bool isUseFirstStream = false;
                bool isUseSecondStream = false;

                foreach (Guid guid in cameraToBeUpdated.Streams)
                {
                    VideoStream videoStream = _mSdkEngine.GetEntity(guid) as VideoStream;

                    if (videoStream == null || !videoStream.VideoCompressions.Any() || videoStream.VideoCompressions.First().Schedule == null) continue;

                    var schedule = videoStream.VideoCompressions.First().Schedule;
                    var capabilities = videoStream.VideoCompressionCapabilities;

                    if (stream1AlgorithmType == videoStream.VideoCompressionAlgorithm)
                        isUseFirstStream = true;
                    else if (stream2AlgorithmType == videoStream.VideoCompressionAlgorithm)
                        isUseSecondStream = true;

                    videoStream.SetFrameRate(schedule, TryToGetFpsValue(isUseFirstStream ? cameraData.Stream1Fps : cameraData.Stream2Fps, capabilities, logger));
                    videoStream.SetResolution(schedule, TryToGetResolution(isUseFirstStream ? cameraData.Stream1Codec : cameraData.Stream2Fps, isUseFirstStream ? cameraData.Stream1Resolution : cameraData.Stream2Resolution, capabilities, logger));
                }
            }
            else
            {
                logger.Log($"Settings update completed for camera {cameraData.CameraName}\n", LogLevel.Info);
            }

            logger.Log($"Settings update completed for camera {cameraData.CameraName}\n", LogLevel.Info);
            return true;
        }

        private VideoCompressionAlgorithmType MapImportedCamAlgorithmType(string codec)
        {
            if (codec.Contains(VideoCompressionAlgorithmType.H264.ToString(), StringComparison.OrdinalIgnoreCase))
                return VideoCompressionAlgorithmType.H264;
            else if (codec.Contains(VideoCompressionAlgorithmType.HEVC.ToString(), StringComparison.OrdinalIgnoreCase))
                return VideoCompressionAlgorithmType.HEVC;
            else if (codec.Contains(VideoCompressionAlgorithmType.Jpeg.ToString(), StringComparison.OrdinalIgnoreCase))
                return VideoCompressionAlgorithmType.Jpeg;
            else if (codec.Contains(VideoCompressionAlgorithmType.Mpeg2.ToString(), StringComparison.OrdinalIgnoreCase))
                return VideoCompressionAlgorithmType.Mpeg2;
            else if (codec.Contains(VideoCompressionAlgorithmType.Mpeg4.ToString(), StringComparison.OrdinalIgnoreCase))
                return VideoCompressionAlgorithmType.Mpeg4;
            else if (codec.Contains(VideoCompressionAlgorithmType.Wavelet.ToString(), StringComparison.OrdinalIgnoreCase))
                return VideoCompressionAlgorithmType.Wavelet;
            else return VideoCompressionAlgorithmType.Unknown;
        }

        private StreamSupportedResolution TryToGetResolution(string codec, string resolution, VideoCompressionCapabilities capabilities, ILogger logger)
        {
            var supportedResolution = capabilities.SupportedResolutions.Where(p => p.ToString() == resolution).First();

            if (supportedResolution == null)
            {
                logger.Log($"Resolution {resolution} isn't supported for this camera. Value is set to default {capabilities.SupportedResolutions.First().ToString()}", LogLevel.Warning);
                supportedResolution = capabilities.SupportedResolutions.First();
            }

            return supportedResolution;
        }

        private int TryToGetFpsValue(string stream1Fps, VideoCompressionCapabilities capabilities, ILogger logger)
        {
            if (int.TryParse(stream1Fps, out int parsedFpsValue))
            {
                if (capabilities.MaxFrameRate > parsedFpsValue && parsedFpsValue > capabilities.MinFrameRate)
                    return parsedFpsValue;
                else
                {
                    logger.Log($"Frame Rate value {stream1Fps} isn't in available range of {capabilities.MinFrameRate} and {capabilities.MaxFrameRate}. Default value {capabilities.MinFrameRate} is being used", LogLevel.Warning);
                    return capabilities.MinFrameRate;
                }
            }
            else
            {
                logger.Log($"Frame Rate value {stream1Fps} can't get converted to integer. Default value {capabilities.MinFrameRate} is being used", LogLevel.Warning);
                return capabilities.MinFrameRate;
            }
        }

        private int TryToGetRetentionValue(string retention, ILogger logger)
        {
            if (int.TryParse(retention, out int parsedRetentionValue))
            {
                return parsedRetentionValue;
            }
            else
            {
                logger.Log($"Retention value {retention} can't get converted to integer. Default value 10 is being used", LogLevel.Warning);
                return 10;
            }
        }

        public bool CheckIfServerExists(string settingsDataServerName, out string availableServerNames)
        {
            throw new NotImplementedException();
        }

        public List<GenetecCamera> CheckIfImportedCamerasExists(List<GenetecCamera> cameraList, ILogger logger)
        {
            var cameraNamesList = _existingCameras.Select(x => x.EntityName).ToList();
            return cameraList.Where(p => cameraNamesList.Contains(p.Ip)).ToList();
        }

        public void Login(SettingsData settingsData)
        {
            _mSdkEngine.BeginLogOn(settingsData.ServerAddress, settingsData.UserName, settingsData.Password);
        }

        private SecureString CreateSecureString(string str)
        {
            var sec = new SecureString();

            if (!string.IsNullOrEmpty(str))
            {
                str.ToCharArray().ToList().ForEach(sec.AppendChar);
            }

            return sec;
        }
    }

    public class ExistingCameraListFoundEventArgs
    {
        public bool IsExistingCamerasFound;
        public List<EntityModel> ExistingCameras;

        public ExistingCameraListFoundEventArgs(bool isExistingCamerasFound, List<EntityModel> existingCameras)
        {
            IsExistingCamerasFound = isExistingCamerasFound;
            ExistingCameras = existingCameras;
        }
    }

    public class AvailableArchiversFoundEventArgs : EventArgs
    {
        public bool IsArchiversFound;
        public List<EntityModel> AvailableArchivers;

        public AvailableArchiversFoundEventArgs(bool isArchiversFound, List<EntityModel> availableArchivers)
        {
            IsArchiversFound = isArchiversFound;
            AvailableArchivers = availableArchivers;
        }
    }

    public class IsLoggedInEventArgs : EventArgs
    {
        public string Message;
        public bool IsLoggedIn;

        public IsLoggedInEventArgs(string message, bool isLoggedIn)
        {
            Message = message;
            IsLoggedIn = isLoggedIn;
        }
    }
}