using CameraImporter.Model.Genetec;
using CameraImporter.Shared;
using CameraImporter.Shared.Interface;
using CameraImporter.SystemSpecific.Genetec.Interface;
using CameraImporter.ViewModel;
using Genetec.Sdk;
using Genetec.Sdk.Entities;
using Genetec.Sdk.Entities.Video;
using Genetec.Sdk.Queries;
using Genetec.Sdk.Workflows.UnitManager;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Genetec.Sdk.EventsArgs;

namespace CameraImporter.SystemSpecific.Genetec
{
    public class GenetecSdkWrapper : IGenetecSdkWrapper
    {
        private Engine m_sdkEngine;

        private readonly List<EntityModel> _existingCameras = new List<EntityModel>();
        private readonly List<EntityModel> _availableArchivers = new List<EntityModel>();

        public event EventHandler<IsLoggedInEventArgs> IsLoggedIn;
        public event EventHandler<AvailableArchiversFoundEventArgs> AvailableArchiversFound;
        public event EventHandler<ExistingCameraListFoundEventArgs> ExistingCameraListFound;

        public void Init()
        {
            m_sdkEngine = new Engine
            {
                ClientCertificate = "KxsD11z743Hf5Gq9mv3+5ekxzemlCiUXkTFY5ba1NOGcLCmGstt2n0zYE9NsNimv"
            };

            m_sdkEngine.LoggedOn += SdkEngine_LoggedOn;
            m_sdkEngine.LoggedOff += SdkEngine_LoggedOff;
            m_sdkEngine.LogonFailed += SdkEngine_LogonFailed;
        }

        public void Dispose()
        {
            SdkAssemblyLoader.Stop();
        }

        public void FetchAvailableCameras()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(_existingCameras.Clear);

            var camerasQuery = m_sdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
            camerasQuery?.EntityTypeFilter.Add(EntityType.Camera);
            camerasQuery?.BeginQuery(OnCameraQueryReceived, camerasQuery);
        }

        //public void FetchAvailableCamerasReturnList()
        //{
        //    var camerasQuery = m_sdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
        //    camerasQuery?.EntityTypeFilter.Add(EntityType.Camera);
        //    QueryCompletedEventArgs results = camerasQuery?.Query();

        //    Collection<GenetecCamera> cameras = new Collection<GenetecCamera>();

        //    if (results.Data != null)
        //    {
        //        List<Guid> camGuids = results.Data.Rows.Cast<DataRow>().Select(row => (Guid)row[0]).ToList();

        //        // Add all cameras found by the SDK engine to the local list
        //        foreach (Guid camId in camGuids)
        //        {
        //            var cam = (Camera)m_sdkEngine.GetEntity(camId);
        //            if (cam == null)
        //                continue;

        //            cam.RecordingConfiguration.RetentionPeriod = new TimeSpan(10, 0, 0, 0);

        //            var attr = cam.ScheduledVideoAttributes;

        //            ReadOnlyCollection<Guid> allStreams = cam.Streams;

        //            ///m_sdkEngine.DeleteEntity(cam);
        //            foreach (Guid guid in allStreams)
        //            {
        //                VideoStream videoStream = m_sdkEngine.GetEntity(guid) as VideoStream;

        //                var schedule = videoStream.VideoCompressions[0].Schedule;
        //                videoStream.SetFrameRate(schedule, 10);
        //                videoStream.SetResolution(schedule, 10);
        //                var resolutions = videoStream.VideoCompressionCapabilities.SupportedResolutions;
        //                var encoder = videoStream.VideoCompressionAlgorithm;
        //                //videoStream.VideoCompressionAlgorithm = VideoCompressionAlgorithmType.Mpeg4;
        //            }

        //            var unit = cam.Unit;

        //            Dispatcher.BeginInvoke(new Action(() =>
        //            {
        //                Cameras.Add(new CameraModel
        //                {
        //                    CameraGuid = camId,
        //                    CameraName = cam.Name,
        //                    CameraIcon = cam.GetIcon(true)
        //                });
        //            }));
        //        }
        //    }


        //}

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

                var cameraEntity = m_sdkEngine.GetEntity<Camera>(cameraguid);
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

            var archiversQuery = m_sdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
            archiversQuery?.EntityTypeFilter.Add(EntityType.Role, (byte)RoleType.Archiver);
            archiversQuery?.BeginQuery(OnArchiverQueryReceived, archiversQuery);
        }

        private void VideoUnitManager_EnrollmentStatusChanged(object sender, UnitEnrolledEventArgs e)
        {
            if(e.EnrollmentResult==EnrollmentResult.Added)
            {
                
            }

            //DisplayLog("Enrollement status changed: " + e.EnrollmentResult);
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
                var archiverEntity = m_sdkEngine.GetEntity<Role>(archiverGuid);
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
            m_sdkEngine.VideoUnitManager.EnrollmentStatusChanged += VideoUnitManager_EnrollmentStatusChanged;
        }

        private void SdkEngine_LoggedOff(object sender, LoggedOffEventArgs e)
        {
            IsLoggedIn?.Invoke(this, new IsLoggedInEventArgs("SDK Logged off", false));
            m_sdkEngine.VideoUnitManager.EnrollmentStatusChanged -= VideoUnitManager_EnrollmentStatusChanged;
        }

        private void SdkEngine_LogonFailed(object sender, LogonFailedEventArgs e)
        {
            IsLoggedIn?.Invoke(this, new IsLoggedInEventArgs($"SDK Logon Failed. Reason: {e.FormattedErrorMessage}", false));
        }

        public async Task<bool> AddCamera(GenetecCamera cameraData, ILogger logger, SettingsData settingsData)
        {
            var videoUnitProductInfo =
                m_sdkEngine.VideoUnitManager
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
                await m_sdkEngine.VideoUnitManager.AddVideoUnit(addVideoUnitInfos, settingsData.Archiver.EntityGuid);

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


            logger.Log($"Settings update completed for camera {cameraData.CameraName}\n", LogLevel.Info);
            return true;
        }

        public bool CheckIfServerExists(string settingsDataServerName, out string availableServerNames)
        {
            throw new NotImplementedException();
        }

        public List<GenetecCamera> CheckIfImportedCamerasExists(List<GenetecCamera> cameraList, ILogger logger)
        {
            return cameraList.Where(p => _existingCameras.Select(x => x.EntityName).Contains(p.CameraName)).ToList();
        }

        public void Login(SettingsData settingsData)
        {
            m_sdkEngine.BeginLogOn(settingsData.ServerAddress, settingsData.UserName, settingsData.Password);
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