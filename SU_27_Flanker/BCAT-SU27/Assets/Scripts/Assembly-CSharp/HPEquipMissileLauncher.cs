using UnityEngine;

public class HPEquipMissileLauncher : HPEquippable
{
	public string missileResourcePath;

	public float perMissileCost;

	public MissileLauncher ml;

	public float shakeMagnitude = -1f;

	private float perMissileRCS = 3f;

	private bool wasEverLoaded;

	public override float GetTotalCost()
	{
		return unitCost + (float)ml.missileCount * perMissileCost;
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		ml.parentActor = base.weaponManager.actor;
		if ((bool)ml.missilePrefab)
		{
			RadarCrossSection component = ml.missilePrefab.GetComponent<RadarCrossSection>();
			if ((bool)component)
			{
				perMissileRCS = component.GetAverageCrossSection();
			}
		}
		ml.OnFiredMissileIdx -= ShakeOnLaunch;
		if (base.weaponManager.isPlayer && shakeMagnitude > 0f)
		{
			ml.OnFiredMissileIdx += ShakeOnLaunch;
		}
		if (ml.missiles == null)
		{
			return;
		}
		Missile[] missiles = ml.missiles;
		foreach (Missile missile in missiles)
		{
			if ((bool)missile)
			{
				missile.gameObject.name = ml.missilePrefab.name + " (" + base.weaponManager.actor.actorName + ")";
			}
		}
	}

	private void ShakeOnLaunch(int idx)
	{
		CamRigRotationInterpolator.ShakeAll(Random.onUnitSphere * shakeMagnitude);
	}

	protected override float _GetRadarCrossSection()
	{
		return baseRadarCrossSection + (float)ml.missileCount * perMissileRCS;
	}

	public override float GetWeaponDamage()
	{
		if (ml.missileCount > 0)
		{
			return ml.GetNextMissile().explodeDamage;
		}
		return 0f;
	}

	public override int GetCount()
	{
		if (!wasEverLoaded)
		{
			if (ml.missileCount <= 0)
			{
				return GetMaxCount();
			}
			wasEverLoaded = true;
		}
		return ml.missileCount;
	}

	public override int GetMaxCount()
	{
		return ml.hardpoints.Length;
	}

	public override float GetEstimatedMass()
	{
		return ml.baseMass + (float)ml.hardpoints.Length * ml.missilePrefab.GetComponent<Missile>().mass;
	}

	public override void OnQuicksaveEquip(ConfigNode eqNode)
	{
		base.OnQuicksaveEquip(eqNode);
		eqNode.SetValue("missileCount", ml.missileCount);
	}

	public override void OnQuickloadEquip(ConfigNode eqNode)
	{
		base.OnQuickloadEquip(eqNode);
		int count = ConfigNodeUtils.ParseInt(eqNode.GetValue("missileCount"));
		ml.LoadCountReverse(count);
	}
}
