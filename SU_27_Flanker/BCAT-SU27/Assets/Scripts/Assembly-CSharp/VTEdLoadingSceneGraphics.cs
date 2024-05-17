using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VTEdLoadingSceneGraphics : MonoBehaviour
{
	public Image fillImage;

	public RawImage mapImage;

	private void Start()
	{
		StartCoroutine(LoadRoutine());
	}

	private IEnumerator LoadRoutine()
	{
		while (!VTMapGenerator.fetch || !VTMapGenerator.fetch.heightMap)
		{
			yield return null;
		}
		if ((bool)mapImage)
		{
			mapImage.texture = VTMapGenerator.fetch.heightMap;
			mapImage.gameObject.SetActive(value: true);
			mapImage.enabled = true;
		}
		float fillT = 0f;
		while (base.enabled)
		{
			float loadPercent = VTMapGenerator.fetch.loadPercent;
			if ((bool)mapImage)
			{
				mapImage.color = new Color(1f, 1f, 1f, loadPercent);
			}
			fillT += 1f / 3f * Time.deltaTime;
			fillImage.fillAmount = Mathf.Max(fillT, loadPercent * 1.5f);
			yield return null;
		}
	}
}
