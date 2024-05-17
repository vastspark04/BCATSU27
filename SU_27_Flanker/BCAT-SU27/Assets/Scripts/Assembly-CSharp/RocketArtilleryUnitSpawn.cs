using UnityEngine;

public class RocketArtilleryUnitSpawn : ArtilleryUnitSpawn
{
	[UnitSpawnAttributeRange("Default Shots Per Salvo", 1f, 12f, UnitSpawnAttributeRange.RangeTypes.Int)]
	public float defaultShotsPerSalvo = 1f;

	[UnitSpawnAttributeRange("Ripple Rate", 20f, 120f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float rippleRate = 60f;

	[UnitSpawn("Allow Reload")]
	public bool allowReload = true;

	[UnitSpawnAttributeRange("Reload Time (s)", 0f, 9999f, UnitSpawnAttributeRange.RangeTypes.Float)]
	public float reloadTime;

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		if (artilleryUnit is RocketLauncherAI)
		{
			RocketLauncherAI obj = (RocketLauncherAI)artilleryUnit;
			obj.rippleRate = rippleRate;
			obj.shotsPerSalvo = Mathf.RoundToInt(defaultShotsPerSalvo);
			obj.allowReload = allowReload;
			obj.reloadTime = reloadTime;
		}
		else
		{
			Debug.LogError("RocketArtilleryUnitSpawn artilleryUnit is not a RocketLauncherAI");
		}
	}

	[VTEvent("Set Shots Per Salvo", "Set the number of rockets the unit will fire on each salvo when automatically engaging visible targets.", new string[] { "Shots" })]
	public void SetShotsPerSalvo([VTRangeParam(1f, 12f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float shots)
	{
		((RocketLauncherAI)artilleryUnit).shotsPerSalvo = Mathf.RoundToInt(shots);
	}

	[VTEvent("Set Ripple Rate", "Set the rate of fire when launching salvos at targets.", new string[] { "RPM" })]
	public void SetRippleRate([VTRangeParam(20f, 120f)] float rpm)
	{
		((RocketLauncherAI)artilleryUnit).rippleRate = rpm;
	}

	[VTEvent("Set Allow Reload", "Set whether this unit is allowed to reload rockets after emptying.", new string[] { "Allow" })]
	public void SetAllowReload(bool allow)
	{
		RocketLauncherAI rocketLauncherAI = (RocketLauncherAI)artilleryUnit;
		rocketLauncherAI.allowReload = allow;
		if (rocketLauncherAI.rocketLauncher.GetCount() == 0)
		{
			rocketLauncherAI.SetEngageEnemies(engageEnemies);
		}
	}

	[VTEvent("Set Reload Time", "Change the time it takes for this unit to reload, if allowed.", new string[] { "Time (s)" })]
	public void SetReloadTime(float time)
	{
		((RocketLauncherAI)artilleryUnit).reloadTime = time;
	}
}
