using UnityEngine;
using UnityEngine.UI;

public class HUDTargetRangeCircle : MonoBehaviour
{
	public Image img;

	public float scale;

	public WeaponManager wm;

	private void Update()
	{
		if ((bool)wm.currentEquip)
		{
			img.enabled = true;
			float fillAmount = Mathf.Clamp01((wm.currentEquip.GetAimPoint() - wm.transform.position).magnitude / scale);
			img.fillAmount = fillAmount;
		}
		else
		{
			img.enabled = false;
		}
	}
}
