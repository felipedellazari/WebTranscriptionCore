using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Kenta.Utils;
using System.Security.Cryptography;

public static partial class __ExtensionMethods {

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	private const Int32 MIN_BUFFER = 0x20000; //128 Kilobytes
	private const Int32 MAX_BUFFER = 0x200000; //2 Megabytes

	[ThreadStatic]
	private static Byte[] ThreadBuffer;

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	internal static Byte[] GetBuffer(Int32 sz = MIN_BUFFER) {
		if (sz > MAX_BUFFER) return new Byte[sz];
		Byte[] buff = ThreadBuffer;
		if (buff == null) {
			ThreadBuffer = buff = new Byte[Math.Max(MIN_BUFFER, sz)];
		} else if (buff.Length < sz) {
			buff = new Byte[sz];
		}//if
		return buff;
	}


	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static void CopyTo(this Stream src, Stream dst, Int64 offset, Int64 count) {
		Byte[] buff = GetBuffer();
		Int64 done = 0;
		src.Position = offset;
		while (done < count) {
			Int32 sz = src.Read(buff, 0, (Int32)Math.Min(count - done, buff.Length));
			if (sz == 0) break;
			dst.Write(buff, 0, sz);
			done += sz;
		}//while		
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static BinaryData CopyAndHash(this Stream src, Stream dst, String salt = null) {
		Int32 sz;
		Byte[] buff = GetBuffer();
		SHA1 sha = SHA1.Create();
		while (true) {
			sz = src.Read(buff, 0, buff.Length);
			if (sz == 0) break;
			dst.Write(buff, 0, buff.Length);
			sha.TransformBlock(buff, 0, buff.Length, null, 0);
		}//while
		if (salt != null) {
			sz = Encoding.UTF8.GetBytes(salt, 0, salt.Length, buff, 0);
			sha.TransformBlock(buff, 0, sz, null, 0);
		}//if
		sha.TransformFinalBlock(buff, 0, 0);
		return new BinaryData(sha.Hash);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static BinaryData CalcHash(this Stream src, String salt = null) {
		Int32 sz;
		Byte[] buff = GetBuffer();
		SHA1 sha = SHA1.Create();
		while (true) {
			sz = src.Read(buff, 0, buff.Length);
			if (sz == 0) break;
			sha.TransformBlock(buff, 0, buff.Length, null, 0);
		}//while
		if (salt != null) {
			sz = Encoding.UTF8.GetBytes(salt, 0, salt.Length, buff, 0);
			sha.TransformBlock(buff, 0, sz, null, 0);
		}//if
		sha.TransformFinalBlock(buff, 0, 0);
		return new BinaryData(sha.Hash);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	[System.Diagnostics.DebuggerNonUserCode]
	public static Byte[] ReadAllBytes(this Stream stream) {
		Byte[] buff;
		Boolean copyNeeded = true;
		try {
			buff = new Byte[stream.Length - stream.Position];
			copyNeeded = false;
		} catch (NotSupportedException) {
			buff = GetBuffer();
		}//try
		Int32 offs = 0;
		while (true) {
			Int32 read = stream.Read(buff, offs, buff.Length - offs);
			if (read == 0) break;
			offs += read;
			if (offs >= buff.Length) {
				Array.Resize(ref buff, buff.Length * 2);
				copyNeeded = true;
			}//if
		}//while
		if (copyNeeded || offs != buff.Length) {
			Byte[] x = new Byte[offs];
			Array.Copy(buff, x, x.Length);
			buff = x;
		}//if
		return buff;
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static void Write(this Stream stream, BinaryData data) {
		stream.Write(data.Data, 0, data.Length);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static void Write(this Stream stream, String text, Encoding encode) {
		Byte[] buff = encode.GetBytes(text);
		stream.Write(buff, 0, buff.Length);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static void Write(this Stream stream, Byte[] buff) {
		stream.Write(buff, 0, buff.Length);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	//public static void Write(this Stream stream, Int16 value) {
	//	Byte[] buff = GetBuffer();
	//	BinUtils.WriteInt16(buff, 0, value);
	//	stream.Write(buff, 0, sizeof(Int16));
	//}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	//public static void Write(this Stream stream, UInt16 value) {
	//	unchecked { stream.Write((Int16)value); }
	//}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	//public static void Write(this Stream stream, Int32 value) {
	//	Byte[] buff = GetBuffer();
	//	BinUtils.WriteInt32(buff, 0, value);
	//	stream.Write(buff, 0, sizeof(Int32));
	//}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	//public static void Write(this Stream stream, UInt32 value) {
	//	unchecked { stream.Write((Int32)value); }
	//}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	//public static void Write(this Stream stream, Int64 value) {
	//	Byte[] buff = GetBuffer();
	//	BinUtils.WriteInt64(buff, 0, value);
	//	stream.Write(buff, 0, sizeof(Int64));
	//}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	//public static void Write(this Stream stream, UInt64 value) {
	//	unchecked { stream.Write((Int64)value); }
	//}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static void Write(this Stream stream, IntPtr buffer, Int32 size) {
		Byte[] buff = GetBuffer();
		Int32 offs = 0;
		do {
			Int32 sz = Math.Min(buff.Length, size - offs);
			Marshal.Copy(buffer + offs, buff, 0, sz);
			stream.Write(buff, 0, sz);
			offs += sz;
		} while (offs < size);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	public static void WriteFile(this Stream stream, String fileName) {
		using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
			fs.CopyTo(stream);
		}
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	[Obsolete]
	public static void Write<T>(this Stream stream, ref T @struct) where T : struct {
		throw new NotImplementedException();
		//if (!typeof(T).IsBittable()) throw new InvalidOperationException("Type is not bittable");
		//Int32 sz = BinUtils.SizeOf<T>();
		//Byte[] buff = GetBuffer(sz);
		//BinUtils.WriteStruct<T>(buff, 0, ref @struct);
		//stream.Write(buff, 0, sz);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	[Obsolete]
	public static void Write<T>(this Stream stream, T[] array) where T : struct {
		stream.Write<T>(array, 0, array.Length);
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	[Obsolete]
	public static void Write<T>(this Stream stream, T[] array, Int32 offset, Int32 count) where T : struct {
		throw new NotImplementedException();
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	[Obsolete]
	public static void Read<T>(this Stream stream, out T @struct) where T : struct {
		throw new NotImplementedException();
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

	[Obsolete]
	public static void Read<T>(this Stream stream, T[] array, Int32 offset, Int32 count) where T : struct {
		throw new NotImplementedException();
		//if (!typeof(T).IsBittable()) throw new InvalidOperationException("Type is not bittable"); 
		//if (offset + count > array.Length) throw new ArgumentOutOfRangeException();
		//Int32 sz = BinUtils.SizeOf<T>();
		//Byte[] buff = GetBuffer(sz);
		//Int32 perStep = buff.Length / sz;
		//count += offset;
		//while (offset < count) {
		//  Int32 eff = Math.Min(perStep, count-offset);
		//  stream.Read(buff, 0, eff*sz);
		//  BinUtils.Blit<Byte,T>(buff, 0, array, offset, eff*sz);
		//  offset += eff;
		//}//while
	}

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------------------//

}

