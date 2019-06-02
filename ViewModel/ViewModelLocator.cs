using CameraImporter.Shared;
using CameraImporter.Shared.Interface;
using CameraImporter.SystemSpecific.Genetec;
using CameraImporter.SystemSpecific.Genetec.Interface;
using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;

namespace CameraImporter.ViewModel
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<ExistingCameraConfirmationPopupViewModel>();
            SimpleIoc.Default.Register<IFileProcessor, FileProcessor>();
            SimpleIoc.Default.Register<ICsvToCameraParser, CsvToCameraToCameraParser>();
            SimpleIoc.Default.Register<ILogger, Logger>();
            SimpleIoc.Default.Register<IFileLoader, FileLoader>();
            SimpleIoc.Default.Register<IFileSystemWrapper, FileSystemWrapper>();
            SimpleIoc.Default.Register<IGenetecSdkWrapper, GenetecSdkWrapper>();
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        public ExistingCameraConfirmationPopupViewModel ExistingCameraConfirmationPopup
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ExistingCameraConfirmationPopupViewModel>();
            }
        }
    }
}