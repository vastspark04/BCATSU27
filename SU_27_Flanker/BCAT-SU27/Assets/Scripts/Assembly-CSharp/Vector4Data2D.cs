using System;
using UnityEngine;

[Serializable]
public class Vector4Data2D
{
	[Serializable]
	public class FloatRow
	{
		public Vector4[] values;
	}

	public FloatRow[] rows;

	private BDTexture dataTex;

	public void ApplyData(BDTexture tex)
	{
		if (rows.Length != tex.height)
		{
			rows = new FloatRow[tex.height];
		}
		if (rows[0] == null || rows[0].values == null || rows[0].values.Length != tex.width)
		{
			for (int i = 0; i < tex.height; i++)
			{
				rows[i] = new FloatRow();
				rows[i].values = new Vector4[tex.width];
			}
		}
		for (int j = 0; j < tex.width; j++)
		{
			for (int k = 0; k < tex.height; k++)
			{
				rows[k].values[j].x = tex.GetColor(j, k).r;
				rows[k].values[j].y = tex.GetColor(j, k).g;
				rows[k].values[j].z = tex.GetColor(j, k).b;
				rows[k].values[j].w = tex.GetColor(j, k).a;
			}
		}
	}

	private void SetupTexture()
	{
		if (dataTex == null || dataTex.height != rows.Length || dataTex.width != rows[0].values.Length)
		{
			dataTex = new BDTexture(rows[0].values.Length, rows.Length);
			dataTex.filterMode = BDTexture.FilterModes.Bilinear;
		}
		for (int i = 0; i < dataTex.width; i++)
		{
			for (int j = 0; j < dataTex.height; j++)
			{
				Vector4 vector = rows[j].values[i];
				BDColor c = new BDColor(vector.x, vector.y, vector.z, vector.w);
				dataTex.SetPixel(i, j, c);
			}
		}
	}

	public Vector4 GetData(float uvX, float uvY)
	{
		if (dataTex == null)
		{
			SetupTexture();
		}
		return dataTex.GetColorUV(uvX, uvY).ToVector();
	}
}
