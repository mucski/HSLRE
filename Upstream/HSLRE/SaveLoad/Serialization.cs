using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HSLRE.SaveLoad
{
	// - https://stackoverflow.com/a/20770973 -
	// https://stackoverflow.com/a/30268350
	internal static class Serialization
	{
		/*public static byte[] XmlSerializeToByte<T>(T value) where T : struct
		{
			/*if (value == null)
			{
				throw new ArgumentNullException();
			}* /

			XmlSerializer serializer = new XmlSerializer(typeof(T));

			using (MemoryStream memoryStream = new MemoryStream())
			using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream))
			{
				serializer.Serialize(xmlWriter, value);
				return memoryStream.ToArray();
			}
		}

		public static T XmlDeserializeFromBytes<T>(byte[] bytes) where T : struct
		{
			if (bytes == null || bytes.Length == 0)
			{
				throw new InvalidOperationException();
			}

			XmlSerializer serializer = new XmlSerializer(typeof(T));

			using (MemoryStream memoryStream = new MemoryStream(bytes))
			using (XmlReader xmlReader = XmlReader.Create(memoryStream))
			{
				return (T)serializer.Deserialize(xmlReader);
			}
		}*/

		public static byte[] GetBytes<T>(this T str) where T : struct
		{
			int size = Marshal.SizeOf(str);
			byte[] bytes = new byte[size];
			IntPtr ptr = Marshal.AllocHGlobal(size);

			Marshal.StructureToPtr(str, ptr, true);
			Marshal.Copy(ptr, bytes, 0, size);
			Marshal.FreeHGlobal(ptr);

			return bytes;
		}

		public static T GetStruct<T>(this byte[] arr) where T : struct
		{
			T str = default;

			int size = Marshal.SizeOf(str);
			IntPtr ptr = Marshal.AllocHGlobal(size);

			Marshal.Copy(arr, 0, ptr, size);

			str = (T)Marshal.PtrToStructure(ptr, str.GetType());
			Marshal.FreeHGlobal(ptr);

			return str;
		}

		//-----------------

		public static string GetSaveDataAsString<T>(this T str) where T : struct
		{
			return Convert.ToBase64String(str.GetBytes());
		}

		public static T GetSaveDataFromString<T>(this string data) where T : struct
		{
			return Convert.FromBase64String(data).GetStruct<T>();
		}
	}
}
