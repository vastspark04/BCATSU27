using System.Collections.Generic;
using UnityEngine;

public class HUDGPSIcon : MonoBehaviour
{
	private WeaponManager wm;

	private float depth;

	private Transform myTransform;

	public GameObject unselectedIconsTemplate;

	private bool noTargets = true;

	private List<GameObject> unselectedIcons = new List<GameObject>();

	private void Awake()
	{
		myTransform = base.transform;
	}

	private void Start()
	{
		wm = GetComponentInParent<WeaponManager>();
		wm.gpsSystem.onGPSTargetsChanged.AddListener(OnGPSTargetsChanged);
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
		base.gameObject.SetActive(value: false);
		OnGPSTargetsChanged();
	}

	private void OnGPSTargetsChanged()
	{
		if (wm.gpsSystem.noGroups)
		{
			base.gameObject.SetActive(value: false);
			noTargets = true;
		}
		else if (wm.gpsSystem.currentGroup.targets.Count > 0)
		{
			noTargets = false;
			base.gameObject.SetActive(value: true);
		}
		else
		{
			noTargets = true;
			base.gameObject.SetActive(value: false);
		}
		UpdateUI();
	}

	private void LateUpdate()
	{
		UpdateUI();
	}

	private void UpdateUI()
	{
		if (!noTargets)
		{
			SetHUDPos(myTransform, wm.gpsSystem.currentGroup.currentTarget.worldPosition);
		}
		UpdateUnselectedIcons();
	}

	private void SetHUDPos(Transform tf, Vector3 worldPos)
	{
		Ray ray = new Ray(VRHead.position, worldPos - VRHead.position);
		tf.position = ray.GetPoint(depth);
		tf.rotation = Quaternion.LookRotation(ray.direction, myTransform.parent.up);
	}

	private void UpdateUnselectedIcons()
	{
		if (unselectedIconsTemplate == null)
		{
			return;
		}
		if (noTargets)
		{
			for (int i = 0; i < unselectedIcons.Count; i++)
			{
				unselectedIcons[i].SetActive(value: false);
			}
			return;
		}
		GPSTargetGroup currentGroup = wm.gpsSystem.currentGroup;
		int count = currentGroup.targets.Count;
		int j;
		for (j = 0; j < count; j++)
		{
			if (j >= unselectedIcons.Count)
			{
				unselectedIcons.Add(Object.Instantiate(unselectedIconsTemplate, myTransform.parent));
			}
			GameObject gameObject = unselectedIcons[j];
			gameObject.SetActive(value: true);
			SetHUDPos(gameObject.transform, currentGroup.targets[j].worldPosition);
		}
		for (; j < unselectedIcons.Count; j++)
		{
			unselectedIcons[j].SetActive(value: false);
		}
	}
}
