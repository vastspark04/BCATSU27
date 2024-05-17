using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterMissile : Missile
{
	[Header("Cluster Missile")]
	public MissileLauncher subMl;

	public float deployDistance;

	private bool launchedSubMl;

	public GameObject[] fairings;

	public float subMissileInterval = 0.2f;

	public float clusterTargetFov = 15f;

	private bool isLaunchingSubML;

	private float altOverTarget;

	private bool mpRemote;

	public event Action OnSubLaunch;

	public event Action<Actor> OnFiredSubmissile;

	public void MP_SetRemote()
	{
		mpRemote = true;
	}

	private void Update()
	{
		if (base.fired && !mpRemote && !launchedSubMl && lastTargetDistance < deployDistance)
		{
			launchedSubMl = true;
			this.OnSubLaunch?.Invoke();
			StartCoroutine(SubLaunchRoutine());
		}
	}

	public void RemoteSubLaunch()
	{
		JettisonFairings();
	}

	public void RemoteFireSubmissile(Actor tgt)
	{
		Missile nextMissile = subMl.GetNextMissile();
		if ((bool)nextMissile)
		{
			nextMissile.explodeDamage = 0f;
			subMl.parentActor = base.actor;
			nextMissile.SetOpticalTarget(tgt.transform, tgt);
			subMl.FireMissile();
		}
	}

	private void JettisonFairings()
	{
		GameObject[] array = fairings;
		foreach (GameObject obj in array)
		{
			obj.transform.parent = null;
			Rigidbody rigidbody = obj.AddComponent<Rigidbody>();
			rigidbody.velocity = base.rb.velocity + 5f * rigidbody.transform.forward;
			rigidbody.angularVelocity = 3f * UnityEngine.Random.onUnitSphere;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			SimpleDrag obj2 = obj.AddComponent<SimpleDrag>();
			obj2.rb = rigidbody;
			obj2.area = 0.0001f;
			obj.AddComponent<FloatingOriginTransform>().SetRigidbody(rigidbody);
			UnityEngine.Object.Destroy(obj, 5f);
		}
	}

	private IEnumerator SubLaunchRoutine()
	{
		JettisonFairings();
		yield return new WaitForSeconds(0.5f);
		altOverTarget = base.transform.position.y - base.estTargetPos.y;
		isLaunchingSubML = true;
		int roleMask = 6;
		int i = 0;
		Vector3 vector = base.transform.position + 10f * base.transform.forward;
		List<Actor> tgts = new List<Actor>();
		TargetManager.instance.GetAllOpticalTargetsInView(base.actor, clusterTargetFov, 10f, 2f * deployDistance, roleMask, vector, base.estTargetPos - vector, tgts, allActors: false, occlusionCheck: false);
		int tgtCount = tgts.Count;
		tgts.Sort((Actor a, Actor b) => (a.position - base.estTargetPos).sqrMagnitude.CompareTo((b.position - base.estTargetPos).sqrMagnitude));
		Debug.Log("Cluster missile firing on " + tgtCount + " targets");
		if (tgtCount > 0)
		{
			while (subMl.missileCount > 0)
			{
				Actor tgt = tgts[i];
				FireSubMissile(tgt);
				i = (i + 1) % tgtCount;
				yield return new WaitForSeconds(subMissileInterval);
			}
		}
		yield return new WaitForSeconds(0.5f);
		proxyDetonateRange = 1000f;
	}

	private void FireSubMissile(Actor tgt)
	{
		subMl.parentActor = base.actor;
		subMl.GetNextMissile().SetOpticalTarget(tgt.transform, tgt);
		subMl.FireMissile();
		this.OnFiredSubmissile?.Invoke(tgt);
	}

	protected override Vector3 GuidedPoint()
	{
		if (isLaunchingSubML)
		{
			Vector3 result = base.estTargetPos;
			result.y += altOverTarget;
			altOverTarget += 20f * Time.deltaTime;
			return result;
		}
		return base.GuidedPoint();
	}
}
