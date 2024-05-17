using System.Collections.Generic;
using UnityEngine;

public class HUDTacticalSituationSymbols : MonoBehaviour
{
	public TacticalSituationController tsc;

	public GameObject enemyLayer;

	public GameObject alliedLayer;

	public GameObject[] airLayer;

	public GameObject[] groundLayer;

	public GameObject missileLayer;

	public GameObject template_enemyAir;

	public GameObject template_alliedAir;

	public GameObject template_enemyGround;

	public GameObject template_alliedGround;

	public GameObject template_missile;

	private bool wasChanged;

	private bool _showEnemies;

	private bool _showAllied;

	private bool _showAir;

	private bool _showGround;

	private bool _showMissiles;

	private float depth;

	private List<HUDTSDSymbol> iconTfs = new List<HUDTSDSymbol>();

	public float minSqrDist = 250000f;

	public bool showEnemies
	{
		get
		{
			return _showEnemies;
		}
		set
		{
			wasChanged = true;
			_showEnemies = value;
			enemyLayer.SetActive(value);
		}
	}

	public bool showAllied
	{
		get
		{
			return _showAllied;
		}
		set
		{
			wasChanged = true;
			_showAllied = value;
			alliedLayer.SetActive(value);
		}
	}

	public bool showAir
	{
		get
		{
			return _showAir;
		}
		set
		{
			wasChanged = true;
			_showAir = value;
			airLayer.SetActive(value);
		}
	}

	public bool showGround
	{
		get
		{
			return _showGround;
		}
		set
		{
			wasChanged = true;
			_showGround = value;
			groundLayer.SetActive(value);
		}
	}

	public bool showMissiles
	{
		get
		{
			return _showMissiles;
		}
		set
		{
			wasChanged = true;
			_showMissiles = value;
			missileLayer.SetActive(value);
		}
	}

	private void Awake()
	{
		template_enemyAir.SetActive(value: false);
		template_alliedAir.SetActive(value: false);
		template_enemyGround.SetActive(value: false);
		template_alliedGround.SetActive(value: false);
		template_missile.SetActive(value: false);
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
	}

	private void Start()
	{
		tsc.OnRegisteredInfo += Tsc_OnRegisteredInfo;
		foreach (TacticalSituationController.TSTargetInfo info in tsc.infos)
		{
			Tsc_OnRegisteredInfo(info);
		}
		if (!wasChanged)
		{
			showEnemies = true;
			showAllied = false;
			showAir = true;
			showGround = true;
			showMissiles = true;
		}
	}

	private void Tsc_OnRegisteredInfo(TacticalSituationController.TSTargetInfo info)
	{
		if (!info.lost && info is TacticalSituationController.TSActorTargetInfo)
		{
			TacticalSituationController.TSActorTargetInfo tSActorTargetInfo = (TacticalSituationController.TSActorTargetInfo)info;
			if (!tSActorTargetInfo.actor)
			{
				iconTfs.Add(null);
				return;
			}
			Transform parent;
			GameObject original;
			if (tSActorTargetInfo.actor.role == Actor.Roles.Missile)
			{
				parent = missileLayer.transform;
				original = template_missile;
			}
			else if (tSActorTargetInfo.actor.finalCombatRole == Actor.Roles.Air)
			{
				if (tSActorTargetInfo.actor.team == tsc.weaponManager.actor.team)
				{
					parent = airLayer[0].transform;
					original = template_alliedAir;
				}
				else
				{
					parent = airLayer[1].transform;
					original = template_enemyAir;
				}
			}
			else if (tSActorTargetInfo.actor.team == tsc.weaponManager.actor.team)
			{
				parent = groundLayer[0].transform;
				original = template_alliedGround;
			}
			else
			{
				parent = groundLayer[1].transform;
				original = template_enemyGround;
			}
			GameObject obj = Object.Instantiate(original, parent);
			obj.SetActive(value: true);
			HUDTSDSymbol component = obj.GetComponent<HUDTSDSymbol>();
			component.InitSymbol(tsc, this, info.dataIdx, depth);
			iconTfs.Add(component);
		}
		else
		{
			iconTfs.Add(null);
		}
	}

	private void Update()
	{
		bool flag = false;
		for (int i = 0; i < iconTfs.Count; i++)
		{
			HUDTSDSymbol hUDTSDSymbol = iconTfs[i];
			if ((bool)hUDTSDSymbol && (flag || hUDTSDSymbol.transform.parent.gameObject.activeSelf))
			{
				flag = true;
				hUDTSDSymbol.UpdateSymbol();
			}
		}
	}
}
