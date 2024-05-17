using System;
using System.Collections.Generic;
using UnityEngine;

public class FlareCountermeasure : Countermeasure, IQSVehicleComponent
{
	public Transform[] ejectTransforms;

	public GameObject flarePrefab;

	public float flareLife = 7f;

	private ObjectPool flarePool;

	public Rigidbody rb;

	public float ejectSpeed = 45f;

	private Queue<CMFlare> firedFlares = new Queue<CMFlare>();

	public AudioSource audioSource;

	public AudioClip fireFlareClip;

	private int altIdx;

	private string qsNodeName => base.gameObject.name + "_FlareCountermeasure";

	protected override void Awake()
	{
		base.Awake();
		if (!rb)
		{
			rb = GetComponentInParent<Rigidbody>();
		}
		flarePool = ObjectPool.CreateObjectPool(flarePrefab, 10, canGrow: true, destroyOnLoad: true);
	}

	private void OnDestroy()
	{
		if ((bool)flarePool && Application.isPlaying)
		{
			flarePool.DestroyPool();
		}
	}

	protected override void OnFireCM()
	{
		base.OnFireCM();
		CountermeasureManager.ReleaseModes releaseModes = CountermeasureManager.ReleaseModes.Single_L;
		if ((bool)manager)
		{
			releaseModes = manager.releaseMode;
		}
		switch (releaseModes)
		{
		case CountermeasureManager.ReleaseModes.Single_Auto:
			if ((bool)manager.launchDetector && manager.launchDetector.launchWasDetected)
			{
				int idx = ((Vector3.Dot(rb.transform.right, manager.launchDetector.lastDetectedMissileLaunchPoint.point - rb.transform.position) > 0f) ? 1 : 0);
				TryFire(ref idx);
			}
			else
			{
				TryFire(ref altIdx);
			}
			break;
		case CountermeasureManager.ReleaseModes.Single_L:
		{
			int idx3 = 0;
			TryFire(ref idx3);
			break;
		}
		case CountermeasureManager.ReleaseModes.Single_R:
		{
			int idx2 = 1;
			TryFire(ref idx2);
			break;
		}
		case CountermeasureManager.ReleaseModes.Double:
		{
			for (int i = 0; i < ejectTransforms.Length; i++)
			{
				if (ConsumeCM(i))
				{
					Vector3 position = ejectTransforms[i].position;
					Vector3 velocity = rb.velocity + UnityEngine.Random.Range(ejectSpeed * 0.9f, ejectSpeed * 1.1f) * VectorUtils.WeightedDirectionDeviation(ejectTransforms[i].forward, 3f);
					FireFlare(position, velocity, flareLife);
				}
			}
			break;
		}
		}
	}

	private void TryFire(ref int idx)
	{
		bool flag = false;
		if (ConsumeCM(idx))
		{
			flag = true;
		}
		else
		{
			idx = (idx + 1) % 2;
			if (ConsumeCM(idx))
			{
				flag = true;
			}
		}
		if (flag)
		{
			Vector3 position = ejectTransforms[idx].position;
			Vector3 velocity = rb.velocity + UnityEngine.Random.Range(ejectSpeed * 0.9f, ejectSpeed * 1.1f) * VectorUtils.WeightedDirectionDeviation(ejectTransforms[idx].forward, 3f);
			FireFlare(position, velocity, flareLife);
			idx = (idx + 1) % 2;
		}
	}

	private void FireFlare(Vector3 position, Vector3 velocity, float lifeTime)
	{
		GameObject pooledObject = flarePool.GetPooledObject();
		pooledObject.transform.position = position;
		CMFlare component = pooledObject.GetComponent<CMFlare>();
		component.flareLife = lifeTime;
		component.velocity = velocity;
		component.transform.rotation = Quaternion.LookRotation(UnityEngine.Random.onUnitSphere);
		pooledObject.SetActive(value: true);
		firedFlares.Enqueue(component);
		component.OnDecayed += OnFlareDecayed;
		if ((bool)audioSource && (bool)fireFlareClip)
		{
			audioSource.PlayOneShot(fireFlareClip);
		}
	}

	private void OnFlareDecayed(CMFlare f)
	{
		if (firedFlares.Count > 0)
		{
			firedFlares.Dequeue();
		}
		f.OnDecayed -= OnFlareDecayed;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(qsNodeName);
		qsNode.AddNode(configNode);
		foreach (CMFlare firedFlare in firedFlares)
		{
			ConfigNode configNode2 = new ConfigNode("FLARE");
			configNode2.SetValue("elapsedTime", Time.time - firedFlare.timeFired);
			configNode2.SetValue("globalPos", VTMapManager.WorldToGlobalPoint(firedFlare.transform.position));
			configNode2.SetValue("velocity", firedFlare.velocity);
			configNode.AddNode(configNode2);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		try
		{
			if (!qsNode.HasNode(qsNodeName))
			{
				return;
			}
			foreach (ConfigNode node in qsNode.GetNode(qsNodeName).GetNodes("FLARE"))
			{
				float value = node.GetValue<float>("elapsedTime");
				Vector3D value2 = node.GetValue<Vector3D>("globalPos");
				Vector3 value3 = node.GetValue<Vector3>("velocity");
				float lifeTime = flareLife - value;
				Vector3 position = VTMapManager.GlobalToWorldPoint(value2);
				FireFlare(position, value3, lifeTime);
			}
		}
		catch (NullReferenceException ex)
		{
			Debug.LogError("Failed to quickload flare countermeasure. \nflarePool is " + (flarePool ? "exist" : "null") + "\n" + ex);
		}
	}
}
