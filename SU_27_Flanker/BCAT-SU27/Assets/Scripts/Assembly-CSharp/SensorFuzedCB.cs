using System;
using System.Collections;
using UnityEngine;

public class SensorFuzedCB : MonoBehaviour
{
	[Serializable]
	public struct SubmunitionGroup
	{
		public float delay;

		public SFSubmunition[] submunitions;
	}

	public float deployAltitude;

	private Missile missile;

	public MissileFairing[] fairings;

	public SubmunitionGroup[] subGroups;

	public Actor.ActorRolesSelection targetsToFind;

	public ParticleSystem fireParticleSystem;

	public int firePSBurstCount = 30;

	[Header("Submunition Overrides")]
	public bool overrideSubmunitions = true;

	public float subMass;

	public MinMax ejectSpeed;

	public MinMax ejectTorque;

	public float subBoostAlt = 60f;

	public float boosterThrust;

	public float boosterBurnTime;

	public float extraSubSpinTorque = 5f;

	public Vector3 localTorqueDir;

	public float fov;

	public int skeetCount = 4;

	public MinMax skeetEjectSpeed;

	public float skeetScanInterval = 0.1f;

	public float skeetMinAlt;

	public Bullet.BulletInfo bulletInfo;

	public float chuteDragArea;

	public float chuteDeployRate;

	private bool mpRemote;

	public event Action OnBeginDeploy;

	public void MP_SetRemote()
	{
		mpRemote = true;
	}

	public void StartProgram()
	{
		if (!mpRemote)
		{
			missile = GetComponent<Missile>();
			StartCoroutine(DelayRoutine());
		}
	}

	private IEnumerator DelayRoutine()
	{
		yield return null;
		while (base.enabled)
		{
			if (Physics.Raycast(new Ray(base.transform.position, Vector3.down), deployAltitude, 1))
			{
				StartCoroutine(DeployRoutine());
				break;
			}
			yield return null;
		}
	}

	public void RemoteBeginDeploy()
	{
		StartCoroutine(DeployRoutine());
	}

	private IEnumerator DeployRoutine()
	{
		this.OnBeginDeploy?.Invoke();
		for (int j = 0; j < fairings.Length; j++)
		{
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(fairings[j].Jettison);
		}
		fireParticleSystem.transform.parent = null;
		for (int i = 0; i < subGroups.Length; i++)
		{
			yield return new WaitForSeconds(subGroups[i].delay);
			for (int k = 0; k < subGroups[i].submunitions.Length; k++)
			{
				SFSubmunition sFSubmunition = subGroups[i].submunitions[k];
				if (mpRemote)
				{
					sFSubmunition.bulletInfo.damage = 0f;
				}
				sFSubmunition.fireParticleSystem = fireParticleSystem;
				sFSubmunition.firePSBurstCount = firePSBurstCount;
				if (overrideSubmunitions)
				{
					SetOverrideSubSpecs(sFSubmunition);
				}
				FloatingOrigin.instance.AddQueuedFixedUpdateAction(sFSubmunition.FireSubmunition);
				if (!mpRemote)
				{
					missile.rb.mass -= sFSubmunition.mass;
				}
			}
		}
		UnityEngine.Object.Destroy(fireParticleSystem.gameObject, 120f);
	}

	private void SetOverrideSubSpecs(SFSubmunition sub)
	{
		sub.mass = subMass;
		sub.ejectSpeed = ejectSpeed;
		sub.ejectTorque = ejectTorque;
		sub.localTorqueDir = localTorqueDir;
		sub.fov = fov;
		sub.skeetCount = skeetCount;
		sub.boostAlt = subBoostAlt;
		sub.skeetEjectSpeed = skeetEjectSpeed;
		sub.bulletInfo = bulletInfo;
		sub.chuteDrag.area = chuteDragArea;
		sub.chuteDeployRate = chuteDeployRate;
		sub.skeetMinAlt = skeetMinAlt;
		sub.spinTorque = extraSubSpinTorque;
		sub.skeetScanInterval = skeetScanInterval;
		for (int i = 0; i < sub.boosters.Length; i++)
		{
			sub.boosters[i].thrust = boosterThrust;
			sub.boosters[i].burnTime = boosterBurnTime;
		}
	}
}
