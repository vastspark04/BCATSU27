using System.Collections;
using UnityEngine;
using UnityEngine.XR;

public class RenderScaleTest : MonoBehaviour
{
	private IEnumerator Start()
	{
		while (base.enabled)
		{
			XRSettings.eyeTextureResolutionScale = 1f;
			yield return new WaitForSeconds(4f);
			XRSettings.eyeTextureResolutionScale = 1.5f;
			yield return new WaitForSeconds(4f);
			XRSettings.eyeTextureResolutionScale = 2f;
			yield return new WaitForSeconds(4f);
			XRSettings.eyeTextureResolutionScale = 0.5f;
			yield return new WaitForSeconds(4f);
			XRSettings.eyeTextureResolutionScale = 0.75f;
			yield return new WaitForSeconds(4f);
		}
	}
}
