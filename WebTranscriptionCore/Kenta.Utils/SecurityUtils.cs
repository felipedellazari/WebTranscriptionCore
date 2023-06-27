using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Kenta.Utils {
	public static class SecurityUtils {

		private static Byte[] iv = new Byte[] { 0xA8, 0x2A, 0xEB, 0xF8, 0x83, 0x9D, 0x0B, 0xF8, 0x1E, 0xE9, 0xDD, 0x45, 0x38, 0x94, 0xD2, 0xCE };
		private static Byte[] key = new Byte[] { 0xA8, 0xEF, 0xB7, 0x55, 0x3C, 0x05, 0x21, 0x91, 0xED, 0x4C, 0x79, 0x22, 0xB7, 0xE9, 0xB1, 0xFA, 0x55, 0x5C, 0xA2, 0x5B, 0x48, 0x0E, 0xA1, 0x17, 0x6A, 0x5E, 0xF7, 0x69, 0x8C, 0xAC, 0xAB, 0x0E };
		private static UInt64[] crc64_tab = null;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		/// <summary>
		/// Criptografa o texto seriazado em Unicode utilizando EAS com uma chave padrão, e converte os dados binários para Base-64
		/// </summary>
		public static String Encrypt(String content, Encoding encode = null) {
			if (content == null) throw new ArgumentNullException("contet");
			if (encode == null) encode = Encoding.Unicode;
			Byte[] buff = encode.GetBytes(content);
			var enc = Aes.Create().CreateEncryptor(key, iv);
			buff = enc.TransformFinalBlock(buff, 0, buff.Length);
			return Convert.ToBase64String(buff, Base64FormattingOptions.None);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		/// <summary>
		/// Descriptografa o texto gerado previamente pela função <see cref="SecurityUtils.Encrypt">SecurityUtils.Encrypt</see>
		/// </summary>
		public static String Decrypt(String content, Encoding encode = null) {
			if (content == null) throw new ArgumentNullException("content");
			if (encode == null) encode = Encoding.Unicode;
			Byte[] buff = Convert.FromBase64String(content);
			var dec = Aes.Create().CreateDecryptor(key, iv);
			buff = dec.TransformFinalBlock(buff, 0, buff.Length);
			return encode.GetString(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		/// <summary>
		/// Criptografa os dados binários utilizando EAS com uma chave padrão
		/// </summary>
		public static BinaryData Encrypt(BinaryData content) {
			if (content == null) throw new ArgumentNullException("contet");
			var enc = Aes.Create().CreateEncryptor(key, iv);
			Byte[] buff = enc.TransformFinalBlock(content.Data, 0, content.Length);
			return new BinaryData(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		/// <summary>
		/// Descriptografa os dados binários gerados previamente pela função <see cref="SecurityUtils.Encrypt">SecurityUtils.Encrypt</see>
		/// </summary>
		public static BinaryData Decrypt(BinaryData content) {
			if (content == null) throw new ArgumentNullException("contet");
			var dec = Aes.Create().CreateDecryptor(key, iv);
			Byte[] buff = dec.TransformFinalBlock(content.Data, 0, content.Length);
			return new BinaryData(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		private static void GenerateCRC64Table() {
			var t = new UInt64[256];
			for (UInt64 i = 0; i < 256; ++i) {
				UInt64 crc = i;
				for (UInt64 j = 0; j < 8; ++j) {
					if ((crc & 1) == 1) {
						crc = (crc >> 1) ^ 0xC96C5795D7870F42;
					} else {
						crc >>= 1;
					}
				}//for
				t[i] = crc;
			}//for
			crc64_tab = t;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//


		public static UInt64 CRC64(Byte[] data, int index = 0, Int32? count = null) {
			if (crc64_tab == null) GenerateCRC64Table();
			UInt64 h = UInt64.MaxValue;
			Int32 a_length = count ?? data.Length;
			for (int i = index; a_length > 0; i++, a_length--) {
				h = (h >> 8) ^ crc64_tab[(byte)h ^ data[i]];
			}//for
			return h ^ UInt64.MaxValue;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static UInt64 CRC64(String content) {
			if (crc64_tab == null) GenerateCRC64Table();
			UInt64 h = UInt64.MaxValue;
			Int32 len = content.Length;
			for (int i = 0; len > 0; i++, len--) {
				Int32 ch = content[i];
				Byte b1 = unchecked((Byte)ch);
				Byte b2 = unchecked((Byte)(ch >> 8));
				h = (h >> 8) ^ crc64_tab[(byte)h ^ b1];
				h = (h >> 8) ^ crc64_tab[(byte)h ^ b2];
			}//for
			return h ^ UInt64.MaxValue;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static UInt64 CRC64(String content, Encoding encode) {
			return SecurityUtils.CRC64(encode.GetBytes(content));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData MD5(Byte[] content, Int32 offset = 0, Int32? count = null) {
			if (count == null) count = content.Length;
			var alg = System.Security.Cryptography.MD5.Create();
			return new BinaryData(alg.ComputeHash(content, offset, count.Value));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData MD5(String content, Encoding encode = null) {
			if (encode == null) encode = Encoding.UTF8;
			return SecurityUtils.MD5(encode.GetBytes(content));
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData MD5(Stream source, Boolean dispose = false, Int64 maxBytes = Int64.MaxValue) {
			var alg = System.Security.Cryptography.MD5.Create();
			Int64 read = 0;
			Byte[] src = new Byte[0x1000];
			while (true) {
				Int32 sz1 = source.Read(src, 0, (Int32)Math.Min(maxBytes - read, src.Length));
				if (sz1 == 0) break;
				alg.TransformBlock(src, 0, sz1, null, 0);
				read += sz1;
			}//while
			alg.TransformFinalBlock(src, 0, 0);
			if (dispose) source.Dispose();
			return new BinaryData(alg.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData SHA1(String content, Encoding encode = null) {
			if (encode == null) encode = Encoding.UTF8;
			Byte[] buff = encode.GetBytes(content);
			var alg = System.Security.Cryptography.SHA1.Create();
			buff = alg.ComputeHash(buff);
			return new BinaryData(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData SHA1(Byte[] data) {
			var alg = System.Security.Cryptography.SHA1.Create();
			data = alg.ComputeHash(data);
			return new BinaryData(data);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData SHA1(Stream content, Boolean disposing) {
			Byte[] buff = new Byte[0x10000];
			var alg = System.Security.Cryptography.SHA1.Create();
			while (true) {
				Int32 sz = content.Read(buff, 0, buff.Length);
				if (sz == 0) break;
				alg.TransformBlock(buff, 0, sz, null, 0);
			}//while
			if (disposing) content.Dispose();
			alg.TransformFinalBlock(buff, 0, 0);
			return new BinaryData(alg.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData SHA256(String content, Encoding encode = null) {
			if (encode == null) encode = Encoding.UTF8;
			Byte[] buff = encode.GetBytes(content);
			var alg = System.Security.Cryptography.SHA256.Create();
			buff = alg.ComputeHash(buff);
			return new BinaryData(buff);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData SHA256(Stream content, Boolean disposing) {
			Byte[] buff = new Byte[0x10000];
			var alg = System.Security.Cryptography.SHA256.Create();
			while (true) {
				Int32 sz = content.Read(buff, 0, buff.Length);
				if (sz == 0) break;
				alg.TransformBlock(buff, 0, sz, null, 0);
			}//while
			if (disposing) content.Dispose();
			alg.TransformFinalBlock(buff, 0, 0);
			return new BinaryData(alg.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData SHA256(IEnumerable<Byte[]> blocks) {
			var sha = System.Security.Cryptography.SHA256.Create();
			foreach (var b in blocks) {
				sha.TransformBlock(b, 0, b.Length, null, 0);
			}//using
			sha.TransformFinalBlock(new Byte[0], 0, 0);
			return new BinaryData(sha.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData SHA512(String content, Encoding encode = null) {
			if (encode == null) encode = Encoding.UTF8;
			Byte[] buff = encode.GetBytes(content);
			var alg = System.Security.Cryptography.SHA512.Create();
			buff = alg.ComputeHash(buff);
			return new BinaryData(buff);
		}


		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData HashStream(Stream stream) {
			Int32 sz;
			Byte[] buff = new Byte[0x8000];
			var sha = System.Security.Cryptography.SHA256.Create();
			while (true) {
				sz = stream.Read(buff, 0, buff.Length);
				if (sz == 0) break;
				sha.TransformBlock(buff, 0, sz, null, 0);
			}//while
			sha.TransformFinalBlock(key, 0, key.Length);
			return new BinaryData(sha.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		/// <summary>
		/// Calcula o hash do arquivo utilizando SHA-256 e adicionando um salt interno.
		/// Utilize esse metodo para garantir a integridade de um arquivo.
		/// </summary>
		public static BinaryData HashFile(String fileName) {
			Int32 sz;
			Byte[] buff = new Byte[0x8000];
			var sha = System.Security.Cryptography.SHA256.Create();
			using (var src = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				while (true) {
					sz = src.Read(buff, 0, buff.Length);
					if (sz == 0) break;
					sha.TransformBlock(buff, 0, sz, null, 0);
				}//while
			}//using
			sha.TransformFinalBlock(key, 0, key.Length);
			return new BinaryData(sha.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		/// <summary>
		/// Calcula o hash de uma lista de arquivos utilizando SHA-256 e adicionando um salt interno.
		/// Utilize esse metodo para garantir a integridade de um arquivo.
		/// </summary>		
		public static BinaryData HashFiles(IEnumerable<String> files) {
			Int32 sz;
			Byte[] buff = new Byte[0x8000];
			var sha = System.Security.Cryptography.SHA256.Create();
			foreach (String f in files) {
				using (var src = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					while (true) {
						sz = src.Read(buff, 0, buff.Length);
						if (sz == 0) break;
						sha.TransformBlock(buff, 0, sz, null, 0);
					}//while
				}//using
			}//foreach
			sha.TransformFinalBlock(key, 0, key.Length);
			return new BinaryData(sha.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		/// <summary>
		/// Calcula o hash de uma lista de arquivos utilizando SHA-256 e adicionando um salt interno.
		/// Utilize esse metodo para garantir a integridade de um arquivo.
		/// </summary>	
		public static BinaryData HashFiles(params String[] files) {
			return SecurityUtils.HashFiles(files.AsEnumerable());
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static BinaryData HashData(Byte[] data) {
			Int32 sz;
			var sha = System.Security.Cryptography.SHA256.Create();
			sha.TransformBlock(data, 0, data.Length, null, 0);
			sha.TransformFinalBlock(key, 0, key.Length);
			return new BinaryData(sha.Hash);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//

		public static UInt64 CRC64File(String fileName) {
			if (crc64_tab == null) GenerateCRC64Table();
			Byte[] data = new Byte[16 * 1024];
			UInt64 h = UInt64.MaxValue;
			using (var f = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				while (f.Position < f.Length) {
					Int32 sz = f.Read(data, 0, data.Length);
					for (int i = 0; i < sz; i++) {
						h = (h >> 8) ^ crc64_tab[(Byte)h ^ data[i]];
					}//for
				}//while
			}//using
			return h ^ UInt64.MaxValue;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------------//
	}


}
