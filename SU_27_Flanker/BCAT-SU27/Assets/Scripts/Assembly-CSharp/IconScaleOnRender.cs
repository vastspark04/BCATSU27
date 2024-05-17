using UnityEngine;

public class IconScaleOnRender : MonoBehaviour
{
	public float iconScale = 1f;

	private float origScale;

	private void OnPreCull()
	{
		origScale = IconScaleTest.IconScaleMultiplier;
		IconScaleTest.IconScaleMultiplier = iconScale;
	}

	private void OnPostRender()
	{
		IconScaleTest.IconScaleMultiplier = origScale;
	}
}
