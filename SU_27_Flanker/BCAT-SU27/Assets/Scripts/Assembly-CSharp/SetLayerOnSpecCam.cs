using UnityEngine;

public class SetLayerOnSpecCam : MonoBehaviour
{
	public int layer;

	public bool smoothLookOnly;

	private int origLayer;

	private void Awake()
	{
		origLayer = base.gameObject.layer;
	}

	private void Start()
	{
		FlybyCameraMFDPage.OnBeginSpectatorCam += FlybyCameraMFDPage_OnBeginSpectatorCam;
		FlybyCameraMFDPage.OnEndSpectatorCam += FlybyCameraMFDPage_OnEndSpectatorCam;
	}

	private void FlybyCameraMFDPage_OnBeginSpectatorCam()
	{
		if (smoothLookOnly && FlybyCameraMFDPage.instance.finalBehavior != FlybyCameraMFDPage.SpectatorBehaviors.SmoothLook)
		{
			base.gameObject.layer = origLayer;
		}
		else
		{
			base.gameObject.layer = layer;
		}
	}

	private void OnDestroy()
	{
		FlybyCameraMFDPage.OnBeginSpectatorCam -= FlybyCameraMFDPage_OnBeginSpectatorCam;
		FlybyCameraMFDPage.OnEndSpectatorCam -= FlybyCameraMFDPage_OnEndSpectatorCam;
	}

	private void FlybyCameraMFDPage_OnEndSpectatorCam()
	{
		base.gameObject.layer = origLayer;
	}
}
