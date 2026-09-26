using System.Collections.Generic;

namespace MeshBridge
{
	public class PackageInfo
	{
		public string Name;

		public string FolderPath;

		public string PobjPath;

		public List<string> BaseNames = new List<string>();

		public List<string> Missing = new List<string>();

		public int ObjectCount;

		public bool Complete
		{
			get
			{
				return Missing.Count == 0;
			}
		}
	}
}
