using System;
using System.Reflection;
using ICities;
using UnityEngine;

namespace MeshBridge
{
	public class Loading : LoadingExtensionBase
	{
		private GameObject host;

		public override void OnLevelLoaded(LoadMode mode)
		{
			base.OnLevelLoaded(mode);
			try
			{
				Log.Write("LOAD", string.Concat("mode=", mode, " unity=", Application.unityVersion, " log=", Log.LogPath));
				Assembly assembly = null;
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (int i = 0; i < assemblies.Length; i++)
				{
					if (assemblies[i].GetName().Name == "ProceduralObjects")
					{
						assembly = assemblies[i];
						break;
					}
				}
				Log.Require(assembly != null, "ProceduralObjects.dll not loaded. Enable Procedural Objects and restart the game.");
				Guid moduleVersionId = assembly.ManifestModule.ModuleVersionId;
				Log.Write("DEPENDENCY", "PO=" + assembly.FullName + " MVID=" + moduleVersionId);
				if (mode != LoadMode.NewGame && mode != LoadMode.LoadGame && mode != LoadMode.NewGameFromScenario)
				{
					Log.Write("LOAD", "Not an in-game load mode; MeshBridge idle.");
				}
				else
				{
					StartBridge();
				}
			}
			catch (Exception ex)
			{
				Log.Write("LOAD_EXCEPTION", ex.GetType().FullName + ": " + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void StartBridge()
		{
			host = new GameObject("MeshBridge.Driver.v005");
			host.hideFlags = HideFlags.DontSave;
			UnityEngine.Object.DontDestroyOnLoad(host);
			Bridge bridge = host.AddComponent<Bridge>();
			bridge.ReconstructDonor();
			Log.Write("READY", "Ctrl+Shift+B: model window. Ctrl+Shift+M: place test prism. Ctrl+Shift+L: audit.");
		}

		public override void OnLevelUnloading()
		{
			base.OnLevelUnloading();
			try
			{
				if (host != null)
				{
					Bridge component = host.GetComponent<Bridge>();
					if (component != null)
					{
						component.Cleanup();
					}
					UnityEngine.Object.DestroyImmediate(host);
					host = null;
				}
				Log.Write("UNLOAD", "Session ends. Owned donors/resources cleaned up; the deterministic donor is reconstructed synchronously on the next city load.");
			}
			catch (Exception ex)
			{
				Log.Write("UNLOAD_EXCEPTION", ex.GetType().FullName + ": " + ex.Message);
			}
		}
	}
}
