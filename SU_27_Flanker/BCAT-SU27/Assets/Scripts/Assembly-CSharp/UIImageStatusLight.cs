using UnityEngine;
using UnityEngine.UI;

public class UIImageStatusLight : EmissiveTextureLight
{
	public Graphic[] images;

	public bool alphaOnly;

	protected override void SetRendererColor(Color c)
	{
		if (alphaOnly)
		{
			for (int i = 0; i < images.Length; i++)
			{
				if ((bool)images[i])
				{
					Color color = images[i].color;
					color.a = c.a;
					images[i].color = color;
				}
			}
			return;
		}
		for (int j = 0; j < images.Length; j++)
		{
			if ((bool)images[j])
			{
				images[j].color = c;
			}
		}
	}
}
