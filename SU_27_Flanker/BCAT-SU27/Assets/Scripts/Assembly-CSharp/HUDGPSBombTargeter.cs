using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDGPSBombTargeter : MonoBehaviour
{
	public RectTransform radiusTransform;

	public GameObject targetTemplate;

	public Transform selectedTargetTf;

	public Transform bearingRotator;

	public Text crossRangeLabel;

	public IGuidedBombWeapon guidedBombRack;

	private WeaponManager wm;

	private MeasurementManager measurements;

	private List<Transform> gpsIconTfs = new List<Transform>();

	private Transform referenceTransform;

	private void Start()
	{
		if (!wm)
		{
			wm = GetComponentInParent<WeaponManager>();
		}
		measurements = GetComponentInParent<MeasurementManager>();
		wm.gpsSystem.onGPSTargetsChanged.AddListener(OnGPSTargetsChanged);
		targetTemplate.SetActive(value: false);
		selectedTargetTf.gameObject.SetActive(value: false);
		AddTargetIcon();
	}

	private void OnGPSTargetsChanged()
	{
		if (base.gameObject.activeSelf)
		{
			SetupIcons();
		}
	}

	private void OnEnable()
	{
		if (!wm)
		{
			wm = GetComponentInParent<WeaponManager>();
		}
		if ((bool)wm && wm.gpsSystem != null)
		{
			OnGPSTargetsChanged();
		}
		if (!referenceTransform)
		{
			referenceTransform = new GameObject("GBUReferenceTransform").transform;
		}
	}

	private void LateUpdate()
	{
		if (guidedBombRack != null && guidedBombRack.HasGuidedBombTarget())
		{
			referenceTransform.position = guidedBombRack.GetImpactPoint();
			Vector3 forward = wm.transform.forward;
			forward.y = 0f;
			referenceTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			float deployRadius = guidedBombRack.GetDeployRadius(referenceTransform.position);
			float num = radiusTransform.rect.width / 2f / deployRadius;
			if (guidedBombRack is HPEquipGPSBombRack)
			{
				int i;
				for (i = 0; i < wm.gpsSystem.currentGroup.targets.Count && i < gpsIconTfs.Count; i++)
				{
					Vector3 vector = referenceTransform.InverseTransformPoint(wm.gpsSystem.currentGroup.targets[i].worldPosition);
					Vector3 vector2 = new Vector3(vector.x, vector.z, 0f) * num;
					gpsIconTfs[i].gameObject.SetActive(value: true);
					gpsIconTfs[i].localPosition = vector2;
					if (i == wm.gpsSystem.currentGroup.currentTargetIdx)
					{
						selectedTargetTf.localPosition = vector2;
						selectedTargetTf.gameObject.SetActive(value: true);
						bearingRotator.localRotation = Quaternion.LookRotation(Vector3.forward, vector2 - bearingRotator.localPosition);
					}
				}
				for (; i < gpsIconTfs.Count; i++)
				{
					gpsIconTfs[i].gameObject.SetActive(value: false);
				}
			}
			else
			{
				selectedTargetTf.gameObject.SetActive(((HPEquippable)guidedBombRack).LaunchAuthorized());
				bearingRotator.gameObject.SetActive(value: false);
				if ((bool)wm.opticalTargeter && wm.opticalTargeter.powered && wm.opticalTargeter.locked)
				{
					gpsIconTfs[0].gameObject.SetActive(value: true);
					Vector3 position = wm.opticalTargeter.lockTransform.position;
					MissileLauncher ml = ((HPEquipMissileLauncher)(HPEquippable)guidedBombRack).ml;
					if ((bool)ml && (bool)ml.GetNextMissile() && !ml.GetNextMissile().opticalLOAL)
					{
						position = wm.opticalTargeter.laserPoint.point;
					}
					Vector3 vector3 = referenceTransform.InverseTransformPoint(position);
					Vector3 vector4 = new Vector3(vector3.x, vector3.z, 0f) * num;
					gpsIconTfs[0].gameObject.SetActive(value: true);
					gpsIconTfs[0].localPosition = vector4;
					selectedTargetTf.localPosition = vector4;
					bearingRotator.gameObject.SetActive(value: true);
					bearingRotator.localRotation = Quaternion.LookRotation(Vector3.forward, vector4 - bearingRotator.localPosition);
				}
				else
				{
					gpsIconTfs[0].gameObject.SetActive(value: false);
				}
			}
			crossRangeLabel.text = $"CR|{measurements.FormattedDistance(deployRadius)}";
		}
		else
		{
			bearingRotator.gameObject.SetActive(value: false);
			selectedTargetTf.gameObject.SetActive(value: false);
		}
	}

	private void AddTargetIcon()
	{
		Transform transform = Object.Instantiate(targetTemplate, targetTemplate.transform.parent).transform;
		transform.localScale = targetTemplate.transform.localScale;
		transform.localRotation = targetTemplate.transform.localRotation;
		transform.gameObject.SetActive(value: false);
		gpsIconTfs.Add(transform);
	}

	private void SetupIcons()
	{
		if (guidedBombRack == null)
		{
			return;
		}
		if (guidedBombRack is HPEquipGPSBombRack)
		{
			if (!wm.gpsSystem.noGroups)
			{
				int count = wm.gpsSystem.currentGroup.targets.Count;
				if (count > gpsIconTfs.Count)
				{
					int num = count - gpsIconTfs.Count;
					for (int i = 0; i < num; i++)
					{
						AddTargetIcon();
					}
				}
				for (int j = 0; j < gpsIconTfs.Count; j++)
				{
					if (j < count)
					{
						gpsIconTfs[j].gameObject.SetActive(value: true);
					}
					else
					{
						gpsIconTfs[j].gameObject.SetActive(value: false);
					}
				}
				if (count > 0)
				{
					selectedTargetTf.gameObject.SetActive(value: true);
					radiusTransform.gameObject.SetActive(value: true);
					bearingRotator.gameObject.SetActive(value: true);
				}
				else
				{
					selectedTargetTf.gameObject.SetActive(value: false);
					crossRangeLabel.text = string.Empty;
					radiusTransform.gameObject.SetActive(value: false);
					bearingRotator.gameObject.SetActive(value: false);
				}
				return;
			}
			foreach (Transform gpsIconTf in gpsIconTfs)
			{
				if ((bool)gpsIconTf)
				{
					gpsIconTf.gameObject.SetActive(value: false);
				}
			}
			selectedTargetTf.gameObject.SetActive(value: false);
			crossRangeLabel.text = string.Empty;
			radiusTransform.gameObject.SetActive(value: false);
			bearingRotator.gameObject.SetActive(value: false);
		}
		else
		{
			selectedTargetTf.gameObject.SetActive(value: true);
			radiusTransform.gameObject.SetActive(value: true);
			bearingRotator.gameObject.SetActive(value: true);
			if (gpsIconTfs.Count < 1)
			{
				AddTargetIcon();
			}
			for (int k = 0; k < gpsIconTfs.Count; k++)
			{
				gpsIconTfs[k].gameObject.SetActive(value: false);
			}
		}
	}
}
