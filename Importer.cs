using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MeshBridge
{
	public static class Importer
	{
		public class ParsedVertices
		{
			public List<Vector3> Positions = new List<Vector3>();

			public List<Vector2> UVs = new List<Vector2>();

			public List<Vector3> Normals = new List<Vector3>();

			public bool HasNormals;
		}

		public class ParsedGroup
		{
			public string Material = "default";

			public List<int> Triangles = new List<int>();

			public ParsedVertices Source;
		}

		public class MtlEntry
		{
			public string DiffuseMap = string.Empty;

			public float Alpha = 1f;

			public Color Colour = Color.white;

			public bool Transparent
			{
				get
				{
					return Alpha < 0.999f;
				}
			}
		}

		public const int MaxVertices = 65534;

		public static readonly List<string> LastErrors = new List<string>();

		public static string ImportFolder
		{
			get
			{
				return Path.Combine(Log.ModRoot, "Import");
			}
		}

		private static string CatalogPath
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(ImportFolder), "Store\\catalog.txt");
			}
		}

		public static List<ImportedResource> ImportAll()
		{
			List<ImportedResource> list = new List<ImportedResource>();
			LastErrors.Clear();
			if (!Directory.Exists(ImportFolder))
			{
				Directory.CreateDirectory(ImportFolder);
				Log.Write("IMPORT", "Created import folder: " + ImportFolder);
				return list;
			}
			List<string> list2 = new List<string>();
			list2.AddRange(Directory.GetFiles(ImportFolder, "*.obj", SearchOption.AllDirectories));
			list2.AddRange(Directory.GetFiles(ImportFolder, "*.fbx", SearchOption.AllDirectories));
			string[] array = list2.ToArray();
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (Path.GetDirectoryName(array[i]) != ImportFolder.TrimEnd('\\'))
				{
					num++;
				}
			}
			Log.Write("IMPORT", "folder=" + ImportFolder + " files=" + array.Length + " (obj+fbx, " + num + " in subfolders)");
			for (int j = 0; j < array.Length; j++)
			{
				try
				{
					List<ImportedResource> list3 = ImportOne(array[j]);
					for (int k = 0; k < list3.Count; k++)
					{
						list.Add(list3[k]);
					}
				}
				catch (Exception ex)
				{
					LastErrors.Add(Path.GetFileName(array[j]) + ": " + ex.Message);
					Log.Write("IMPORT_FAILED", "file=" + Path.GetFileName(array[j]) + " " + ex.GetType().Name + ": " + ex.Message);
				}
			}
			if (list.Count > 0)
			{
				WriteCatalog(list);
			}
			return list;
		}

		private static string FriendlyNameFor(string path)
		{
			string directoryName = Path.GetDirectoryName(path);
			string b = ImportFolder.TrimEnd('\\');
			if (string.Equals(directoryName, b, StringComparison.OrdinalIgnoreCase))
			{
				return Path.GetFileNameWithoutExtension(path);
			}
			int num = Directory.GetFiles(directoryName, "*.obj").Length + Directory.GetFiles(directoryName, "*.fbx").Length;
			return (num != 1) ? Path.GetFileNameWithoutExtension(path) : new DirectoryInfo(directoryName).Name;
		}

		private static List<ImportedResource> ImportOne(string path)
		{
			string fileName = Path.GetFileName(path);
			string text = FriendlyNameFor(path);
			List<ImportedResource> list = new List<ImportedResource>();
			bool flag = Path.GetExtension(path).ToLower() == ".fbx";
			List<ParsedGroup> list2 = ((!flag) ? Parse(path) : ParseFbx(path));
			if (list2.Count == 0)
			{
				throw new InvalidOperationException("No geometry parsed.");
			}
			Dictionary<string, MtlEntry> mtl = ((!flag) ? ReadMtl(path) : new Dictionary<string, MtlEntry>());
			for (int i = 0; i < list2.Count; i++)
			{
				ParsedGroup parsedGroup = list2[i];
				if (parsedGroup.Triangles.Count == 0)
				{
					continue;
				}
				int num = CountDistinct(parsedGroup.Triangles);
				if (num != 0)
				{
					if (num > 65534)
					{
						throw new InvalidOperationException("Part '" + parsedGroup.Material + "' needs " + num + " vertices, over this engine's " + 65534 + " limit. Decimate the model in Blender, or split it into separate materials or separate files.");
					}
					Mesh mesh = BuildPartMesh(parsedGroup, text + ((list2.Count <= 1) ? string.Empty : ("." + parsedGroup.Material)));
					if (mesh.vertexCount != num)
					{
						UnityEngine.Object.DestroyImmediate(mesh);
						throw new InvalidOperationException("Part '" + parsedGroup.Material + "' built " + mesh.vertexCount + " vertices where " + num + " were expected — the engine rejected the mesh.");
					}
					string text2 = Store.Commit(mesh);
					string text3 = ((list2.Count <= 1) ? string.Empty : ("." + Slug(parsedGroup.Material)));
					string text4 = "MeshBridge." + Slug(text) + text3 + "." + text2.Substring(0, 8) + "_Data";
					ImportedResource importedResource = new ImportedResource();
					importedResource.DonorName = text4;
					importedResource.FriendlyName = text + ((list2.Count <= 1) ? string.Empty : (" [" + parsedGroup.Material + "]"));
					importedResource.SourceFile = fileName;
					importedResource.GeometryId = text2;
					importedResource.Mesh = mesh;
					importedResource.VertexCount = mesh.vertexCount;
					importedResource.TriangleCount = mesh.triangles.Length / 3;
					importedResource.MaterialName = parsedGroup.Material;
					importedResource.ModelKey = ModelKeyFromDonor(text4);
					Store.WriteBinding(text4, text2, "PROP");
					WriteImportRecipe(text4, parsedGroup.Material, mtl, Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
					list.Add(importedResource);
					Log.Write("IMPORT_OK", "file=" + fileName + " part=" + parsedGroup.Material + " donor=" + text4 + " geometryId=" + text2 + " vertices=" + importedResource.VertexCount + " triangles=" + importedResource.TriangleCount + " bounds=" + mesh.bounds.size.ToString("F3"));
				}
			}
			if (list.Count > 1)
			{
				Log.Write("IMPORT_SPLIT", "file=" + fileName + " parts=" + list.Count + " — placed together they reassemble, because part vertices keep the source model's coordinates rather than being recentred.");
			}
			return list;
		}

		private static int CountDistinct(List<int> indices)
		{
			Dictionary<int, bool> dictionary = new Dictionary<int, bool>();
			for (int i = 0; i < indices.Count; i++)
			{
				if (!dictionary.ContainsKey(indices[i]))
				{
					dictionary.Add(indices[i], true);
				}
			}
			return dictionary.Count;
		}

		private static Mesh BuildPartMesh(ParsedGroup pg, string name)
		{
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			List<Vector3> list = new List<Vector3>();
			List<Vector2> list2 = new List<Vector2>();
			List<Vector3> list3 = new List<Vector3>();
			List<int> list4 = new List<int>();
			for (int i = 0; i < pg.Triangles.Count; i++)
			{
				int num = pg.Triangles[i];
				int value;
				if (!dictionary.TryGetValue(num, out value))
				{
					value = list.Count;
					dictionary.Add(num, value);
					list.Add(pg.Source.Positions[num]);
					list2.Add(pg.Source.UVs[num]);
					list3.Add(pg.Source.Normals[num]);
				}
				list4.Add(value);
			}
			Mesh mesh = new Mesh();
			mesh.name = name;
			mesh.vertices = list.ToArray();
			mesh.uv = list2.ToArray();
			mesh.triangles = list4.ToArray();
			if (pg.Source.HasNormals)
			{
				mesh.normals = list3.ToArray();
			}
			else
			{
				mesh.RecalculateNormals();
			}
			mesh.RecalculateBounds();
			Tangents(mesh);
			return mesh;
		}

		private static bool ApplyAssetConvention(MaterialRecipe recipe, string dir, string baseName, string materialName, out bool transparent)
		{
			transparent = false;
			string[] array = new string[4] { ".png", ".jpg", ".jpeg", ".tga" };
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array2 = new string[6] { "_d", "_a", "_c", "_i", "_n", "_s" };
			for (int i = 0; i < array2.Length; i++)
			{
				for (int j = 0; j < array.Length; j++)
				{
					string text = Path.Combine(dir, baseName + array2[i] + array[j]);
					if (File.Exists(text))
					{
						dictionary[array2[i]] = text;
						break;
					}
				}
			}
			if (dictionary.Count == 0)
			{
				return false;
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(",");
				}
				stringBuilder.Append(item.Key);
			}
			Log.Write("ASSET_CONVENTION", string.Concat("model=", baseName, " maps=[", stringBuilder, "]"));
			Texture2D texture2D = ((!dictionary.ContainsKey("_d")) ? null : Materials.LoadFile(dictionary["_d"], false));
			Texture2D texture2D2 = ((!dictionary.ContainsKey("_a")) ? null : Materials.LoadFile(dictionary["_a"], true));
			Texture2D texture2D3 = ((!dictionary.ContainsKey("_c")) ? null : Materials.LoadFile(dictionary["_c"], true));
			Texture2D texture2D4 = ((!dictionary.ContainsKey("_i")) ? null : Materials.LoadFile(dictionary["_i"], true));
			Texture2D texture2D5 = ((!dictionary.ContainsKey("_n")) ? null : Materials.LoadFile(dictionary["_n"], true));
			Texture2D texture2D6 = ((!dictionary.ContainsKey("_s")) ? null : Materials.LoadFile(dictionary["_s"], true));
			try
			{
				if (texture2D != null)
				{
					recipe.MainTexId = Materials.CommitBytes(File.ReadAllBytes(dictionary["_d"]));
				}
				if (texture2D2 != null || texture2D3 != null || texture2D4 != null)
				{
					recipe.AciTexId = Materials.CommitComposite(texture2D2, texture2D3, texture2D4, new Color32(byte.MaxValue, 0, 0, byte.MaxValue), "ACI for " + baseName);
				}
				if (texture2D5 != null || texture2D6 != null)
				{
					Texture2D r = texture2D5;
					Texture2D texture2D7 = ((!(texture2D5 == null)) ? Green(texture2D5) : null);
					try
					{
						recipe.XysTexId = Materials.CommitComposite(r, texture2D7, texture2D6, new Color32(128, 128, 0, byte.MaxValue), "XYS for " + baseName);
					}
					finally
					{
						if (texture2D7 != null)
						{
							UnityEngine.Object.DestroyImmediate(texture2D7);
						}
					}
				}
				transparent = Materials.HasNonWhite(texture2D2);
				if (transparent)
				{
					Log.Write("ASSET_CONVENTION_GLASS", "model=" + baseName + " — _a map contains non-opaque pixels; using the transparent shader.");
				}
				return true;
			}
			finally
			{
				if (texture2D != null)
				{
					UnityEngine.Object.DestroyImmediate(texture2D);
				}
				if (texture2D2 != null)
				{
					UnityEngine.Object.DestroyImmediate(texture2D2);
				}
				if (texture2D3 != null)
				{
					UnityEngine.Object.DestroyImmediate(texture2D3);
				}
				if (texture2D4 != null)
				{
					UnityEngine.Object.DestroyImmediate(texture2D4);
				}
				if (texture2D5 != null)
				{
					UnityEngine.Object.DestroyImmediate(texture2D5);
				}
				if (texture2D6 != null)
				{
					UnityEngine.Object.DestroyImmediate(texture2D6);
				}
			}
		}

		private static Texture2D Green(Texture2D n)
		{
			Color32[] pixels = n.GetPixels32();
			Color32[] array = new Color32[pixels.Length];
			for (int i = 0; i < pixels.Length; i++)
			{
				array[i] = new Color32(pixels[i].g, pixels[i].g, pixels[i].g, byte.MaxValue);
			}
			Texture2D texture2D = new Texture2D(n.width, n.height, TextureFormat.RGBA32, false, true);
			texture2D.SetPixels32(array);
			texture2D.Apply();
			return texture2D;
		}

		private static void WriteImportRecipe(string donorName, string materialName, Dictionary<string, MtlEntry> mtl, string dir, string modelBaseName)
		{
			try
			{
				MtlEntry value;
				bool flag = mtl.TryGetValue(materialName, out value);
				bool flag2 = materialName.ToLower().Contains("glass") || materialName.ToLower().Contains("transparent");
				bool flag3 = (flag && value.Transparent) || flag2;
				MaterialRecipe materialRecipe = new MaterialRecipe();
				if (flag)
				{
					materialRecipe.Colour = value.Colour;
				}
				bool transparent;
				bool flag4 = ApplyAssetConvention(materialRecipe, dir, modelBaseName, materialName, out transparent);
				if (flag4 && transparent)
				{
					flag3 = true;
				}
				materialRecipe.Shader = ((!flag3) ? "Custom/Props/Prop/Default" : "Custom/Vehicles/Vehicle/Rotors");
				if (flag && value.DiffuseMap.Length > 0)
				{
					string path = Path.Combine(dir, value.DiffuseMap);
					if (File.Exists(path))
					{
						materialRecipe.MainTexId = Materials.CommitBytes(File.ReadAllBytes(path));
					}
					else
					{
						Log.Write("MTL_TEXTURE_MISSING", "material=" + materialName + " map_Kd='" + value.DiffuseMap + "' not found beside the .obj.");
					}
				}
				if (materialRecipe.MainTexId.Length == 0)
				{
					Color color = ((!flag) ? Color.white : value.Colour);
					materialRecipe.MainTexId = Materials.CommitFlat(new Color32((byte)(Mathf.Clamp01(color.r) * 255f), (byte)(Mathf.Clamp01(color.g) * 255f), (byte)(Mathf.Clamp01(color.b) * 255f), byte.MaxValue), 16);
					Log.Write("MTL_FLAT_DIFFUSE", "material=" + materialName + " no map_Kd; generated a flat diffuse from Kd.");
				}
				if (flag3)
				{
					float num = ((!flag || !(value.Alpha < 0.999f)) ? 0.35f : value.Alpha);
					byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(num * 255f), 0, 255);
					materialRecipe.AciTexId = Materials.CommitFlat(new Color32(b, 0, 0, b), 16);
					materialRecipe.XysTexId = Materials.CommitFlat(new Color32(128, 128, 0, byte.MaxValue), 16);
					Log.Write("MTL_GLASS", "material=" + materialName + " alpha=" + num.ToString("F2") + " — generated flat ACI (red=alpha) and neutral XYS for the Rotors shader.");
				}
				Materials.WriteRecipe(donorName, materialRecipe);
				Log.Write("IMPORT_MATERIAL", "donor=" + donorName + " source=" + ((!flag4) ? "mtl" : "asset-convention maps") + " mtl=" + materialName + " transparent=" + flag3 + ((!flag3 || (flag && value.Transparent)) ? string.Empty : " (by name)") + " shader=" + materialRecipe.Shader + " diffuse=" + ((materialRecipe.MainTexId.Length <= 0) ? "<none>" : materialRecipe.MainTexId.Substring(0, 8)));
			}
			catch (Exception ex)
			{
				Log.Write("IMPORT_MATERIAL_FAILED", donorName + ": " + ex.GetType().Name + " " + ex.Message);
			}
		}

		private static int AttributeIndex(double[] data, int[] index, string mapping, string reference, int polygonVertex, int controlPoint, int stride)
		{
			if (data == null)
			{
				return -1;
			}
			int num = ((!(mapping == "ByControlPoint")) ? polygonVertex : controlPoint);
			if (reference == "IndexToDirect" || reference == "Index")
			{
				if (index == null || num >= index.Length)
				{
					return -1;
				}
				return index[num];
			}
			return num;
		}

		private static List<ParsedGroup> ParseFbx(string path)
		{
			if (!Fbx.IsBinary(path))
			{
				throw new InvalidOperationException("ASCII FBX is not supported. Re-export as binary FBX.");
			}
			FbxNode fbxNode = Fbx.Read(path);
			FbxNode fbxNode2 = fbxNode.Find("Objects");
			if (fbxNode2 == null)
			{
				throw new InvalidOperationException("FBX has no Objects section.");
			}
			List<FbxNode> list = fbxNode2.FindAll("Geometry");
			if (list.Count == 0)
			{
				throw new InvalidOperationException("FBX has no Geometry nodes.");
			}
			List<string> list2 = new List<string>();
			List<FbxNode> list3 = fbxNode2.FindAll("Material");
			for (int i = 0; i < list3.Count; i++)
			{
				string text = list3[i].Text(1);
				int num = text.IndexOf("\0");
				if (num > 0)
				{
					text = text.Substring(0, num);
				}
				list2.Add((text.Length <= 0) ? ("material" + i) : text);
			}
			Log.Write("FBX_OBJECTS", "geometries=" + list.Count + " materials=" + list2.Count + ((list.Count <= 1) ? string.Empty : " — only the first geometry is imported in this version."));
			FbxNode fbxNode3 = list[0];
			double[] array = ((fbxNode3.Find("Vertices") != null) ? fbxNode3.Find("Vertices").Doubles() : null);
			int[] array2 = ((fbxNode3.Find("PolygonVertexIndex") != null) ? fbxNode3.Find("PolygonVertexIndex").Ints() : null);
			if (array == null || array2 == null)
			{
				throw new InvalidOperationException("FBX geometry has no vertices or polygon indices.");
			}
			double[] array3 = null;
			int[] array4 = null;
			string text2 = "Direct";
			string text3 = "ByPolygonVertex";
			FbxNode fbxNode4 = fbxNode3.Find("LayerElementUV");
			if (fbxNode4 != null)
			{
				FbxNode fbxNode5 = fbxNode4.Find("UV");
				FbxNode fbxNode6 = fbxNode4.Find("UVIndex");
				FbxNode fbxNode7 = fbxNode4.Find("ReferenceInformationType");
				FbxNode fbxNode8 = fbxNode4.Find("MappingInformationType");
				if (fbxNode5 != null)
				{
					array3 = fbxNode5.Doubles();
				}
				if (fbxNode6 != null)
				{
					array4 = fbxNode6.Ints();
				}
				if (fbxNode7 != null)
				{
					text2 = fbxNode7.Text(0);
				}
				if (fbxNode8 != null)
				{
					text3 = fbxNode8.Text(0);
				}
			}
			double[] array5 = null;
			int[] index = null;
			string text4 = "Direct";
			string text5 = "ByPolygonVertex";
			FbxNode fbxNode9 = fbxNode3.Find("LayerElementNormal");
			if (fbxNode9 != null)
			{
				FbxNode fbxNode10 = fbxNode9.Find("Normals");
				FbxNode fbxNode11 = fbxNode9.Find("NormalsIndex");
				FbxNode fbxNode12 = fbxNode9.Find("ReferenceInformationType");
				FbxNode fbxNode13 = fbxNode9.Find("MappingInformationType");
				if (fbxNode10 != null)
				{
					array5 = fbxNode10.Doubles();
				}
				if (fbxNode11 != null)
				{
					index = fbxNode11.Ints();
				}
				if (fbxNode12 != null)
				{
					text4 = fbxNode12.Text(0);
				}
				if (fbxNode13 != null)
				{
					text5 = fbxNode13.Text(0);
				}
			}
			int[] array6 = null;
			FbxNode fbxNode14 = fbxNode3.Find("LayerElementMaterial");
			if (fbxNode14 != null)
			{
				FbxNode fbxNode15 = fbxNode14.Find("Materials");
				if (fbxNode15 != null)
				{
					array6 = fbxNode15.Ints();
				}
			}
			ParsedVertices parsedVertices = new ParsedVertices();
			parsedVertices.HasNormals = array5 != null;
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			List<ParsedGroup> list4 = new List<ParsedGroup>();
			Dictionary<int, ParsedGroup> dictionary2 = new Dictionary<int, ParsedGroup>();
			List<int> list5 = new List<int>();
			int num2 = 0;
			for (int j = 0; j < array2.Length; j++)
			{
				int num3 = array2[j];
				bool flag = num3 < 0;
				int num4 = ((!flag) ? num3 : (~num3));
				int num5 = AttributeIndex(array3, array4, text3, text2, j, num4, 2);
				int num6 = AttributeIndex(array5, index, text5, text4, j, num4, 3);
				string key = num4 + "/" + num5 + "/" + num6;
				int value;
				if (!dictionary.TryGetValue(key, out value))
				{
					value = parsedVertices.Positions.Count;
					// FIX: same Blender -> Unity handedness correction as the OBJ path.
					parsedVertices.Positions.Add(new Vector3(-(float)array[num4 * 3], (float)array[num4 * 3 + 1], (float)array[num4 * 3 + 2]));
					parsedVertices.UVs.Add((array3 == null || num5 < 0 || num5 * 2 + 1 >= array3.Length) ? Vector2.zero : new Vector2((float)array3[num5 * 2], (float)array3[num5 * 2 + 1]));
					parsedVertices.Normals.Add((array5 == null || num6 < 0 || num6 * 3 + 2 >= array5.Length) ? Vector3.zero : new Vector3(-(float)array5[num6 * 3], (float)array5[num6 * 3 + 1], (float)array5[num6 * 3 + 2]));
					dictionary.Add(key, value);
				}
				list5.Add(value);
				if (flag)
				{
					int num7 = 0;
					if (array6 != null && array6.Length > 0)
					{
						num7 = ((array6.Length == 1) ? array6[0] : ((num2 < array6.Length) ? array6[num2] : 0));
					}
					ParsedGroup value2;
					if (!dictionary2.TryGetValue(num7, out value2))
					{
						value2 = new ParsedGroup();
						value2.Source = parsedVertices;
						value2.Material = ((num7 < 0 || num7 >= list2.Count) ? ("material" + num7) : list2[num7]);
						dictionary2.Add(num7, value2);
						list4.Add(value2);
					}
					// FIX: restore winding after the X-negation mirror (see OBJ path).
					for (int k = 1; k < list5.Count - 1; k++)
					{
						value2.Triangles.Add(list5[0]);
						value2.Triangles.Add(list5[k + 1]);
						value2.Triangles.Add(list5[k]);
					}
					list5.Clear();
					num2++;
				}
			}
			Log.Write("FBX_LAYERS", "uv=" + text3 + "/" + text2 + " normals=" + text5 + "/" + text4 + " uvValues=" + ((array3 != null) ? (array3.Length / 2) : 0) + " uvIndices=" + ((array4 != null) ? array4.Length : 0) + " polygonVertices=" + array2.Length + " controlPoints=" + array.Length / 3);
			if (parsedVertices.UVs.Count > 0)
			{
				float num8 = float.MaxValue;
				float num9 = float.MinValue;
				float num10 = float.MaxValue;
				float num11 = float.MinValue;
				for (int l = 0; l < parsedVertices.UVs.Count; l++)
				{
					Vector2 vector = parsedVertices.UVs[l];
					if (vector.x < num8)
					{
						num8 = vector.x;
					}
					if (vector.x > num9)
					{
						num9 = vector.x;
					}
					if (vector.y < num10)
					{
						num10 = vector.y;
					}
					if (vector.y > num11)
					{
						num11 = vector.y;
					}
				}
				StringBuilder stringBuilder = new StringBuilder();
				for (int m = 0; m < Mathf.Min(4, parsedVertices.UVs.Count); m++)
				{
					if (m > 0)
					{
						stringBuilder.Append(" ");
					}
					stringBuilder.Append(parsedVertices.UVs[m].ToString("F3"));
				}
				Log.Write("FBX_UV_RANGE", "u=[" + num8.ToString("F3") + "," + num9.ToString("F3") + "] v=[" + num10.ToString("F3") + "," + num11.ToString("F3") + "] first=" + stringBuilder);
			}
			Log.Write("FBX_GEOMETRY", "vertices=" + parsedVertices.Positions.Count + " polygons=" + num2 + " parts=" + list4.Count + " uvs=" + (array3 != null) + " normals=" + (array5 != null));
			return list4;
		}

		private static Dictionary<string, MtlEntry> ReadMtl(string objPath)
		{
			Dictionary<string, MtlEntry> dictionary = new Dictionary<string, MtlEntry>();
			try
			{
				string directoryName = Path.GetDirectoryName(objPath);
				List<string> list = new List<string>();
				using (StreamReader streamReader = new StreamReader(objPath))
				{
					string text;
					while ((text = streamReader.ReadLine()) != null)
					{
						string text2 = text.Trim();
						if (text2.StartsWith("mtllib "))
						{
							list.Add(text2.Substring(7).Trim());
						}
					}
				}
				string item = Path.GetFileNameWithoutExtension(objPath) + ".mtl";
				if (!list.Contains(item))
				{
					list.Add(item);
				}
				for (int i = 0; i < list.Count; i++)
				{
					string path = Path.Combine(directoryName, list[i]);
					if (!File.Exists(path))
					{
						continue;
					}
					MtlEntry mtlEntry = null;
					string[] array = File.ReadAllLines(path);
					for (int j = 0; j < array.Length; j++)
					{
						string[] array2 = array[j].Trim().Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
						if (array2.Length < 2)
						{
							continue;
						}
						if (array2[0] == "newmtl")
						{
							mtlEntry = new MtlEntry();
							dictionary[array2[1]] = mtlEntry;
						}
						else if (mtlEntry != null)
						{
							if (array2[0] == "map_Kd")
							{
								mtlEntry.DiffuseMap = string.Join(" ", array2, 1, array2.Length - 1).Trim();
							}
							else if (array2[0] == "d")
							{
								mtlEntry.Alpha = F(array2[1]);
							}
							else if (array2[0] == "Tr")
							{
								mtlEntry.Alpha = 1f - F(array2[1]);
							}
							else if (array2[0] == "Kd" && array2.Length >= 4)
							{
								mtlEntry.Colour = new Color(F(array2[1]), F(array2[2]), F(array2[3]), 1f);
							}
						}
					}
					Log.Write("MTL_READ", "file=" + list[i] + " materials=" + dictionary.Count);
				}
			}
			catch (Exception ex)
			{
				Log.Write("MTL_FAILED", ex.GetType().Name + ": " + ex.Message);
			}
			return dictionary;
		}

		private static List<ParsedGroup> Parse(string path)
		{
			List<Vector3> list = new List<Vector3>();
			List<Vector2> list2 = new List<Vector2>();
			List<Vector3> list3 = new List<Vector3>();
			ParsedVertices parsedVertices = new ParsedVertices();
			List<ParsedGroup> list4 = new List<ParsedGroup>();
			ParsedGroup parsedGroup = new ParsedGroup();
			parsedGroup.Source = parsedVertices;
			list4.Add(parsedGroup);
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			char[] separator = new char[2] { ' ', '\t' };
			using (StreamReader streamReader = new StreamReader(path))
			{
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					if (text.Length == 0 || text[0] == '#')
					{
						continue;
					}
					string[] array = text.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
					if (array.Length < 2)
					{
						continue;
					}
					if (array[0] == "v" && array.Length >= 4)
					{
						// FIX: negate X to correct the Blender (right-handed) -> Unity
						// (left-handed) handedness mismatch. Without this, geometry
						// arrives mirrored on X (see GitHub issue #1).
						list.Add(new Vector3(-F(array[1]), F(array[2]), F(array[3])));
					}
					else if (array[0] == "vt" && array.Length >= 3)
					{
						list2.Add(new Vector2(F(array[1]), F(array[2])));
					}
					else if (array[0] == "vn" && array.Length >= 4)
					{
						// FIX: normals need the same X negation as positions, or
						// lighting/backface behaviour will be wrong even though
						// the shape itself looks correct.
						list3.Add(new Vector3(-F(array[1]), F(array[2]), F(array[3])));
					}
					else if (array[0] == "usemtl" && array.Length >= 2)
					{
						string text2 = array[1];
						ParsedGroup parsedGroup2 = null;
						for (int i = 0; i < list4.Count; i++)
						{
							if (list4[i].Material == text2)
							{
								parsedGroup2 = list4[i];
								break;
							}
						}
						if (parsedGroup2 == null)
						{
							parsedGroup2 = new ParsedGroup();
							parsedGroup2.Material = text2;
							parsedGroup2.Source = parsedVertices;
							list4.Add(parsedGroup2);
						}
						parsedGroup = parsedGroup2;
					}
					else
					{
						if (!(array[0] == "f") || array.Length < 4)
						{
							continue;
						}
						List<int> list5 = new List<int>();
						for (int j = 1; j < array.Length; j++)
						{
							string text3 = array[j];
							int value;
							if (!dictionary.TryGetValue(text3, out value))
							{
								string[] array2 = text3.Split('/');
								int num = Idx(array2[0], list.Count);
								int num2 = ((array2.Length <= 1 || array2[1].Length <= 0) ? (-1) : Idx(array2[1], list2.Count));
								int num3 = ((array2.Length <= 2 || array2[2].Length <= 0) ? (-1) : Idx(array2[2], list3.Count));
								if (num < 0 || num >= list.Count)
								{
									throw new InvalidOperationException("Vertex index out of range: " + text3);
								}
								value = parsedVertices.Positions.Count;
								parsedVertices.Positions.Add(list[num]);
								parsedVertices.UVs.Add((num2 < 0 || num2 >= list2.Count) ? Vector2.zero : list2[num2]);
								if (num3 >= 0 && num3 < list3.Count)
								{
									parsedVertices.Normals.Add(list3[num3]);
									parsedVertices.HasNormals = true;
								}
								else
								{
									parsedVertices.Normals.Add(Vector3.zero);
								}
								dictionary.Add(text3, value);
							}
							list5.Add(value);
						}
						// FIX: negating X above is a mirror reflection, which reverses
						// triangle winding. Swap the last two indices to restore
						// correct front-face winding (otherwise the shape is right
						// but normals/culling read as inside-out).
						for (int k = 1; k < list5.Count - 1; k++)
						{
							parsedGroup.Triangles.Add(list5[0]);
							parsedGroup.Triangles.Add(list5[k + 1]);
							parsedGroup.Triangles.Add(list5[k]);
						}
					}
				}
			}
			List<ParsedGroup> list6 = new List<ParsedGroup>();
			for (int l = 0; l < list4.Count; l++)
			{
				if (list4[l].Triangles.Count > 0)
				{
					list6.Add(list4[l]);
				}
			}
			return list6;
		}

		private static float F(string s)
		{
			return float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
		}

		private static int Idx(string s, int count)
		{
			int num = int.Parse(s, CultureInfo.InvariantCulture);
			return (num <= 0) ? (count + num) : (num - 1);
		}

		// A model key groups the resources that belong to ONE model, so that Place
		// puts them down together. The donor name carries everything needed:
		//   MeshBridge.<model>.<material>.<hash8>_Data   one model, several materials
		//   MeshBridge.<model>.<hash8>_Data              one model, one material
		// A single-material model is keyed on its hash as well as its name, because
		// two imports of the same file share a name but are different geometry, and
		// grouping them would place both meshes on top of each other.
		public static string ModelKeyFromDonor(string donorName)
		{
			string text = donorName;
			if (text.StartsWith("MeshBridge."))
			{
				text = text.Substring("MeshBridge.".Length);
			}
			if (text.EndsWith("_Data"))
			{
				text = text.Substring(0, text.Length - 5);
			}
			string[] array = text.Split('.');
			if (array.Length >= 3)
			{
				return array[0];
			}
			if (array.Length == 2)
			{
				return array[0] + "#" + array[1];
			}
			return (text.Length != 0) ? text : donorName;
		}

		// The hash qualifier is for grouping, not for reading. Strip it for display.
		public static string ModelLabel(string modelKey)
		{
			int num = modelKey.IndexOf('#');
			return (num > 0) ? modelKey.Substring(0, num) : modelKey;
		}

		private static string Slug(string s)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char c in s)
			{
				if (char.IsLetterOrDigit(c))
				{
					stringBuilder.Append(c);
				}
				else if (c == ' ' || c == '_' || c == '-')
				{
					stringBuilder.Append('_');
				}
			}
			string text = stringBuilder.ToString().Trim('_');
			return (text.Length != 0) ? text : "model";
		}

		private static void Tangents(Mesh mesh)
		{
			Vector3[] vertices = mesh.vertices;
			Vector2[] uv = mesh.uv;
			int[] triangles = mesh.triangles;
			Vector3[] normals = mesh.normals;
			Vector3[] array = new Vector3[vertices.Length];
			Vector3[] array2 = new Vector3[vertices.Length];
			Vector4[] array3 = new Vector4[vertices.Length];
			for (int i = 0; i < triangles.Length; i += 3)
			{
				int num = triangles[i];
				int num2 = triangles[i + 1];
				int num3 = triangles[i + 2];
				Vector3 vector = vertices[num];
				Vector3 vector2 = vertices[num2];
				Vector3 vector3 = vertices[num3];
				Vector2 vector4 = uv[num];
				Vector2 vector5 = uv[num2];
				Vector2 vector6 = uv[num3];
				float num4 = vector2.x - vector.x;
				float num5 = vector3.x - vector.x;
				float num6 = vector2.y - vector.y;
				float num7 = vector3.y - vector.y;
				float num8 = vector2.z - vector.z;
				float num9 = vector3.z - vector.z;
				float num10 = vector5.x - vector4.x;
				float num11 = vector6.x - vector4.x;
				float num12 = vector5.y - vector4.y;
				float num13 = vector6.y - vector4.y;
				float num14 = num10 * num13 - num11 * num12;
				float num15 = ((!Mathf.Approximately(num14, 0f)) ? (1f / num14) : 0f);
				Vector3 vector7 = new Vector3((num13 * num4 - num12 * num5) * num15, (num13 * num6 - num12 * num7) * num15, (num13 * num8 - num12 * num9) * num15);
				Vector3 vector8 = new Vector3((num10 * num5 - num11 * num4) * num15, (num10 * num7 - num11 * num6) * num15, (num10 * num9 - num11 * num8) * num15);
				array[num] += vector7;
				array[num2] += vector7;
				array[num3] += vector7;
				array2[num] += vector8;
				array2[num2] += vector8;
				array2[num3] += vector8;
			}
			for (int j = 0; j < vertices.Length; j++)
			{
				Vector3 normal = normals[j];
				Vector3 tangent = array[j];
				if (tangent.sqrMagnitude < 1E-12f)
				{
					tangent = ((!(Mathf.Abs(normal.x) < 0.9f)) ? Vector3.up : Vector3.right);
				}
				Vector3.OrthoNormalize(ref normal, ref tangent);
				array3[j] = new Vector4(tangent.x, tangent.y, tangent.z, (!(Vector3.Dot(Vector3.Cross(normal, tangent), array2[j]) < 0f)) ? 1f : (-1f));
			}
			mesh.tangents = array3;
		}

		private static void WriteCatalog(List<ImportedResource> items)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("# MeshBridge import catalog");
				stringBuilder.AppendLine("# donorName | source file | geometryId | vertices | triangles");
				stringBuilder.AppendLine("# The donor name is what a saved city references. The geometryId is the");
				stringBuilder.AppendLine("# content hash of the committed buffers. Editing a source file and");
				stringBuilder.AppendLine("# re-importing produces a NEW donor name; existing cities are unaffected.");
				for (int i = 0; i < items.Count; i++)
				{
					ImportedResource importedResource = items[i];
					stringBuilder.AppendLine(importedResource.DonorName + " | " + importedResource.SourceFile + " | " + importedResource.GeometryId + " | " + importedResource.VertexCount + " | " + importedResource.TriangleCount + ((importedResource.MaterialName.Length <= 0) ? string.Empty : (" | mtl:" + importedResource.MaterialName)));
				}
				string directoryName = Path.GetDirectoryName(CatalogPath);
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				File.WriteAllText(CatalogPath, stringBuilder.ToString());
				Log.Write("CATALOG", "entries=" + items.Count + " path=" + CatalogPath);
			}
			catch (Exception ex)
			{
				Log.Write("CATALOG_FAILED", ex.Message);
			}
		}
	}
}
