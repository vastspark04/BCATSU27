using UnityEngine;

public class SAAWUnit : MonoBehaviour, IOptionalStopToEngage, IEngageEnemies
{
	public SAMLauncher samLauncher;

	public IRSamLauncher irSamLauncher;

	public GunTurretAI gunTurret;

	public GroundUnitMover mover;

	public VisualTargetFinder saawTargetFinder;

	public bool stopToEngage;

	private bool alive = true;

	private Health health;

	private bool engageEnemies = true;

	private bool isSamEngaging
	{
		get
		{
			if ((bool)samLauncher)
			{
				return samLauncher.engagingTarget;
			}
			if ((bool)irSamLauncher)
			{
				return irSamLauncher.isEngaging;
			}
			return false;
		}
	}

	private void Awake()
	{
		health = GetComponent<Health>();
		health.OnDeath.AddListener(Health_OnDeath);
	}

	private void Health_OnDeath()
	{
		alive = false;
		mover.move = false;
		if ((bool)saawTargetFinder)
		{
			saawTargetFinder.enabled = false;
		}
		Radar[] componentsInChildren = GetComponentsInChildren<Radar>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].KillRadar();
		}
		LockingRadar[] componentsInChildren2 = GetComponentsInChildren<LockingRadar>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].enabled = false;
		}
	}

	private void SetSAMEnabled(bool e)
	{
		if ((bool)samLauncher)
		{
			samLauncher.weaponEnabled = e;
		}
		else if ((bool)irSamLauncher)
		{
			irSamLauncher.engageEnemies = e;
		}
	}

	private void Update()
	{
		if (alive)
		{
			if (stopToEngage)
			{
				mover.move = !isSamEngaging && !gunTurret.engagingTarget;
			}
			if (gunTurret.enabled && gunTurret.engagingTarget)
			{
				SetSAMEnabled(e: false);
			}
			else
			{
				SetSAMEnabled(engageEnemies);
			}
			if (isSamEngaging)
			{
				gunTurret.enabled = false;
			}
			else
			{
				gunTurret.enabled = engageEnemies;
			}
		}
	}

	public void SetStopToEngage(bool stopToEngage)
	{
		this.stopToEngage = stopToEngage;
	}

	public void SetEngageEnemies(bool engage)
	{
		engageEnemies = engage;
	}
}
