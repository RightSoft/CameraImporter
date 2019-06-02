using System;

namespace CameraImporter.Model
{   
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
