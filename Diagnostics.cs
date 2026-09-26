using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

namespace MeshBridge
{
	// A one-click report a beta tester can send without being asked to hunt for
	// files. Collected on demand only — nothing here runs unless the user presses
	// the button, so there is no background cost.
	public static class Diagnostics
	{
		private const int MaxLogBytes = 8388608;

		public static string Root
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Log.LogPath)), "Diagnostics");
			}
		}

		public static string GameLogPath
		{
			get
			{
				try
				{
					return Path.Combine(Application.dataPath, "output_log.txt");
				}
				catch
				{
					return string.Empty;
				}
			}
		}

		// The logs carry absolute paths, which on Windows contain the account name.
		// That is the tester's, not ours, and it is not needed to diagnose anything.
		public static string Redact(string text)
		{
			if (text == null || text.Length == 0)
			{
				return string.Empty;
			}
			string text2 = text;
			try
			{
				string userName = Environment.UserName;
				if (userName.Length > 1)
				{
					text2 = text2.Replace("\\" + userName + "\\", "\\<user>\\");
					text2 = text2.Replace("/" + userName + "/", "/<user>/");
				}
				// SpecialFolder.UserProfile does not exist on this framework version.
				string environmentVariable = Environment.GetEnvironmentVariable("USERPROFILE");
				if (environmentVariable != null && environmentVariable.Length > 3)
				{
					text2 = text2.Replace(environmentVariable, "<userprofile>");
				}
			}
			catch
			{
			}
			return text2;
		}

		private static string ReadTail(string path, out long originalBytes, out bool truncated)
		{
			originalBytes = 0L;
			truncated = false;
			if (path.Length == 0 || !File.Exists(path))
			{
				return string.Empty;
			}
			using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				originalBytes = fileStream.Length;
				long num = 0L;
				if (fileStream.Length > MaxLogBytes)
				{
					num = fileStream.Length - MaxLogBytes;
					truncated = true;
					fileStream.Seek(num, SeekOrigin.Begin);
				}
				byte[] array = new byte[fileStream.Length - num];
				int num2 = fileStream.Read(array, 0, array.Length);
				return Encoding.UTF8.GetString(array, 0, num2);
			}
		}

		// What the ZIP will contain, so the user can see it before it is written
		// rather than after it has been sent.
		public static List<string> Preview()
		{
			List<string> list = new List<string>();
			list.Add(Describe("meshbridge.log", Log.LogPath, "everything MeshBridge logged this session and before"));
			list.Add(Describe("output_log.txt", GameLogPath, "the game's own log — your mod list and any mod's errors"));
			list.Add("state.txt  —  MeshBridge version, store and package counts, settings");
			list.Add("readme.txt  —  what this is, and a form to describe the problem");
			return list;
		}

		private static string Describe(string name, string path, string note)
		{
			if (path.Length == 0 || !File.Exists(path))
			{
				return name + "  —  not found, will be skipped";
			}
			long length = new FileInfo(path).Length;
			return name + "  —  " + (length / 1024L) + " KB, " + note;
		}

		private static string State()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("MeshBridge " + Log.Version);
			stringBuilder.AppendLine("captured " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
			stringBuilder.AppendLine("unity " + Application.unityVersion + "   platform " + Application.platform);
			stringBuilder.AppendLine("screen " + Screen.width + "x" + Screen.height);
			stringBuilder.AppendLine();
			try
			{
				Dictionary<string, string> dictionary = Store.ReadBindings();
				stringBuilder.AppendLine("bindings: " + dictionary.Count);
				int num = 0;
				foreach (KeyValuePair<string, string> item in dictionary)
				{
					if (!Store.Has(item.Value))
					{
						num++;
						stringBuilder.AppendLine("  MISSING PAYLOAD  " + item.Key + " -> " + item.Value);
					}
				}
				stringBuilder.AppendLine("missing payloads: " + num);
			}
			catch (Exception ex)
			{
				stringBuilder.AppendLine("bindings: unreadable — " + ex.Message);
			}
			stringBuilder.AppendLine();
			try
			{
				List<PackageInfo> list = Packages.List();
				stringBuilder.AppendLine("packages: " + list.Count);
				for (int i = 0; i < list.Count; i++)
				{
					stringBuilder.AppendLine("  " + list[i].Name + "  objects=" + list[i].ObjectCount + " resources=" + list[i].BaseNames.Count + ((!list[i].Complete) ? (" INCOMPLETE missing=" + list[i].Missing.Count) : " complete"));
				}
			}
			catch (Exception ex2)
			{
				stringBuilder.AppendLine("packages: unreadable — " + ex2.Message);
			}
			return Redact(stringBuilder.ToString());
		}

		private static string Readme()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("MeshBridge diagnostic report");
			stringBuilder.AppendLine("============================");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("This was created by pressing Diagnostics in the MeshBridge window.");
			stringBuilder.AppendLine("Send the whole .zip.");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("What is inside");
			stringBuilder.AppendLine("  meshbridge.log   what MeshBridge did");
			stringBuilder.AppendLine("  output_log.txt   the game's own log, which lists your installed mods");
			stringBuilder.AppendLine("                   and records errors from any of them, not only this one");
			stringBuilder.AppendLine("  state.txt        MeshBridge version, store contents, package status");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Your Windows account name has been replaced with <user> in the logs.");
			stringBuilder.AppendLine("You are welcome to open any of these files and read them first.");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("IMPORTANT — capture the report before restarting");
			stringBuilder.AppendLine("  The game overwrites output_log.txt every time it launches. If you quit");
			stringBuilder.AppendLine("  and reload to make the report, the evidence is already gone. Press");
			stringBuilder.AppendLine("  Diagnostics in the same session the problem happened in.");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Please fill this in");
			stringBuilder.AppendLine("-------------------");
			stringBuilder.AppendLine("What were you doing?");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("What did you expect to happen?");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("What happened instead?");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Does it happen every time, or did it happen once?");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("If a model is involved: where did it come from, and what is it?");
			stringBuilder.AppendLine();
			return stringBuilder.ToString();
		}

		// SharpZipLib 0.86 encodes entry names with IBM code page 437 by default and
		// Unity's trimmed Mono has no such encoding, so the first PutNextEntry throws
		// NotSupportedException and the archive is left empty. UTF-8 is always present.
		private static void UseUtf8Names()
		{
			try
			{
				if (ZipConstants.DefaultCodePage != 65001)
				{
					ZipConstants.DefaultCodePage = 65001;
				}
			}
			catch (Exception ex)
			{
				Log.Write("DIAGNOSTIC", "could not set zip code page: " + ex.Message);
			}
		}

		private static void AddText(ZipOutputStream zip, string name, string content)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(content);
			ZipEntry zipEntry = new ZipEntry(name);
			zipEntry.IsUnicodeText = true;
			zipEntry.DateTime = DateTime.Now;
			zipEntry.Size = bytes.Length;
			zip.PutNextEntry(zipEntry);
			zip.Write(bytes, 0, bytes.Length);
			zip.CloseEntry();
		}

		private static bool AddLog(ZipOutputStream zip, string name, string path)
		{
			long originalBytes;
			bool truncated;
			string text = ReadTail(path, out originalBytes, out truncated);
			if (text.Length == 0)
			{
				return false;
			}
			string text2 = Redact(text);
			if (truncated)
			{
				text2 = "[MeshBridge] This log was " + originalBytes + " bytes. Only the last " + 8388608 + " bytes are included." + Environment.NewLine + text2;
			}
			AddText(zip, name, text2);
			return true;
		}

		public static string Build()
		{
			if (!Directory.Exists(Root))
			{
				Directory.CreateDirectory(Root);
			}
			string text = Path.Combine(Root, "MeshBridge-diagnostic-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip");
			UseUtf8Names();
			int num = 0;
			try
			{
			using (FileStream baseOutputStream = File.Create(text))
			{
				using (ZipOutputStream zipOutputStream = new ZipOutputStream(baseOutputStream))
				{
					zipOutputStream.SetLevel(6);
					AddText(zipOutputStream, "readme.txt", Readme());
					AddText(zipOutputStream, "state.txt", State());
					if (AddLog(zipOutputStream, "meshbridge.log", Log.LogPath))
					{
						num++;
					}
					if (AddLog(zipOutputStream, "output_log.txt", GameLogPath))
					{
						num++;
					}
					zipOutputStream.Finish();
				}
			}
			}
			catch (Exception)
			{
				// Half a zip is worse than none: a tester would send it and we would
				// spend a round trip discovering it was empty.
				try
				{
					if (File.Exists(text))
					{
						File.Delete(text);
					}
				}
				catch
				{
				}
				throw;
			}
			long length = new FileInfo(text).Length;
			if (length < 256L)
			{
				File.Delete(text);
				throw new IOException("the archive came out empty (" + length + " bytes) — nothing was collected");
			}
			Log.Write("DIAGNOSTIC", "wrote " + Path.GetFileName(text) + " logs=" + num + " bytes=" + length);
			return text;
		}
	}
}
