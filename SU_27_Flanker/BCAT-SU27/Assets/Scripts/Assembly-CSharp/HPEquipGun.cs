using System.Collections;
using UnityEngine;

public class HPEquipGun : HPEquippable, IGDSCompatible
{
	public Gun gun;

	public bool triggerOpenBay = true;

	public float bayDoorThreshold = 0.5f;

	private InternalWeaponBay iwb;

	public float perAmmoCost = 1f;

	public float shakeMagnitude = -1f;

	private bool firing;

	private bool bayOpen;

	private Coroutine iwbRoutine;

	private CamRigRotationInterpolator shaker;

	public override float GetTotalCost()
	{
		return unitCost + perAmmoCost * (float)gun.currentAmmo;
	}

	public override float GetWeaponDamage()
	{
		return gun.bulletInfo.damage;
	}

	public override Vector3 GetAimPoint()
	{
		return CalculateImpact();
	}

	public override void OnStartFire()
	{
		base.OnStartFire();
		firing = true;
		if (iwbRoutine != null)
		{
			StopCoroutine(iwbRoutine);
		}
		if ((bool)iwb && triggerOpenBay)
		{
			iwbRoutine = StartCoroutine(IWBRoutine());
		}
		else
		{
			gun.SetFire(fire: true);
		}
	}

	public override void OnTriggerAxis(float axis)
	{
		base.OnTriggerAxis(axis);
		if (triggerOpenBay)
		{
			if (base.itemActivated && (bool)iwb && axis > 0.05f && !bayOpen)
			{
				bayOpen = true;
				iwb.RegisterOpenReq(this);
			}
			if (axis <= 0.05f && bayOpen)
			{
				iwb.UnregisterOpenReq(this);
				bayOpen = false;
			}
		}
	}

	private IEnumerator IWBRoutine()
	{
		while (iwb.doorState < bayDoorThreshold)
		{
			yield return null;
		}
		gun.SetFire(fire: true);
	}

	public override void OnStopFire()
	{
		base.OnStopFire();
		firing = false;
		gun.SetFire(fire: false);
		if (iwbRoutine != null)
		{
			StopCoroutine(iwbRoutine);
		}
	}

	public Transform GetFireTransform()
	{
		return gun.fireTransforms[0];
	}

	public float GetMuzzleVelocity()
	{
		return gun.bulletInfo.speed;
	}

	public override int GetCount()
	{
		return gun.currentAmmo;
	}

	public override int GetMaxCount()
	{
		return gun.maxAmmo;
	}

	private Vector3 CalculateImpact()
	{
		Vector3 result = Vector3.zero;
		float muzzleVelocity = GetMuzzleVelocity();
		Vector3 vector = base.weaponManager.actor.velocity + GetFireTransform().forward * muzzleVelocity;
		float num = 0f;
		Vector3 vector2 = GetFireTransform().position;
		float num2 = 0.3f;
		bool flag = false;
		while (!flag)
		{
			Vector3 vector3 = vector2 + vector * num2;
			if (Physics.Linecast(vector2, vector3, out var hitInfo, 1))
			{
				return hitInfo.point;
			}
			vector += Physics.gravity * num2;
			vector2 = vector3;
			num += muzzleVelocity * num2;
			result = vector3;
			if (num > 4000f)
			{
				flag = true;
			}
		}
		if (flag && Physics.Raycast(new Ray(vector2, vector), out var hitInfo2, 5000f, 1))
		{
			result = hitInfo2.point;
		}
		return result;
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		gun.actor = base.weaponManager.actor;
		if (gun.actor == FlightSceneManager.instance.playerActor && shakeMagnitude > 0f)
		{
			shaker = base.weaponManager.GetComponentInChildren<CamRigRotationInterpolator>();
			if ((bool)shaker)
			{
				gun.OnFired += Shake;
			}
		}
		if (triggerOpenBay)
		{
			iwb = base.weaponManager.GetIWBForEquip(hardpointIdx);
			if (!iwb || !base.weaponManager.isPlayer)
			{
				iwb = null;
				triggerOpenBay = false;
			}
		}
	}

	private void Shake()
	{
		CamRigRotationInterpolator.ShakeAll(Random.onUnitSphere * shakeMagnitude);
		FlybyCameraMFDPage.ShakeSpectatorCamera(30f * shakeMagnitude / (FlybyCameraMFDPage.instance.flybyCam.transform.position - GetFireTransform().position).sqrMagnitude);
	}

	public override void OnQuicksaveEquip(ConfigNode eqNode)
	{
		base.OnQuicksaveEquip(eqNode);
		eqNode.SetValue("ammoCount", gun.currentAmmo);
	}

	public override void OnQuickloadEquip(ConfigNode eqNode)
	{
		base.OnQuickloadEquip(eqNode);
		int currentAmmo = ConfigNodeUtils.ParseInt(eqNode.GetValue("ammoCount"));
		gun.currentAmmo = currentAmmo;
	}
}
