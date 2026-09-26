using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace MeshBridge
{
	public static class Fbx
	{
		private const string Magic = "Kaydara FBX Binary  ";

		public static bool IsBinary(string path)
		{
			try
			{
				using (FileStream fileStream = File.OpenRead(path))
				{
					byte[] array = new byte[21];
					if (fileStream.Read(array, 0, 21) < 21)
					{
						return false;
					}
					return Encoding.ASCII.GetString(array, 0, 20) == "Kaydara FBX Binary  ";
				}
			}
			catch
			{
				return false;
			}
		}

		public static FbxNode Read(string path)
		{
			using (FileStream input = File.OpenRead(path))
			{
				using (BinaryReader binaryReader = new BinaryReader(input))
				{
					byte[] bytes = binaryReader.ReadBytes(23);
					if (Encoding.ASCII.GetString(bytes, 0, 20) != "Kaydara FBX Binary  ")
					{
						throw new InvalidOperationException("Not a binary FBX. ASCII FBX is not supported; re-export as binary.");
					}
					uint num = binaryReader.ReadUInt32();
					bool wide = num >= 7500;
					FbxNode fbxNode = new FbxNode();
					fbxNode.Name = "<root>";
					while (true)
					{
						FbxNode fbxNode2 = ReadNode(binaryReader, wide);
						if (fbxNode2 == null)
						{
							break;
						}
						fbxNode.Children.Add(fbxNode2);
					}
					Log.Write("FBX_READ", "version=" + num + " topLevelNodes=" + fbxNode.Children.Count);
					return fbxNode;
				}
			}
		}

		private static FbxNode ReadNode(BinaryReader r, bool wide)
		{
			long num = (long)((!wide) ? r.ReadUInt32() : r.ReadUInt64());
			long num2 = (long)((!wide) ? r.ReadUInt32() : r.ReadUInt64());
			long num3 = (long)((!wide) ? r.ReadUInt32() : r.ReadUInt64());
			byte count = r.ReadByte();
			if (num == 0)
			{
				return null;
			}
			FbxNode fbxNode = new FbxNode();
			fbxNode.Name = Encoding.ASCII.GetString(r.ReadBytes(count));
			long num4 = r.BaseStream.Position + num3;
			for (long num5 = 0L; num5 < num2; num5++)
			{
				fbxNode.Properties.Add(ReadProperty(r));
			}
			if (r.BaseStream.Position != num4)
			{
				r.BaseStream.Position = num4;
			}
			while (r.BaseStream.Position < num)
			{
				FbxNode fbxNode2 = ReadNode(r, wide);
				if (fbxNode2 == null)
				{
					break;
				}
				fbxNode.Children.Add(fbxNode2);
			}
			r.BaseStream.Position = num;
			return fbxNode;
		}

		private static object ReadProperty(BinaryReader r)
		{
			char c = (char)r.ReadByte();
			switch (c)
			{
			case 'Y':
				return (int)r.ReadInt16();
			case 'C':
				return r.ReadByte() != 0;
			case 'I':
				return r.ReadInt32();
			case 'F':
				return (double)r.ReadSingle();
			case 'D':
				return r.ReadDouble();
			case 'L':
				return r.ReadInt64();
			case 'R':
			case 'S':
			{
				int count = r.ReadInt32();
				byte[] array = r.ReadBytes(count);
				return (c != 'S') ? ((object)array) : ((object)Encoding.UTF8.GetString(array));
			}
			case 'f':
				return ToDoubles(ReadArray(r, 4));
			case 'd':
				return ReadDoubleArray(r);
			case 'l':
				return ToInts(ReadLongArray(r));
			case 'i':
				return ReadIntArray(r);
			case 'b':
				return ReadArray(r, 1);
			default:
				throw new InvalidOperationException("Unknown FBX property type '" + c + "'.");
			}
		}

		private static byte[] ReadArray(BinaryReader r, int stride)
		{
			int num = r.ReadInt32();
			int num2 = r.ReadInt32();
			int count = r.ReadInt32();
			byte[] array = r.ReadBytes(count);
			if (num2 == 0)
			{
				return array;
			}
			using (MemoryStream baseInputStream = new MemoryStream(array))
			{
				using (InflaterInputStream inflaterInputStream = new InflaterInputStream(baseInputStream))
				{
					using (MemoryStream memoryStream = new MemoryStream())
					{
						byte[] array2 = new byte[8192];
						int count2;
						while ((count2 = inflaterInputStream.Read(array2, 0, array2.Length)) > 0)
						{
							memoryStream.Write(array2, 0, count2);
						}
						return memoryStream.ToArray();
					}
				}
			}
		}

		private static double[] ReadDoubleArray(BinaryReader r)
		{
			byte[] array = ReadArray(r, 8);
			double[] array2 = new double[array.Length / 8];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = BitConverter.ToDouble(array, i * 8);
			}
			return array2;
		}

		private static int[] ReadIntArray(BinaryReader r)
		{
			byte[] array = ReadArray(r, 4);
			int[] array2 = new int[array.Length / 4];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = BitConverter.ToInt32(array, i * 4);
			}
			return array2;
		}

		private static long[] ReadLongArray(BinaryReader r)
		{
			byte[] array = ReadArray(r, 8);
			long[] array2 = new long[array.Length / 8];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = BitConverter.ToInt64(array, i * 8);
			}
			return array2;
		}

		private static double[] ToDoubles(byte[] b)
		{
			double[] array = new double[b.Length / 4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = BitConverter.ToSingle(b, i * 4);
			}
			return array;
		}

		private static int[] ToInts(long[] l)
		{
			int[] array = new int[l.Length];
			for (int i = 0; i < l.Length; i++)
			{
				array[i] = (int)l[i];
			}
			return array;
		}
	}
}
