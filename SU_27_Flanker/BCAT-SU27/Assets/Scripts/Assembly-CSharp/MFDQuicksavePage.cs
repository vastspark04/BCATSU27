using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MFDQuicksavePage : MonoBehaviour
{
	public Actor actor;

	public GameObject saveButtonLabel;

	public GameObject loadAreaObject;

	public GameObject saveNotAvailableObj;

	public GameObject saveAvailableLabel;

	public Text savesAvailableText;

	public Text lastSaveTimeText;

	public GameObject savedDisplayObj;

	private bool isQuicksaving;

	private void Awake()
	{
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
	}

	private void OnEnable()
	{
		VTScenario current = VTScenario.current;
		if (current.qsMode == QuicksaveManager.QSModes.None || (current.qsLimit > 0 && QuicksaveManager.instance.savesUsed >= current.qsLimit))
		{
			saveNotAvailableObj.SetActive(value: true);
		}
		else if (current.qsMode == QuicksaveManager.QSModes.Rearm_Only)
		{
			StartCoroutine(CheckInRearmZoneRoutine());
		}
		if (current.qsLimit > 0)
		{
			savesAvailableText.gameObject.SetActive(value: true);
			saveAvailableLabel.gameObject.SetActive(value: true);
			savesAvailableText.text = (current.qsLimit - QuicksaveManager.instance.savesUsed).ToString();
		}
		else
		{
			savesAvailableText.gameObject.SetActive(value: false);
			saveAvailableLabel.gameObject.SetActive(value: false);
		}
		savedDisplayObj.SetActive(value: false);
	}

	private void OnDisable()
	{
		savedDisplayObj.SetActive(value: false);
		isQuicksaving = false;
	}

	private void Update()
	{
		if (QuicksaveManager.quickloadAvailable)
		{
			loadAreaObject.gameObject.SetActive(value: true);
			float num = FlightSceneManager.instance.missionElapsedTime - QuicksaveManager.quicksaveMET;
			num = Mathf.Floor(num / 60f);
			lastSaveTimeText.text = $"{num} {VTLStaticStrings.s_endMission_minsAgo}";
		}
		else
		{
			loadAreaObject.SetActive(value: false);
		}
	}

	private IEnumerator QsRoutine()
	{
		if (!QuicksaveManager.instance.Quicksave())
		{
			yield break;
		}
		isQuicksaving = true;
		VTScenario current = VTScenario.current;
		if (current.qsLimit > 0)
		{
			if (QuicksaveManager.instance.savesUsed >= current.qsLimit)
			{
				saveNotAvailableObj.SetActive(value: true);
			}
			else
			{
				savesAvailableText.text = (current.qsLimit - QuicksaveManager.instance.savesUsed).ToString();
			}
		}
		savedDisplayObj.SetActive(value: true);
		yield return new WaitForSeconds(3f);
		savedDisplayObj.SetActive(value: false);
		isQuicksaving = false;
	}

	public void Save()
	{
		if (QuicksaveManager.instance.CheckScenarioQsLimits() && !isQuicksaving)
		{
			StartCoroutine(QsRoutine());
		}
	}

	public void Load()
	{
		if (QuicksaveManager.quickloadAvailable)
		{
			QuicksaveManager.instance.Quickload();
		}
	}

	private IEnumerator CheckInRearmZoneRoutine()
	{
		while (base.enabled)
		{
			if (CheckIsInRearmZone())
			{
				saveNotAvailableObj.SetActive(value: false);
			}
			else
			{
				saveNotAvailableObj.SetActive(value: true);
			}
			yield return new WaitForSeconds(1f);
		}
	}

	private bool CheckIsInRearmZone()
	{
		List<ReArmingPoint> reArmingPoints = ReArmingPoint.reArmingPoints;
		for (int i = 0; i < reArmingPoints.Count; i++)
		{
			ReArmingPoint reArmingPoint = reArmingPoints[i];
			if (reArmingPoint.team == actor.team && (reArmingPoint.transform.position - actor.position).sqrMagnitude < reArmingPoint.radius * reArmingPoint.radius)
			{
				return true;
			}
		}
		return false;
	}
}
