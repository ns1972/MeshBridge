using System;
using System.Collections.Generic;
using System.IO;

namespace MeshBridge
{
	public static class Manifest
	{
		public const string DataId = "MeshBridge.Manifest.v1";

		private const int Schema = 1;

		public static List<ManifestEntry> Pending = new List<ManifestEntry>();

		public static bool PendingValid;

		public static byte[] Serialise(List<ManifestEntry> entries)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					binaryWriter.Write(1);
					binaryWriter.Write(entries.Count);
					for (int i = 0; i < entries.Count; i++)
					{
						binaryWriter.Write(entries[i].DonorName ?? string.Empty);
						binaryWriter.Write(entries[i].GeometryId ?? string.Empty);
						binaryWriter.Write(entries[i].Instances);
					}
					binaryWriter.Flush();
					return memoryStream.ToArray();
				}
			}
		}

		public static List<ManifestEntry> Deserialise(byte[] bytes)
		{
			List<ManifestEntry> list = new List<ManifestEntry>();
			if (bytes == null || bytes.Length == 0)
			{
				return list;
			}
			using (MemoryStream input = new MemoryStream(bytes))
			{
				using (BinaryReader binaryReader = new BinaryReader(input))
				{
					int num = binaryReader.ReadInt32();
					if (num != 1)
					{
						throw new InvalidOperationException("Unsupported manifest schema " + num + ".");
					}
					int num2 = binaryReader.ReadInt32();
					for (int i = 0; i < num2; i++)
					{
						ManifestEntry manifestEntry = new ManifestEntry();
						manifestEntry.DonorName = binaryReader.ReadString();
						manifestEntry.GeometryId = binaryReader.ReadString();
						manifestEntry.Instances = binaryReader.ReadInt32();
						list.Add(manifestEntry);
					}
					return list;
				}
			}
		}
	}
}
