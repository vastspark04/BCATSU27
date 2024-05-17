using System.Collections;
using UnityEngine;

public class CameraFramerate : MonoBehaviour
{
	public float frameRate = 30f;

	private Camera cam;

	private WaitForSeconds frameWait;

	private WaitForEndOfFrame endWait;

	private void Start()
	{
		frameWait = new WaitForSeconds(1f / frameRate);
		endWait = new WaitForEndOfFrame();
	}

	private void OnEnable()
	{
		cam = GetComponent<Camera>();
		cam.enabled = false;
		StartCoroutine(RenderRoutine());
	}

	private IEnumerator RenderRoutine()
	{
		while (base.enabled)
		{
			yield return frameWait;
			yield return endWait;
			cam.Render();
		}
	}
}
