using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;

namespace WebTranscriptionCore {

	public static class Kenta_Utils_Extensions {
		public static string Cleanup(this string str, bool blankAsNull = true) {
			if (str == null) return blankAsNull ? null : "";
			str = (str ?? "").Trim();
			if (str.Length == 0 && blankAsNull) return null;
			return str;
		}
		public static string GetDescription(this Enum value) {
			Type t = value.GetType();
			string v = value.ToString();
			FieldInfo fi = t.GetField(v);
			if (fi == null) return v;
			DescriptionAttribute attr = fi.Get<DescriptionAttribute>();
			return (attr == null) ? v : attr.Description;
		}
		public static T Get<T>(this MemberInfo minfo, bool inherit = false) where T : Attribute => (minfo.GetCustomAttributes(typeof(T), inherit) as T[]).FirstOrDefault();

		public static bool IsGenericOf(this Type type, Type parent) {
			if (type.IsInterface != parent.IsInterface) return false;
			while (type != null) {
				if (type.IsGenericType && type.GetGenericTypeDefinition() == parent) return true;
				type = type.BaseType;
			}
			return false;
		}

		public static String ToLdapForm(this SecurityIdentifier si) {
			Byte[] buff = new Byte[si.BinaryLength];
			si.GetBinaryForm(buff, 0);
			return "\\" + buff.ToString("\\", x => x.ToString("x2"));
		}

		public static String ToString<T>(this T[] array, String glue, Func<T, String> converter) {
			if (array.Length == 0) return "";
			StringBuilder sb = new StringBuilder();
			sb.Append(converter(array[0]));
			for (Int32 n = 1; n < array.Length; n++) {
				sb.Append(glue);
				sb.Append(converter(array[n]));
			}
			return sb.ToString();
		}

	}
}