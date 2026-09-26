using System;
using System.Collections.Generic;
using UnityEngine;

namespace MeshBridge
{
	public static class Fixture
	{
		public const float Radius = 3f;

		public const float Height = 4f;

		private static readonly float[] RadiusScale = new float[5] { 1f, 0.72f, 1.15f, 0.83f, 1.31f };

		private static readonly Vector3 TopOffset = new Vector3(0.9f, 0f, -0.45f);

		private const float TopTwistDegrees = 14f;

		private static Vector3 Ring(int i, bool top)
		{
			float num = 72f;
			float num2 = (float)i * num + ((!top) ? 0f : 14f);
			float f = num2 * ((float)Math.PI / 180f);
			float num3 = 3f * RadiusScale[i];
			Vector3 vector = new Vector3(Mathf.Cos(f) * num3, (!top) ? 0f : 4f, Mathf.Sin(f) * num3);
			if (top)
			{
				return vector + TopOffset;
			}
			return vector;
		}

		public static void Data(out Vector3[] vertices, out int[] triangles, out Vector2[] uv)
		{
			List<Vector3> list = new List<Vector3>();
			List<int> list2 = new List<int>();
			List<Vector2> list3 = new List<Vector2>();
			for (int i = 0; i < 5; i++)
			{
				Vector3 item = Ring(i, false);
				list.Add(item);
				list3.Add(new Vector2(0.5f + item.x / 9f, 0.5f + item.z / 9f));
			}
			for (int j = 1; j < 4; j++)
			{
				list2.Add(0);
				list2.Add(j);
				list2.Add(j + 1);
			}
			for (int k = 0; k < 5; k++)
			{
				Vector3 item2 = Ring(k, true);
				list.Add(item2);
				list3.Add(new Vector2(0.5f + item2.x / 9f, 0.5f + item2.z / 9f));
			}
			for (int l = 1; l < 4; l++)
			{
				list2.Add(5);
				list2.Add(5 + l + 1);
				list2.Add(5 + l);
			}
			for (int m = 0; m < 5; m++)
			{
				int i2 = (m + 1) % 5;
				Vector3 item3 = Ring(m, false);
				Vector3 item4 = Ring(i2, false);
				Vector3 item5 = Ring(m, true);
				Vector3 item6 = Ring(i2, true);
				int count = list.Count;
				list.Add(item3);
				list.Add(item4);
				list.Add(item6);
				list.Add(item5);
				list3.Add(new Vector2(0f, 0f));
				list3.Add(new Vector2(1f, 0f));
				list3.Add(new Vector2(1f, 1f));
				list3.Add(new Vector2(0f, 1f));
				list2.Add(count);
				list2.Add(count + 2);
				list2.Add(count + 1);
				list2.Add(count);
				list2.Add(count + 3);
				list2.Add(count + 2);
			}
			vertices = list.ToArray();
			triangles = list2.ToArray();
			uv = list3.ToArray();
		}

		public static Mesh Create(string name)
		{
			Vector3[] vertices;
			int[] triangles;
			Vector2[] uv;
			Data(out vertices, out triangles, out uv);
			Mesh mesh = new Mesh();
			mesh.name = name;
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.uv = uv;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			Tangents(mesh);
			return mesh;
		}

		private static void Tangents(Mesh mesh)
		{
			Vector3[] vertices = mesh.vertices;
			Vector2[] uv = mesh.uv;
			int[] triangles = mesh.triangles;
			Vector3[] normals = mesh.normals;
			Vector3[] array = new Vector3[vertices.Length];
			Vector3[] array2 = new Vector3[vertices.Length];
			Vector4[] array3 = new Vector4[vertices.Length];
			for (int i = 0; i < triangles.Length; i += 3)
			{
				int num = triangles[i];
				int num2 = triangles[i + 1];
				int num3 = triangles[i + 2];
				Vector3 vector = vertices[num];
				Vector3 vector2 = vertices[num2];
				Vector3 vector3 = vertices[num3];
				Vector2 vector4 = uv[num];
				Vector2 vector5 = uv[num2];
				Vector2 vector6 = uv[num3];
				float num4 = vector2.x - vector.x;
				float num5 = vector3.x - vector.x;
				float num6 = vector2.y - vector.y;
				float num7 = vector3.y - vector.y;
				float num8 = vector2.z - vector.z;
				float num9 = vector3.z - vector.z;
				float num10 = vector5.x - vector4.x;
				float num11 = vector6.x - vector4.x;
				float num12 = vector5.y - vector4.y;
				float num13 = vector6.y - vector4.y;
				float num14 = num10 * num13 - num11 * num12;
				float num15 = ((!Mathf.Approximately(num14, 0f)) ? (1f / num14) : 0f);
				Vector3 vector7 = new Vector3((num13 * num4 - num12 * num5) * num15, (num13 * num6 - num12 * num7) * num15, (num13 * num8 - num12 * num9) * num15);
				Vector3 vector8 = new Vector3((num10 * num5 - num11 * num4) * num15, (num10 * num7 - num11 * num6) * num15, (num10 * num9 - num11 * num8) * num15);
				array[num] += vector7;
				array[num2] += vector7;
				array[num3] += vector7;
				array2[num] += vector8;
				array2[num2] += vector8;
				array2[num3] += vector8;
			}
			for (int j = 0; j < vertices.Length; j++)
			{
				Vector3 normal = normals[j];
				Vector3 tangent = array[j];
				Vector3.OrthoNormalize(ref normal, ref tangent);
				array3[j].x = tangent.x;
				array3[j].y = tangent.y;
				array3[j].z = tangent.z;
				array3[j].w = ((!(Vector3.Dot(Vector3.Cross(normal, tangent), array2[j]) < 0f)) ? 1f : (-1f));
			}
			mesh.tangents = array3;
		}
	}
}
