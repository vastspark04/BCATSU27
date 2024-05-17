using System;
using System.Collections;
using UnityEngine;
using VTOLVR.Multiplayer;

public class MissileLauncher : MonoBehaviour, IMassObject, IParentRBDependent, IUsesInternalWeaponBay
{
	public Transform[] hardpoints;

	public Transform[] overrideDecoupleDirections;

	public GameObject missilePrefab;

	public float baseMass;

	public bool loadOnStart;

	public bool useEdgeTf = true;

	public bool hideUntilLaunch;

	public AudioSource[] launchAudioSources;

	public AudioClip[] launchAudioClips;

	[HideInInspector]
	public Missile[] missiles;

	public float overrideDecoupleSpeed = -1f;

	public float overrideDropTime = -1f;

	public bool debugMissiles;

	[HideInInspector]
	public Actor parentActor;

	[Header("Internal Weapon Bay")]
	public bool openAndCloseBayOnLaunch;

	protected int missileIdx => hardpoints.Length - missileCount;

	public int missileCount { get; private set; }

	public Rigidbody parentRb { get; private set; }

	public bool remoteOnly { get; set; }

	public bool waitingForWpnBay { get; private set; }

	public InternalWeaponBay iwb { get; private set; }

	public event Action<Actor> OnRemoteFiredMissile;

	public event Action<int> OnFiredMissileIdx;

	public event Action<Missile> OnLoadMissile;

	public void SetParentRigidbody(Rigidbody rb)
	{
		parentRb = rb;
	}

	private void Awake()
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		if (loadOnStart && !VTOLMPUtils.IsMultiplayer())
		{
			LoadAllMissiles();
		}
	}

	private void Start()
	{
		if (!parentActor)
		{
			parentActor = GetComponentInParent<Actor>();
		}
		if (launchAudioSources != null && launchAudioSources.Length != launchAudioClips.Length)
		{
			Debug.LogErrorFormat("Missile launcher {0} audio sources don't match audio clips.", base.gameObject.name);
		}
	}

	protected virtual void RemoteFire()
	{
		InvokeRemoteFiredEvent(null);
	}

	protected void InvokeRemoteFiredEvent(Actor actor)
	{
		this.OnRemoteFiredMissile?.Invoke(actor);
	}

	public virtual void RemoteFireOn(Actor actor)
	{
		FireMissile();
	}

	public void FireMissile()
	{
		if (missileCount == 0)
		{
			return;
		}
		if (remoteOnly)
		{
			Debug.Log("MissileLauncher invoking OnRemoteFiredMissile");
			RemoteFire();
			return;
		}
		Missile missile = missiles[missileIdx];
		if (debugMissiles)
		{
			missile.debugMissile = true;
			if ((bool)missile.heatSeeker)
			{
				missile.heatSeeker.debugSeeker = true;
			}
		}
		if ((bool)parentActor)
		{
			Actor actor = missile.gameObject.AddComponent<Actor>();
			actor.role = Actor.Roles.Missile;
			actor.SetMissile(missile);
			actor.fixedVelocityUpdate = false;
			actor.team = parentActor.team;
			actor.iconType = UnitIconManager.MapIconTypes.Missile;
			Teams teams = Teams.Allied;
			if (VTOLMPUtils.IsMultiplayer())
			{
				teams = VTOLMPLobbyManager.localPlayerInfo.team;
			}
			if (parentActor.team == teams && missile.guidanceMode == Missile.GuidanceModes.GPS)
			{
				actor.drawIcon = true;
				actor.useIconRotation = true;
				actor.iconRotationReference = actor.transform;
			}
			else
			{
				actor.drawIcon = false;
			}
			actor.actorName = $"{missile.gameObject.name} ({parentActor.actorName})";
			ModuleRWR componentInChildren = actor.GetComponentInChildren<ModuleRWR>(includeInactive: true);
			if ((bool)componentInChildren)
			{
				componentInChildren.SetActor(actor);
			}
		}
		if (hideUntilLaunch && (bool)missile.hiddenMissileObject)
		{
			missile.hiddenMissileObject.SetActive(value: true);
		}
		HPEquippable componentImplementing = base.gameObject.GetComponentImplementing<HPEquippable>();
		if ((bool)componentImplementing && (bool)componentImplementing.weaponManager)
		{
			componentImplementing.weaponManager.lastFiredMissile = missile;
		}
		if (iwb != null && openAndCloseBayOnLaunch)
		{
			StartCoroutine(IWBFireRoutine(missileIdx));
		}
		else
		{
			FinallyFire(missileIdx);
		}
		missileCount--;
	}

	private void FinallyFire(int mIdx)
	{
		Missile missile = missiles[mIdx];
		if (!missile)
		{
			return;
		}
		missile.launcherRB = parentRb;
		missile.launchedByActor = parentActor;
		missile.Fire();
		this.OnFiredMissileIdx?.Invoke(mIdx);
		if (launchAudioSources != null)
		{
			for (int i = 0; i < launchAudioSources.Length; i++)
			{
				launchAudioSources[i].PlayOneShot(launchAudioClips[i]);
			}
		}
		missiles[mIdx] = null;
	}

	private IEnumerator IWBFireRoutine(int mIdx)
	{
		waitingForWpnBay = true;
		if (openAndCloseBayOnLaunch)
		{
			iwb.RegisterOpenReq(this);
		}
		while (iwb.doorState < 0.99f)
		{
			yield return null;
		}
		FinallyFire(mIdx);
		if (openAndCloseBayOnLaunch)
		{
			yield return new WaitForSeconds(1f);
		}
		waitingForWpnBay = false;
		iwb.UnregisterOpenReq(this);
	}

	public Missile GetNextMissile()
	{
		if (missiles != null && missiles.Length != 0 && missileIdx < missiles.Length)
		{
			return missiles[missileIdx];
		}
		return null;
	}

	public void LoadMissile(int idx)
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		if (!(missiles[idx] != null))
		{
			LoadMissile(LoadMissile(missilePrefab, hardpoints[idx], useEdgeTf, hideUntilLaunch), idx);
		}
	}

	public void LoadMissile(Missile m, int idx)
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		if (idx >= missiles.Length)
		{
			Debug.LogError("LoadMissile idx was out of bounds! " + UIUtils.GetHierarchyString(base.gameObject), base.gameObject);
			return;
		}
		if (missiles[idx] != null && missiles[idx] != m)
		{
			Debug.Log("Removing missile " + missiles[idx].gameObject.name + " to make way for " + m.gameObject.name);
			UnityEngine.Object.Destroy(missiles[idx].gameObject);
		}
		missiles[idx] = m;
		if (overrideDecoupleSpeed > 0f)
		{
			missiles[idx].decoupleSpeed = overrideDecoupleSpeed;
		}
		if (overrideDecoupleDirections != null && overrideDecoupleDirections.Length > idx && overrideDecoupleDirections[idx] != null)
		{
			missiles[idx].overrideDecoupleDirTf = overrideDecoupleDirections[idx];
		}
		if (overrideDropTime >= 0f)
		{
			missiles[idx].thrustDelay = overrideDropTime;
		}
		missileCount++;
		OnLoadedMissile(idx, missiles[idx]);
	}

	protected virtual void OnLoadedMissile(int idx, Missile m)
	{
		this.OnLoadMissile?.Invoke(m);
	}

	public void LoadCount(int count)
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		int i;
		for (i = 0; i < missiles.Length && i < count; i++)
		{
			if (missiles[i] == null)
			{
				LoadMissile(i);
			}
		}
		for (; i < missiles.Length; i++)
		{
			if (missiles[i] != null)
			{
				UnityEngine.Object.Destroy(missiles[i].gameObject);
				missiles[i] = null;
			}
		}
		missileCount = count;
	}

	public void LoadCountReverse(int count)
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		int num = missiles.Length - 1;
		int num2 = 0;
		while (num >= 0 && num2 < count)
		{
			if (missiles[num] == null)
			{
				LoadMissile(num);
			}
			num--;
			num2++;
		}
		while (num >= 0)
		{
			if (missiles[num] != null)
			{
				UnityEngine.Object.Destroy(missiles[num].gameObject);
				missiles[num] = null;
			}
			num--;
		}
		missileCount = count;
	}

	public void RefreshCount()
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		missileCount = 0;
		for (int i = 0; i < missiles.Length; i++)
		{
			if ((bool)missiles[i] && !missiles[i].fired)
			{
				missileCount++;
			}
		}
	}

	[ContextMenu("Reload")]
	public void ContextReload()
	{
		if (Application.isPlaying)
		{
			LoadAllMissiles();
		}
	}

	public void LoadAllMissiles()
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		for (int i = 0; i < hardpoints.Length; i++)
		{
			LoadMissile(i);
		}
	}

	public void RemoveAllMissiles()
	{
		if (missiles == null || missiles.Length == 0)
		{
			missiles = new Missile[hardpoints.Length];
		}
		for (int i = 0; i < missiles.Length; i++)
		{
			if (missiles[i] != null)
			{
				UnityEngine.Object.Destroy(missiles[i].gameObject);
			}
		}
		missileCount = 0;
	}

	[ContextMenu("Clear Hardpoints")]
	public void ContextClearHardpoints()
	{
		hardpoints = new Transform[0];
	}

	public float GetMass()
	{
		float num = baseMass;
		if (missiles != null)
		{
			for (int i = 0; i < missiles.Length; i++)
			{
				if ((bool)missiles[i])
				{
					num += missiles[i].mass;
				}
			}
		}
		return num;
	}

	public static Missile LoadMissile(GameObject missilePrefab, Transform hardpoint, bool useEdgeTf, bool hideUntilLaunch, bool instantiate = true)
	{
		GameObject gameObject = ((!instantiate) ? missilePrefab : UnityEngine.Object.Instantiate(missilePrefab, hardpoint.position, hardpoint.rotation, hardpoint));
		Missile component = gameObject.GetComponent<Missile>();
		component.gameObject.name = missilePrefab.name;
		if (hideUntilLaunch)
		{
			if ((bool)component.hiddenMissileObject)
			{
				gameObject.SetActive(value: true);
				component.hiddenMissileObject.SetActive(value: false);
			}
			else
			{
				gameObject.SetActive(value: false);
			}
		}
		else
		{
			gameObject.SetActive(value: true);
		}
		gameObject.transform.localScale = Vector3.one;
		if ((bool)component.edgeTransform && useEdgeTf)
		{
			component.edgeTransform.parent = hardpoint;
			component.transform.parent = component.edgeTransform;
			component.edgeTransform.localPosition = Vector3.zero;
			component.edgeTransform.localRotation = Quaternion.identity;
			component.transform.parent = hardpoint;
			component.edgeTransform.parent = component.transform;
		}
		else
		{
			component.transform.parent = hardpoint;
			component.transform.localPosition = Vector3.zero;
			component.transform.localRotation = Quaternion.identity;
		}
		return component;
	}

	public void SetInternalWeaponBay(InternalWeaponBay bay)
	{
		iwb = bay;
	}
}
