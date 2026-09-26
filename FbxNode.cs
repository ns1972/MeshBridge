using System.Collections.Generic;

namespace MeshBridge
{
	public class FbxNode
	{
		public string Name = string.Empty;

		public List<object> Properties = new List<object>();

		public List<FbxNode> Children = new List<FbxNode>();

		public FbxNode Find(string name)
		{
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i].Name == name)
				{
					return Children[i];
				}
			}
			return null;
		}

		public List<FbxNode> FindAll(string name)
		{
			List<FbxNode> list = new List<FbxNode>();
			for (int i = 0; i < Children.Count; i++)
			{
				if (Children[i].Name == name)
				{
					list.Add(Children[i]);
				}
			}
			return list;
		}

		public double[] Doubles()
		{
			for (int i = 0; i < Properties.Count; i++)
			{
				double[] array = Properties[i] as double[];
				if (array != null)
				{
					return array;
				}
			}
			return null;
		}

		public int[] Ints()
		{
			for (int i = 0; i < Properties.Count; i++)
			{
				int[] array = Properties[i] as int[];
				if (array != null)
				{
					return array;
				}
			}
			return null;
		}

		public string Text(int index)
		{
			if (index < 0 || index >= Properties.Count)
			{
				return string.Empty;
			}
			return (Properties[index] as string) ?? string.Empty;
		}
	}
}
