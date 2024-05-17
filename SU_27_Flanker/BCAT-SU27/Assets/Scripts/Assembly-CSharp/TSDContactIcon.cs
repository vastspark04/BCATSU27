using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class TSDContactIcon : MonoBehaviour
{
	public GameObject alliedAirObj;

	public GameObject alliedGroundObj;

	public GameObject enemyAirObj;

	public GameObject enemyGroundObj;

	public GameObject missileObj;

	public Image radarIcon;

	public int tscIdx;

	public bool isGPS;

	public GPSTargetGroup gpsGroup;

	public int gpsIdx;

	private GameObject activeIcon;

	private Teams myTeam;

	private bool hasSetRadar;

	private Coroutine flashRoutine;

	public GPSTarget gpsData
	{
		get
		{
			if (gpsGroup != null && gpsIdx < gpsGroup.targets.Count)
			{
				return gpsGroup.targets[gpsIdx];
			}
			return null;
		}
	}

	public bool isFlashing { get; private set; }

	public void SetupForActor(Actor a)
	{
		alliedAirObj.SetActive(value: false);
		alliedGroundObj.SetActive(value: false);
		enemyAirObj.SetActive(value: false);
		enemyGroundObj.SetActive(value: false);
		missileObj.SetActive(value: false);
		radarIcon.gameObject.SetActive(value: false);
		myTeam = Teams.Allied;
		if (VTOLMPUtils.IsMultiplayer())
		{
			myTeam = VTOLMPLobbyManager.localPlayerInfo.team;
		}
		if ((bool)a)
		{
			if (a.finalCombatRole == Actor.Roles.Air)
			{
				if (a.team == myTeam)
				{
					alliedAirObj.SetActive(value: true);
					activeIcon = alliedAirObj;
				}
				else
				{
					enemyAirObj.SetActive(value: true);
					activeIcon = enemyAirObj;
				}
			}
			else if (a.finalCombatRole == Actor.Roles.Ground || a.finalCombatRole == Actor.Roles.GroundArmor || a.finalCombatRole == Actor.Roles.Ship)
			{
				if (a.team == myTeam)
				{
					alliedGroundObj.SetActive(value: true);
					activeIcon = alliedGroundObj;
				}
				else
				{
					enemyGroundObj.SetActive(value: true);
					activeIcon = enemyGroundObj;
				}
			}
			else if (a.role == Actor.Roles.Missile)
			{
				missileObj.SetActive(value: true);
				activeIcon = missileObj;
			}
		}
		if (!alliedAirObj.activeSelf)
		{
			Object.Destroy(alliedAirObj);
		}
		if (!alliedGroundObj.activeSelf)
		{
			Object.Destroy(alliedGroundObj);
		}
		if (!enemyAirObj.activeSelf)
		{
			Object.Destroy(enemyAirObj);
		}
		if (!enemyGroundObj.activeSelf)
		{
			Object.Destroy(enemyGroundObj);
		}
		if (!missileObj.activeSelf)
		{
			Object.Destroy(missileObj);
		}
	}

	private void OnEnable()
	{
		if ((bool)activeIcon)
		{
			activeIcon.SetActive(value: true);
			StopFlash();
		}
	}

	public void SetRadar()
	{
		if (!hasSetRadar)
		{
			hasSetRadar = true;
			radarIcon.gameObject.SetActive(value: true);
			radarIcon.color = activeIcon.GetComponent<Image>().color;
		}
	}

	public void Flash(float duration)
	{
		if (flashRoutine != null)
		{
			StopCoroutine(flashRoutine);
		}
		isFlashing = true;
		flashRoutine = StartCoroutine(FlashRoutine(duration));
	}

	public void StopFlash()
	{
		if (flashRoutine != null)
		{
			StopCoroutine(flashRoutine);
		}
		isFlashing = false;
	}

	private IEnumerator FlashRoutine(float duration)
	{
		float t = Time.time;
		activeIcon.SetActive(!activeIcon.activeSelf);
		while (Time.time - t < duration)
		{
			yield return new WaitForSeconds(0.3f);
			activeIcon.SetActive(!activeIcon.activeSelf);
		}
		activeIcon.SetActive(value: true);
		isFlashing = false;
	}
}
