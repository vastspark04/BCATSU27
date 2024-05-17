using System;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	// Token: 0x02000035 RID: 53
	internal class Triangles
	{
		// Token: 0x06000100 RID: 256 RVA: 0x0000CFD0 File Offset: 0x0000B1D0
		private static bool HasMeshes()
		{
			if (Triangles.meshes == null)
			{
				return false;
			}
			for (int i = 0; i < Triangles.meshes.Length; i++)
			{
				if (null == Triangles.meshes[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000D00C File Offset: 0x0000B20C
		private static void Cleanup()
		{
			if (Triangles.meshes == null)
			{
				return;
			}
			for (int i = 0; i < Triangles.meshes.Length; i++)
			{
				if (null != Triangles.meshes[i])
				{
					UnityEngine.Object.DestroyImmediate(Triangles.meshes[i]);
					Triangles.meshes[i] = null;
				}
			}
			Triangles.meshes = null;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000D05C File Offset: 0x0000B25C
		private static Mesh[] GetMeshes(int totalWidth, int totalHeight)
		{
			if (Triangles.HasMeshes() && Triangles.currentTris == totalWidth * totalHeight)
			{
				return Triangles.meshes;
			}
			int num = 21666;
			int num2 = totalWidth * totalHeight;
			Triangles.currentTris = num2;
			Triangles.meshes = new Mesh[Mathf.CeilToInt(1f * (float)num2 / (1f * (float)num))];
			int num3 = 0;
			for (int i = 0; i < num2; i += num)
			{
				int triCount = Mathf.FloorToInt((float)Mathf.Clamp(num2 - i, 0, num));
				Triangles.meshes[num3] = Triangles.GetMesh(triCount, i, totalWidth, totalHeight);
				num3++;
			}
			return Triangles.meshes;
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000D0EC File Offset: 0x0000B2EC
		private static Mesh GetMesh(int triCount, int triOffset, int totalWidth, int totalHeight)
		{
			Mesh mesh = new Mesh();
			mesh.hideFlags = HideFlags.DontSave;
			Vector3[] array = new Vector3[triCount * 3];
			Vector2[] array2 = new Vector2[triCount * 3];
			Vector2[] array3 = new Vector2[triCount * 3];
			int[] array4 = new int[triCount * 3];
			for (int i = 0; i < triCount; i++)
			{
				int num = i * 3;
				int num2 = triOffset + i;
				float num3 = Mathf.Floor((float)(num2 % totalWidth)) / (float)totalWidth;
				float num4 = Mathf.Floor((float)(num2 / totalWidth)) / (float)totalHeight;
				Vector3 vector = new Vector3(num3 * 2f - 1f, num4 * 2f - 1f, 1f);
				array[num] = vector;
				array[num + 1] = vector;
				array[num + 2] = vector;
				array2[num] = new Vector2(0f, 0f);
				array2[num + 1] = new Vector2(1f, 0f);
				array2[num + 2] = new Vector2(0f, 1f);
				array3[num] = new Vector2(num3, num4);
				array3[num + 1] = new Vector2(num3, num4);
				array3[num + 2] = new Vector2(num3, num4);
				array4[num] = num;
				array4[num + 1] = num + 1;
				array4[num + 2] = num + 2;
			}
			mesh.vertices = array;
			mesh.triangles = array4;
			mesh.uv = array2;
			mesh.uv2 = array3;
			return mesh;
		}

		// Token: 0x040001C8 RID: 456
		private static Mesh[] meshes;

		// Token: 0x040001C9 RID: 457
		private static int currentTris;
	}
}
