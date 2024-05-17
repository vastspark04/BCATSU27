using UnityEngine;

public class IRMissileLauncher : MissileLauncher
{
	[Tooltip("Used for head-tracking mode.  Automatically assigned to VRHead by HPEquipIRML if configured to useVrHead")]
	public Transform headTransform;

	private bool irmlEnabled;

	public Transform vssReferenceTransform { get; set; }

	public Missile activeMissile => base.missiles[base.missileIdx];

	public int GetAmmoCount()
	{
		return base.missileCount;
	}

	public Vector3 GetAimPoint()
	{
		if (base.missileCount > 0)
		{
			Transform transform = base.missiles[base.missileIdx].heatSeeker.transform;
			return transform.position + transform.forward * 4000f;
		}
		return base.transform.position + 4000f * hardpoints[0].forward;
	}

	public void EnableWeapon()
	{
		irmlEnabled = true;
		if (base.missileCount > 0 && base.missiles != null && (bool)base.missiles[base.missileIdx])
		{
			base.missiles[base.missileIdx].heatSeeker.enabled = true;
			base.missiles[base.missileIdx].heatSeeker.seekerEnabled = true;
			base.missiles[base.missileIdx].heatSeeker.EnableAudio();
		}
	}

	public void DisableWeapon()
	{
		irmlEnabled = false;
		if (base.missileCount > 0 && base.missiles != null && (bool)base.missiles[base.missileIdx])
		{
			base.missiles[base.missileIdx].heatSeeker.seekerEnabled = false;
			base.missiles[base.missileIdx].heatSeeker.DisableAudio();
			base.missiles[base.missileIdx].heatSeeker.enabled = false;
		}
	}

	protected override void OnLoadedMissile(int idx, Missile m)
	{
		base.OnLoadedMissile(idx, m);
		m.heatSeeker.seekerEnabled = irmlEnabled;
		m.heatSeeker.enabled = irmlEnabled;
		if (irmlEnabled)
		{
			m.heatSeeker.EnableAudio();
		}
		else
		{
			m.heatSeeker.DisableAudio();
		}
	}

	public bool TryFireMissile()
	{
		if (base.missileCount > 0 && base.missiles[base.missileIdx].hasTarget)
		{
			base.missiles[base.missileIdx].heatSeeker.DisableAudio();
			base.missiles[base.missileIdx].heatSeeker.SetHardLock();
			FireMissile();
			EnableWeapon();
			return true;
		}
		return false;
	}

	protected override void RemoteFire()
	{
		Missile nextMissile = GetNextMissile();
		if ((bool)nextMissile && nextMissile.hasTarget)
		{
			Actor likelyTargetActor = nextMissile.heatSeeker.likelyTargetActor;
			InvokeRemoteFiredEvent(likelyTargetActor);
		}
	}

	public override void RemoteFireOn(Actor actor)
	{
		Missile nextMissile = GetNextMissile();
		if ((bool)nextMissile)
		{
			nextMissile.heatSeeker.DisableAudio();
			if ((bool)actor)
			{
				nextMissile.heatSeeker.RemoteSetHardLock(actor.position);
			}
			else
			{
				Debug.LogError("IR Missile was copilot remote launched without a target actor.");
			}
			FireMissile();
		}
	}
}
