using System;
using System.Collections;
using UnityEngine;

public class CarrierCable : MonoBehaviour
{
	public float maxDistance = 40f;

	public float maxHorizontalDistance = 12f;

	public float maxNegativeDistance = 30f;

	public float maxUndergroundDistance = 2f;

	public float floorYPos = -0.2f;

	public Transform cable1;

	public Transform cable2;

	public GameObject trigger;

	public float repairTime = 60f;

	private bool hasReset;

	private Vector3 cablePoint;

	private float cableDist;

	private Vector3 cableVel;

	private float maxCableRetractSpeed = 6f;

	private float cableRetractForce = 35f;

	private float cableRetractDamp = 4f;

	public Tailhook hook { get; private set; }

	public event Action<Tailhook> OnSetHook;

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawLine(base.transform.position, base.transform.position + maxDistance * base.transform.forward);
	}

	private void LateUpdate()
	{
		if ((bool)hook)
		{
			cablePoint = base.transform.InverseTransformPoint(hook.hookPointTf.position);
			UpdateCableModel();
			hasReset = false;
			if (cableDist > maxDistance || cablePoint.y < 0f - maxUndergroundDistance || Mathf.Abs(cablePoint.x) > maxHorizontalDistance || cablePoint.z < 0f - maxNegativeDistance)
			{
				BreakCable();
			}
			cableVel = Vector3.zero;
		}
		else
		{
			if (hasReset)
			{
				return;
			}
			if (cableVel.sqrMagnitude < 0.1f && cablePoint.sqrMagnitude < 0.1f)
			{
				ResetCable();
				return;
			}
			Vector3 vector = (0f - cableRetractForce) * cablePoint;
			Vector3 vector2 = -cableVel * cableRetractDamp;
			cableVel += (vector + vector2) * Time.deltaTime;
			cableVel.z = Mathf.Sign(cableVel.z) * Mathf.Min(Mathf.Abs(cableVel.z), maxCableRetractSpeed);
			cableVel.y -= 9.81f * Time.deltaTime;
			if (cablePoint.y < floorYPos)
			{
				if (cableVel.y < -0.1f)
				{
					cableVel.y = (0f - cableVel.y) * 0.5f;
					cablePoint.y = floorYPos;
				}
				else if (Mathf.Abs(cableVel.y) <= 0.1f)
				{
					cablePoint.y = floorYPos;
					cableVel.y = 0f;
				}
			}
			cablePoint += cableVel * Time.deltaTime;
			UpdateCableModel();
		}
	}

	private void ResetCable()
	{
		hasReset = true;
		float z = Vector3.Distance(cable1.position, cable2.position) / 2f;
		cable1.LookAt(cable2.position);
		cable2.LookAt(cable1.position);
		cable1.localScale = new Vector3(1f, 1f, z);
		cable2.localScale = new Vector3(1f, 1f, z);
	}

	private void UpdateCableModel()
	{
		Vector3 vector = base.transform.TransformPoint(cablePoint);
		cable1.LookAt(vector);
		cable2.LookAt(vector);
		float num = Vector3.Distance(cable1.position, vector);
		float num2 = Vector3.Distance(cable2.position, vector);
		cableDist = Mathf.Max(num, num2);
		cable1.localScale = new Vector3(1f, 1f, num);
		cable2.localScale = new Vector3(1f, 1f, num2);
	}

	private void BreakCable()
	{
		if ((bool)hook)
		{
			hook.PlayBreakSound();
			hook.FreeHook();
		}
		SetHook(null);
		cable1.gameObject.SetActive(value: false);
		cable2.gameObject.SetActive(value: false);
		trigger.SetActive(value: false);
		FlightLogger.Log($"{base.gameObject.name} arrestor cable broke!");
		StartCoroutine(RepairRoutine());
	}

	private IEnumerator RepairRoutine()
	{
		yield return new WaitForSeconds(repairTime);
		FixCable();
	}

	private void FixCable()
	{
		cable1.gameObject.SetActive(value: true);
		cable2.gameObject.SetActive(value: true);
		trigger.SetActive(value: true);
		ResetCable();
	}

	public void SetHook(Tailhook hook)
	{
		this.hook = hook;
		this.OnSetHook?.Invoke(hook);
	}

	public void RemoteSetHook(Tailhook hook)
	{
		this.hook = hook;
	}
}
