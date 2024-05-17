using System;
using UnityEngine;

public class HUDTSCTarget : MonoBehaviour
{
	public TacticalSituationController tsc;

	public GameObject friendlyObj;

	private HUDMaskToggler hudMask;

	public GameObject iconObj;

	public float maxMaskedAngle = 2f;

	public float outOfViewBlinkRate = 4f;

	private float depth;

	private void Start()
	{
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
		hudMask = GetComponentInParent<HUDMaskToggler>();
	}

	private void LateUpdate()
	{
		TacticalSituationController.TSTargetInfo currentSelectionInfo = tsc.GetCurrentSelectionInfo();
		if (currentSelectionInfo != null)
		{
			iconObj.SetActive(value: true);
			Vector3 point = currentSelectionInfo.point;
			if (currentSelectionInfo is TacticalSituationController.TSActorTargetInfo)
			{
				TacticalSituationController.TSActorTargetInfo tSActorTargetInfo = (TacticalSituationController.TSActorTargetInfo)currentSelectionInfo;
				Actor actor = tSActorTargetInfo.actor;
				friendlyObj.SetActive(actor.team == tsc.weaponManager.actor.team);
				point += tSActorTargetInfo.velocity * (Time.time - tSActorTargetInfo.detectionTime);
			}
			else
			{
				friendlyObj.SetActive(value: false);
			}
			Vector3 vector = depth * (point - VRHead.position).normalized;
			if (hudMask.isMasked && Vector3.Angle(base.transform.forward, vector) > maxMaskedAngle)
			{
				vector = Vector3.RotateTowards(base.transform.forward, vector, maxMaskedAngle * ((float)Math.PI / 180f), float.MaxValue);
				iconObj.SetActive(Mathf.RoundToInt(Time.time * outOfViewBlinkRate) % 2 == 0);
			}
			iconObj.transform.position = VRHead.position + vector;
			iconObj.transform.rotation = Quaternion.LookRotation(vector, iconObj.transform.parent.up);
		}
		else
		{
			iconObj.SetActive(value: false);
		}
	}
}
