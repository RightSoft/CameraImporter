using System.Collections.Generic;
using CameraImporter.Model;
using CameraImporter.Model.Genetec;

namespace CameraImporter.Shared.Interface
{
    public interface ICsvToCameraParser
    {
        List<GenetecCamera> Parse(string content);
    }
}