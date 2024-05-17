using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDRadarContacts : MonoBehaviour
{
	public MFDRadarUI radarUI;

	public Actor myActor;

	public GameObject groundTargetTemplate;

	public GameObject airTargetTemplate;

	public GameObject missileTargetTemplate;

	public Transform lockedTargetTf;

	private float depth = 1000f;

	private List<GameObject> groundIcons = new List<GameObject>();

	private List<GameObject> airIcons = new List<GameObject>();

	private List<GameObject> missileIcons = new List<GameObject>();

	private Coroutine updateRoutine;

	private void Start()
	{
		radarUI.OnSetPlayerRadar += RadarUI_OnSetPlayerRadar;
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
		groundTargetTemplate.SetActive(value: false);
		airTargetTemplate.SetActive(value: false);
		missileTargetTemplate.SetActive(value: false);
		lockedTargetTf.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		RadarUI_OnSetPlayerRadar(radarUI.playerRadar, radarUI.lockingRadar);
		HideAllIcons();
	}

	private void RadarUI_OnSetPlayerRadar(Radar r, LockingRadar lr)
	{
		if (!r)
		{
			if (updateRoutine != null)
			{
				StopCoroutine(updateRoutine);
			}
			HideAllIcons();
		}
		else
		{
			if (updateRoutine != null)
			{
				StopCoroutine(updateRoutine);
			}
			updateRoutine = StartCoroutine(UpdateRoutine());
		}
	}

	private void HideAllIcons()
	{
		foreach (GameObject groundIcon in groundIcons)
		{
			groundIcon.gameObject.SetActive(value: false);
		}
		foreach (GameObject airIcon in airIcons)
		{
			airIcon.gameObject.SetActive(value: false);
		}
		foreach (GameObject missileIcon in missileIcons)
		{
			missileIcon.gameObject.SetActive(value: false);
		}
		lockedTargetTf.gameObject.SetActive(value: false);
	}

	private void ProcessActor(Actor a, ref int gIdx, ref int aIdx, ref int mIdx)
	{
		if (!a || (a.team == myActor.team && !(a == radarUI.currentLockedActor)))
		{
			return;
		}
		Transform transform;
		if ((bool)radarUI.currentLockedActor && a == radarUI.currentLockedActor)
		{
			transform = lockedTargetTf;
		}
		else if (a.role == Actor.Roles.Air)
		{
			if (aIdx >= airIcons.Count)
			{
				transform = Object.Instantiate(airTargetTemplate, airTargetTemplate.transform.parent).transform;
				airIcons.Add(transform.gameObject);
			}
			else
			{
				transform = airIcons[aIdx].transform;
			}
			aIdx++;
		}
		else if (a.role == Actor.Roles.Missile)
		{
			if (mIdx >= missileIcons.Count)
			{
				transform = Object.Instantiate(missileTargetTemplate, missileTargetTemplate.transform.parent).transform;
				missileIcons.Add(transform.gameObject);
			}
			else
			{
				transform = missileIcons[mIdx].transform;
			}
			mIdx++;
		}
		else
		{
			if (gIdx >= groundIcons.Count)
			{
				transform = Object.Instantiate(groundTargetTemplate, groundTargetTemplate.transform.parent).transform;
				groundIcons.Add(transform.gameObject);
			}
			else
			{
				transform = groundIcons[gIdx].transform;
			}
			gIdx++;
		}
		transform.gameObject.SetActive(value: true);
		Vector3 vector = (a.position - VRHead.position).normalized * depth;
		transform.position = VRHead.position + vector;
		transform.rotation = Quaternion.LookRotation(vector, transform.parent.up);
	}

	private IEnumerator UpdateRoutine()
	{
		while (base.enabled && (bool)radarUI.playerRadar)
		{
			while ((bool)radarUI.playerRadar && radarUI.playerRadar.radarEnabled)
			{
				Radar playerRadar = radarUI.playerRadar;
				int gIdx = 0;
				int aIdx = 0;
				int mIdx = 0;
				if (!radarUI.currentLockedActor)
				{
					lockedTargetTf.gameObject.SetActive(value: false);
				}
				else
				{
					ProcessActor(radarUI.currentLockedActor, ref gIdx, ref aIdx, ref mIdx);
				}
				for (int i = 0; i < playerRadar.detectedUnits.Count; i++)
				{
					Actor actor = playerRadar.detectedUnits[i];
					if (actor != radarUI.currentLockedActor)
					{
						ProcessActor(actor, ref gIdx, ref aIdx, ref mIdx);
					}
				}
				for (int j = gIdx; j < groundIcons.Count; j++)
				{
					groundIcons[j].gameObject.SetActive(value: false);
				}
				for (int k = aIdx; k < airIcons.Count; k++)
				{
					airIcons[k].gameObject.SetActive(value: false);
				}
				for (int l = mIdx; l < missileIcons.Count; l++)
				{
					missileIcons[l].gameObject.SetActive(value: false);
				}
				yield return null;
			}
			HideAllIcons();
			while ((bool)radarUI.playerRadar && !radarUI.playerRadar.radarEnabled)
			{
				yield return null;
			}
		}
	}
}
