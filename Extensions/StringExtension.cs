using System;

namespace CameraImporter.Extensions
{
    public static class StringExtension
    {
        public static string PermissiveSubstring(this string input, int startIndex, int length)
        {
            var output = input.Substring(startIndex, input.Length - startIndex <= length ? input.Length - startIndex : length);
            return output;
        }

        public static string Left(this string input, int length)
        {
            return input.PermissiveSubstring(0, length);
        }

        public static string Right(this string input, int length)
        {
            var output = input.Reverse().PermissiveSubstring(0, length).Reverse();
            return output;
        }

        public static string Reverse(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
