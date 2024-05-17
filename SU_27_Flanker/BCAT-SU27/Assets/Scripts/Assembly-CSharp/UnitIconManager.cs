using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class UnitIconManager : MonoBehaviour
{
	public enum MapIconTypes
	{
		Circle,
		Carrier,
		EnemyAir,
		EnemyGround,
		FriendlyAir,
		FriendlyGround,
		SAM,
		Missile,
		Structure
	}

	public delegate void IconEvent(UnitIcon icon);

	public class UnitIcon
	{
		public float timeCreated;

		private Transform iconTransform;

		private Transform mapIconTf;

		public List<Image> images = new List<Image>();

		public Text callsignText;

		private MapIconTypes currentIconType = (MapIconTypes)(-1);

		private Transform _mapVelTf;

		private float scale;

		private SpriteRenderer image;

		private Vector3 iconOffset;

		private int raycastThisFrame;

		private bool iconIsVisible;

		public Color color;

		public GameObject iconTemplate;

		public Actor actor { get; private set; }

		public Transform unitTransform => actor.transform;

		public bool stationary { get; private set; }

		public Transform mapIconTransform
		{
			get
			{
				if (!mapIconTf)
				{
					Debug.Log("UnitIconManager: Creating map icon for " + actor.actorName);
					mapIconTf = Object.Instantiate(instance.GetMapIconTemplate(actor.iconType)).transform;
					images.Clear();
					Image[] componentsInChildren = mapIconTf.GetComponentsInChildren<Image>(includeInactive: true);
					foreach (Image image in componentsInChildren)
					{
						images.Add(image);
						image.color = color;
					}
					if (actor.role == Actor.Roles.Air && !_mapVelTf)
					{
						images.Add(mapVelVectorTf.GetComponent<Image>());
					}
					mapIconSwapper = mapIconTf.GetComponent<MapIconImageSwapper>();
					mapIconTf.gameObject.SetActive(value: false);
					if ((VTOLMPUtils.IsMultiplayer() && (bool)actor.GetComponent<PlayerEntityIdentifier>()) || TestCallsignTexts(this))
					{
						GameObject gameObject = Object.Instantiate(instance.mpCallsignTemplate, mapIconTf);
						gameObject.transform.localPosition = Vector3.zero;
						gameObject.transform.localScale = Vector3.one;
						callsignText = gameObject.GetComponentInChildren<Text>(includeInactive: true);
						callsignText.text = string.Empty;
						callsignText.color = color;
						callsignText.gameObject.SetActive(value: false);
					}
				}
				return mapIconTf;
			}
		}

		public Transform mapVelVectorTf
		{
			get
			{
				if (!_mapVelTf && actor.finalCombatRole == Actor.Roles.Air)
				{
					_mapVelTf = Object.Instantiate(instance.mapVelVectorTemplate).transform;
					_mapVelTf.gameObject.SetActive(value: true);
					_mapVelTf.SetParent(mapIconTransform);
					_mapVelTf.localPosition = Vector3.zero;
					_mapVelTf.localRotation = Quaternion.identity;
					_mapVelTf.localScale = Vector3.one;
					Image component = mapVelVectorTf.GetComponent<Image>();
					Color color = this.color;
					color.a = component.color.a;
					component.color = color;
				}
				return _mapVelTf;
			}
		}

		public int priority { get; private set; }

		public float rotation => actor.iconRotation;

		private bool showIcons
		{
			get
			{
				if (actor.role == Actor.Roles.Missile)
				{
					return false;
				}
				return UnitIconManager.showIcons;
			}
		}

		public MapIconImageSwapper mapIconSwapper { get; private set; }

		private static bool TestCallsignTexts(UnitIcon icon)
		{
			if (Application.isEditor && instance.testCallsignTexts)
			{
				return icon.actor.role == Actor.Roles.Air;
			}
			return false;
		}

		public UnitIcon(Actor actor, float scale, GameObject iconTemplate, Color color, Vector3 offset, Sprite groupedSprite = null)
		{
			timeCreated = Time.time;
			this.actor = actor;
			if ((actor.role == Actor.Roles.Ground || actor.role == Actor.Roles.GroundArmor) && !actor.gameObject.GetComponentImplementing<GroundUnitMover>())
			{
				stationary = true;
			}
			this.iconTemplate = iconTemplate;
			this.color = color;
			if (showIcons)
			{
				iconTransform = Object.Instantiate(iconTemplate).transform;
				iconTransform.gameObject.SetActive(value: true);
				iconTransform.SetParent(iconTemplate.transform.parent, worldPositionStays: false);
				image = iconTransform.gameObject.GetComponentInChildren<SpriteRenderer>();
				image.color = color;
			}
			this.scale = scale;
			priority = actor.mapPriority;
			iconOffset = offset;
			mapIconTransform.gameObject.SetActive(value: false);
			raycastThisFrame = Random.Range(0, 10);
		}

		public void Update()
		{
			if (showIcons)
			{
				if (!iconTransform)
				{
					iconTransform = Object.Instantiate(iconTemplate).transform;
					iconTransform.gameObject.SetActive(value: true);
					iconTransform.SetParent(iconTemplate.transform.parent, worldPositionStays: false);
					image = iconTransform.gameObject.GetComponentInChildren<SpriteRenderer>();
					image.color = color;
				}
				if (raycastThisFrame > 9)
				{
					iconIsVisible = !Physics.Linecast(unitTransform.position, VRHead.position, 1);
					raycastThisFrame = 0;
					iconTransform.gameObject.SetActive(iconIsVisible);
				}
				else
				{
					raycastThisFrame++;
				}
				if (iconIsVisible && VRHead.instance != null)
				{
					iconTransform.position = unitTransform.TransformPoint(iconOffset);
					iconTransform.rotation = Quaternion.LookRotation(unitTransform.position - VRHead.position, Vector3.up);
					float num = Vector3.Distance(unitTransform.position, VRHead.instance.transform.position);
					float num2 = num * (VRHead.instance.fieldOfView / 180f);
					iconTransform.localScale = Vector3.one * scale * num2 * GLOBAL_ICON_SIZE;
					float iconVisibility = instance.GetIconVisibility(num);
					image.color = new Color(image.color.r, image.color.g, image.color.b, iconVisibility);
				}
			}
			else if ((bool)iconTransform)
			{
				Object.Destroy(iconTransform.gameObject);
			}
		}

		public void Destroy()
		{
			if ((bool)iconTransform)
			{
				Object.Destroy(iconTransform.gameObject);
			}
			if ((bool)mapIconTf)
			{
				Object.Destroy(mapIconTf.gameObject);
			}
		}
	}

	public GameObject iconTemplate;

	public GameObject iconAlliedTemplate;

	public GameObject[] mapIconTemplates;

	public GameObject mapVelVectorTemplate;

	public GameObject mpCallsignTemplate;

	public GameObject mapTgpIcon;

	public Color alliedColor;

	public Color enemyColor;

	public GameObject testCanvas;

	private List<UnitIcon> icons = new List<UnitIcon>();

	private Dictionary<Actor, UnitIcon> iconDict = new Dictionary<Actor, UnitIcon>();

	public IconEvent OnRegisterIcon;

	public IconEvent OnUnregisterIcon;

	public AnimationCurve iconVisibilityCurve;

	public float globalIconSize = 1f;

	public static float GLOBAL_ICON_SIZE;

	public bool testCallsignTexts;

	private static bool offlineShowIcons;

	private static bool isMP;

	private bool debug_offlineShowIcons;

	private bool debug_isMP;

	public static UnitIconManager instance { get; private set; }

	private static bool showIcons
	{
		get
		{
			if (!isMP)
			{
				return offlineShowIcons;
			}
			return VTOLMPSceneManager.unitIcons;
		}
	}

	private void Awake()
	{
		instance = this;
		GLOBAL_ICON_SIZE = globalIconSize;
	}

	private void Start()
	{
		iconTemplate.SetActive(value: false);
		iconAlliedTemplate.SetActive(value: false);
		testCanvas.SetActive(value: false);
		offlineShowIcons = GameSettings.CurrentSettings.GetBoolSetting("UNIT_ICONS");
		isMP = VTOLMPUtils.IsMultiplayer();
	}

	public void RegisterIcon(Actor a, float scale, Vector3 offset)
	{
		foreach (UnitIcon icon in icons)
		{
			if (icon.actor.GetInstanceID() == a.GetInstanceID())
			{
				return;
			}
		}
		Teams teams = Teams.Allied;
		if (VTOLMPUtils.IsMultiplayer())
		{
			teams = VTOLMPLobbyManager.localPlayerInfo.team;
		}
		Color color = ((a.team == teams) ? alliedColor : enemyColor);
		GameObject gameObject = ((a.team == teams) ? iconAlliedTemplate : iconTemplate);
		UnitIcon unitIcon = new UnitIcon(a, scale, gameObject, color, offset);
		icons.Add(unitIcon);
		iconDict.Add(a, unitIcon);
		if (OnRegisterIcon != null)
		{
			OnRegisterIcon(unitIcon);
		}
	}

	public void UpdateIconTeams()
	{
		Teams teams = Teams.Allied;
		if (VTOLMPUtils.IsMultiplayer())
		{
			teams = VTOLMPLobbyManager.localPlayerInfo.team;
		}
		foreach (UnitIcon icon in icons)
		{
			icon.color = ((icon.actor.team == teams) ? alliedColor : enemyColor);
			icon.iconTemplate = ((icon.actor.team == teams) ? iconAlliedTemplate : iconTemplate);
			switch (icon.actor.iconType)
			{
			case MapIconTypes.EnemyAir:
			case MapIconTypes.FriendlyAir:
				icon.actor.iconType = ((icon.actor.team == teams) ? MapIconTypes.FriendlyAir : MapIconTypes.EnemyAir);
				break;
			case MapIconTypes.EnemyGround:
			case MapIconTypes.FriendlyGround:
				icon.actor.iconType = ((icon.actor.team == teams) ? MapIconTypes.FriendlyGround : MapIconTypes.EnemyGround);
				break;
			}
			icon.Destroy();
		}
	}

	public void UnregisterIcon(Actor a)
	{
		iconDict.TryGetValue(a, out var value);
		if (value != null)
		{
			UnregisterIcon(value);
		}
	}

	private void UnregisterIcon(UnitIcon icon)
	{
		if (OnUnregisterIcon != null)
		{
			OnUnregisterIcon(icon);
		}
		iconDict.Remove(icon.actor);
		icons.Remove(icon);
		icon.Destroy();
	}

	public void UnregisterAll()
	{
		UnitIcon[] array = icons.ToArray();
		foreach (UnitIcon icon in array)
		{
			UnregisterIcon(icon);
		}
	}

	private void LateUpdate()
	{
		int count = icons.Count;
		for (int i = 0; i < count; i++)
		{
			icons[i].Update();
		}
	}

	public GameObject GetMapIconTemplate(MapIconTypes iconType)
	{
		return mapIconTemplates[(int)iconType];
	}

	private float GetIconVisibility(float distance)
	{
		return iconVisibilityCurve.Evaluate(distance) * Mathf.Lerp(1f, VTOLVRConstants.UNITICON_NVG_MUL, NightVisionGoggles.nvgEffectScale);
	}

	public UnitIcon[] GetRegisteredIcons()
	{
		return icons.ToArray();
	}
}
