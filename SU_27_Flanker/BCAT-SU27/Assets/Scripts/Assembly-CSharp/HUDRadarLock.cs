using System;
using UnityEngine;

public class HUDRadarLock : MonoBehaviour
{
	public MFDRadarUI radarUI;

	public LockingRadar radar;

	private HUDMaskToggler hudMask;

	public GameObject iconObj;

	public GameObject friendlyObj;

	public GameObject trackTemplate;

	private HUDRadarTrackIcon[] trackIcons;

	public float maxMaskedAngle = 2f;

	public float outOfViewBlinkRate = 4f;

	private float depth;

	private Actor actor;

	private void Start()
	{
		actor = GetComponentInParent<Actor>();
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
		hudMask = GetComponentInParent<HUDMaskToggler>();
		trackIcons = new HUDRadarTrackIcon[radarUI.softLockCount];
		for (int i = 0; i < radarUI.softLockCount; i++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(trackTemplate, base.transform.parent);
			HUDRadarTrackIcon component = obj.GetComponent<HUDRadarTrackIcon>();
			component.trackText.text = (i + 1).ToString();
			trackIcons[i] = component;
			obj.SetActive(value: false);
		}
		trackTemplate.SetActive(value: false);
	}

	private void LateUpdate()
	{
		if (radar.IsLocked())
		{
			iconObj.SetActive(value: true);
			Actor actor = radar.currentLock.actor;
			friendlyObj.SetActive(actor.team == this.actor.team);
			Vector3 vector = depth * (actor.position - VRHead.position).normalized;
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
		for (int i = 0; i < radarUI.softLockCount; i++)
		{
			MFDRadarUI.UIRadarContact uIRadarContact = radarUI.softLocks[i];
			if (uIRadarContact != null && uIRadarContact.active)
			{
				bool showCircle = !radar.IsLocked() || !(uIRadarContact.actor == radar.currentLock.actor);
				trackIcons[i].gameObject.SetActive(value: true);
				trackIcons[i].SetTrack(uIRadarContact.detectedPosition.point, showCircle);
			}
			else
			{
				trackIcons[i].gameObject.SetActive(value: false);
			}
		}
	}
}
