using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ColossalFramework;
using ProceduralObjects;
using ProceduralObjects.Classes;
using UnityEngine;

namespace MeshBridge
{
	public class Bridge : MonoBehaviour
	{
		private enum TextureKind
		{
			Checker,
			ACI,
			XYS
		}

		public const string DonorName = "MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data";

		private static readonly string[] ScaffoldCandidates = new string[10] { "Mailbox01", "mailbox02", "Doghouse", "Bird bath", "Flower pot 01", "Flower pot 02", "Flower pot 03", "High Birdbath", "grill", "table-set" };

		private PropInfo donor;

		private Material material;

		private Texture2D checker;

		private Texture2D aci;

		private Texture2D xys;

		private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

		private string donorFullHash;

		private string donorTopoHash;

		private string textureHash;

		private string geometryId;

		private readonly List<ImportedResource> imported = new List<ImportedResource>();

		private readonly Dictionary<string, PropInfo> importedDonors = new Dictionary<string, PropInfo>();

		private readonly Dictionary<string, BuildingInfo> buildingDonors = new Dictionary<string, BuildingInfo>();

		private bool catalogueInjected;

		private PropInfo cachedScaffold;

		private bool windowOpen;

		private Rect windowRect = new Rect(120f, 120f, 460f, 0f);

		private Vector2 windowScroll;

		private string windowStatus = string.Empty;

		private bool packagesTab;

		private float uiScale;

		private GUISkin scaledSkin;

		private float skinBuiltFor;

		private bool showHelp;

		private readonly List<string> expandedModels = new List<string>();

		private string confirmKey = string.Empty;

		private bool confirmIsDelete;

		private Rect buttonRect = new Rect(-1f, -1f, 0f, 0f);

		private List<PackageInfo> packageCache;

		private List<string> poExportCache;

		private bool showDiagnostics;

		private bool showUnprotected;

		private string searchText = string.Empty;

		private int sortMode;

		private int filterMode;

		private readonly List<string> favourites = new List<string>();

		private readonly List<string> recentlyPlaced = new List<string>();

		private bool listPrefsLoaded;

		private readonly Dictionary<int, Texture2D> swatches = new Dictionary<int, Texture2D>();

		private string packageName = "My creation";

		private int selectedExport;

		private static Bridge live;

		private readonly List<string> unresolved = new List<string>();

		private int standInsServed;

		private readonly List<string> unprotected = new List<string>();

		private bool created;

		private bool attempted;

		private bool reconstructionFailed;

		private float nextAudit;

		private static BuildingInfo cachedBuildingTemplate;

		private void Awake()
		{
			live = this;
		}

		public void ReconstructDonor()
		{
			Log.Write("RECONSTRUCT_TIMING", "realtimeSinceStartup=" + Time.realtimeSinceStartup.ToString("F6") + " frameCount=" + Time.frameCount + " event=reconstruction_start");
			try
			{
				PropInfo[] array = FindByName("MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data");
				if (array.Length == 1 && donor != null && array[0] == donor)
				{
					Log.Write("DONOR_RECONSTRUCT", "action=reuse matches=1 instance=" + donor.GetInstanceID());
					return;
				}
				if (array.Length > 0)
				{
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] != null && array[i].gameObject != null)
						{
							UnityEngine.Object.DestroyImmediate(array[i].gameObject);
						}
					}
					Log.Write("DONOR_RECONSTRUCT", "action=rebuild matches=" + array.Length + " (destroyed stale)");
				}
				else
				{
					Log.Write("DONOR_RECONSTRUCT", "action=rebuild matches=0");
				}
				CreateDonor();
				BuildImportedDonors();
				CheckManifest();
				reconstructionFailed = false;
			}
			catch (Exception ex)
			{
				reconstructionFailed = true;
				Log.Write("LOAD_EXCEPTION", ex.GetType().FullName + ": " + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void CreateDonor()
		{
			PropInfo propInfo = (cachedScaffold = FindScaffold());
			Mesh mesh = ResolveGeometry();
			Own(mesh);
			Log.Require(mesh.isReadable, "Donor mesh is not readable; PO cannot build a vertex list from it.");
			Log.MeshHash(mesh, out donorFullHash, out donorTopoHash);
			BuildTextures();
			LogMaterialProbe("SCAFFOLD_MATERIAL_PROBE", propInfo.m_material);
			material = new Material(propInfo.m_material);
			material.name = "MeshBridge.Material.v001";
			material.mainTexture = checker;
			material.mainTextureScale = Vector2.one;
			material.mainTextureOffset = Vector2.zero;
			Own(material);
			LogMaterialProbe("DONOR_MATERIAL_PROBE", material);
			PropInfo propInfo2 = BuildDonorObject("MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data", mesh);
			donor = propInfo2;
			Log.Write("SCAFFOLD", "name=" + propInfo.name + " shader=" + propInfo.m_material.shader.name);
			Log.Write("DONOR_MESH", "geometryId=" + geometryId + " " + Log.MeshInfo(mesh));
			Log.Write("DONOR_MATERIAL", "shader=" + material.shader.name + " mainTextureSHA256=" + textureHash + " materialInstance=" + material.GetInstanceID());
			VerifyDonorMaterial();
			CheckDiscoverability();
		}

		private Mesh ResolveGeometry()
		{
			Dictionary<string, string> dictionary = Store.ReadBindings();
			string value = null;
			dictionary.TryGetValue("MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data", out value);
			Mesh mesh = Fixture.Create("MeshBridge.SkewPentagon.v001");
			string text = Store.IdOf(mesh);
			Log.Write("GEOMETRY_ID", "generated=" + text + " bound=" + ((value != null) ? value : "<none>"));
			if (value == null)
			{
				Store.Commit(mesh);
				Store.WriteBinding("MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data", text);
				geometryId = text;
				Log.Write("GEOMETRY_RESOLVE", "source=generated id=" + text + " (first bind)");
				return mesh;
			}
			if (value == text)
			{
				if (!Store.Has(value))
				{
					Store.Commit(mesh);
				}
				UnityEngine.Object.DestroyImmediate(mesh);
				Mesh mesh2 = Store.Load(value, "MeshBridge.SkewPentagon.v001");
				geometryId = value;
				string full;
				string topologyUV;
				Log.MeshHash(mesh2, out full, out topologyUV);
				Log.Write("GEOMETRY_RESOLVE", "source=store id=" + value + " roundTripIdMatches=" + (Store.IdOf(mesh2) == value));
				return mesh2;
			}
			UnityEngine.Object.DestroyImmediate(mesh);
			Log.Write("GEOMETRY_MISMATCH", "donor=MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data bound=" + value + " generated=" + text + " action=serving bound payload; saved cities depend on it. Bind new geometry to a NEW donor name.");
			Mesh result = Store.Load(value, "MeshBridge.SkewPentagon.v001");
			geometryId = value;
			return result;
		}

		private PropInfo BuildDonorObject(string name, Mesh mesh)
		{
			PropInfo propInfo = cachedScaffold;
			GameObject gameObject = new GameObject(name);
			gameObject.SetActive(false);
			gameObject.hideFlags = HideFlags.DontSave;
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			Own(gameObject);
			PropInfo propInfo2 = gameObject.AddComponent<PropInfo>();
			propInfo2.name = name;
			propInfo2.m_cachedName = name;
			propInfo2.m_prefabDataIndex = -1;
			propInfo2.m_class = propInfo.m_class;
			propInfo2.m_isDecal = false;
			propInfo2.m_isMarker = false;
			propInfo2.m_useColorVariations = false;
			propInfo2.m_createRuining = false;
			propInfo2.m_minScale = 1f;
			propInfo2.m_maxScale = 1f;
			propInfo2.m_color0 = Color.white;
			propInfo2.m_color1 = Color.white;
			propInfo2.m_color2 = Color.white;
			propInfo2.m_color3 = Color.white;
			propInfo2.m_maxRenderDistance = 1000f;
			propInfo2.m_variations = new PropInfo.Variation[0];
			propInfo2.m_effects = new PropInfo.Effect[0];
			propInfo2.m_parkingSpaces = new PropInfo.ParkingSpace[0];
			propInfo2.m_specialPlaces = new PropInfo.SpecialPlace[0];
			propInfo2.m_mesh = mesh;
			propInfo2.m_material = material;
			propInfo2.m_lodMesh = null;
			propInfo2.m_lodMaterial = null;
			propInfo2.m_lodObject = null;
			propInfo2.m_lodHasDifferentShader = false;
			propInfo2.m_lodRenderDistance = 0f;
			propInfo2.m_lodCount = 0;
			propInfo2.m_lodLocations = new Vector4[0];
			propInfo2.m_lodObjectIndices = new Vector4[0];
			propInfo2.m_lodColors = new Vector4[0];
			if (propInfo.m_generatedInfo != null)
			{
				PropInfoGen propInfoGen = UnityEngine.Object.Instantiate(propInfo.m_generatedInfo);
				propInfoGen.name = name + " (GeneratedInfo)";
				Own(propInfoGen);
				propInfo2.m_generatedInfo = propInfoGen;
			}
			return propInfo2;
		}

		private BuildingInfo BuildBuildingDonorObject(string name, Mesh mesh, Material mat)
		{
			GameObject gameObject = new GameObject(name);
			gameObject.SetActive(false);
			gameObject.hideFlags = HideFlags.DontSave;
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			Own(gameObject);
			BuildingInfo buildingInfo = gameObject.AddComponent<BuildingInfo>();
			buildingInfo.name = name;
			buildingInfo.m_cachedName = name;
			buildingInfo.m_prefabDataIndex = -1;
			buildingInfo.m_useColorVariations = false;
			buildingInfo.m_color0 = Color.white;
			buildingInfo.m_color1 = Color.white;
			buildingInfo.m_color2 = Color.white;
			buildingInfo.m_color3 = Color.white;
			buildingInfo.m_mesh = mesh;
			buildingInfo.m_material = mat;
			buildingInfo.m_lodMesh = null;
			buildingInfo.m_lodMaterial = null;
			buildingInfo.m_lodObject = null;
			buildingInfo.m_subBuildings = new BuildingInfo.SubInfo[0];
			buildingInfo.m_props = new BuildingInfo.Prop[0];
			buildingInfo.m_paths = new BuildingInfo.PathInfo[0];
			BuildingInfo buildingInfo2 = FindBuildingClassTemplate();
			if (buildingInfo2 != null)
			{
				buildingInfo.m_class = buildingInfo2.m_class;
				buildingInfo.m_generatedInfo = buildingInfo2.m_generatedInfo;
			}
			else
			{
				Log.Write("BUILDING_TEMPLATE_NONE", "no class template found; m_class left null.");
			}
			return buildingInfo;
		}

		private static BuildingInfo FindBuildingClassTemplate()
		{
			if (cachedBuildingTemplate != null)
			{
				return cachedBuildingTemplate;
			}
			BuildingInfo[] array = Resources.FindObjectsOfTypeAll<BuildingInfo>();
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i] == null) && !(array[i].m_class == null) && !(array[i].m_mesh == null))
				{
					cachedBuildingTemplate = array[i];
					Log.Write("BUILDING_TEMPLATE", "using '" + array[i].name + "' for item class / generated info.");
					return cachedBuildingTemplate;
				}
			}
			return null;
		}

		private void BuildImportedDonors()
		{
			imported.Clear();
			importedDonors.Clear();
			catalogueInjected = false;
			Store.Migrate();
			Materials.Migrate();
			RebuildFromStore();
			ImportNew();
			RefreshDonorMaterials();
			Log.Write("IMPORT_SUMMARY", "donorsAvailable=" + importedDonors.Count + " buildingStandIns=" + buildingDonors.Count);
		}

		private void RefreshDonorMaterials()
		{
			int num = 0;
			foreach (KeyValuePair<string, PropInfo> importedDonor in importedDonors)
			{
				if (!(importedDonor.Value == null))
				{
					Material material = BuildRestoredMaterial(importedDonor.Key);
					if (!(material == null) && !(importedDonor.Value.m_material == material))
					{
						importedDonor.Value.m_material = material;
						importedDonor.Value.m_lodMaterial = null;
						num++;
					}
				}
			}
			foreach (KeyValuePair<string, BuildingInfo> buildingDonor in buildingDonors)
			{
				if (!(buildingDonor.Value == null))
				{
					Material material2 = BuildRestoredMaterial(buildingDonor.Key);
					if (!(material2 == null) && !(buildingDonor.Value.m_material == material2))
					{
						buildingDonor.Value.m_material = material2;
						buildingDonor.Value.m_lodMaterial = null;
						num++;
					}
				}
			}
			if (num > 0)
			{
				Log.Write("MATERIAL_REFRESH", "donorsUpdated=" + num);
			}
		}

		private void RebuildFromStore()
		{
			standInsServed = 0;
			unprotected.Clear();
			Dictionary<string, string> dictionary;
			Dictionary<string, string> dictionary2;
			try
			{
				dictionary = Store.ReadBindings();
				dictionary2 = Store.ReadTypes();
			}
			catch (Exception ex)
			{
				Log.Write("STORE_EXCEPTION", "reading bindings: " + ex.Message);
				return;
			}
			int num = 0;
			int num2 = 0;
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				string key = item.Key;
				string value = item.Value;
				if (key == "MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data")
				{
					continue;
				}
				bool flag = !key.StartsWith("MeshBridge.");
				if (flag && AnyPrefabNamed(key))
				{
					// The original asset is installed, so no stand-in is built. The
					// payload still has to be checked: it is the only thing that will
					// stand in on the day the asset goes away, and that day is far too
					// late to discover it is gone. Nothing is at risk right now, so this
					// is a separate warning from a missing dependency.
					if (!Store.Has(value))
					{
						unprotected.Add(key);
						Log.Write("CAPTURE_UNPROTECTED", "base=" + key + " geometryId=" + value + " — original asset is installed, but the captured payload is absent from the store. Nothing is wrong today; if this asset is ever removed, MeshBridge can no longer stand in for it. Run Protect city to recapture.");
					}
					Log.Write("CAPTURE_PRESENT", "base=" + key + " — original asset is installed; not standing in.");
				}
				else
				{
					if (importedDonors.ContainsKey(key))
					{
						continue;
					}
					string value2 = "PROP";
					dictionary2.TryGetValue(key, out value2);
					bool flag2 = flag && value2 == "BUILDING";
					if (!Store.Has(value))
					{
						num2++;
						Log.Write("RESOURCE_MISSING", "donor=" + key + " geometryId=" + value + " — payload absent from store. Any saved object using this donor will fail to restore. Do not save this city.");
						continue;
					}
					try
					{
						Mesh mesh = Store.Load(value, FriendlyFromDonor(key));
						Own(mesh);
						PropInfo[] array = FindByName(key);
						for (int i = 0; i < array.Length; i++)
						{
							if (array[i] != null && array[i].gameObject != null)
							{
								UnityEngine.Object.DestroyImmediate(array[i].gameObject);
							}
						}
						Material material = BuildRestoredMaterial(key);
						if (flag2)
						{
							Material mat = ((!(material != null)) ? this.material : material);
							BuildingInfo value3 = BuildBuildingDonorObject(key, mesh, mat);
							buildingDonors.Add(key, value3);
							standInsServed++;
							Log.Write("CAPTURE_STANDIN", "base=" + key + " type=BUILDING — original asset missing; serving captured geometry" + ((!(material != null)) ? " with placeholder material." : " with captured material."));
							num++;
							continue;
						}
						PropInfo propInfo = BuildDonorObject(key, mesh);
						if (material != null)
						{
							propInfo.m_material = material;
							propInfo.m_lodMaterial = material;
						}
						importedDonors.Add(key, propInfo);
						if (flag)
						{
							standInsServed++;
							Log.Write("CAPTURE_STANDIN", "base=" + key + " type=PROP — original asset missing; serving captured geometry" + ((!(material != null)) ? " with placeholder material." : " with captured material."));
						}
						// A captured Workshop base is not a MeshBridge model. It exists so PO
						// can resolve a base name whose asset is gone, and placing one would
						// mean nothing. It stays out of the Stored Assets list; its state is
						// reported through the dependency warning instead.
						if (!flag)
						{
							ImportedResource importedResource = new ImportedResource();
							importedResource.DonorName = key;
							importedResource.FriendlyName = FriendlyFromDonor(key);
							importedResource.ModelKey = ModelKeyFromDonor(key);
							importedResource.SourceFile = "(from store)";
							importedResource.GeometryId = value;
							importedResource.Mesh = mesh;
							importedResource.VertexCount = mesh.vertexCount;
							importedResource.TriangleCount = mesh.triangles.Length / 3;
							imported.Add(importedResource);
						}
						num++;
					}
					catch (Exception ex2)
					{
						Log.Write("STORE_REBUILD_FAILED", "donor=" + key + " " + ex2.GetType().Name + ": " + ex2.Message);
					}
				}
			}
			if (unprotected.Count > 0)
			{
				Log.Write("CAPTURE_UNPROTECTED_SUMMARY", "bases=" + unprotected.Count + " — captured geometry missing from the store for assets that are still installed. Protection for these is gone, though nothing is broken today.");
			}
			Log.Write("STORE_REBUILD", "bindings=" + dictionary.Count + " rebuilt=" + num + " missingPayloads=" + num2);
		}

		public int ImportNew()
		{
			List<ImportedResource> list;
			try
			{
				list = Importer.ImportAll();
			}
			catch (Exception ex)
			{
				Log.Write("IMPORT_EXCEPTION", ex.GetType().FullName + ": " + ex.Message);
				return 0;
			}
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				ImportedResource importedResource = list[i];
				if (importedDonors.ContainsKey(importedResource.DonorName))
				{
					if (importedResource.Mesh != null)
					{
						UnityEngine.Object.DestroyImmediate(importedResource.Mesh);
					}
					continue;
				}
				try
				{
					PropInfo[] array = FindByName(importedResource.DonorName);
					for (int j = 0; j < array.Length; j++)
					{
						if (array[j] != null && array[j].gameObject != null)
						{
							UnityEngine.Object.DestroyImmediate(array[j].gameObject);
						}
					}
					Own(importedResource.Mesh);
					PropInfo propInfo = BuildDonorObject(importedResource.DonorName, importedResource.Mesh);
					Material material = BuildRestoredMaterial(importedResource.DonorName);
					if (material != null)
					{
						propInfo.m_material = material;
						propInfo.m_lodMaterial = material;
					}
					importedDonors.Add(importedResource.DonorName, propInfo);
					imported.Add(importedResource);
					num++;
					Log.Write("IMPORT_DONOR", "donor=" + importedResource.DonorName + " geometryId=" + importedResource.GeometryId + " vertices=" + importedResource.VertexCount);
				}
				catch (Exception ex2)
				{
					Log.Write("IMPORT_DONOR_FAILED", "donor=" + importedResource.DonorName + " " + ex2.GetType().Name + ": " + ex2.Message);
				}
			}
			if (num > 0)
			{
				catalogueInjected = false;
			}
			return num;
		}

		private static string FriendlyFromDonor(string donorName)
		{
			string text = StripDonor(donorName);
			int num = text.IndexOf('.');
			if (num > 0)
			{
				return text.Substring(0, num) + " [" + text.Substring(num + 1) + "]";
			}
			return (text.Length != 0) ? text : donorName;
		}

		// One definition of the grouping rule, in Importer, used by both the import
		// path and the store-rebuild path below.
		private static string ModelKeyFromDonor(string donorName)
		{
			return Importer.ModelKeyFromDonor(donorName);
		}

		private static string StripDonor(string donorName)
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
			int num = text.LastIndexOf('.');
			if (num > 0)
			{
				text = text.Substring(0, num);
			}
			return text;
		}

		private void InjectCatalogue()
		{
			ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
			if (instance == null || instance.availableProceduralInfos == null || instance.availableProceduralInfos.Count == 0)
			{
				return;
			}
			int num = 0;
			foreach (KeyValuePair<string, PropInfo> importedDonor in importedDonors)
			{
				if (importedDonor.Value == null)
				{
					continue;
				}
				bool flag = false;
				for (int i = 0; i < instance.availableProceduralInfos.Count; i++)
				{
					ProceduralInfo proceduralInfo = instance.availableProceduralInfos[i];
					if (proceduralInfo != null && proceduralInfo.propPrefab == importedDonor.Value)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					instance.availableProceduralInfos.Add(new ProceduralInfo(importedDonor.Value, false));
					num++;
				}
			}
			catalogueInjected = true;
			Log.Write("CATALOGUE_INJECT", "added=" + num + " catalogueSize=" + instance.availableProceduralInfos.Count + " (imported models now selectable in PO)");
		}

		private PropInfo FindScaffold()
		{
			PropInfo[] array = Resources.FindObjectsOfTypeAll<PropInfo>();
			Dictionary<string, PropInfo> dictionary = new Dictionary<string, PropInfo>();
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i] == null) && array[i].name != null && !dictionary.ContainsKey(array[i].name))
				{
					dictionary.Add(array[i].name, array[i]);
				}
			}
			for (int j = 0; j < ScaffoldCandidates.Length; j++)
			{
				string text = ScaffoldCandidates[j];
				PropInfo value;
				if (!dictionary.TryGetValue(text, out value))
				{
					Log.Write("SCAFFOLD_REJECT", "name=" + text + " candidate=" + (j + 1) + " reasons=not loaded");
					continue;
				}
				List<string> list = ScaffoldRejection(value);
				if (list.Count == 0)
				{
					Log.Write("SCAFFOLD_ACCEPT", "name=" + text + " candidate=" + (j + 1) + " shader=" + value.m_material.shader.name + " (hard-coded candidate #" + (j + 1) + ")");
					return value;
				}
				Log.Write("SCAFFOLD_REJECT", "name=" + text + " candidate=" + (j + 1) + " reasons=" + string.Join("; ", list.ToArray()));
			}
			throw new InvalidOperationException("No hard-coded scaffold candidate validated. See SCAFFOLD_REJECT; no fallback scan.");
		}

		private static List<string> ScaffoldRejection(PropInfo info)
		{
			List<string> list = new List<string>();
			if (info.m_material == null)
			{
				list.Add("m_material == null");
			}
			else
			{
				Shader shader = info.m_material.shader;
				if (shader == null)
				{
					list.Add("material.shader == null");
				}
				else if (!shader.name.StartsWith("Custom/Props/Prop/"))
				{
					list.Add("shader '" + shader.name + "' does not start with Custom/Props/Prop/");
				}
				string text = info.m_material.name;
				if (text != null && text.ToLower().Contains("[proceduralobj]"))
				{
					list.Add("material name carries [ProceduralObj]");
				}
			}
			if (info.m_mesh == null)
			{
				list.Add("m_mesh == null");
			}
			if (info.m_isDecal)
			{
				list.Add("m_isDecal == true");
			}
			if (info.m_isMarker)
			{
				list.Add("m_isMarker == true");
			}
			return list;
		}

		private void BuildTextures()
		{
			checker = Texture("MeshBridge.Checker.v001", 64, TextureKind.Checker);
			aci = Texture("MeshBridge.ACI.v001", 4, TextureKind.ACI);
			xys = Texture("MeshBridge.XYS.v001", 4, TextureKind.XYS);
			textureHash = Log.TextureHash(checker);
		}

		private Texture2D Texture(string name, int size, TextureKind kind)
		{
			Texture2D texture2D = new Texture2D(size, size, TextureFormat.RGBA32, false);
			texture2D.name = name;
			texture2D.filterMode = FilterMode.Point;
			texture2D.wrapMode = TextureWrapMode.Repeat;
			Color32[] array = new Color32[size * size];
			for (int i = 0; i < size; i++)
			{
				for (int j = 0; j < size; j++)
				{
					Color32 color;
					switch (kind)
					{
					case TextureKind.Checker:
						color = ((((j / 8 + i / 8) & 1) != 0) ? new Color32(30, 30, 30, byte.MaxValue) : new Color32(235, 235, 235, byte.MaxValue));
						break;
					case TextureKind.ACI:
						color = new Color32(byte.MaxValue, 0, 0, byte.MaxValue);
						break;
					default:
						color = new Color32(128, 128, 0, byte.MaxValue);
						break;
					}
					array[i * size + j] = color;
				}
			}
			texture2D.SetPixels32(array);
			texture2D.Apply();
			Own(texture2D);
			return texture2D;
		}

		private static void LogMaterialProbe(string tag, Material m)
		{
			if (m == null)
			{
				Log.Write(tag, "material == null");
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("shader=").Append((!(m.shader == null)) ? m.shader.name : "<null>");
			stringBuilder.Append(" renderQueue=").Append(m.renderQueue);
			stringBuilder.Append(" keywords=[");
			string[] shaderKeywords = m.shaderKeywords;
			for (int i = 0; i < shaderKeywords.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(",");
				}
				stringBuilder.Append(shaderKeywords[i]);
			}
			stringBuilder.Append("]");
			string[] array = new string[5] { "_MainTex", "_ACIMap", "_XYSMap", "_Color", "_ColorV0" };
			for (int j = 0; j < array.Length; j++)
			{
				if (!m.HasProperty(array[j]))
				{
					stringBuilder.Append(" ").Append(array[j]).Append("=<absent>");
					continue;
				}
				Texture texture = null;
				try
				{
					texture = m.GetTexture(array[j]);
				}
				catch
				{
				}
				if (texture != null)
				{
					stringBuilder.Append(" ").Append(array[j]).Append("=tex:")
						.Append(texture.width)
						.Append("x")
						.Append(texture.height);
				}
				else
				{
					stringBuilder.Append(" ").Append(array[j]).Append("=")
						.Append(m.GetColor(array[j]).ToString());
				}
			}
			Log.Write(tag, stringBuilder.ToString());
		}

		private void VerifyDonorMaterial()
		{
			Log.Require(donor.m_material != null, "Donor has no material.");
			Log.Require(donor.m_material.shader != null, "Donor material has no shader.");
			Log.Require(donor.m_material.shader.name.StartsWith("Custom/Props/Prop/"), "Donor must use a prop shader; got '" + donor.m_material.shader.name + "'.");
			Log.Require(donor.m_mesh != null && donor.m_mesh.isReadable, "Donor mesh missing or not readable.");
		}

		private void CheckDiscoverability()
		{
			PropInfo[] array = FindByName("MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data");
			bool flag = array.Length == 1 && array[0] == donor;
			Log.Write("DISCOVERABILITY", "exactNameMatches=" + array.Length + " sameInstance=" + flag + " donorInstance=" + donor.GetInstanceID() + " active=" + donor.gameObject.activeSelf + " lifetime=DontDestroyOnLoad+DontSave; simulationPrefabRegistration=not required by inspected PO lookup");
			Log.Require(flag, "Donor is not uniquely discoverable through PO's Resources lookup.");
		}

		private static bool AnyPrefabNamed(string name)
		{
			if (FindByName(name).Length > 0)
			{
				return true;
			}
			BuildingInfo[] array = Resources.FindObjectsOfTypeAll<BuildingInfo>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null && array[i].name == name)
				{
					return true;
				}
			}
			return false;
		}

		private static PropInfo[] FindByName(string name)
		{
			PropInfo[] array = Resources.FindObjectsOfTypeAll<PropInfo>();
			List<PropInfo> list = new List<PropInfo>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null && array[i].name == name)
				{
					list.Add(array[i]);
				}
			}
			return list.ToArray();
		}

		private void Spawn()
		{
			try
			{
				Log.Require(!reconstructionFailed, "Load-time donor reconstruction failed; inspect LOAD_EXCEPTION.");
				Log.Require(donor != null, "No donor available.");
				ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
				Log.Require(instance != null, "PO is disabled or not ready. Wait for city loading to finish and retry.");
				Log.Require(instance.proceduralObjects != null, "PO object list not initialised yet; wait and retry.");
				Log.Require(RenderOptions.instance != null, "PO RenderOptions not ready; wait and retry.");
				Log.Require(!instance.editingWholeModel && !instance.movingWholeModel && !instance.placingSelection, "Finish/exit PO editing or placement before triggering MeshBridge.");
				Log.Require(ToolsModifierControl.cameraController != null, "Camera controller not ready.");
				CheckDiscoverability();
				Vector3 currentPosition = ToolsModifierControl.cameraController.m_currentPosition;
				float num = Singleton<TerrainManager>.instance.SampleDetailHeight(currentPosition);
				Log.Require(!float.IsNaN(num) && !float.IsInfinity(num), "Invalid terrain height.");
				Vector3 position = new Vector3(currentPosition.x, num + 0.25f, currentPosition.z);
				Log.Write("PLACEMENT", "camera target ground + 0.25m=" + position.ToString("F3") + " rotation=identity");
				int count = instance.proceduralObjects.Count;
				Log.Write("PO_CALL", "SpawnObject(new ProceduralInfo(donor, false), null); countBefore=" + count);
				ProceduralObject proceduralObject = instance.SpawnObject(new ProceduralInfo(donor, false));
				Log.Require(proceduralObject != null, "SpawnObject returned null.");
				Log.Write("PO_RETURN", "id=" + proceduralObject.id + " countAfter=" + instance.proceduralObjects.Count + " meshStatus=" + proceduralObject.meshStatus);
				Log.Write("UNIQUE_BEGIN", "id=" + proceduralObject.id + " sharedBefore=" + (proceduralObject.meshStatus == 1));
				proceduralObject.MakeUniqueMesh();
				Log.Write("UNIQUE_END", "meshStatus=" + proceduralObject.meshStatus + " sharedAfter=" + (proceduralObject.meshStatus == 1));
				proceduralObject.SetPosition(position);
				proceduralObject.SetRotation(Quaternion.identity);
				proceduralObject.RecalculateBoundsNormalsExtras(proceduralObject.meshStatus);
				created = true;
				AuditObject(proceduralObject);
				Log.Write("CREATED_PENDING_MANUAL_TEST", "id=" + proceduralObject.id + " Runtime invariants passed. Select the prism in PO, enter the vertex editor, move a vertex, then press Ctrl+Shift+L and compare fullSHA256 across a cold restart.");
			}
			catch (Exception ex)
			{
				Log.Write("SPAWN_EXCEPTION", ex.GetType().FullName + ": " + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void Audit()
		{
			try
			{
				if (donor == null)
				{
					Log.Write("AUDIT", "No donor; inspect LOAD_EXCEPTION for reconstruction failure.");
					return;
				}
				string full;
				string topologyUV;
				Log.MeshHash(donor.m_mesh, out full, out topologyUV);
				bool flag = full == donorFullHash && topologyUV == donorTopoHash;
				if (!flag)
				{
					Log.Write("AUDIT", "IMMUTABILITY FAILURE: donor geometry changed. expected=" + donorFullHash + " actual=" + full);
				}
				CheckDiscoverability();
				ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
				if (instance == null || instance.proceduralObjects == null)
				{
					Log.Write("AUDIT", "PO logic disappeared.");
					return;
				}
				int num = 0;
				for (int i = 0; i < instance.proceduralObjects.Count; i++)
				{
					ProceduralObject proceduralObject = instance.proceduralObjects[i];
					if (proceduralObject != null && !(proceduralObject.basePrefabName != "MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data"))
					{
						num++;
						AuditObject(proceduralObject);
					}
				}
				Log.Write("AUDIT", "objectsMatchingDonor=" + num + " totalPOs=" + instance.proceduralObjects.Count + " donorUnchanged=" + flag + " createdThisSession=" + created);
			}
			catch (Exception ex)
			{
				Log.Write("AUDIT_EXCEPTION", ex.GetType().FullName + ": " + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void AuditObject(ProceduralObject po)
		{
			Log.Write("PO_AUDIT", "id=" + po.id + " basePrefabName=" + po.basePrefabName + " baseInfoType=" + po.baseInfoType + " meshStatus=" + po.meshStatus + " editableVertices=" + ((po.vertices != null) ? po.vertices.Length : (-1)) + " position=" + po.m_position.ToString("F3") + " " + Log.MeshInfo(po.m_mesh));
		}

		public int InventoryExisting()
		{
			ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
			if (instance == null || instance.proceduralObjects == null)
			{
				windowStatus = "PO not ready.";
				return 0;
			}
			Dictionary<string, string> dictionary;
			try
			{
				dictionary = Store.ReadBindings();
			}
			catch
			{
				dictionary = new Dictionary<string, string>();
			}
			Dictionary<string, MaterialRecipe> dictionary2;
			try
			{
				dictionary2 = Materials.ReadRecipes();
			}
			catch
			{
				dictionary2 = new Dictionary<string, MaterialRecipe>();
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			HashSet<string> hashSet = new HashSet<string>();
			for (int i = 0; i < instance.proceduralObjects.Count; i++)
			{
				ProceduralObject proceduralObject = instance.proceduralObjects[i];
				if (proceduralObject == null || proceduralObject.basePrefabName == null)
				{
					continue;
				}
				string basePrefabName = proceduralObject.basePrefabName;
				if (basePrefabName.StartsWith("MeshBridge.") || hashSet.Contains(basePrefabName))
				{
					continue;
				}
				hashSet.Add(basePrefabName);
				if (dictionary.ContainsKey(basePrefabName) && Store.Has(dictionary[basePrefabName]))
				{
					num2++;
					if ((!dictionary2.ContainsKey(basePrefabName) || !dictionary2[basePrefabName].HasAny || dictionary2[basePrefabName].Shader.Length == 0) && CaptureMaterialFor(proceduralObject, basePrefabName))
					{
						num5++;
					}
					continue;
				}
				try
				{
					Mesh mesh = null;
					if (proceduralObject.baseInfoType == "PROP" && proceduralObject._baseProp != null)
					{
						mesh = proceduralObject._baseProp.m_mesh;
					}
					else if (proceduralObject.baseInfoType == "BUILDING" && proceduralObject._baseBuilding != null)
					{
						mesh = proceduralObject._baseBuilding.m_mesh;
					}
					if (mesh == null)
					{
						num3++;
						Log.Write("INVENTORY_SKIP", "base=" + basePrefabName + " reason=source prefab not resolved (asset already missing — cannot capture what is not loaded)");
						continue;
					}
					if (!mesh.isReadable)
					{
						num3++;
						Log.Write("INVENTORY_SKIP", "base=" + basePrefabName + " reason=mesh not CPU-readable");
						continue;
					}
					if (mesh.vertexCount > 65534)
					{
						num3++;
						Log.Write("INVENTORY_SKIP", "base=" + basePrefabName + " reason=vertexCount " + mesh.vertexCount + " exceeds engine limit");
						continue;
					}
					Mesh mesh2 = new Mesh();
					mesh2.name = basePrefabName;
					mesh2.vertices = mesh.vertices;
					mesh2.triangles = mesh.triangles;
					if (mesh.uv != null && mesh.uv.Length == mesh.vertexCount)
					{
						mesh2.uv = mesh.uv;
					}
					if (mesh.normals != null && mesh.normals.Length == mesh.vertexCount)
					{
						mesh2.normals = mesh.normals;
					}
					else
					{
						mesh2.RecalculateNormals();
					}
					if (mesh.tangents != null && mesh.tangents.Length == mesh.vertexCount)
					{
						mesh2.tangents = mesh.tangents;
					}
					mesh2.RecalculateBounds();
					string text = Store.Commit(mesh2);
					Store.WriteBinding(basePrefabName, text, proceduralObject.baseInfoType);
					UnityEngine.Object.DestroyImmediate(mesh2);
					if (CaptureMaterialFor(proceduralObject, basePrefabName))
					{
						num5++;
					}
					num++;
					Log.Write("INVENTORY_CAPTURE", "base=" + basePrefabName + " type=" + proceduralObject.baseInfoType + " geometryId=" + text + " vertices=" + mesh.vertexCount + " triangles=" + mesh.triangles.Length / 3);
				}
				catch (Exception ex)
				{
					num4++;
					Log.Write("INVENTORY_FAILED", "base=" + basePrefabName + " " + ex.GetType().Name + ": " + ex.Message);
				}
			}
			Log.Write("INVENTORY_SUMMARY", "distinctBases=" + hashSet.Count + " captured=" + num + " alreadyStored=" + num2 + " materialsCaptured=" + num5 + " skipped=" + num3 + " failed=" + num4);
			windowStatus = "Inventory: " + num + " geometry, " + num5 + " materials captured. " + num2 + " already stored, " + num3 + " skipped, " + num4 + " failed.";
			return num;
		}

		private Material BuildRestoredMaterial(string donorName)
		{
			try
			{
				Dictionary<string, MaterialRecipe> dictionary = Materials.ReadRecipes();
				MaterialRecipe value;
				if (!dictionary.TryGetValue(donorName, out value))
				{
					return null;
				}
				if (cachedScaffold == null)
				{
					return null;
				}
				if (!value.HasAny && value.Shader.Length == 0)
				{
					return null;
				}
				Shader shader = ((value.Shader.Length <= 0) ? null : Shader.Find(value.Shader));
				Material material;
				if (shader != null)
				{
					material = new Material(cachedScaffold.m_material);
					material.shader = shader;
				}
				else
				{
					material = new Material(cachedScaffold.m_material);
					if (value.Shader.Length > 0)
					{
						Log.Write("MATERIAL_SHADER_MISSING", "donor=" + donorName + " shader='" + value.Shader + "' not found; falling back to " + ((!(material.shader == null)) ? material.shader.name : "<null>") + ". Appearance may differ from the original.");
					}
				}
				material.name = donorName + ".mat";
				int num = 0;
				Texture2D texture2D = Materials.LoadTexture(value.MainTexId, false, donorName + "._MainTex");
				if (texture2D != null)
				{
					Own(texture2D);
					material.mainTexture = texture2D;
					num++;
				}
				if (material.HasProperty("_ACIMap"))
				{
					Texture2D texture2D2 = Materials.LoadTexture(value.AciTexId, true, donorName + "._ACIMap");
					if (texture2D2 != null)
					{
						Own(texture2D2);
						material.SetTexture("_ACIMap", texture2D2);
						num++;
					}
				}
				if (material.HasProperty("_XYSMap"))
				{
					Texture2D texture2D3 = Materials.LoadTexture(value.XysTexId, true, donorName + "._XYSMap");
					if (texture2D3 != null)
					{
						Own(texture2D3);
						material.SetTexture("_XYSMap", texture2D3);
						num++;
					}
				}
				if (material.HasProperty("_Color"))
				{
					material.SetColor("_Color", value.Colour);
				}
				bool flag = value.Shader.Length > 0 && cachedScaffold.m_material.shader != null && value.Shader != cachedScaffold.m_material.shader.name;
				Own(material);
				Log.Write("MATERIAL_RESTORE", "donor=" + donorName + " shader=" + ((!(material.shader == null)) ? material.shader.name : "<null>") + " requested=" + ((value.Shader.Length <= 0) ? "<none>" : value.Shader) + " texturesLoaded=" + num);
				return (num <= 0 && !flag) ? null : material;
			}
			catch (Exception ex)
			{
				Log.Write("MATERIAL_RESTORE_FAILED", donorName + ": " + ex.GetType().Name + " " + ex.Message);
				return null;
			}
		}

		private bool CaptureMaterialFor(ProceduralObject po, string baseName)
		{
			try
			{
				Material material = null;
				if (po.baseInfoType == "PROP" && po._baseProp != null)
				{
					material = po._baseProp.m_material;
				}
				else if (po.baseInfoType == "BUILDING" && po._baseBuilding != null)
				{
					material = po._baseBuilding.m_material;
				}
				if (material == null)
				{
					Log.Write("MATERIAL_NONE", "base=" + baseName + " — source material not resolved.");
					return false;
				}
				MaterialRecipe materialRecipe = Materials.Capture(material);
				if (!materialRecipe.HasAny)
				{
					Log.Write("MATERIAL_NONE", "base=" + baseName + " — material has no capturable textures (shader=" + ((!(material.shader == null)) ? material.shader.name : "<null>") + ").");
					return false;
				}
				Materials.WriteRecipe(baseName, materialRecipe);
				return true;
			}
			catch (Exception ex)
			{
				Log.Write("MATERIAL_CAPTURE_FAILED", baseName + ": " + ex.GetType().Name + " " + ex.Message);
				return false;
			}
		}

		public static List<ManifestEntry> BuildManifestEntries()
		{
			List<ManifestEntry> list = new List<ManifestEntry>();
			ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
			if (instance == null || instance.proceduralObjects == null)
			{
				return list;
			}
			Dictionary<string, string> dictionary;
			try
			{
				dictionary = Store.ReadBindings();
			}
			catch
			{
				dictionary = new Dictionary<string, string>();
			}
			Dictionary<string, ManifestEntry> dictionary2 = new Dictionary<string, ManifestEntry>();
			for (int i = 0; i < instance.proceduralObjects.Count; i++)
			{
				ProceduralObject proceduralObject = instance.proceduralObjects[i];
				if (proceduralObject != null && proceduralObject.basePrefabName != null && (proceduralObject.basePrefabName.StartsWith("MeshBridge.") || dictionary.ContainsKey(proceduralObject.basePrefabName)))
				{
					ManifestEntry value;
					if (!dictionary2.TryGetValue(proceduralObject.basePrefabName, out value))
					{
						value = new ManifestEntry();
						value.DonorName = proceduralObject.basePrefabName;
						string value2;
						value.GeometryId = ((!dictionary.TryGetValue(proceduralObject.basePrefabName, out value2)) ? string.Empty : value2);
						value.Instances = 0;
						dictionary2.Add(proceduralObject.basePrefabName, value);
						list.Add(value);
					}
					value.Instances++;
				}
			}
			if (live != null)
			{
				for (int j = 0; j < Manifest.Pending.Count; j++)
				{
					ManifestEntry manifestEntry = Manifest.Pending[j];
					if (!dictionary2.ContainsKey(manifestEntry.DonorName) && live.unresolved.Contains(manifestEntry.DonorName))
					{
						list.Add(manifestEntry);
					}
				}
			}
			return list;
		}

		private void CheckManifest()
		{
			// standInsServed and unprotected are filled by RebuildFromStore, which runs
			// BEFORE this. Clearing them here threw away everything it found, so the
			// window and the status button reported all-clear while the log showed
			// otherwise. They are reset at the start of the rebuild instead.
			unresolved.Clear();
			if (!Manifest.PendingValid)
			{
				return;
			}
			if (Manifest.Pending.Count == 0)
			{
				Log.Write("MANIFEST_CHECK", "city declares no MeshBridge dependencies.");
				return;
			}
			int num = 0;
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < Manifest.Pending.Count; i++)
			{
				ManifestEntry manifestEntry = Manifest.Pending[i];
				if (manifestEntry.DonorName == "MeshBridge.v001.SkewPentagon.Checker.7F1425B9_Data" || importedDonors.ContainsKey(manifestEntry.DonorName) || buildingDonors.ContainsKey(manifestEntry.DonorName) || AnyPrefabNamed(manifestEntry.DonorName))
				{
					num++;
					continue;
				}
				unresolved.Add(manifestEntry.DonorName);
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(manifestEntry.DonorName).Append(" (").Append(manifestEntry.Instances)
					.Append(" objects");
				if (manifestEntry.GeometryId.Length > 0)
				{
					stringBuilder.Append(", geometry ").Append(manifestEntry.GeometryId.Substring(0, 8));
				}
				stringBuilder.Append(")");
			}
			if (unresolved.Count == 0)
			{
				Log.Write("MANIFEST_CHECK", "declared=" + Manifest.Pending.Count + " resolved=" + num + " — all dependencies present.");
				windowStatus = "This city: all " + num + " dependencies present.";
			}
			else
			{
				windowOpen = true;
				windowStatus = "WARNING: " + unresolved.Count + " missing dependency(ies). Do not save this city until resolved.";
				Log.Write("MANIFEST_MISSING", string.Concat("declared=", Manifest.Pending.Count, " resolved=", num, " missing=", unresolved.Count, " [", stringBuilder, "] — objects using these donors will fail to restore, and saving now will discard them. Restore the geometry payloads before saving."));
			}
		}

		public string ExportRecoveryPackage()
		{
			List<ManifestEntry> list = BuildManifestEntries();
			string text = DateTime.Now.ToString("yyyyMMdd-HHmmss");
			string text2 = "city";
			try
			{
				if (Singleton<SimulationManager>.exists && Singleton<SimulationManager>.instance.m_metaData != null && !string.IsNullOrEmpty(Singleton<SimulationManager>.instance.m_metaData.m_CityName))
				{
					text2 = Singleton<SimulationManager>.instance.m_metaData.m_CityName;
				}
			}
			catch
			{
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char c in text2)
			{
				if (char.IsLetterOrDigit(c))
				{
					stringBuilder.Append(c);
				}
				else if (c == ' ' || c == '-' || c == '_')
				{
					stringBuilder.Append('_');
				}
			}
			text2 = stringBuilder.ToString().Trim('_');
			if (text2.Length == 0)
			{
				text2 = "city";
			}
			string text3 = Path.Combine(Path.GetDirectoryName(Importer.ImportFolder), "Recovery\\" + text2 + "-" + text);
			Directory.CreateDirectory(text3);
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder2.AppendLine("# MeshBridge recovery package  city=" + text2 + "  " + text);
			stringBuilder2.AppendLine("# Copy the .mbg and .mbt files into Addons/Mods/MeshBridge/Store and");
			stringBuilder2.AppendLine("# append the binding lines to Store/bindings.txt.");
			stringBuilder2.AppendLine();
			Dictionary<string, MaterialRecipe> dictionary;
			try
			{
				dictionary = Materials.ReadRecipes();
			}
			catch
			{
				dictionary = new Dictionary<string, MaterialRecipe>();
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			for (int j = 0; j < list.Count; j++)
			{
				ManifestEntry manifestEntry = list[j];
				stringBuilder2.AppendLine(manifestEntry.DonorName + " = " + manifestEntry.GeometryId + "   # instances: " + manifestEntry.Instances);
				if (manifestEntry.GeometryId.Length == 0)
				{
					num2++;
					continue;
				}
				try
				{
					string text4 = Store.ResolvePayload(manifestEntry.GeometryId);
					if (File.Exists(text4))
					{
						File.Copy(text4, Path.Combine(text3, manifestEntry.GeometryId + ".mbg"), true);
						num++;
					}
					else
					{
						num2++;
					}
				}
				catch (Exception ex)
				{
					Log.Write("RECOVERY_COPY_FAILED", manifestEntry.GeometryId + ": " + ex.Message);
				}
				MaterialRecipe value;
				if (!dictionary.TryGetValue(manifestEntry.DonorName, out value))
				{
					continue;
				}
				stringBuilder2.AppendLine("    material = " + value.MainTexId + "," + value.AciTexId + "," + value.XysTexId);
				string[] array = new string[3] { value.MainTexId, value.AciTexId, value.XysTexId };
				for (int k = 0; k < array.Length; k++)
				{
					if (array[k].Length == 0)
					{
						continue;
					}
					try
					{
						string text5 = Materials.TexPath(array[k]);
						string text6 = Path.Combine(text3, array[k] + ".mbt");
						if (File.Exists(text5) && !File.Exists(text6))
						{
							File.Copy(text5, text6, true);
							num3++;
						}
					}
					catch
					{
					}
				}
			}
			File.WriteAllText(Path.Combine(text3, "manifest.txt"), stringBuilder2.ToString());
			Log.Write("RECOVERY_EXPORT", "path=" + text3 + " entries=" + list.Count + " payloadsCopied=" + num + " payloadsAbsent=" + num2 + " texturesCopied=" + num3);
			return text3;
		}

		// Live usage in the city that is loaded now. This is the check that stops
		// someone unbinding or deleting geometry their own city is standing on. It
		// cannot see other saves, so it catches the common mistake, not every one.
		private static int LiveUsers(List<ImportedResource> parts)
		{
			int num = 0;
			try
			{
				List<ManifestEntry> list = BuildManifestEntries();
				for (int i = 0; i < list.Count; i++)
				{
					for (int j = 0; j < parts.Count; j++)
					{
						if (list[i].DonorName == parts[j].DonorName)
						{
							num += list[i].Instances;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Write("USAGE_CHECK_FAILED", ex.Message + " — treating as in use.");
				return -1;
			}
			return num;
		}

		// Unbind: reversible, payload kept. Delete: permanent, payload destroyed.
		private int ForgetModel(List<ImportedResource> parts, bool deletePayload)
		{
			int num = 0;
			for (int i = 0; i < parts.Count; i++)
			{
				string donorName = parts[i].DonorName;
				string geometryId = parts[i].GeometryId;
				Store.RemoveBinding(donorName);
				Materials.RemoveRecipe(donorName);
				PropInfo value;
				if (importedDonors.TryGetValue(donorName, out value))
				{
					if (value != null && value.gameObject != null)
					{
						UnityEngine.Object.DestroyImmediate(value.gameObject);
					}
					importedDonors.Remove(donorName);
				}
				if (deletePayload && Store.PayloadUsers(geometryId) == 0 && Store.DeletePayload(geometryId))
				{
					num++;
				}
			}
			for (int j = imported.Count - 1; j >= 0; j--)
			{
				if (parts.Contains(imported[j]))
				{
					imported.RemoveAt(j);
				}
			}
			Log.Write((!deletePayload) ? "MODEL_REMOVED" : "MODEL_DELETED", "parts=" + parts.Count + " payloadsDeleted=" + num);
			return num;
		}

		private static string SettingsPath
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Log.LogPath)), "settings.txt");
			}
		}

		// IMGUI draws at a fixed pixel size and does not follow the game's UI scaling,
		// so the window shrinks as resolution rises. Default from the screen height and
		// let the user override it.
		private float Scale
		{
			get
			{
				if (uiScale <= 0f)
				{
					uiScale = LoadScale();
				}
				return uiScale;
			}
		}

		private static string ReadSetting(string key)
		{
			try
			{
				if (File.Exists(SettingsPath))
				{
					string[] array = File.ReadAllLines(SettingsPath);
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i].StartsWith(key + "="))
						{
							return array[i].Substring(key.Length + 1).Trim();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Write("SETTINGS", "could not read " + SettingsPath + ": " + ex.Message);
			}
			return string.Empty;
		}

		private void SaveSettings()
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("uiScale=" + Scale.ToString("0.00", CultureInfo.InvariantCulture));
				stringBuilder.AppendLine("buttonX=" + buttonRect.x.ToString("0", CultureInfo.InvariantCulture));
				stringBuilder.AppendLine("buttonY=" + buttonRect.y.ToString("0", CultureInfo.InvariantCulture));
				stringBuilder.AppendLine("favourites=" + string.Join("|", favourites.ToArray()));
				stringBuilder.AppendLine("recent=" + string.Join("|", recentlyPlaced.ToArray()));
				File.WriteAllText(SettingsPath, stringBuilder.ToString());
			}
			catch (Exception ex)
			{
				Log.Write("SETTINGS", "could not write " + SettingsPath + ": " + ex.Message);
			}
		}

		private static float LoadScale()
		{
			try
			{
				if (File.Exists(SettingsPath))
				{
					string[] array = File.ReadAllLines(SettingsPath);
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i].StartsWith("uiScale="))
						{
							float result;
							if (float.TryParse(array[i].Substring(8).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result) && result >= 0.75f && result <= 3f)
							{
								return result;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Write("SETTINGS", "could not read " + SettingsPath + ": " + ex.Message);
			}
			float num = (float)Screen.height / 1080f;
			return Mathf.Clamp(Mathf.Round(num * 4f) / 4f, 1f, 2.5f);
		}

		private void SetScale(float value)
		{
			uiScale = Mathf.Clamp(value, 0.75f, 3f);
			scaledSkin = null;
			SaveSettings();
			Log.Write("UI_SCALE", "scale=" + uiScale.ToString("0.00", CultureInfo.InvariantCulture));
		}

		// A scaled copy of the skin, applied only around our own window and put back
		// afterwards so no other mod's GUI is affected.
		private GUISkin Skin()
		{
			if (scaledSkin == null || skinBuiltFor != Scale)
			{
				GUISkin guiSkin = UnityEngine.Object.Instantiate(GUI.skin);
				guiSkin.hideFlags = HideFlags.DontSave;
				int num = Mathf.RoundToInt(12f * Scale);
				GUIStyle[] array = new GUIStyle[8] { guiSkin.label, guiSkin.button, guiSkin.toggle, guiSkin.box, guiSkin.window, guiSkin.textField, guiSkin.textArea, guiSkin.verticalScrollbar };
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null)
					{
						array[i].fontSize = num;
						array[i].wordWrap = false;
					}
				}
				guiSkin.window.fontSize = Mathf.RoundToInt(13f * Scale);
				scaledSkin = guiSkin;
				skinBuiltFor = Scale;
			}
			return scaledSkin;
		}

		private float W(float pixels)
		{
			return pixels * Scale;
		}

		private void OnGUI()
		{
			GUISkin skin = GUI.skin;
			GUI.skin = Skin();
			DrawStatusButton();
			if (windowOpen)
			{
				windowRect = GUILayout.Window(19778, windowRect, DrawWindow, "MeshBridge " + Log.Version, GUILayout.Width(W(460f)));
			}
			GUI.skin = skin;
		}

		// Status in two channels, colour and word, because colour alone fails for
		// roughly one man in twelve. 0 green, 1 yellow, 2 red, 3 nothing to report.
		private int StatusLevel()
		{
			if (unresolved.Count > 0)
			{
				return 2;
			}
			if (standInsServed > 0 || unprotected.Count > 0)
			{
				return 1;
			}
			if (Manifest.PendingValid && Manifest.Pending.Count == 0 && imported.Count == 0)
			{
				return 3;
			}
			return 0;
		}

		private void DrawStatusButton()
		{
			if (buttonRect.x < 0f)
			{
				float num = 0f;
				float num2 = 0f;
				float.TryParse(ReadSetting("buttonX"), NumberStyles.Float, CultureInfo.InvariantCulture, out num);
				float.TryParse(ReadSetting("buttonY"), NumberStyles.Float, CultureInfo.InvariantCulture, out num2);
				if (num <= 0f || num2 <= 0f || num > (float)Screen.width || num2 > (float)Screen.height)
				{
					num = 16f;
					num2 = (float)Screen.height - W(90f);
				}
				buttonRect = new Rect(num, num2, W(160f), W(34f));
			}
			buttonRect.width = W(160f);
			buttonRect.height = W(34f);
			Rect rect = buttonRect;
			buttonRect = GUI.Window(19779, buttonRect, DrawStatusButtonContents, string.Empty);
			if (buttonRect.x != rect.x || buttonRect.y != rect.y)
			{
				SaveSettings();
			}
		}

		// GUI.backgroundColor tints the skin's button texture, and CS1's is almost
		// black, so a tinted button reads as black whatever colour is applied. A solid
		// swatch drawn as a style background is unambiguous, and the text is coloured
		// too so the state is legible even at a glance.
		private Texture2D Swatch(Color colour)
		{
			int key = ((int)(colour.r * 255f) << 16) | ((int)(colour.g * 255f) << 8) | (int)(colour.b * 255f);
			Texture2D value;
			if (!swatches.TryGetValue(key, out value) || value == null)
			{
				value = new Texture2D(1, 1, TextureFormat.RGBA32, false);
				value.hideFlags = HideFlags.DontSave;
				value.SetPixel(0, 0, colour);
				value.Apply();
				swatches[key] = value;
			}
			return value;
		}

		private void DrawStatusButtonContents(int id)
		{
			int num = StatusLevel();
			string text;
			Color backgroundColor;
			switch (num)
			{
			case 2:
				text = "MISSING";
				backgroundColor = new Color(1f, 0.35f, 0.32f);
				break;
			case 1:
				text = "STAND-IN";
				backgroundColor = new Color(1f, 0.82f, 0.25f);
				break;
			case 3:
				text = "MeshBridge";
				backgroundColor = new Color(0.72f, 0.72f, 0.72f);
				break;
			default:
				text = "OK";
				backgroundColor = new Color(0.45f, 0.9f, 0.5f);
				break;
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label("\u283f", GUILayout.Width(W(14f)));
			GUIStyle gUIStyle = new GUIStyle();
			gUIStyle.normal.background = Swatch(backgroundColor);
			gUIStyle.margin = new RectOffset(0, 4, 4, 4);
			GUILayout.Label(GUIContent.none, gUIStyle, GUILayout.Width(W(16f)), GUILayout.Height(W(16f)));
			Color backgroundColor2 = GUI.backgroundColor;
			Color contentColor = GUI.contentColor;
			GUI.backgroundColor = backgroundColor;
			GUI.contentColor = backgroundColor;
			if (GUILayout.Button(text, GUILayout.Width(W(104f))))
			{
				windowOpen = !windowOpen;
				Log.Write("WINDOW", ((!windowOpen) ? "closed" : "opened") + " from status button");
			}
			GUI.backgroundColor = backgroundColor2;
			GUI.contentColor = contentColor;
			GUILayout.EndHorizontal();
			GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
		}

		private void DrawWindow(int id)
		{
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			// Toggles drawn with the button style, so the tab you are on stays visibly
			// held down instead of both looking identical.
			if (GUILayout.Toggle(!packagesTab, "Stored Assets", GUI.skin.button, GUILayout.Width(W(130f))))
			{
				packagesTab = false;
			}
			if (GUILayout.Toggle(packagesTab, "PO Assets", GUI.skin.button, GUILayout.Width(W(110f))))
			{
				if (!packagesTab)
				{
					InvalidatePackageCache();
				}
				packagesTab = true;
			}
			GUILayout.FlexibleSpace();
			GUILayout.Label("Text size", GUILayout.Width(W(60f)));
			if (GUILayout.Button("\u2212", GUILayout.Width(W(28f))))
			{
				SetScale(Scale - 0.25f);
			}
			if (GUILayout.Button("+", GUILayout.Width(W(28f))))
			{
				SetScale(Scale + 0.25f);
			}
			GUILayout.EndHorizontal();
			GUILayout.Space(6f);
			if (packagesTab)
			{
				windowScroll = GUILayout.BeginScrollView(windowScroll, GUILayout.Height(W(300f)));
				DrawPackages();
				GUILayout.EndScrollView();
				GUILayout.Space(6f);
				if (windowStatus.Length > 0)
				{
					GUILayout.Label(windowStatus);
				}
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Close", GUILayout.Width(W(90f))))
				{
					windowOpen = false;
				}
				GUILayout.EndHorizontal();
				GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
				return;
			}
			if (reconstructionFailed)
			{
				GUILayout.Label("Donor reconstruction failed this session. See LOAD_EXCEPTION in the log.");
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("?", GUILayout.Width(W(28f))))
			{
				showHelp = !showHelp;
			}
			GUILayout.Label("How this works");
			GUILayout.EndHorizontal();
			if (showHelp)
			{
				GUILayout.Label("Put .obj or .fbx files in the Import folder, then press Refresh.");
				GUILayout.Label("Subfolders are read too, so a model can sit with its textures.");
				GUILayout.Label("The list below is the store. Once a model is imported it stays");
				GUILayout.Label("available even if you delete the file, and your cities keep working.");
				GUILayout.Label("A model missing from the list has not been imported yet.");
				GUILayout.Space(4f);
				GUILayout.Label(Importer.ImportFolder);
			}
			GUILayout.Space(6f);
			if (unresolved.Count > 0)
			{
				GUILayout.Label("MISSING DEPENDENCIES (" + unresolved.Count + ") — do not save this city:");
				for (int i = 0; i < unresolved.Count; i++)
				{
					GUILayout.Label("   " + unresolved[i]);
				}
				GUILayout.Space(6f);
			}
			// Not a missing dependency: nothing is broken now. It means the safety net
			// for an installed asset has quietly gone, which the user can only learn
			// here, because the asset still resolves.
			// Collapsed by default: it is information, not an emergency. The missing-
			// dependency warning above stays open, because that one is about to cost the
			// user objects and hiding it would defeat the point of having it.
			if (unprotected.Count > 0)
			{
				GUILayout.BeginHorizontal();
				if (GUILayout.Button((!showUnprotected) ? "▸" : "▾", GUILayout.Width(W(26f))))
				{
					showUnprotected = !showUnprotected;
				}
				GUILayout.Label("PROTECTION LOST (" + unprotected.Count + ") — these still work, but are no longer backed up");
				GUILayout.EndHorizontal();
				if (showUnprotected)
				{
					for (int up = 0; up < unprotected.Count && up < 12; up++)
					{
						GUILayout.Label("      " + unprotected[up]);
					}
					if (unprotected.Count > 12)
					{
						GUILayout.Label("      and " + (unprotected.Count - 12) + " more — see the log");
					}
					GUILayout.Label("      Run Protect city to capture them again.");
					GUILayout.Space(4f);
				}
			}
			// Only drawn when PO has a live selection, so it is not a permanent control.
			ProceduralObjectsLogic instance2 = ProceduralObjectsLogic.instance;
			if (instance2 != null && instance2.pObjSelection != null && instance2.pObjSelection.Count > 0)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(instance2.pObjSelection.Count + " selected in PO", GUILayout.Width(W(140f)));
				if (GUILayout.Button("Select whole assemblies", GUILayout.Width(W(180f))))
				{
					int num5 = ExpandSelectionToAssemblies();
					windowStatus = ((num5 <= 0) ? "Nothing to add — every selected object was already whole." : ("Added " + num5 + " missing part(s) to the selection."));
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(4f);
			}
			// Count what the list below will actually draw: one entry per model, and
			// only models with a live donor. importedDonors also holds captured
			// stand-ins, which are not listed, so it is the wrong thing to count.
			List<string> list = VisibleModelKeys();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Stored Assets (" + list.Count + ")", GUILayout.Width(W(130f)));
			GUILayout.Label("Find", GUILayout.Width(W(32f)));
			searchText = GUILayout.TextField(searchText, GUILayout.Width(W(120f)));
			if (GUILayout.Button("x", GUILayout.Width(W(24f))))
			{
				searchText = string.Empty;
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button((sortMode != 0) ? "Size" : "A-Z", GUILayout.Width(W(60f))))
			{
				sortMode = 1 - sortMode;
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			if (GUILayout.Toggle(filterMode == 0, "All", GUI.skin.button, GUILayout.Width(W(70f))))
			{
				filterMode = 0;
			}
			if (GUILayout.Toggle(filterMode == 1, "Favourites", GUI.skin.button, GUILayout.Width(W(100f))))
			{
				filterMode = 1;
			}
			if (GUILayout.Toggle(filterMode == 2, "Recent", GUI.skin.button, GUILayout.Width(W(80f))))
			{
				filterMode = 2;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			// Fit the list to what is in it, up to a cap, so a two-model library does not
			// sit in a half-empty window.
			int num6 = list.Count;
			for (int ex = 0; ex < list.Count; ex++)
			{
				if (expandedModels.Contains(list[ex]))
				{
					num6 += 4;
				}
			}
			float num7 = Mathf.Clamp((float)num6 * 26f * Scale + W(16f), W(50f), W(340f));
			windowScroll = GUILayout.BeginScrollView(windowScroll, GUILayout.Height(num7));
			// The test prism is the acceptance fixture from the test protocol, not a
			// model anyone wants to place. Ctrl+Shift+M still spawns it.
			if (list.Count == 0)
			{
				GUILayout.Label((searchText.Length <= 0) ? ((filterMode != 0) ? "   Nothing in this view yet." : "   Nothing imported yet.") : ("   Nothing matches \"" + searchText + "\"."));
			}
			for (int k = 0; k < list.Count; k++)
			{
				string text = list[k];
				List<ImportedResource> list2 = new List<ImportedResource>();
				int num = 0;
				int num2 = 0;
				for (int l = 0; l < imported.Count; l++)
				{
					string text2 = ((imported[l].ModelKey.Length <= 0) ? imported[l].DonorName : imported[l].ModelKey);
					PropInfo value;
					if (!(text2 != text) && importedDonors.TryGetValue(imported[l].DonorName, out value) && !(value == null))
					{
						list2.Add(imported[l]);
						num += imported[l].VertexCount;
						num2 += imported[l].TriangleCount;
					}
				}
				if (list2.Count == 0)
				{
					continue;
				}
				bool flag = true;
				for (int m = 0; m < list2.Count; m++)
				{
					if (list2[m].SourceFile != "(from store)")
					{
						flag = false;
					}
				}
				// One line per model. Geometry ids, source file and the per-part breakdown
				// go behind the arrow: that is what you want when something is wrong, not
				// while you are placing things.
				bool flag3 = expandedModels.Contains(text);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button((!flag3) ? "▸" : "▾", GUILayout.Width(W(26f))))
				{
					if (flag3)
					{
						expandedModels.Remove(text);
					}
					else
					{
						expandedModels.Add(text);
					}
				}
				bool flag5 = favourites.Contains(text);
				if (GUILayout.Button((!flag5) ? "☆" : "★", GUILayout.Width(W(26f))))
				{
					ToggleFavourite(text);
				}
				GUILayout.Label(Importer.ModelLabel(text) + ((list2.Count <= 1) ? string.Empty : ("  (" + list2.Count + " parts)")), GUILayout.Width(W(190f)));
				GUILayout.Label(num + " verts", GUILayout.Width(W(85f)));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Remove", GUILayout.Width(W(70f))))
				{
					confirmKey = text;
					confirmIsDelete = false;
				}
				if (GUILayout.Button("Place", GUILayout.Width(W(70f))))
				{
					PlaceModel(text, text);
					NotePlaced(text);
				}
				GUILayout.EndHorizontal();
				if (confirmKey == text)
				{
					int num4 = LiveUsers(list2);
					GUILayout.BeginHorizontal();
					if (num4 != 0)
					{
						GUILayout.Label("      In use by " + ((num4 >= 0) ? (num4 + " object(s)") : "objects") + " in this city — not removed.");
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("OK", GUILayout.Width(W(70f))))
						{
							confirmKey = string.Empty;
						}
					}
					else
					{
						GUILayout.Label("      " + ((!confirmIsDelete) ? "Remove from the list? The geometry stays in the store." : ((!flag) ? "Delete the stored geometry permanently?" : "Delete permanently? The store holds the only copy.")));
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Cancel", GUILayout.Width(W(70f))))
						{
							confirmKey = string.Empty;
						}
						if (GUILayout.Button((!confirmIsDelete) ? "Remove" : "Delete", GUILayout.Width(W(70f))))
						{
							int num5 = ForgetModel(list2, confirmIsDelete);
							windowStatus = ((!confirmIsDelete) ? ("Removed " + Importer.ModelLabel(text) + " from the list. Its geometry is still in the store.") : ((num5 <= 0) ? ("Removed " + Importer.ModelLabel(text) + ". Its geometry stayed in the store — another model uses it.") : ("Deleted " + Importer.ModelLabel(text) + " and its geometry from the store permanently.")));
							confirmKey = string.Empty;
							expandedModels.Remove(text);
							GUILayout.EndHorizontal();
							break;
						}
					}
					GUILayout.EndHorizontal();
				}
				if (flag3)
				{
					GUILayout.Label("      " + num2 + " tris" + ((list2.Count != 1) ? string.Empty : ("   geometry " + list2[0].GeometryId.Substring(0, 8))));
					if (list2.Count > 1)
					{
						for (int n = 0; n < list2.Count; n++)
						{
							GUILayout.Label("      · " + list2[n].MaterialName + "   " + list2[n].VertexCount + " verts   " + list2[n].GeometryId.Substring(0, 8));
						}
					}
					GUILayout.Label("      " + ((!flag) ? ("source: " + list2[0].SourceFile) : "no source file in the Import folder — served from the store"));
					GUILayout.BeginHorizontal();
					GUILayout.Space(W(24f));
					if (GUILayout.Button("Delete permanently", GUILayout.Width(W(150f))))
					{
						confirmKey = text;
						confirmIsDelete = true;
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.Space(4f);
				}
			}
			if (imported.Count == 0)
			{
				GUILayout.Label("No imported models. Put .obj or .fbx files in the Import folder.");
			}
			if (Importer.LastErrors.Count > 0)
			{
				GUILayout.Space(6f);
				GUILayout.Label("Files that could not be imported:");
				for (int num3 = 0; num3 < Importer.LastErrors.Count; num3++)
				{
					GUILayout.Label("   " + Importer.LastErrors[num3]);
				}
			}
			GUILayout.EndScrollView();
			GUILayout.Space(6f);
			if (windowStatus.Length > 0)
			{
				GUILayout.Label(windowStatus);
			}
			// Shown before anything is written, so the user can see what leaves their
			// machine rather than find out afterwards.
			if (showDiagnostics)
			{
				GUILayout.Label("A diagnostic report collects these into one .zip you can send:");
				List<string> list3 = Diagnostics.Preview();
				for (int m = 0; m < list3.Count; m++)
				{
					GUILayout.Label("   · " + list3[m]);
				}
				GUILayout.Label("Your Windows account name is replaced with <user> in the logs.");
				GUILayout.Label("Capture it in the session the problem happened in — the game");
				GUILayout.Label("overwrites its own log every time it starts.");
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Create report", GUILayout.Width(W(130f))))
				{
					try
					{
						windowStatus = "Diagnostic report written to " + Diagnostics.Build();
					}
					catch (Exception ex4)
					{
						windowStatus = "Diagnostic report failed: " + ex4.Message;
						Log.Write("DIAGNOSTIC_FAILED", ex4.GetType().Name + ": " + ex4.Message);
					}
				}
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(6f);
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Refresh", GUILayout.Width(W(90f))))
			{
				int num4 = ImportNew();
				InvalidatePackageCache();
				windowStatus = ((num4 <= 0) ? "Nothing new in the Import folder." : ("Imported " + num4 + " new model(s) from the Import folder."));
			}
			if (GUILayout.Button("Audit", GUILayout.Width(W(90f))))
			{
				Audit();
			}
			if (GUILayout.Button("Protect city", GUILayout.Width(W(110f))))
			{
				try
				{
					InventoryExisting();
				}
				catch (Exception ex)
				{
					windowStatus = "Inventory failed: " + ex.Message;
				}
			}
			if (GUILayout.Button("Export backup", GUILayout.Width(W(120f))))
			{
				try
				{
					windowStatus = "Recovery package written to " + ExportRecoveryPackage();
				}
				catch (Exception ex2)
				{
					windowStatus = "Export failed: " + ex2.Message;
				}
			}
			if (GUILayout.Button("Diagnostics", GUILayout.Width(W(110f))))
			{
				showDiagnostics = !showDiagnostics;
			}
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Close", GUILayout.Width(W(90f))))
			{
				windowOpen = false;
			}
			GUILayout.EndHorizontal();
			GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
		}

		private void PlaceDonor(PropInfo info, string label)
		{
			try
			{
				ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
				Log.Require(instance != null && instance.proceduralObjects != null, "PO not ready.");
				Log.Require(RenderOptions.instance != null, "PO RenderOptions not ready.");
				Log.Require(!instance.editingWholeModel && !instance.movingWholeModel && !instance.placingSelection, "Finish PO editing or placement first.");
				Log.Require(ToolsModifierControl.cameraController != null, "Camera controller not ready.");
				Vector3 currentPosition = ToolsModifierControl.cameraController.m_currentPosition;
				float num = Singleton<TerrainManager>.instance.SampleDetailHeight(currentPosition);
				Vector3 position = new Vector3(currentPosition.x, num + 0.25f, currentPosition.z);
				Log.Write("PLACE_CALL", "donor=" + info.name + " at " + position.ToString("F3") + " countBefore=" + instance.proceduralObjects.Count);
				ProceduralObject proceduralObject = instance.SpawnObject(new ProceduralInfo(info, false));
				Log.Require(proceduralObject != null, "SpawnObject returned null.");
				proceduralObject.MakeUniqueMesh();
				proceduralObject.SetPosition(position);
				proceduralObject.SetRotation(Quaternion.identity);
				proceduralObject.RecalculateBoundsNormalsExtras(proceduralObject.meshStatus);
				created = true;
				windowStatus = "Placed " + label + " (id " + proceduralObject.id + ") at the camera target.";
				Log.Write("PLACE_OK", "donor=" + info.name + " id=" + proceduralObject.id + " countAfter=" + instance.proceduralObjects.Count);
				AuditObject(proceduralObject);
			}
			catch (Exception ex)
			{
				windowStatus = "Place failed: " + ex.Message;
				Log.Write("PLACE_EXCEPTION", ex.GetType().FullName + ": " + ex.Message + "\n" + ex.StackTrace);
			}
		}

		private void PlaceModel(string modelKey, string label)
		{
			try
			{
				ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
				Log.Require(instance != null && instance.proceduralObjects != null, "PO not ready.");
				Log.Require(RenderOptions.instance != null, "PO RenderOptions not ready.");
				Log.Require(!instance.editingWholeModel && !instance.movingWholeModel && !instance.placingSelection, "Finish PO editing or placement first.");
				Log.Require(ToolsModifierControl.cameraController != null, "Camera controller not ready.");
				List<ImportedResource> list = new List<ImportedResource>();
				for (int i = 0; i < imported.Count; i++)
				{
					if (imported[i].ModelKey == modelKey)
					{
						list.Add(imported[i]);
					}
				}
				Log.Require(list.Count > 0, "No parts found for '" + modelKey + "'.");
				Vector3 currentPosition = ToolsModifierControl.cameraController.m_currentPosition;
				float num = Singleton<TerrainManager>.instance.SampleDetailHeight(currentPosition);
				Vector3 position = new Vector3(currentPosition.x, num + 0.25f, currentPosition.z);
				List<ProceduralObject> list2 = new List<ProceduralObject>();
				for (int j = 0; j < list.Count; j++)
				{
					PropInfo value;
					if (importedDonors.TryGetValue(list[j].DonorName, out value) && !(value == null))
					{
						ProceduralObject proceduralObject = instance.SpawnObject(new ProceduralInfo(value, false));
						if (proceduralObject != null)
						{
							proceduralObject.MakeUniqueMesh();
							proceduralObject.SetPosition(position);
							proceduralObject.SetRotation(Quaternion.identity);
							proceduralObject.RecalculateBoundsNormalsExtras(proceduralObject.meshStatus);
							list2.Add(proceduralObject);
						}
					}
				}
				Log.Require(list2.Count > 0, "No parts placed.");
				if (list2.Count > 1 && instance.groups != null)
				{
					POGroup pOGroup = new POGroup();
					pOGroup.root = list2[0];
					list2[0].isRootOfGroup = true;
					for (int k = 0; k < list2.Count; k++)
					{
						pOGroup.AddToGroup(list2[k]);
					}
					instance.groups.Add(pOGroup);
					Log.Write("PLACE_GROUP", "model=" + modelKey + " parts=" + list2.Count + " grouped root=" + list2[0].id);
				}
				created = true;
				windowStatus = "Placed " + label + " (" + list2.Count + " part" + ((list2.Count != 1) ? "s" : string.Empty) + ") at the camera target.";
				Log.Write("PLACE_OK", "model=" + modelKey + " parts=" + list2.Count + " at " + position.ToString("F3"));
			}
			catch (Exception ex)
			{
				windowStatus = "Place failed: " + ex.Message;
				Log.Write("PLACE_EXCEPTION", ex.GetType().FullName + ": " + ex.Message);
			}
		}

		private void InstallAndPlace(PackageInfo pkg, bool place)
		{
			string text = Packages.Install(pkg);
			InvalidatePackageCache();
			int num = importedDonors.Count + buildingDonors.Count;
			RebuildFromStore();
			RefreshDonorMaterials();
			int num2 = importedDonors.Count + buildingDonors.Count - num;
			Log.Write("PACKAGE_DONORS", "name=" + pkg.Name + " donorsBuiltNow=" + num2 + " — no reload required.");
			if (!place || text.Length == 0)
			{
				windowStatus = "Installed '" + pkg.Name + "'. " + num2 + " donor(s) ready. Place it from PO's externals list.";
				return;
			}
			try
			{
				ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
				Log.Require(instance != null, "PO not ready.");
				ExternalInfo external = new ExternalInfo(pkg.Name, text, ClipboardProceduralObjects.ClipboardType.Selection, false, false);
				instance.PlaceExternal(external);
				windowOpen = false;
				windowStatus = "Placed '" + pkg.Name + "' — position it with the mouse.";
				Log.Write("PACKAGE_PLACE", "name=" + pkg.Name + " handed to PO's paste tool.");
			}
			catch (Exception ex)
			{
				windowStatus = "Installed, but placing failed: " + ex.Message + " — reload the city and place it from PO's externals list.";
				Log.Write("PACKAGE_PLACE_FAILED", ex.GetType().Name + ": " + ex.Message);
			}
		}

		// Packages.List() reads every package folder and parses each creation.pobj,
		// which carries full vertex data. OnGUI runs at least twice per frame plus
		// once per input event, so calling it from the draw path meant re-reading
		// megabytes off disk a hundred-odd times a second and stalling the main
		// thread — the whole game stuttered, not just this window. It is rebuilt on
		// entry to the tab and after anything that changes a package, never on a
		// repaint.
		// Favourites and recents are a handful of model keys. They live beside the
		// UI scale rather than in the store, because they are a view preference and
		// must never affect what a saved city resolves.
		private void LoadListPrefs()
		{
			if (listPrefsLoaded)
			{
				return;
			}
			listPrefsLoaded = true;
			SplitInto(ReadSetting("favourites"), favourites);
			SplitInto(ReadSetting("recent"), recentlyPlaced);
		}

		private static void SplitInto(string packed, List<string> target)
		{
			target.Clear();
			if (packed == null || packed.Length == 0)
			{
				return;
			}
			string[] array = packed.Split('|');
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Length > 0 && !target.Contains(array[i]))
				{
					target.Add(array[i]);
				}
			}
		}

		private void NotePlaced(string modelKey)
		{
			LoadListPrefs();
			recentlyPlaced.Remove(modelKey);
			recentlyPlaced.Insert(0, modelKey);
			while (recentlyPlaced.Count > 12)
			{
				recentlyPlaced.RemoveAt(recentlyPlaced.Count - 1);
			}
			SaveSettings();
		}

		private void ToggleFavourite(string modelKey)
		{
			LoadListPrefs();
			if (favourites.Contains(modelKey))
			{
				favourites.Remove(modelKey);
			}
			else
			{
				favourites.Add(modelKey);
			}
			SaveSettings();
		}

		// Filtering and sorting run over the in-memory list only. No disk, no
		// allocation beyond the key list, so this is safe to do on the draw path —
		// unlike the package list, which is not.
		private List<string> VisibleModelKeys()
		{
			LoadListPrefs();
			List<string> list = new List<string>();
			for (int i = 0; i < imported.Count; i++)
			{
				string text = ((imported[i].ModelKey.Length <= 0) ? imported[i].DonorName : imported[i].ModelKey);
				if (list.Contains(text))
				{
					continue;
				}
				PropInfo value;
				if (!importedDonors.TryGetValue(imported[i].DonorName, out value) || value == null)
				{
					continue;
				}
				if (filterMode == 1 && !favourites.Contains(text))
				{
					continue;
				}
				if (filterMode == 2 && !recentlyPlaced.Contains(text))
				{
					continue;
				}
				if (searchText.Length > 0 && Importer.ModelLabel(text).ToLowerInvariant().IndexOf(searchText.ToLowerInvariant()) < 0)
				{
					continue;
				}
				list.Add(text);
			}
			if (filterMode == 2)
			{
				List<string> list2 = new List<string>();
				for (int j = 0; j < recentlyPlaced.Count; j++)
				{
					if (list.Contains(recentlyPlaced[j]))
					{
						list2.Add(recentlyPlaced[j]);
					}
				}
				return list2;
			}
			if (sortMode == 1)
			{
				list.Sort(CompareBySize);
			}
			else
			{
				list.Sort(CompareByLabel);
			}
			return list;
		}

		private static int CompareByLabel(string a, string b)
		{
			return string.Compare(Importer.ModelLabel(a), Importer.ModelLabel(b), StringComparison.OrdinalIgnoreCase);
		}

		private int VertsFor(string modelKey)
		{
			int num = 0;
			for (int i = 0; i < imported.Count; i++)
			{
				string text = ((imported[i].ModelKey.Length <= 0) ? imported[i].DonorName : imported[i].ModelKey);
				if (text == modelKey)
				{
					num += imported[i].VertexCount;
				}
			}
			return num;
		}

		private int CompareBySize(string a, string b)
		{
			return VertsFor(b).CompareTo(VertsFor(a));
		}

		// PO already does area selection, and it already groups the parts of a
		// multi-material import (V26). What it does not do is join the two: a rubber
		// band that clips one part of an assembly selects that part alone. This
		// completes the selection instead of adding a second selection tool.
		private static int ExpandSelectionToAssemblies()
		{
			ProceduralObjectsLogic instance = ProceduralObjectsLogic.instance;
			if (instance == null || instance.pObjSelection == null || instance.pObjSelection.Count == 0)
			{
				return 0;
			}
			List<ProceduralObject> list = new List<ProceduralObject>();
			for (int i = 0; i < instance.pObjSelection.Count; i++)
			{
				ProceduralObject proceduralObject = instance.pObjSelection[i];
				if (proceduralObject == null)
				{
					continue;
				}
				if (proceduralObject.group == null || proceduralObject.group.objects == null)
				{
					if (!list.Contains(proceduralObject))
					{
						list.Add(proceduralObject);
					}
					continue;
				}
				for (int j = 0; j < proceduralObject.group.objects.Count; j++)
				{
					ProceduralObject proceduralObject2 = proceduralObject.group.objects[j];
					if (proceduralObject2 != null && !list.Contains(proceduralObject2))
					{
						list.Add(proceduralObject2);
					}
				}
			}
			int num = list.Count - instance.pObjSelection.Count;
			if (num <= 0)
			{
				Log.Write("SELECT_ASSEMBLY", "selected=" + instance.pObjSelection.Count + " added=0 — every selected object was already whole.");
				return 0;
			}
			// Filled in place: PO holds this list by reference in several places, so
			// handing it a new List would leave those pointing at the old one.
			instance.pObjSelection.Clear();
			instance.pObjSelection.AddRange(list);
			Log.Write("SELECT_ASSEMBLY", "selection expanded to " + list.Count + " objects, added=" + num);
			return num;
		}

		private void InvalidatePackageCache()
		{
			packageCache = null;
			poExportCache = null;
		}

		private List<PackageInfo> CachedPackages()
		{
			if (packageCache == null)
			{
				DateTime utcNow = DateTime.UtcNow;
				packageCache = Packages.List();
				Log.Write("PACKAGE_LIST_MS", "packages=" + packageCache.Count + " ms=" + (DateTime.UtcNow - utcNow).TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture));
			}
			return packageCache;
		}

		private List<string> CachedPoExports()
		{
			if (poExportCache == null)
			{
				poExportCache = Packages.ListPoExports();
			}
			return poExportCache;
		}

		private void DrawPackages()
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("?", GUILayout.Width(W(28f))))
			{
				showHelp = !showHelp;
			}
			GUILayout.Label("How this works");
			GUILayout.EndHorizontal();
			if (showHelp)
			{
				GUILayout.Label("A creation is a PO export packaged with the geometry it needs, so");
				GUILayout.Label("it works in another city or on another PC. Export a selection with");
				GUILayout.Label("PO's own export button first, then save it here.");
				GUILayout.Label("Place drops it into the world. Install copies it in without placing.");
				GUILayout.Space(4f);
				GUILayout.Label(Packages.Root);
			}
			GUILayout.Space(6f);
			List<PackageInfo> list2 = CachedPackages();
			List<string> list = CachedPoExports();
			GUILayout.Label("New creation from a PO export (" + list.Count + " available)");
			if (list.Count > 0)
			{
				if (selectedExport >= list.Count)
				{
					selectedExport = 0;
				}
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("<", GUILayout.Width(W(26f))))
				{
					selectedExport = (selectedExport + list.Count - 1) % list.Count;
				}
				string text3 = Path.GetFileNameWithoutExtension(list[selectedExport]);
				string text4 = "";
				try
				{
					DateTime lastWriteTime = File.GetLastWriteTime(list[selectedExport]);
					text4 = ((!(lastWriteTime.Date == DateTime.Now.Date)) ? lastWriteTime.ToString("d MMM HH:mm") : ("today " + lastWriteTime.ToString("HH:mm")));
				}
				catch
				{
				}
				GUILayout.Label(text3, GUILayout.Width(W(190f)));
				GUILayout.Label(((selectedExport != 0) ? "" : "newest  ") + text4, GUILayout.Width(W(130f)));
				if (GUILayout.Button(">", GUILayout.Width(W(26f))))
				{
					selectedExport = (selectedExport + 1) % list.Count;
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Package name", GUILayout.Width(W(100f)));
				packageName = GUILayout.TextField(packageName, GUILayout.Width(W(200f)));
				GUILayout.EndHorizontal();
				// Saving writes into PO Assets\<name>, so an existing creation of that
				// name is replaced. Say so before it happens, not after.
				bool flag6 = false;
				for (int n2 = 0; n2 < list2.Count; n2++)
				{
					if (string.Equals(list2[n2].Name, packageName.Trim(), StringComparison.OrdinalIgnoreCase))
					{
						flag6 = true;
					}
				}
				GUILayout.Label("   Packaging: " + text3);
				if (flag6)
				{
					GUILayout.Label("   A creation named \"" + packageName.Trim() + "\" already exists. Saving replaces it.");
				}
				if (GUILayout.Button((!flag6) ? "Save as PO Asset" : "Replace existing", GUILayout.Width(W(160f))))
				{
					try
					{
						PackageInfo packageInfo = Packages.Save(list[selectedExport], packageName);
						InvalidatePackageCache();
						windowStatus = ((!packageInfo.Complete) ? ("Saved '" + packageInfo.Name + "' but " + packageInfo.Missing.Count + " dependency(ies) are not in the store. Run Protect city, then save again.") : ("Saved '" + packageInfo.Name + "' — " + packageInfo.ObjectCount + " objects, all dependencies packaged."));
					}
					catch (Exception ex)
					{
						windowStatus = "Save failed: " + ex.Message;
					}
				}
			}
			GUILayout.Space(8f);
			GUILayout.Label("Saved creations (" + list2.Count + ")");
			if (list2.Count == 0)
			{
				GUILayout.Label("   None yet.");
			}
			for (int i = 0; i < list2.Count; i++)
			{
				PackageInfo packageInfo2 = list2[i];
				bool flag4 = expandedModels.Contains("pkg:" + packageInfo2.Name);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button((!flag4) ? "▸" : "▾", GUILayout.Width(W(26f))))
				{
					if (flag4)
					{
						expandedModels.Remove("pkg:" + packageInfo2.Name);
					}
					else
					{
						expandedModels.Add("pkg:" + packageInfo2.Name);
					}
				}
				GUILayout.Label(packageInfo2.Name, GUILayout.Width(W(180f)));
				GUILayout.Label((!packageInfo2.Complete) ? (packageInfo2.Missing.Count + " MISSING") : (packageInfo2.ObjectCount + " objects"), GUILayout.Width(W(90f)));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Place", GUILayout.Width(W(70f))))
				{
					try
					{
						InstallAndPlace(packageInfo2, true);
					}
					catch (Exception ex2)
					{
						windowStatus = "Failed: " + ex2.Message;
					}
				}
				if (GUILayout.Button("Install", GUILayout.Width(W(70f))))
				{
					try
					{
						InstallAndPlace(packageInfo2, false);
					}
					catch (Exception ex3)
					{
						windowStatus = "Install failed: " + ex3.Message;
					}
				}
				GUILayout.EndHorizontal();
				if (flag4)
				{
					GUILayout.Label("      " + packageInfo2.ObjectCount + " objects across " + packageInfo2.BaseNames.Count + " resource(s)");
					if (!packageInfo2.Complete)
					{
						GUILayout.Label("      Incomplete — these are named but not carried:");
						for (int k = 0; k < packageInfo2.Missing.Count; k++)
						{
							GUILayout.Label("        · " + packageInfo2.Missing[k]);
						}
						GUILayout.Label("      Run Protect city, then save the creation again.");
					}
					GUILayout.BeginHorizontal();
					GUILayout.Space(W(24f));
					if (confirmKey != "pkg:" + packageInfo2.Name)
					{
						if (GUILayout.Button("Delete creation", GUILayout.Width(W(130f))))
						{
							confirmKey = "pkg:" + packageInfo2.Name;
						}
					}
					else
					{
						GUILayout.Label("Delete this creation? The models it used stay in the store.");
						if (GUILayout.Button("Cancel", GUILayout.Width(W(70f))))
						{
							confirmKey = string.Empty;
						}
						if (GUILayout.Button("Delete", GUILayout.Width(W(70f))))
						{
							windowStatus = (Packages.Delete(packageInfo2) ? ("Deleted creation '" + packageInfo2.Name + "'.") : ("Could not delete '" + packageInfo2.Name + "' — see the log."));
							InvalidatePackageCache();
							confirmKey = string.Empty;
							expandedModels.Remove("pkg:" + packageInfo2.Name);
							GUILayout.EndHorizontal();
							break;
						}
					}
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.Space(4f);
				}
			}
		}

		private void Own(UnityEngine.Object o)
		{
			if (!(o == null))
			{
				o.hideFlags = HideFlags.DontSave;
				owned.Add(o);
			}
		}

		public void Cleanup()
		{
			for (int i = 0; i < owned.Count; i++)
			{
				if (owned[i] != null)
				{
					UnityEngine.Object.DestroyImmediate(owned[i]);
				}
			}
			owned.Clear();
			donor = null;
			material = null;
			checker = (aci = (xys = null));
			created = false;
			attempted = false;
			imported.Clear();
			importedDonors.Clear();
			buildingDonors.Clear();
			catalogueInjected = false;
			cachedScaffold = null;
			cachedBuildingTemplate = null;
			unresolved.Clear();
			if (live == this)
			{
				live = null;
			}
			Log.Write("CLEANUP", "Destroyed MeshBridge-owned donor/resources at level unload; no game or PO assets modified.");
		}

		private void OnDestroy()
		{
			Cleanup();
		}

		private void Update()
		{
			bool flag = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
			if (flag && Input.GetKeyDown(KeyCode.M))
			{
				if (attempted)
				{
					Log.Write("TRIGGER", "One creation attempt per loaded city. Use Ctrl+Shift+L to audit; reload a test city to retry.");
				}
				else
				{
					attempted = true;
					Spawn();
				}
			}
			if (flag && Input.GetKeyDown(KeyCode.L))
			{
				Audit();
			}
			if (flag && Input.GetKeyDown(KeyCode.B))
			{
				windowOpen = !windowOpen;
				Log.Write("WINDOW", (!windowOpen) ? "closed" : "opened");
			}
			if (!catalogueInjected && importedDonors.Count > 0)
			{
				InjectCatalogue();
			}
			if (created && Time.realtimeSinceStartup > nextAudit)
			{
				nextAudit = Time.realtimeSinceStartup + 30f;
				Audit();
			}
		}
	}
}
