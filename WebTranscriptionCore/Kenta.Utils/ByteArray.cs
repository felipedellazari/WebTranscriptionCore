using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

public static partial class __ExtensionMethods {

	public static String ToHexDecimal(this Byte[] array) {
		String CHARS = "0123456789abcdef";
		Char[] buff = new Char[array.Length * 2];
		Int32 x = 0;
		for (Int32 n = 0; n < array.Length; n++) {
			buff[x++] = CHARS[(array[n] >> 4)];
			buff[x++] = CHARS[(array[n] & 0xF)];
		}//for
		return new String(buff);
	}

}

