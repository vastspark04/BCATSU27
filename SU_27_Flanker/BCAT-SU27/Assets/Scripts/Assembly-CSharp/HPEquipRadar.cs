using UnityEngine;

public class HPEquipRadar : HPEquippable, IMassObject
{
	public Radar radar;

	public LockingRadar lr;

	public Transform elevationTransform;

	public GroundRadarDispatcher groundRadarComputeDispatcher;

	public float mass;

	private MFDRadarUI radarUI;

	public float GetMass()
	{
		return mass;
	}

	protected override void OnEquip()
	{
		radarUI = base.weaponManager.GetComponentInChildren<MFDRadarUI>(includeInactive: true);
		if ((bool)radar)
		{
			radar.myActor = base.weaponManager.actor;
			radar.radarEnabled = false;
		}
		if ((bool)lr)
		{
			base.weaponManager.SetLockingRadar(lr);
			lr.myActor = base.weaponManager.actor;
		}
		if ((bool)radarUI)
		{
			radarUI.radarCtrlr.elevationTransform = elevationTransform;
			radarUI.SetPlayerRadar(radar, lr);
			if ((bool)radarUI.groundRadarImage && (bool)groundRadarComputeDispatcher)
			{
				radarUI.groundRadarImage.texture = groundRadarComputeDispatcher.outputRT;
				radarUI.grDispatcher = groundRadarComputeDispatcher;
			}
		}
	}

	public override void OnUnequip()
	{
		if ((bool)radarUI)
		{
			radarUI.SetPlayerRadar(null, null);
		}
	}
}
