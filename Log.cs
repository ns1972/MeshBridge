using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

namespace MeshBridge
{
	public static class Log
	{
		// static readonly, not const. A const is inlined into every use site at compile
		// time, which is how the log stamp, the window title and the Content Manager
		// name all stayed on an old number after a rebuild.
		public static readonly string Version = "0.18.3";

		// Derived from where this DLL actually runs, not hardcoded, so a dev/testing
		// copy in a differently-named mod folder writes its own logs/store/import
		// alongside itself instead of silently landing in the published mod's folder.
		public static readonly string ModRoot = ComputeModRoot();

		// Prefer wherever this DLL is actually running from, so a differently-named
		// dev/testing folder gets its own logs/store/import. Some loaders load the
		// assembly's bytes into memory rather than from a file path, which leaves
		// Location empty -- fall back to the standard published location rather
		// than throw, since this runs in a static constructor and any exception
		// here would take down every use of this class for the rest of the session.
		private static string ComputeModRoot()
		{
			try
			{
				string location = Assembly.GetExecutingAssembly().Location;
				if (!string.IsNullOrEmpty(location))
				{
					string directoryName = Path.GetDirectoryName(location);
					if (!string.IsNullOrEmpty(directoryName))
					{
						return directoryName;
					}
				}
			}
			catch
			{
			}
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Colossal Order\\Cities_Skylines\\Addons\\Mods\\MeshBridge");
		}

		private static readonly string PathName = Path.Combine(Path.Combine(ModRoot, "Logs"), "MeshBridge.log");

		public static string LogPath
		{
			get
			{
				return PathName;
			}
		}

		public static void Write(string tag, string message)
		{
			string text = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + " [MeshBridge " + Version + "][" + tag + "] " + message;
			Debug.Log(text);
			try
			{
				string directoryName = Path.GetDirectoryName(PathName);
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				File.AppendAllText(PathName, text + Environment.NewLine);
			}
			catch (Exception ex)
			{
				Debug.LogError("[MeshBridge] Log file unavailable: " + ex.Message);
			}
		}

		public static void Require(bool condition, string message)
		{
			if (!condition)
			{
				throw new InvalidOperationException(message);
			}
		}

		public static string Hash(byte[] data)
		{
			using (SHA256 sHA = SHA256.Create())
			{
				return BitConverter.ToString(sHA.ComputeHash(data)).Replace("-", string.Empty);
			}
		}

		public static void MeshHash(Mesh mesh, out string full, out string topologyUV)
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
					using (MemoryStream memoryStream2 = new MemoryStream())
					{
						using (BinaryWriter binaryWriter2 = new BinaryWriter(memoryStream2))
						{
							for (int i = 0; i < triangles.Length; i++)
							{
								binaryWriter.Write(triangles[i]);
								binaryWriter2.Write(triangles[i]);
							}
							for (int j = 0; j < uv.Length; j++)
							{
								binaryWriter.Write(uv[j].x);
								binaryWriter.Write(uv[j].y);
								binaryWriter2.Write(uv[j].x);
								binaryWriter2.Write(uv[j].y);
							}
							for (int k = 0; k < vertices.Length; k++)
							{
								binaryWriter2.Write(vertices[k].x);
								binaryWriter2.Write(vertices[k].y);
								binaryWriter2.Write(vertices[k].z);
							}
							for (int l = 0; l < normals.Length; l++)
							{
								binaryWriter2.Write(normals[l].x);
								binaryWriter2.Write(normals[l].y);
								binaryWriter2.Write(normals[l].z);
							}
							for (int m = 0; m < tangents.Length; m++)
							{
								binaryWriter2.Write(tangents[m].x);
								binaryWriter2.Write(tangents[m].y);
								binaryWriter2.Write(tangents[m].z);
								binaryWriter2.Write(tangents[m].w);
							}
							binaryWriter.Flush();
							binaryWriter2.Flush();
							topologyUV = Hash(memoryStream.ToArray());
							full = Hash(memoryStream2.ToArray());
						}
					}
				}
			}
		}

		public static string TextureHash(Texture texture)
		{
			Texture2D texture2D = texture as Texture2D;
			if (texture2D == null)
			{
				return "<not-a-texture2d>";
			}
			try
			{
				Color32[] pixels = texture2D.GetPixels32();
				byte[] array = new byte[pixels.Length * 4];
				for (int i = 0; i < pixels.Length; i++)
				{
					array[i * 4] = pixels[i].r;
					array[i * 4 + 1] = pixels[i].g;
					array[i * 4 + 2] = pixels[i].b;
					array[i * 4 + 3] = pixels[i].a;
				}
				return Hash(array);
			}
			catch (Exception ex)
			{
				return "<unreadable: " + ex.Message + ">";
			}
		}

		public static string MeshInfo(Mesh mesh)
		{
			string full;
			string topologyUV;
			MeshHash(mesh, out full, out topologyUV);
			return "meshInstance=" + mesh.GetInstanceID() + " vertices=" + mesh.vertexCount + " triangles=" + mesh.triangles.Length / 3 + " UVs=" + mesh.uv.Length + " readable=" + mesh.isReadable + " bounds=" + mesh.bounds.size.ToString("F3") + " center=" + mesh.bounds.center.ToString("F3") + " fullSHA256=" + full + " topologyUV_SHA256=" + topologyUV;
		}
	}
}
