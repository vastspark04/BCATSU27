using UnityEngine;
using UnityEngine.UI;

public class HUDColorChanger : MonoBehaviour
{
	public Color newColor;

	[ContextMenu("Apply")]
	public void Apply()
	{
		if (newColor.g < newColor.b || newColor.g < newColor.r)
		{
			Debug.Log("New HUD color is not a shade of green -> irreversible! Aborting.");
			return;
		}
		MaskableGraphic[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<MaskableGraphic>();
		foreach (MaskableGraphic maskableGraphic in componentsInChildrenImplementing)
		{
			if (maskableGraphic.color.g > maskableGraphic.color.r && maskableGraphic.color.g > maskableGraphic.color.b)
			{
				Color color = newColor;
				color.a = maskableGraphic.color.a;
				maskableGraphic.color = color;
			}
		}
	}
}
