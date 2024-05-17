using UnityEngine;
using VTOLVR.Multiplayer;

public class AngularVelocityAutoBail : MonoBehaviour, IPersistentVehicleData
{
	public Rigidbody rb;

	public VehicleMaster vm;

	public float speedThreshold;

	public float timeThreshold;

	private float currentAngVelMag;

	public bool autoBailEnabled;

	public float addGRate = 2f;

	public ABObjectToggler indicator;

	public VehiclePart[] requireDeadParts;

	public MultiUserVehicleSync muvs;

	private float timeAccum;

	private void Start()
	{
		UpdateIndicator();
	}

	public void Toggle()
	{
		autoBailEnabled = !autoBailEnabled;
		UpdateIndicator();
	}

	private void FixedUpdate()
	{
		if (!autoBailEnabled || (VTOLMPUtils.IsMultiplayer() && !muvs.IsControlOwner()))
		{
			return;
		}
		currentAngVelMag = rb.angularVelocity.magnitude;
		if (currentAngVelMag > speedThreshold)
		{
			if (RequiredPartDied())
			{
				timeAccum += Time.fixedDeltaTime;
				if ((bool)BlackoutEffect.instance && !BlackoutEffect.instance.accelDied)
				{
					BlackoutEffect.instance.AddG(addGRate * Time.fixedDeltaTime);
				}
				if (timeAccum > timeThreshold)
				{
					vm.KillPilot();
				}
			}
		}
		else
		{
			timeAccum = 0f;
		}
	}

	public bool RequiredPartDied()
	{
		bool flag = false;
		for (int i = 0; i < requireDeadParts.Length; i++)
		{
			if (flag)
			{
				break;
			}
			if ((bool)requireDeadParts[i] && requireDeadParts[i].partDied)
			{
				flag = true;
			}
		}
		return flag;
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		vDataNode.SetValue("autoBail", autoBailEnabled);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		ConfigNodeUtils.TryParseValue(vDataNode, "autoBail", ref autoBailEnabled);
		UpdateIndicator();
	}

	private void UpdateIndicator()
	{
		if ((bool)indicator)
		{
			if (autoBailEnabled)
			{
				indicator.SetToB();
			}
			else
			{
				indicator.SetToA();
			}
		}
	}
}
