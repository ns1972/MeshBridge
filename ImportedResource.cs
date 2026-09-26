using UnityEngine;

namespace MeshBridge
{
	public class ImportedResource
	{
		public string DonorName;

		public string FriendlyName;

		public string SourceFile;

		public string GeometryId;

		public Mesh Mesh;

		public int VertexCount;

		public int TriangleCount;

		public string MaterialName = string.Empty;

		public string ModelKey = string.Empty;
	}
}
