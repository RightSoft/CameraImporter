using System.Collections.Generic;

namespace CameraImporter.Model
{
    public class CameraValidationSetting
    {
        public string ValidationType;
        public string TargetCameraProperty;
        public string MinValue;
        public string MaxValue;
        public string ObjectType;
        public List<string> PossibleValues;
    }
}
