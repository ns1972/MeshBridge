using System;
using System.Collections.Generic;
using System.Text;
using ICities;

namespace MeshBridge
{
	public class Serializer : SerializableDataExtensionBase
	{
		public override void OnLoadData()
		{
			base.OnLoadData();
			Manifest.Pending = new List<ManifestEntry>();
			Manifest.PendingValid = false;
			try
			{
				byte[] array = base.serializableDataManager.LoadData("MeshBridge.Manifest.v1");
				if (array == null || array.Length == 0)
				{
					Log.Write("MANIFEST_LOAD", "No MeshBridge manifest in this save (city predates the manifest, or uses no MeshBridge resources).");
					Manifest.PendingValid = true;
					return;
				}
				Manifest.Pending = Manifest.Deserialise(array);
				Manifest.PendingValid = true;
				Log.Write("MANIFEST_LOAD", "entries=" + Manifest.Pending.Count + " bytes=" + array.Length);
			}
			catch (Exception ex)
			{
				Log.Write("MANIFEST_LOAD_FAILED", ex.GetType().Name + ": " + ex.Message + " — treating as unknown dependencies rather than none.");
			}
		}

		public override void OnSaveData()
		{
			base.OnSaveData();
			try
			{
				List<ManifestEntry> list = Bridge.BuildManifestEntries();
				byte[] array = Manifest.Serialise(list);
				base.serializableDataManager.SaveData("MeshBridge.Manifest.v1", array);
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < list.Count; i++)
				{
					if (i > 0)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(list[i].DonorName).Append("x").Append(list[i].Instances);
					if (list[i].GeometryId.Length == 0)
					{
						stringBuilder.Append("(UNRESOLVED)");
					}
				}
				Log.Write("MANIFEST_SAVE", "entries=" + list.Count + " bytes=" + array.Length + ((list.Count <= 0) ? string.Empty : string.Concat(" [", stringBuilder, "]")));
			}
			catch (Exception ex)
			{
				Log.Write("MANIFEST_SAVE_FAILED", ex.GetType().Name + ": " + ex.Message);
			}
		}
	}
}
