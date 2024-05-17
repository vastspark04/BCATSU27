using System.Collections;
using UnityEngine;

public class FLKMissile : MonoBehaviour, IQSMissileComponent
{
	public Missile missile;

	public Gun gun;

	public MissileFairing[] fairings;

	public float programStartTime;

	public float fireDelay;

	private string nodeName = "FLKMissile";

	private bool programStarted;

	private bool gunStarted;

	private float gunStartTime;

	private float prgStartedTime;

	public void OnLaunched()
	{
		StartCoroutine(FLKRoutine());
	}

	public void OnQuickloadedMissile(ConfigNode qsNode, float elapsedTime)
	{
		gun.SetFire(fire: false);
		if (!qsNode.HasNode(nodeName))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(nodeName);
		if (node.GetValue<bool>("gunStarted"))
		{
			gun.SetFire(fire: true);
			return;
		}
		float prgElapsedTime = 0f;
		if (node.GetValue<bool>("programStarted"))
		{
			prgElapsedTime = node.GetValue<float>("prgElapsedTime");
		}
		StartCoroutine(FLKRoutine(prgElapsedTime));
	}

	public void OnQuicksavedMissile(ConfigNode qsNode, float elapsedTime)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		qsNode.AddNode(configNode);
		configNode.SetValue("gunStarted", gunStarted);
		configNode.SetValue("programStarted", programStarted);
		configNode.SetValue("prgElapsedTime", Time.time - prgStartedTime);
	}

	private IEnumerator FLKRoutine(float prgElapsedTime = 0f)
	{
		gun.actor = missile.actor;
		while (missile.timeToImpact > programStartTime)
		{
			yield return null;
		}
		programStarted = true;
		prgStartedTime = Time.time;
		for (int i = 0; i < fairings.Length; i++)
		{
			if ((bool)fairings[i])
			{
				fairings[i].Jettison();
				fairings[i] = null;
			}
		}
		yield return new WaitForSeconds(Mathf.Max(0f, fireDelay - prgElapsedTime));
		gun.SetFire(fire: true);
		gunStarted = true;
		gunStartTime = Time.time;
		while (gun.currentAmmo > 0)
		{
			yield return null;
		}
		missile.Detonate();
	}
}
