using System;
using System.ComponentModel;

namespace CameraImporter.Extensions
{
    public static class EnumExtension
    {
        public static string GetDescription(this Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var attribute = value.GetAttribute<DescriptionAttribute>();

            return (attribute == null) ? value.ToString() : attribute.Description;
        }

        private static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            if (value == null)
            {
                return null;
            }

            var memberInfo = value.GetType().GetMember(value.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);

            return (T)attributes[0];
        }
    }
}
