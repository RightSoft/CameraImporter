using CameraImporter.Model.Genetec;
using CameraImporter.Shared;
using CameraImporter.Shared.Interface;
using CameraImporter.SystemSpecific.Genetec.Interface;
using CameraImporter.ViewModel;
using Genetec.Sdk;
using Genetec.Sdk.Entities;
using Genetec.Sdk.Queries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace CameraImporter.SystemSpecific.Genetec
{
    public class GenetecSdkWrapper : IGenetecSdkWrapper
    {
        private Engine m_sdkEngine;

        private readonly Guid _fpsTranslationId = new Guid("d661e46c-dcb0-4402-b9b3-0e9e5d07ffd9");
        private readonly Guid _codecTranslationId = new Guid("b134d66d-db9c-4822-baa0-5ce4b04ec29a");
        private readonly Guid _resolutionTranslationId = new Guid("b0323065-599f-4d73-b18e-55c0193368bc");

        private List<EntityModel> _availableArchivers = new List<EntityModel>();
        private List<EntityModel> _availableCameras = new List<EntityModel>();


        private const string VIDEOSTREAM1_DISPLAYNAME = "Video stream 01";
        private const string VIDEOSTREAM2_DISPLAYNAME = "Video stream 02";

        public event EventHandler<IsLoggedInEventArgs> IsLoggedIn;
        public event EventHandler<AvailableArchiversFoundEventArgs> AvailableArchiversFound;

        private EntityModel m_selectedArchiverModel;
        private EntityModel m_selectedCameraModel;

        public GenetecSdkWrapper()
        {
            Debug.WriteLine("genetec constructor");

            SdkAssemblyLoader.Start();
        }

        public void Init()
        {
            m_sdkEngine = new Engine();

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
            System.Windows.Application.Current.Dispatcher.Invoke(_availableCameras.Clear);

            var camerasQuery = m_sdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
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
                if (_availableCameras.Any(o => o.EntityGuid == cameraguid))
                {
                    continue;
                }

                var cameraEntity = m_sdkEngine.GetEntity<Camera>(cameraguid);
                if (cameraEntity == null)
                {
                    continue;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    _availableCameras.Add(new EntityModel
                    {
                        EntityGuid = cameraguid,
                        EntityName = cameraEntity.Name,
                    });
                }));
            }
        }

        public void FetchAvailableArchivers()
        {
            var archiversQuery = m_sdkEngine.ReportManager.CreateReportQuery(ReportType.EntityConfiguration) as EntityConfigurationQuery;
            archiversQuery?.EntityTypeFilter.Add(EntityType.Role, (byte)RoleType.Archiver);
            archiversQuery?.BeginQuery(OnArchiverQueryReceived, archiversQuery);
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
        }

        private void SdkEngine_LoggedOff(object sender, LoggedOffEventArgs e)
        {
            IsLoggedIn?.Invoke(this, new IsLoggedInEventArgs("SDK Logged off", false));
        }

        private void SdkEngine_LogonFailed(object sender, LogonFailedEventArgs e)
        {
            IsLoggedIn?.Invoke(this, new IsLoggedInEventArgs($"SDK Logon Failed. Reason: {e.FormattedErrorMessage}", false));
        }

        public bool AddCamera(GenetecCamera cameraData, ILogger logger)
        {
            logger.Log($"Camera added successfully: {cameraData.CameraName}", LogLevel.Info);

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
            return null;
        }

        public void Login(SettingsData settingsData)
        {
            m_sdkEngine.BeginLogOn(settingsData.ServerAddress, settingsData.UserName, settingsData.Password);
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