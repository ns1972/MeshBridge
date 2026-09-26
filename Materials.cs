using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MeshBridge
{
	public static class Materials
	{
		private const string Magic = "MBT1";

		private static string Root
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(Importer.ImportFolder), "Store");
			}
		}

		private static string RecipesPath
		{
			get
			{
				return Path.Combine(Root, "materials.txt");
			}
		}

		public static string TextureDir
		{
			get
			{
				return Path.Combine(Root, "textures");
			}
		}

		private static string ShardedTexPath(string id)
		{
			if (string.IsNullOrEmpty(id) || id.Length < 2)
			{
				throw new ArgumentException("Texture id must be a full hash; got '" + id + "'.");
			}
			return Path.Combine(Path.Combine(TextureDir, id.Substring(0, 2)), id + ".mbt");
		}

		private static string LegacyTexPath(string id)
		{
			return Path.Combine(Root, id + ".mbt");
		}

		public static string TexPath(string id)
		{
			if (string.IsNullOrEmpty(id) || id.Length < 2)
			{
				return string.Empty;
			}
			string text = ShardedTexPath(id);
			if (File.Exists(text))
			{
				return text;
			}
			string text2 = LegacyTexPath(id);
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
				string[] files = Directory.GetFiles(Root, "*.mbt");
				for (int i = 0; i < files.Length; i++)
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
					if (fileNameWithoutExtension.Length != 64)
					{
						continue;
					}
					string text = ShardedTexPath(fileNameWithoutExtension);
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
						Log.Write("TEXTURE_MIGRATE_FAILED", "id=" + fileNameWithoutExtension);
					}
				}
				if (num > 0)
				{
					Log.Write("TEXTURE_MIGRATE", "texture payloads moved to sharded layout: " + num);
				}
			}
			catch (Exception ex)
			{
				Log.Write("TEXTURE_MIGRATE_FAILED", ex.Message);
			}
			return num;
		}

		public static MaterialRecipe Capture(Material source)
		{
			MaterialRecipe materialRecipe = new MaterialRecipe();
			if (source == null)
			{
				return materialRecipe;
			}
			materialRecipe.MainTexId = CaptureSlot(source, "_MainTex");
			materialRecipe.AciTexId = CaptureSlot(source, "_ACIMap");
			materialRecipe.XysTexId = CaptureSlot(source, "_XYSMap");
			if (source.HasProperty("_Color"))
			{
				materialRecipe.Colour = source.GetColor("_Color");
			}
			if (source.shader != null)
			{
				materialRecipe.Shader = source.shader.name;
			}
			return materialRecipe;
		}

		private static string CaptureSlot(Material m, string property)
		{
			try
			{
				if (!m.HasProperty(property))
				{
					return string.Empty;
				}
				Texture texture = m.GetTexture(property);
				if (texture == null)
				{
					return string.Empty;
				}
				byte[] array = ToPng(texture);
				if (array == null || array.Length == 0)
				{
					return string.Empty;
				}
				string text = Log.Hash(array);
				string path = TexPath(text);
				if (!File.Exists(path))
				{
					path = ShardedTexPath(text);
					string directoryName = Path.GetDirectoryName(path);
					if (!Directory.Exists(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					string text2 = path + ".staging";
					File.WriteAllBytes(text2, array);
					byte[] data = File.ReadAllBytes(text2);
					if (Log.Hash(data) != text)
					{
						try
						{
							File.Delete(text2);
						}
						catch
						{
						}
						throw new InvalidOperationException("Texture staging verification failed.");
					}
					if (File.Exists(path))
					{
						File.Delete(path);
					}
					File.Move(text2, path);
					Log.Write("TEXTURE_COMMIT", "slot=" + property + " id=" + text + " bytes=" + array.Length + " size=" + texture.width + "x" + texture.height);
				}
				return text;
			}
			catch (Exception ex)
			{
				Log.Write("TEXTURE_CAPTURE_FAILED", property + ": " + ex.GetType().Name + " " + ex.Message);
				return string.Empty;
			}
		}

		private static byte[] ToPng(Texture tex)
		{
			RenderTexture active = RenderTexture.active;
			RenderTexture temporary = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
			try
			{
				Graphics.Blit(tex, temporary);
				RenderTexture.active = temporary;
				Texture2D texture2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
				texture2D.ReadPixels(new Rect(0f, 0f, tex.width, tex.height), 0, 0);
				texture2D.Apply();
				byte[] result = texture2D.EncodeToPNG();
				UnityEngine.Object.DestroyImmediate(texture2D);
				return result;
			}
			finally
			{
				RenderTexture.active = active;
				RenderTexture.ReleaseTemporary(temporary);
			}
		}

		public static string CommitBytes(byte[] data)
		{
			if (data == null || data.Length == 0)
			{
				return string.Empty;
			}
			string text = Log.Hash(data);
			string path = TexPath(text);
			if (File.Exists(path))
			{
				return text;
			}
			path = ShardedTexPath(text);
			string directoryName = Path.GetDirectoryName(path);
			if (!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			string text2 = path + ".staging";
			File.WriteAllBytes(text2, data);
			if (Log.Hash(File.ReadAllBytes(text2)) != text)
			{
				try
				{
					File.Delete(text2);
				}
				catch
				{
				}
				throw new InvalidOperationException("Texture staging verification failed.");
			}
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			File.Move(text2, path);
			Log.Write("TEXTURE_COMMIT", "source=import id=" + text + " bytes=" + data.Length);
			return text;
		}

		public static string CommitFlat(Color32 colour, int size)
		{
			Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, false);
			try
			{
				Color32[] array = new Color32[size * size];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = colour;
				}
				texture2D.SetPixels32(array);
				texture2D.Apply();
				return CommitBytes(texture2D.EncodeToPNG());
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(texture2D);
			}
		}

		public static string CommitComposite(Texture2D r, Texture2D g, Texture2D b, Color32 fallback, string label)
		{
			int num = 0;
			int num2 = 0;
			Texture2D[] array = new Texture2D[3] { r, g, b };
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i] == null))
				{
					if (array[i].width > num)
					{
						num = array[i].width;
					}
					if (array[i].height > num2)
					{
						num2 = array[i].height;
					}
				}
			}
			if (num == 0)
			{
				return string.Empty;
			}
			Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, false);
			try
			{
				Color32[] array2 = new Color32[num * num2];
				Color32[] array3 = Sample(r, num, num2);
				Color32[] array4 = Sample(g, num, num2);
				Color32[] array5 = Sample(b, num, num2);
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j] = new Color32((array3 == null) ? fallback.r : array3[j].r, (array4 == null) ? fallback.g : array4[j].r, (array5 == null) ? fallback.b : array5[j].r, byte.MaxValue);
				}
				texture2D.SetPixels32(array2);
				texture2D.Apply();
				string text = CommitBytes(texture2D.EncodeToPNG());
				Log.Write("TEXTURE_COMPOSE", label + " " + num + "x" + num2 + " r=" + (r != null) + " g=" + (g != null) + " b=" + (b != null) + " id=" + ((text.Length < 8) ? "<none>" : text.Substring(0, 8)));
				return text;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(texture2D);
			}
		}

		private static Color32[] Sample(Texture2D t, int w, int h)
		{
			if (t == null)
			{
				return null;
			}
			if (t.width == w && t.height == h)
			{
				return t.GetPixels32();
			}
			Color32[] array = new Color32[w * h];
			for (int i = 0; i < h; i++)
			{
				for (int j = 0; j < w; j++)
				{
					array[i * w + j] = t.GetPixelBilinear((float)j / (float)w, (float)i / (float)h);
				}
			}
			return array;
		}

		public static Texture2D LoadFile(string path, bool linear)
		{
			try
			{
				if (!File.Exists(path))
				{
					return null;
				}
				Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
				if (!texture2D.LoadImage(File.ReadAllBytes(path)))
				{
					UnityEngine.Object.DestroyImmediate(texture2D);
					return null;
				}
				return texture2D;
			}
			catch
			{
				return null;
			}
		}

		public static bool HasNonWhite(Texture2D t)
		{
			if (t == null)
			{
				return false;
			}
			Color32[] pixels = t.GetPixels32();
			for (int i = 0; i < pixels.Length; i++)
			{
				if (pixels[i].r < 250)
				{
					return true;
				}
			}
			return false;
		}

		public static Texture2D LoadTexture(string id, bool linear, string name)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			string path = TexPath(id);
			if (!File.Exists(path))
			{
				return null;
			}
			byte[] data = File.ReadAllBytes(path);
			if (Log.Hash(data) != id)
			{
				Log.Write("TEXTURE_CORRUPT", "id=" + id + " — hash mismatch; refusing to load.");
				return null;
			}
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
			if (!texture2D.LoadImage(data))
			{
				UnityEngine.Object.DestroyImmediate(texture2D);
				return null;
			}
			texture2D.name = name;
			texture2D.Apply();
			return texture2D;
		}

		public static bool Has(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			string text = TexPath(id);
			return text.Length > 0 && File.Exists(text);
		}

		public static Dictionary<string, MaterialRecipe> ReadRecipes()
		{
			Dictionary<string, MaterialRecipe> dictionary = new Dictionary<string, MaterialRecipe>();
			if (!File.Exists(RecipesPath))
			{
				return dictionary;
			}
			string[] array = File.ReadAllLines(RecipesPath);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0 || text[0] == '#')
				{
					continue;
				}
				int num = text.IndexOf('=');
				if (num <= 0)
				{
					continue;
				}
				string key = text.Substring(0, num).Trim();
				string[] array2 = text.Substring(num + 1).Trim().Split(',');
				if (array2.Length >= 3)
				{
					MaterialRecipe materialRecipe = new MaterialRecipe();
					materialRecipe.MainTexId = array2[0].Trim();
					materialRecipe.AciTexId = array2[1].Trim();
					materialRecipe.XysTexId = array2[2].Trim();
					float result;
					float result2;
					float result3;
					float result4;
					if (array2.Length >= 7 && float.TryParse(array2[3], out result) && float.TryParse(array2[4], out result2) && float.TryParse(array2[5], out result3) && float.TryParse(array2[6], out result4))
					{
						materialRecipe.Colour = new Color(result, result2, result3, result4);
					}
					if (array2.Length >= 8)
					{
						materialRecipe.Shader = array2[7].Trim();
					}
					dictionary[key] = materialRecipe;
				}
			}
			return dictionary;
		}

		// Drops a donor's material recipe. Texture payloads are shared by content hash
		// and are left alone.
		public static void RemoveRecipe(string donorName)
		{
			try
			{
				Dictionary<string, MaterialRecipe> dictionary = ReadRecipes();
				if (!dictionary.ContainsKey(donorName))
				{
					return;
				}
				dictionary.Remove(donorName);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("# MeshBridge material recipes");
				stringBuilder.AppendLine("# donorName = mainTexId,aciTexId,xysTexId,r,g,b,a,shaderName");
				foreach (KeyValuePair<string, MaterialRecipe> item in dictionary)
				{
					MaterialRecipe value = item.Value;
					stringBuilder.AppendLine(item.Key + " = " + value.MainTexId + "," + value.AciTexId + "," + value.XysTexId + "," + value.Colour.r + "," + value.Colour.g + "," + value.Colour.b + "," + value.Colour.a + "," + ((value.Shader.Length <= 0) ? "Custom/Props/Prop/Default" : value.Shader));
				}
				if (!Directory.Exists(Root))
				{
					Directory.CreateDirectory(Root);
				}
				string text = RecipesPath + ".staging";
				File.WriteAllText(text, stringBuilder.ToString());
				if (File.Exists(RecipesPath))
				{
					File.Delete(RecipesPath);
				}
				File.Move(text, RecipesPath);
				Log.Write("MATERIAL_UNBIND", "donor=" + donorName);
			}
			catch (Exception ex)
			{
				Log.Write("MATERIAL_UNBIND_FAILED", donorName + ": " + ex.Message);
			}
		}

		public static void WriteRecipe(string donorName, MaterialRecipe recipe)
		{
			Dictionary<string, MaterialRecipe> dictionary = ReadRecipes();
			dictionary[donorName] = recipe;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("# MeshBridge material recipes");
			stringBuilder.AppendLine("# donorName = mainTexId,aciTexId,xysTexId,r,g,b,a,shaderName");
			foreach (KeyValuePair<string, MaterialRecipe> item in dictionary)
			{
				MaterialRecipe value = item.Value;
				stringBuilder.AppendLine(item.Key + " = " + value.MainTexId + "," + value.AciTexId + "," + value.XysTexId + "," + value.Colour.r + "," + value.Colour.g + "," + value.Colour.b + "," + value.Colour.a + "," + ((value.Shader.Length <= 0) ? "Custom/Props/Prop/Default" : value.Shader));
			}
			if (!Directory.Exists(Root))
			{
				Directory.CreateDirectory(Root);
			}
			string text = RecipesPath + ".staging";
			File.WriteAllText(text, stringBuilder.ToString());
			if (File.Exists(RecipesPath))
			{
				File.Delete(RecipesPath);
			}
			File.Move(text, RecipesPath);
			Log.Write("MATERIAL_BIND", "donor=" + donorName + " main=" + Short(recipe.MainTexId) + " aci=" + Short(recipe.AciTexId) + " xys=" + Short(recipe.XysTexId) + " shader=" + ((recipe.Shader.Length <= 0) ? "<none>" : recipe.Shader));
		}

		private static string Short(string id)
		{
			return (id.Length < 8) ? "<none>" : id.Substring(0, 8);
		}
	}
}
