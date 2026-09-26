using ICities;

namespace MeshBridge
{
	public class Mod : IUserMod
	{
		public string Name
		{
			get
			{
				return "MeshBridge " + Log.Version;
			}
		}

		public string Description
		{
			get
			{
				return "by Night Hare \u2014 imports .obj and .fbx models into Procedural Objects as editable objects, and protects your PO base-prefab dependencies. Ctrl+Shift+B opens the window. Pre-release.";
			}
		}
	}
}
