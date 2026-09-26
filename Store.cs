using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MeshBridge
{
	public static class Store
	{
		private const string Magic = "MBG1";

		private const int Schema = 1;

		private static string Root
		{
			get
			{
				return Path.Combine(Log.ModRoot, "Store");
			}
		}

		private static string BindingsPath
		{
			get
			{
				return Path.Combine(Root, "bindings.txt");
			}
		}

		public static string GeometryDir
		{
			get
			{
				return Path.Combine(Root, "geometry");
			}
		}

		private static string PayloadPath(string id)
		{
			return Path.Combine(Path.Combine(GeometryDir, id.Substring(0, 2)), id + ".mbg");
		}

		private static string LegacyPayloadPath(string id)
		{
			return Path.Combine(Root, id + ".mbg");
		}

		public static string ResolvePayload(string id)
		{
			string text = PayloadPath(id);
			if (File.Exists(text))
			{
				return text;
			}
			string text2 = LegacyPayloadPath(id);
			if (File.Exists(text2))
			{
				return text2;
			}
			return text;
		}

		public static int Migrate()
		{
			int num = 0;
			try
			{
				if (!Directory.Exists(Root))
				{
					return 0;
				}
				string[] files = Directory.GetFiles(Root, "*.mbg");
				for (int i = 0; i < files.Length; i++)
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
					if (fileNameWithoutExtension.Length != 64)
					{
						continue;
					}
					string text = PayloadPath(fileNameWithoutExtension);
					string directoryName = Path.GetDirectoryName(text);
					if (!Directory.Exists(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					if (File.Exists(text))
					{
						File.Delete(files[i]);
						continue;
					}
					File.Copy(files[i], text, true);
					if (Log.Hash(File.ReadAllBytes(text)) == fileNameWithoutExtension)
					{
						File.Delete(files[i]);
						num++;
					}
					else
					{
						File.Delete(text);
						Log.Write("STORE_MIGRATE_FAILED", "id=" + fileNameWithoutExtension);
					}
				}
				if (num > 0)
				{
					Log.Write("STORE_MIGRATE", "geometry payloads moved to sharded layout: " + num);
				}
			}
			catch (Exception ex)
			{
				Log.Write("STORE_MIGRATE_FAILED", ex.Message);
			}
			return num;
		}

		private static void EnsureRoot()
		{
			if (!Directory.Exists(Root))
			{
				Directory.CreateDirectory(Root);
			}
			if (!Directory.Exists(GeometryDir))
			{
				Directory.CreateDirectory(GeometryDir);
			}
		}

		private static void EnsureShard(string path)
		{
			string directoryName = Path.GetDirectoryName(path);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
		}

		public static byte[] Canonicalise(Mesh mesh)
		{
			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;
			Vector2[] uv = mesh.uv;
			Vector3[] normals = mesh.normals;
			Vector4[] tangents = mesh.tangents;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					binaryWriter.Write(Encoding.ASCII.GetBytes("MBG1"));
					binaryWriter.Write(1);
					binaryWriter.Write(vertices.Length);
					binaryWriter.Write(triangles.Length);
					binaryWriter.Write(uv.Length);
					binaryWriter.Write(normals.Length);
					binaryWriter.Write(tangents.Length);
					for (int i = 0; i < vertices.Length; i++)
					{
						binaryWriter.Write(vertices[i].x);
						binaryWriter.Write(vertices[i].y);
						binaryWriter.Write(vertices[i].z);
					}
					for (int j = 0; j < triangles.Length; j++)
					{
						binaryWriter.Write(triangles[j]);
					}
					for (int k = 0; k < uv.Length; k++)
					{
						binaryWriter.Write(uv[k].x);
						binaryWriter.Write(uv[k].y);
					}
					for (int l = 0; l < normals.Length; l++)
					{
						binaryWriter.Write(normals[l].x);
						binaryWriter.Write(normals[l].y);
						binaryWriter.Write(normals[l].z);
					}
					for (int m = 0; m < tangents.Length; m++)
					{
						binaryWriter.Write(tangents[m].x);
						binaryWriter.Write(tangents[m].y);
						binaryWriter.Write(tangents[m].z);
						binaryWriter.Write(tangents[m].w);
					}
					binaryWriter.Flush();
					return memoryStream.ToArray();
				}
			}
		}

		public static Mesh Rebuild(byte[] bytes, string name)
		{
			using (MemoryStream input = new MemoryStream(bytes))
			{
				using (BinaryReader binaryReader = new BinaryReader(input))
				{
					string text = Encoding.ASCII.GetString(binaryReader.ReadBytes(4));
					if (text != "MBG1")
					{
						throw new InvalidOperationException("Not a MeshBridge geometry payload.");
					}
					int num = binaryReader.ReadInt32();
					if (num != 1)
					{
						throw new InvalidOperationException("Unsupported store schema " + num + "; expected " + 1 + ".");
					}
					int num2 = binaryReader.ReadInt32();
					int num3 = binaryReader.ReadInt32();
					int num4 = binaryReader.ReadInt32();
					int num5 = binaryReader.ReadInt32();
					int num6 = binaryReader.ReadInt32();
					if (num2 > 65534)
					{
						throw new InvalidOperationException("Payload exceeds the 65534-vertex limit for this engine.");
					}
					Vector3[] array = new Vector3[num2];
					for (int i = 0; i < num2; i++)
					{
						array[i] = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
					}
					int[] array2 = new int[num3];
					for (int j = 0; j < num3; j++)
					{
						array2[j] = binaryReader.ReadInt32();
						if (array2[j] < 0 || array2[j] >= num2)
						{
							throw new InvalidOperationException("Index out of range in payload.");
						}
					}
					Vector2[] array3 = new Vector2[num4];
					for (int k = 0; k < num4; k++)
					{
						array3[k] = new Vector2(binaryReader.ReadSingle(), binaryReader.ReadSingle());
					}
					Vector3[] array4 = new Vector3[num5];
					for (int l = 0; l < num5; l++)
					{
						array4[l] = new Vector3(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
					}
					Vector4[] array5 = new Vector4[num6];
					for (int m = 0; m < num6; m++)
					{
						array5[m] = new Vector4(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle());
					}
					Mesh mesh = new Mesh();
					mesh.name = name;
					mesh.vertices = array;
					mesh.triangles = array2;
					if (num4 == num2)
					{
						mesh.uv = array3;
					}
					if (num5 == num2)
					{
						mesh.normals = array4;
					}
					if (num6 == num2)
					{
						mesh.tangents = array5;
					}
					mesh.RecalculateBounds();
					return mesh;
				}
			}
		}

		public static string IdOf(Mesh mesh)
		{
			return Log.Hash(Canonicalise(mesh));
		}

		public static string Commit(Mesh mesh)
		{
			EnsureRoot();
			byte[] array = Canonicalise(mesh);
			string text = Log.Hash(array);
			string text2 = ResolvePayload(text);
			EnsureShard(PayloadPath(text));
			if (File.Exists(text2))
			{
				byte[] a = File.ReadAllBytes(text2);
				if (!SameBytes(a, array))
				{
					throw new InvalidOperationException("Store integrity failure: existing payload for " + text + " differs from new content. Refusing to overwrite.");
				}
				Log.Write("STORE_COMMIT", "action=exists id=" + text + " bytes=" + array.Length);
				return text;
			}
			string text3 = text2 + ".staging";
			File.WriteAllBytes(text3, array);
			byte[] array2 = File.ReadAllBytes(text3);
			if (!SameBytes(array2, array) || Log.Hash(array2) != text)
			{
				try
				{
					File.Delete(text3);
				}
				catch
				{
				}
				throw new InvalidOperationException("Store staging verification failed for " + text + ".");
			}
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			File.Move(text3, text2);
			Log.Write("STORE_COMMIT", "action=published id=" + text + " bytes=" + array.Length);
			return text;
		}

		public static void CommitBytes(string id, byte[] bytes)
		{
			EnsureRoot();
			string text = PayloadPath(id);
			EnsureShard(text);
			if (File.Exists(text))
			{
				return;
			}
			string text2 = text + ".staging";
			File.WriteAllBytes(text2, bytes);
			if (Log.Hash(File.ReadAllBytes(text2)) != id)
			{
				try
				{
					File.Delete(text2);
				}
				catch
				{
				}
				throw new InvalidOperationException("Payload staging verification failed for " + id + ".");
			}
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(text2, text);
			Log.Write("STORE_COMMIT", "action=installed id=" + id + " bytes=" + bytes.Length);
		}

		public static bool Has(string id)
		{
			return File.Exists(PayloadPath(id)) || File.Exists(LegacyPayloadPath(id));
		}

		public static Mesh Load(string id, string meshName)
		{
			string path = ResolvePayload(id);
			if (!File.Exists(path))
			{
				throw new InvalidOperationException("Missing geometry payload " + id + " in store.");
			}
			byte[] array = File.ReadAllBytes(path);
			string text = Log.Hash(array);
			if (text != id)
			{
				throw new InvalidOperationException("Store corruption: payload " + id + " hashes to " + text + ".");
			}
			Mesh result = Rebuild(array, meshName);
			Log.Write("STORE_LOAD", "id=" + id + " bytes=" + array.Length + " verified=True");
			return result;
		}

		private static bool SameBytes(byte[] a, byte[] b)
		{
			if (a == null || b == null || a.Length != b.Length)
			{
				return false;
			}
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}
			return true;
		}

		public static Dictionary<string, string> ReadBindings()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (!File.Exists(BindingsPath))
			{
				return dictionary;
			}
			string[] array = File.ReadAllLines(BindingsPath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0 || text[0] == '#')
				{
					continue;
				}
				int num = text.IndexOf('=');
				if (num > 0)
				{
					string text2 = text.Substring(0, num).Trim();
					string text3 = text.Substring(num + 1).Trim();
					int num2 = text3.IndexOf('|');
					if (num2 > 0)
					{
						text3 = text3.Substring(0, num2).Trim();
					}
					if (text2.Length > 0 && text3.Length > 0)
					{
						dictionary[text2] = text3;
					}
				}
			}
			return dictionary;
		}

		public static Dictionary<string, string> ReadTypes()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (!File.Exists(BindingsPath))
			{
				return dictionary;
			}
			string[] array = File.ReadAllLines(BindingsPath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0 || text[0] == '#')
				{
					continue;
				}
				int num = text.IndexOf('=');
				if (num > 0)
				{
					string key = text.Substring(0, num).Trim();
					string text2 = text.Substring(num + 1);
					int num2 = text2.IndexOf('|');
					if (num2 > 0)
					{
						dictionary[key] = text2.Substring(num2 + 1).Trim();
					}
				}
			}
			return dictionary;
		}

		// Drops a donor's binding. The payload is untouched, so this is reversible:
		// re-importing the same file produces the same geometry id and rebinds it.
		public static void RemoveBinding(string donorName)
		{
			EnsureRoot();
			Dictionary<string, string> dictionary = ReadBindings();
			Dictionary<string, string> dictionary2 = ReadTypes();
			if (!dictionary.ContainsKey(donorName))
			{
				return;
			}
			dictionary.Remove(donorName);
			dictionary2.Remove(donorName);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# MeshBridge donor bindings. donorName = geometryId | baseType");
			stringBuilder.AppendLine("# A saved city references the donor name. This file says which exact");
			stringBuilder.AppendLine("# geometry that name must resolve to. Do not edit by hand.");
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				string value;
				stringBuilder.AppendLine(item.Key + " = " + item.Value + " | " + ((!dictionary2.TryGetValue(item.Key, out value)) ? "PROP" : value));
			}
			string text = BindingsPath + ".staging";
			File.WriteAllText(text, stringBuilder.ToString());
			if (File.Exists(BindingsPath))
			{
				File.Delete(BindingsPath);
			}
			File.Move(text, BindingsPath);
			Log.Write("STORE_UNBIND", "donor=" + donorName + " — binding removed, payload kept.");
		}

		// How many bindings still point at this geometry. Content addressing means two
		// donors can share one payload, so it must not be deleted while any remain.
		public static int PayloadUsers(string geometryId)
		{
			int num = 0;
			foreach (KeyValuePair<string, string> binding in ReadBindings())
			{
				if (binding.Value == geometryId)
				{
					num++;
				}
			}
			return num;
		}

		// Irreversible. The payload is the only copy of the geometry once the source
		// file is gone, which it usually is.
		public static bool DeletePayload(string geometryId)
		{
			string text = ResolvePayload(geometryId);
			if (text == null || !File.Exists(text))
			{
				Log.Write("STORE_DELETE", "id=" + geometryId + " — no payload on disk.");
				return false;
			}
			long length = new FileInfo(text).Length;
			File.Delete(text);
			Log.Write("STORE_DELETE", "id=" + geometryId + " bytes=" + length + " — payload deleted permanently.");
			return true;
		}

		public static void WriteBinding(string donorName, string geometryId)
		{
			WriteBinding(donorName, geometryId, "PROP");
		}

		public static void WriteBinding(string donorName, string geometryId, string baseType)
		{
			EnsureRoot();
			Dictionary<string, string> dictionary = ReadBindings();
			Dictionary<string, string> dictionary2 = ReadTypes();
			dictionary[donorName] = geometryId;
			dictionary2[donorName] = baseType;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# MeshBridge donor bindings. donorName = geometryId | baseType");
			stringBuilder.AppendLine("# A saved city references the donor name. This file says which exact");
			stringBuilder.AppendLine("# geometry that name must resolve to. Do not edit by hand.");
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				string value;
				stringBuilder.AppendLine(item.Key + " = " + item.Value + " | " + ((!dictionary2.TryGetValue(item.Key, out value)) ? "PROP" : value));
			}
			string text = BindingsPath + ".staging";
			File.WriteAllText(text, stringBuilder.ToString());
			if (File.Exists(BindingsPath))
			{
				File.Delete(BindingsPath);
			}
			File.Move(text, BindingsPath);
			Log.Write("STORE_BIND", "donor=" + donorName + " geometryId=" + geometryId + " type=" + baseType);
		}
	}
}
