using UnityEngine;

public class ExteriorLightsController : MonoBehaviour
{
	public StrobeLightController strobeLights;

	public ObjectPowerUnit[] landingLights;

	public NavLightController navLightController;

	public ObjectPowerUnit[] navLights;

	public void SetNavLights(int state)
	{
		if ((bool)navLightController)
		{
			navLightController.SetPower(state);
		}
		for (int i = 0; i < navLights.Length; i++)
		{
			navLights[i].SetConnection(state);
		}
	}

	public void SetNavLights3Way(int st)
	{
		switch (st)
		{
		case 0:
			SetNavLights(0);
			break;
		case 2:
			SetNavLights(1);
			break;
		}
	}

	public void SetLandingLights(int state)
	{
		for (int i = 0; i < landingLights.Length; i++)
		{
			landingLights[i].SetConnection(state);
		}
	}

	public void SetLandingLights3Way(int st)
	{
		switch (st)
		{
		case 0:
			SetLandingLights(0);
			break;
		case 2:
			SetLandingLights(1);
			break;
		}
	}

	public void SetStrobeLights(int state)
	{
		strobeLights.SetStrobePower(state);
	}

	public void SetStrobeLights3Way(int st)
	{
		switch (st)
		{
		case 0:
			SetStrobeLights(0);
			break;
		case 2:
			SetStrobeLights(1);
			break;
		}
	}

	public void SetAllLights(int state)
	{
		SetNavLights(state);
		SetStrobeLights(state);
		SetLandingLights(state);
	}
}
