using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OilRigDestruction : MonoBehaviour, IQSVehicleComponent
{
	public DestructionDebris[] legs;

	public GameObject legDestructionRotationObject;

	public float legDestructionRotationRadius = 45f;

	public float legDestructionRotationAngle = 15f;

	public float legDestructionRotationSpeed = 3f;

	public float ldrHeight = -14f;

	private bool flDestroyed;

	private bool frDestroyed;

	private bool rlDestroyed;

	private bool rrDestroyed;

	public UnityEvent OnTwoLegsDestroyed;

	public Health[] requiredComponentsToKill;

	public Health masterHealth;

	private int rKilled;

	private bool twoLegsDown;

	private string nodeName => "OilRigDestruction_" + base.gameObject.name;

	private void Start()
	{
		DestructionDebris[] array = legs;
		foreach (DestructionDebris destructionDebris in array)
		{
			Vector3 vector = base.transform.InverseTransformPoint(destructionDebris.transform.position);
			if (vector.x > 0f)
			{
				if (vector.z > 0f)
				{
					destructionDebris.health.OnDeath.AddListener(FRLegDied);
				}
				else
				{
					destructionDebris.health.OnDeath.AddListener(RRLegDied);
				}
			}
			else if (vector.z > 0f)
			{
				destructionDebris.health.OnDeath.AddListener(FLLegDied);
			}
			else
			{
				destructionDebris.health.OnDeath.AddListener(RLLegDied);
			}
		}
		Health[] array2 = requiredComponentsToKill;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnDeath.AddListener(RequiredComponentKilled);
		}
		masterHealth.OnDeath.AddListener(OnMasterHealthDeath);
	}

	private void RequiredComponentKilled()
	{
		rKilled++;
		if (rKilled >= requiredComponentsToKill.Length)
		{
			masterHealth.Kill();
		}
	}

	private void FLLegDied()
	{
		flDestroyed = true;
		CheckLegs();
	}

	private void FRLegDied()
	{
		frDestroyed = true;
		CheckLegs();
	}

	private void RLLegDied()
	{
		rlDestroyed = true;
		CheckLegs();
	}

	private void RRLegDied()
	{
		rrDestroyed = true;
		CheckLegs();
	}

	private void OnMasterHealthDeath()
	{
		if (!twoLegsDown)
		{
			List<Health> list = new List<Health>();
			DestructionDebris[] array = legs;
			foreach (DestructionDebris destructionDebris in array)
			{
				if (destructionDebris.health.normalizedHealth > 0f)
				{
					list.Add(destructionDebris.health);
				}
			}
			while (!twoLegsDown)
			{
				int index = UnityEngine.Random.Range(0, list.Count);
				list[index].Kill();
				list.RemoveAt(index);
			}
		}
		Health[] array2 = requiredComponentsToKill;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].KillDelayed(UnityEngine.Random.Range(0f, 3f));
		}
	}

	private void CheckLegs()
	{
		if (twoLegsDown)
		{
			return;
		}
		if (flDestroyed)
		{
			if (frDestroyed)
			{
				LegsDestroyed(new Vector3(0f, 0f, 0f - legDestructionRotationRadius));
			}
			else if (rlDestroyed)
			{
				LegsDestroyed(new Vector3(legDestructionRotationRadius, 0f, 0f));
			}
		}
		else if (rrDestroyed)
		{
			if (frDestroyed)
			{
				LegsDestroyed(new Vector3(0f - legDestructionRotationRadius, 0f, 0f));
			}
			else if (rlDestroyed)
			{
				LegsDestroyed(new Vector3(0f, 0f, legDestructionRotationRadius));
			}
		}
	}

	private void LegsDestroyed(Vector3 localRotPoint)
	{
		twoLegsDown = true;
		localRotPoint.y = ldrHeight;
		StartCoroutine(LegDestroyedRoutine(localRotPoint));
		if (OnTwoLegsDestroyed != null)
		{
			OnTwoLegsDestroyed.Invoke();
		}
	}

	private IEnumerator LegDestroyedRoutine(Vector3 localRotPoint)
	{
		Rigidbody m_rb = legDestructionRotationObject.AddComponent<Rigidbody>();
		m_rb.isKinematic = true;
		m_rb.interpolation = RigidbodyInterpolation.Interpolate;
		m_rb.mass = 1000f;
		IParentRBDependent[] componentsInChildrenImplementing = legDestructionRotationObject.GetComponentsInChildrenImplementing<IParentRBDependent>();
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].SetParentRigidbody(m_rb);
		}
		Vector3 vecToModel = legDestructionRotationObject.transform.position - base.transform.TransformPoint(localRotPoint);
		Quaternion origRot = legDestructionRotationObject.transform.rotation;
		float angle = 0f;
		while (angle != legDestructionRotationAngle)
		{
			angle = Mathf.MoveTowards(angle, legDestructionRotationAngle, legDestructionRotationSpeed * Time.fixedDeltaTime);
			Vector3 normalized = Vector3.Cross(-vecToModel, Vector3.up).normalized;
			Quaternion quaternion = Quaternion.AngleAxis(angle, normalized);
			Vector3 vector = quaternion * vecToModel;
			Quaternion rot = quaternion * origRot;
			Vector3 vector2 = base.transform.TransformPoint(localRotPoint) + vector;
			m_rb.velocity = (vector2 - m_rb.position) / Time.fixedDeltaTime;
			m_rb.MovePosition(vector2);
			m_rb.angularVelocity = legDestructionRotationSpeed * ((float)Math.PI / 180f) * normalized;
			m_rb.MoveRotation(rot);
			yield return new WaitForFixedUpdate();
		}
		Health[] array = requiredComponentsToKill;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Kill();
		}
		m_rb.velocity = Vector3.zero;
		m_rb.angularVelocity = Vector3.zero;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(nodeName);
		List<bool> list = new List<bool>();
		for (int i = 0; i < legs.Length; i++)
		{
			list.Add(legs[i].health.normalizedHealth <= 0f);
		}
		configNode.SetValue("legsDead", list);
		configNode.SetValue("masterDead", masterHealth.normalizedHealth <= 0f);
		List<bool> list2 = new List<bool>();
		for (int j = 0; j < requiredComponentsToKill.Length; j++)
		{
			list2.Add(requiredComponentsToKill[j].normalizedHealth <= 0f);
		}
		configNode.SetValue("reqCompsDead", list2);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(nodeName);
		if (node == null)
		{
			return;
		}
		List<bool> value = node.GetValue<List<bool>>("legsDead");
		for (int i = 0; i < value.Count; i++)
		{
			if (value[i])
			{
				legs[i].health.Kill();
			}
		}
		List<bool> value2 = node.GetValue<List<bool>>("reqCompsDead");
		for (int j = 0; j < value2.Count; j++)
		{
			if (value2[j])
			{
				requiredComponentsToKill[j].Kill();
			}
		}
		if (node.GetValue<bool>("masterDead"))
		{
			masterHealth.QS_Kill();
		}
	}
}
