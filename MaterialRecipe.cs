using UnityEngine;

namespace MeshBridge
{
	public class MaterialRecipe
	{
		public string MainTexId = string.Empty;

		public string AciTexId = string.Empty;

		public string XysTexId = string.Empty;

		public Color Colour = Color.white;

		public string Shader = string.Empty;

		public bool HasAny
		{
			get
			{
				return MainTexId.Length > 0 || AciTexId.Length > 0 || XysTexId.Length > 0;
			}
		}
	}
}
