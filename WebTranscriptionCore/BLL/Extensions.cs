using System.Globalization;

namespace WebTranscriptionCore {
	public static class Extensions {
		public static bool In(this FlowStepStatus value, params FlowStepStatus[] values) {
			for (int i = 0; i < values.Length; i++) {
				if (value == values[i]) return true;
			}
			return false;
		}
		public static Clob ToClob(this string value) {
			if (value == null) return null;
			return new Clob(value);
		}

		public static string ToStringGB(this double value) {
			return value.ToString(CultureInfo.GetCultureInfo("en-GB"));
		}
	}
}