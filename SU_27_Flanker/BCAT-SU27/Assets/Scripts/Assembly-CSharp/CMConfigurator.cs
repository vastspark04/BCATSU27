using UnityEngine;
using UnityEngine.UI;

public class CMConfigurator : MonoBehaviour
{
	[HideInInspector]
	public int cmIdx;

	public Transform barTf;

	public Text countText;

	public LoadoutConfigurator lc;

	public void SetNormValue(float n)
	{
		if ((bool)lc && lc.cms != null && lc.cms.Count > cmIdx)
		{
			n = Mathf.Clamp01(n);
			barTf.localScale = new Vector3(1f, n, 1f);
			countText.text = Mathf.Round(n * (float)lc.cms[cmIdx].maxCount).ToString();
		}
	}
}
