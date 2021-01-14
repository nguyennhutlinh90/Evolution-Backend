using Newtonsoft.Json;

using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Evolution_Backend
{
    internal static class Extension
    {
        public static T MapTo<T>(this object obj)
        {
            var jsonSerialize = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(jsonSerialize, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            });
        }

        public static T GetAttribute<TAttr, T>(this Enum value, Func<TAttr, T> expression) where TAttr : Attribute
        {
            var memberInfo = value.GetType().GetMember(value.ToString());

            if (!memberInfo.Any())
                return default;

            var attribute = Attribute.GetCustomAttribute(memberInfo[0], typeof(TAttr));
            if (attribute == null)
                return default;

            return expression((TAttr)attribute);
        }

        public static string GetDecription<T>(this T value, params object[] args) where T : Enum
        {
            var description = GetAttribute<DescriptionAttribute, string>(value, arg => arg.Description);
            if (string.IsNullOrEmpty(description))
                return description;
            return string.Format(description, args);
        }

        public static string GetName<T>(this int value, string nullValue = "") where T : Enum
        {
            try
            {
                return Enum.GetName(typeof(T), value)?.ToLower();
            }
            catch (Exception)
            {
                return nullValue;
            }
        }

        public static int? GetValue<T>(this string name) where T : Enum
        {
            try
            {
                name = Enum.GetNames(typeof(T)).FirstOrDefault(n => n.ToLower() == name.ToLower());
                return (int)Enum.Parse(typeof(T), name);
            }
            catch
            {
                return null;
            }
        }

        public static string HashPassword(this string password)
        {
            using (var algorithm = SHA1.Create())
            {
                var hashedBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}
