using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94TEDACFunctions : MonoBehaviour
{
	public TargetingMFDPage tgpPage;

	public MFDRadarUI radarUI;

	public DashMapDisplay mapPage;

	private Vector3 ts;

	private bool awaitingRelease;

	public void OnThumbstickUp()
	{
		awaitingRelease = false;
	}

	public void SetRightThumbstick(Vector3 ts)
	{
		this.ts = ts;
		if (tgpPage.isSOI)
		{
			TGPRightInput(ts);
		}
		else if (radarUI.isSOI)
		{
			RadarRightInput(ts);
		}
		else if ((bool)mapPage.mfdPage && mapPage.mfdPage.isSOI)
		{
			MapRightInput(ts);
		}
	}

	private void MapRightInput(Vector3 ts)
	{
		float num = Mathf.Abs(ts.y);
		if (num > 0.8f && !awaitingRelease)
		{
			if (ts.y > 0f)
			{
				mapPage.ZoomIn();
			}
			else
			{
				mapPage.ZoomOut();
			}
			awaitingRelease = true;
		}
		else if (awaitingRelease && num < 0.1f)
		{
			awaitingRelease = false;
		}
	}

	private void TGPRightInput(Vector3 ts)
	{
		float num = Mathf.Abs(ts.y);
		if (num > 0.8f && !awaitingRelease)
		{
			if (ts.y > 0f)
			{
				tgpPage.ZoomIn();
			}
			else
			{
				tgpPage.ZoomOut();
			}
			awaitingRelease = true;
		}
		else if (awaitingRelease && num < 0.1f)
		{
			awaitingRelease = false;
		}
	}

	private void RadarRightInput(Vector3 ts)
	{
		radarUI.radarCtrlr.OnElevationInput(ts.y);
		float num = Mathf.Abs(ts.x);
		if (num > 0.8f && !awaitingRelease)
		{
			if (ts.x > 0f)
			{
				radarUI.RangeUp();
			}
			else
			{
				radarUI.RangeDown();
			}
			awaitingRelease = true;
		}
		else if (awaitingRelease && num < 0.1f)
		{
			awaitingRelease = false;
		}
	}
}

}