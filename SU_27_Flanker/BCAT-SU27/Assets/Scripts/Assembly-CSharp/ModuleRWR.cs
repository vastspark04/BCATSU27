using System;
using System.Collections.Generic;
using UnityEngine;

public class ModuleRWR : MonoBehaviour
{
	public delegate void RWRDelegate(RWRContact contact);

	[Serializable]
	public class RWRContact
	{
		public Actor radarActor;

		public string radarSymbol;

		private FixedPoint _detPoint;

		public float signalStrength;

		public bool locked;

		private float persistTime;

		private float timeDetected = float.MinValue;

		private float lockTimeDetected;

		public Vector3 detectedPosition
		{
			get
			{
				return _detPoint.point;
			}
			set
			{
				_detPoint = new FixedPoint(value);
			}
		}

		public bool active
		{
			get
			{
				if ((bool)radarActor)
				{
					return Time.time - timeDetected < persistTime;
				}
				return false;
			}
		}

		public float GetTimeDetected()
		{
			return timeDetected;
		}

		public void UpdateExisting(float persistTime, float signalStrength, Vector3 radarPosition, bool locked)
		{
			this.persistTime = Mathf.Max(this.persistTime, persistTime);
			this.signalStrength = signalStrength;
			detectedPosition = radarPosition;
			timeDetected = Time.time;
			if (locked)
			{
				this.locked = locked;
				lockTimeDetected = Time.time;
			}
			else if (Time.time - lockTimeDetected > this.persistTime)
			{
				locked = false;
			}
		}

		public void UpdateNew(Actor radarActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength, bool locked)
		{
			this.radarActor = radarActor;
			this.radarSymbol = radarSymbol;
			this.persistTime = persistTime;
			UpdateExisting(persistTime, signalStrength, radarPosition, locked);
		}

		public ConfigNode SaveToConfigNode(string nodeName)
		{
			ConfigNode configNode = new ConfigNode(nodeName);
			ConfigNode node = QuicksaveManager.SaveActorIdentifierToNode(radarActor, "radarActor");
			configNode.AddNode(node);
			configNode.SetValue("radarSymbol", radarSymbol);
			configNode.SetValue("detGlobalPoint", _detPoint.globalPoint);
			configNode.SetValue("signalStrength", signalStrength);
			configNode.SetValue("locked", locked);
			configNode.SetValue("persistTime", persistTime);
			configNode.SetValue("timeDetectedElapsed", Time.time - timeDetected);
			return configNode;
		}

		public void LoadFromConfigNode(ConfigNode node)
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node.GetNode("radarActor"));
			if ((bool)actor)
			{
				radarActor = actor;
				radarSymbol = node.GetValue("radarSymbol");
				_detPoint = new FixedPoint(node.GetValue<Vector3D>("detGlobalPoint"));
				signalStrength = node.GetValue<float>("signalStrength");
				locked = node.GetValue<bool>("locked");
				persistTime = node.GetValue<float>("persistTime");
				timeDetected = Time.time - node.GetValue<float>("timeDetectedElapsed");
			}
		}
	}

	public Actor myActor;

	public float receiverSensitivity = 1100f;

	public int maxContacts = 20;

	private int contactsCount;

	public RWRContact[] contacts;

	public bool useAntennae;

	public Transform[] antennae;

	public float antennaFov;

	public float overridePersistTime = -1f;

	private float lastLockTime;

	private float lastMissileLockTime;

	private bool hasSetupContactArray;

	public bool isLocked { get; private set; }

	public bool isMissileLocked { get; private set; }

	public bool quickloaded { get; private set; }

	public event RWRDelegate OnDetectPing;

	public event RWRDelegate OnLockDetectPing;

	public event Action OnEnableRWR;

	public event Action OnDisableRWR;

	public bool IsLockedBy(Actor a)
	{
		if (!a)
		{
			return false;
		}
		for (int i = 0; i < contacts.Length; i++)
		{
			RWRContact rWRContact = contacts[i];
			if (rWRContact.locked && rWRContact.radarActor == a)
			{
				return true;
			}
		}
		return false;
	}

	public List<Actor> GetDetectedActors(TeamOptions teamOption)
	{
		List<Actor> list = new List<Actor>();
		RWRContact[] array = contacts;
		foreach (RWRContact rWRContact in array)
		{
			if (rWRContact.active && (bool)rWRContact.radarActor && (teamOption == TeamOptions.BothTeams || (teamOption == TeamOptions.OtherTeam && rWRContact.radarActor.team != myActor.team) || (teamOption == TeamOptions.SameTeam && rWRContact.radarActor.team == myActor.team)))
			{
				list.Add(rWRContact.radarActor);
			}
		}
		return list;
	}

	private void OnEnable()
	{
		if (myActor == null)
		{
			myActor = GetComponentInParent<Actor>();
		}
		if ((bool)myActor && !myActor.rwrs.Contains(this))
		{
			myActor.rwrs.Add(this);
		}
		if (this.OnEnableRWR != null)
		{
			this.OnEnableRWR();
		}
	}

	public void SetActor(Actor a)
	{
		if ((bool)myActor)
		{
			myActor.rwrs.Remove(this);
		}
		myActor = a;
		if ((bool)myActor && !myActor.rwrs.Contains(this))
		{
			myActor.rwrs.Add(this);
		}
	}

	private void OnDisable()
	{
		if (this.OnDisableRWR != null)
		{
			this.OnDisableRWR();
		}
	}

	private void Awake()
	{
		if (myActor == null)
		{
			myActor = GetComponentInParent<Actor>();
		}
		if ((bool)myActor && !myActor.rwrs.Contains(this))
		{
			myActor.rwrs.Add(this);
		}
		SetupContactsArray();
	}

	private void Update()
	{
		if (isLocked && Time.time - lastLockTime > 1f)
		{
			isLocked = false;
		}
		if (isMissileLocked && Time.time - lastMissileLockTime > 1f)
		{
			isMissileLocked = false;
		}
	}

	private void SetupContactsArray()
	{
		if (!hasSetupContactArray)
		{
			contactsCount = maxContacts;
			contacts = new RWRContact[contactsCount];
			for (int i = 0; i < contactsCount; i++)
			{
				contacts[i] = new RWRContact();
			}
			hasSetupContactArray = true;
		}
	}

	public void Radar_OnLockPing(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength)
	{
		OnPing(detectedActor, sourceActor, radarSymbol, persistTime * 2f, radarPosition, signalStrength, locked: true);
	}

	public void Radar_OnDetect(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength)
	{
		OnPing(detectedActor, sourceActor, radarSymbol, persistTime, radarPosition, signalStrength, locked: false);
	}

	private void OnPing(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength, bool locked)
	{
		if (!(detectedActor == myActor) || !IsDetected(radarPosition, signalStrength))
		{
			return;
		}
		if (overridePersistTime > 0f)
		{
			persistTime = overridePersistTime;
		}
		int num = -1;
		for (int i = 0; i < contactsCount; i++)
		{
			if (contacts[i].radarActor == sourceActor)
			{
				contacts[i].UpdateExisting(persistTime, signalStrength, radarPosition, locked);
				if (locked)
				{
					isLocked = true;
					lastLockTime = Time.time;
					if (sourceActor.role == Actor.Roles.Missile)
					{
						isMissileLocked = true;
						lastMissileLockTime = Time.time;
					}
					if (this.OnLockDetectPing != null)
					{
						this.OnLockDetectPing(contacts[i]);
					}
				}
				else if (this.OnDetectPing != null)
				{
					this.OnDetectPing(contacts[i]);
				}
				return;
			}
			if (num < 0 && !contacts[i].active)
			{
				num = i;
			}
		}
		if (num < 0)
		{
			return;
		}
		contacts[num].UpdateNew(sourceActor, radarSymbol, persistTime, radarPosition, signalStrength, locked);
		if (locked)
		{
			isLocked = true;
			lastLockTime = Time.time;
			if (sourceActor.role == Actor.Roles.Missile)
			{
				isMissileLocked = true;
				lastMissileLockTime = Time.time;
			}
			if (this.OnLockDetectPing != null)
			{
				this.OnLockDetectPing(contacts[num]);
			}
		}
		else if (this.OnDetectPing != null)
		{
			this.OnDetectPing(contacts[num]);
		}
	}

	private bool IsDetected(Vector3 radarPosition, float signalStrength)
	{
		if (Radar.ADV_RADAR && signalStrength * 100f / (radarPosition - base.transform.position).sqrMagnitude < 1f / receiverSensitivity)
		{
			return false;
		}
		if (useAntennae)
		{
			float num = antennaFov / 2f;
			for (int i = 0; i < antennae.Length; i++)
			{
				if (Vector3.Angle(radarPosition - antennae[i].position, antennae[i].forward) < num)
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("ModuleRWR");
		qsNode.AddNode(configNode);
		for (int i = 0; i < contacts.Length; i++)
		{
			if (contacts[i] != null && contacts[i].active)
			{
				ConfigNode configNode2 = contacts[i].SaveToConfigNode("contact");
				configNode2.SetValue("cIdx", i);
				configNode.AddNode(configNode2);
			}
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		quickloaded = true;
		SetupContactsArray();
		string text = "ModuleRWR";
		if (!qsNode.HasNode(text))
		{
			return;
		}
		foreach (ConfigNode node in qsNode.GetNode(text).GetNodes("contact"))
		{
			int value = node.GetValue<int>("cIdx");
			contacts[value].LoadFromConfigNode(node);
		}
	}
}
