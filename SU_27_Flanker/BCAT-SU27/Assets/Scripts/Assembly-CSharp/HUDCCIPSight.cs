using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class HUDCCIPSight : MonoBehaviour
{
	public Transform reticleTransform;

	public Image rangeImage;

	private WeaponManager wm;

	private ICCIPCompatible weapon;

	[Space]
	public Transform ccrpLineParent;

	public Transform releasePointTf;

	public Transform releaseIndicatorTf;

	public GameObject releaseMessageObj;

	private Transform refTf;

	private CollimatedHUDUI hud;

	[Header("Curved CCIP")]
	public UILineRenderer3D ccipLR;

	public Transform ccipLRStartPos;

	private void GenerateCCIPLR()
	{
		float magnitude = ccipLR.transform.InverseTransformVector(Vector3.forward).magnitude;
		Vector3 vector = hud.depth * magnitude * Vector3.back;
		Vector3 localPosition = ccipLRStartPos.localPosition;
		float num = Vector3.Angle(Vector3.forward, localPosition - vector);
		float num2 = 8f;
		List<Vector3> list = new List<Vector3>();
		for (float num3 = num; num3 < 60f; num3 += num2)
		{
			Vector3 vector2 = -vector;
			Vector3 item = vector + Quaternion.AngleAxis(num3, Vector3.left) * vector2;
			list.Add(item);
		}
		ccipLR.Points = list.ToArray();
	}

	private void Start()
	{
		hud = GetComponentInParent<CollimatedHUDUI>();
		refTf = base.transform.root;
		wm = GetComponentInParent<WeaponManager>();
		if ((bool)ccipLR)
		{
			GenerateCCIPLR();
		}
	}

	private void Update()
	{
		if (weapon == null)
		{
			return;
		}
		reticleTransform.gameObject.SetActive(value: true);
		Vector3 upwards = reticleTransform.parent.InverseTransformDirection(Vector3.up);
		float t = 0f;
		Vector3 vector = ((!(weapon is HPEquipBombRack)) ? weapon.GetImpactPoint() : ((HPEquipBombRack)weapon).GetImpactPointWithLead(out t));
		bool flag = false;
		Vector3 vector2 = Vector3.zero;
		bool flag2 = true;
		if (weapon is HPEquipBombRack)
		{
			if ((bool)wm.opticalTargeter && wm.opticalTargeter.locked && !wm.opticalTargeter.lockedSky)
			{
				vector2 = wm.opticalTargeter.lockTransform.position + wm.opticalTargeter.targetVelocity * t;
				flag = true;
			}
			if (((HPEquipBombRack)weapon).TimeToImpact() < 0f)
			{
				flag2 = false;
			}
		}
		else if (weapon is HPEquipGPSBombRack && wm.gpsSystem.hasTarget)
		{
			vector2 = wm.gpsSystem.currentGroup.currentTarget.worldPosition;
			flag = true;
		}
		if (flag)
		{
			ccrpLineParent.gameObject.SetActive(value: true);
			ccrpLineParent.position = WorldToHUDPosition(vector2, spherical: false);
			ccrpLineParent.localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
			Vector3 vector3 = refTf.InverseTransformPoint(vector2);
			float num = refTf.InverseTransformPoint(vector).z - vector3.z;
			Vector3 vector4 = ccrpLineParent.InverseTransformPoint(base.transform.position);
			Vector3 localPosition = releaseIndicatorTf.localPosition;
			localPosition.y = vector4.y;
			releaseIndicatorTf.localPosition = localPosition;
			Vector3 localPosition2 = releasePointTf.localPosition;
			localPosition2.y = localPosition.y - num / 3f;
			releasePointTf.localPosition = localPosition2;
			releaseMessageObj.SetActive(Mathf.Abs(num) < 50f);
		}
		else
		{
			ccrpLineParent.gameObject.SetActive(value: false);
		}
		if (!flag2)
		{
			ccrpLineParent.gameObject.SetActive(value: false);
			reticleTransform.gameObject.SetActive(value: false);
		}
		float magnitude = (vector - VRHead.instance.transform.position).magnitude;
		reticleTransform.position = WorldToHUDPosition(vector, ccipLR);
		if ((bool)ccipLR)
		{
			reticleTransform.localRotation = Quaternion.LookRotation(reticleTransform.parent.InverseTransformDirection(reticleTransform.position - VRHead.position), upwards);
		}
		else
		{
			reticleTransform.localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
		}
		rangeImage.fillAmount = Mathf.Clamp01(magnitude / 16000f);
	}

	private Vector3 WorldToHUDPosition(Vector3 worldPos, bool spherical)
	{
		if (spherical)
		{
			return VRHead.position + (worldPos - VRHead.position).normalized * hud.depth;
		}
		Plane plane = new Plane(base.transform.forward, base.transform.position);
		Ray ray = new Ray(VRHead.position, worldPos - VRHead.position);
		if (plane.Raycast(ray, out var enter))
		{
			if (enter < 60000f)
			{
				return ray.GetPoint(enter);
			}
			return VRHead.position - VRHead.instance.transform.forward * 1000f;
		}
		return VRHead.position - VRHead.instance.transform.forward * 1000f;
	}

	public void SetWeapon(ICCIPCompatible w)
	{
		weapon = w;
	}
}
