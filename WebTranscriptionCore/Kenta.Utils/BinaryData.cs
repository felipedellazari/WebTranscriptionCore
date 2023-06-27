using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Kenta.Utils.Data;

namespace Kenta.Utils {

	/// <summary>
	/// Classe utilizada para guardar dados binários, de uma forma que permita a comparação e formatação
	/// </summary>
	[Serializable]
	public struct BinaryData :  IComparable, IComparable<BinaryData>, IEquatable<BinaryData>, ISqlMapped {

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static readonly BinaryData Empty = new BinaryData(new Byte[0]);

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData FromBase64(String base64) {
			if (base64 == null) throw new ArgumentNullException("base64");
			return new BinaryData(Convert.FromBase64String(base64));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData FromHexDecimal(String hexDecimal) {
			if (hexDecimal == null) throw new ArgumentNullException("hexDecimal");
			if ((hexDecimal.Length % 2) != 0) throw new ArgumentException("Argument must be multiple of 2");
			Byte[] buff = new Byte[hexDecimal.Length / 2];
			Int32 value = 0;
			for (Int32 i = 0; i < hexDecimal.Length; i++) {
				Char ch = hexDecimal[i];
				Int32 n = 0;
				if (ch < '0') throw new FormatException("Value contains a non-hexdecimal character");
				if (ch <= '9') { n = (Int32)ch - (Int32)'0'; goto @parsed; }
				if (ch < 'A') throw new FormatException("Value contains a non-hexdecimal character");
				if (ch <= 'F') { n = 10 + (Int32)ch - (Int32)'A'; goto @parsed; }
				if (ch < 'a') throw new FormatException("Value contains a non-hexdecimal character");
				if (ch <= 'f') { n = 10 + (Int32)ch - (Int32)'a'; goto @parsed; }
				throw new FormatException("Value contains a non-hexdecimal character");
				@parsed:
				if ((i & 1) == 0) {
					value = n;
				} else {
					buff[i / 2] = (Byte)((value << 4) + n);
				}
			}//for
			return new BinaryData(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		private Byte[] data;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public BinaryData(Byte[] data) {
			if (data == null) throw new ArgumentNullException("data");
			this.data = data;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public BinaryData(String data, Encoding encode = null) {
			if (data == null) throw new ArgumentNullException("data");
			if (encode == null) encode = Encoding.Unicode;
			this.data = encode.GetBytes(data);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public BinaryData(Stream data) {
			this.data = data.ReadAllBytes();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public Byte[] Data {
			get {
				return this.data;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public Int32 Length {
			get {
				if (this.data == null) return 0;
				return this.data.Length;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public Byte this[Int32 index] {
			get {
				return this.data[index];
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public void SaveToFile(String fileName) {
			File.WriteAllBytes(fileName, this.data);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public void SaveToRegistry(String keyName, String valueName) {
			Microsoft.Win32.Registry.SetValue(keyName, valueName, this.data);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public byte[] Compress() {
			byte[] buffer = this.data;
			using (var compress = new MemoryStream())
			using (var gz = new System.IO.Compression.GZipStream(compress, System.IO.Compression.CompressionMode.Compress)) {
				gz.Write(buffer, 0, buffer.Length);
				gz.Close();
				return compress.ToArray();
			}
			/*
		var ms = new MemoryStream(this.data.Length);
		var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress);
		gz.Write(this.data, 0, this.data.Length);
		//gz.Flush();
			gz.Close();
		return new BinaryData(ms.ToArray());
			 */
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public BinaryData Decompress() {
			Byte[] buff = new Byte[this.data.Length];
			var ms = new MemoryStream(this.data);
			var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
			buff = gz.ReadAllBytes();
			//Bug do GZipStream
			if (buff.Length == 0) buff = gz.ReadAllBytes();
			return new BinaryData(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public override Boolean Equals(Object obj) {
			if (!(obj is BinaryData)) return false;
			return ((IEquatable<BinaryData>)this).Equals(obj);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public override Int32 GetHashCode() {
			UInt32 x = 0;
			for (Int32 i = 0; i < this.data.Length; i++) {
				x = (x << 5) | this.data[i];
			}//for
			return (Int32)x;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		//public override String ToString() {
		//	return this.data.ToHexDecimal();
		//}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public Guid ToGuid() {
			return new Guid(this.data);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#region IFormattable Members
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		//String IFormattable.ToString(String format, IFormatProvider formatProvider) {
		//	return this.data.ToHexDecimal();
		//}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#endregion
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#region IComparable Members
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		Int32 IComparable.CompareTo(Object obj) {
			if (Object.ReferenceEquals(obj, null)) return 1;
			if (!(obj is BinaryData)) throw new ArgumentException("value is not a BinaryData");
			return ((IComparable<BinaryData>)this).CompareTo((BinaryData)obj);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#endregion
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#region IComparable<BinaryData> Members
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		Int32 IComparable<BinaryData>.CompareTo(BinaryData other) {
			Int32 x = this.data.Length.CompareTo(other.data.Length);
			if (x != 0) return x;
			for (Int32 i = 0; i < this.data.Length; i++) {
				x = this.data[i].CompareTo(other.data[i]);
				if (x != 0) return x;
			}//for
			return 0;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#endregion
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#region IEquatable<BinaryData> Members
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public Boolean Equals(BinaryData other) {
			return ArrayEquals(this.data, other.data);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
		#endregion
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static Boolean operator ==(BinaryData b1, BinaryData b2) {
			return ((IEquatable<BinaryData>)b1).Equals(b2);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static Boolean operator !=(BinaryData b1, BinaryData b2) {
			return !((IEquatable<BinaryData>)b1).Equals(b2);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static explicit operator Byte[](BinaryData value) {
			return value.data;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static explicit operator BinaryData(Byte[] value) {
			return new BinaryData(value);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static Boolean ArrayEquals(Byte[] b1, Byte[] b2) {
			if (Object.ReferenceEquals(b1, b2)) return true;
			if (b1.Length != b2.Length) return false;
			for (Int32 i = 0; i < b1.Length; i++) {
				if (b1[i] != b2[i]) return false;
			}//for
			return true;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData LoadFromFile(String fileName, Int64 offset = 0, Int32? count = null) {
			FileStream fs = null;
			try {
				fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				if (offset > fs.Length) throw new ArgumentOutOfRangeException("offset");
				if (count.HasValue) {
					if (((Int64)count.Value + offset) > fs.Length) throw new ArgumentOutOfRangeException("count");
				} else {
					count = checked((Int32)fs.Length);
				}//if
				Byte[] buff = new Byte[count.Value];
				fs.Position = offset;
				fs.Read(buff, 0, buff.Length);
				return new BinaryData(buff);
			} finally {
				if (fs != null) fs.Dispose();
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		Object ISqlMapped.GetValueForDbParameter(DatabaseType type) {
			return this.data;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData FromRegistry(String keyName, String valueName) {
			Byte[] buff = (Microsoft.Win32.Registry.GetValue(keyName, valueName, null) as Byte[]);
			if (buff == null) return BinaryData.Empty;
			return new BinaryData(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public Stream GetStream() {
			return new MemoryStream(this.data, false);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public String ToBase64() {
			return Convert.ToBase64String(this.data);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public Stream ToStream() {
			return new MemoryStream(this.data, false);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public BinaryData CalcMD5() {
			return SecurityUtils.MD5(this.data, 0, this.data.Length);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public BinaryData CalcSHA256(Byte[] salt = null) {
			Byte[][] dt = (salt == null) ? new Byte[][] { this.data } : new Byte[][] { this.data, salt };
			return SecurityUtils.SHA256(dt);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData FromStream(Stream stream, Boolean disposing = false) {
			var d = new BinaryData(stream.ReadAllBytes());
			if (disposing) stream.Dispose();
			return d;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public void WriteTo(Stream stream) {
			stream.Write(this.data, 0, this.data.Length);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

	}
}
