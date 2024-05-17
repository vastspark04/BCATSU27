using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using VTNetworking;
using VTOLVR.Multiplayer;

public class DashMapDisplay : MonoBehaviour, IQSVehicleComponent
{
	private class MapGrid
	{
		public IntVector2 grid;

		public RenderTexture texture;

		public GameObject tileObject;
	}

	public float frameRate = 10f;

	public DynamicMapCamera dMapCam;

	public RectTransform mapDisplayTransform;

	public RectTransform waypointLineTransform;

	public Transform compassTf;

	public RectTransform gridTf;

	public float[] cameraOrthoSizes;

	public int orthoIdx;

	public GameObject mapTileTemplate;

	public Transform mapTransform;

	private Dictionary<IntVector2, MapGrid> mapTiles = new Dictionary<IntVector2, MapGrid>();

	private List<MapGrid> activeMapGrids = new List<MapGrid>();

	private Stack<MapGrid> tilePool = new Stack<MapGrid>();

	public Transform playerIconTf;

	public float unitIconScale = 1f;

	public GameObject crosshairObject;

	private float mapSize;

	private float camFactor;

	public float mapScale = 1f;

	public Transform referenceTransform;

	private List<UnitIconManager.UnitIcon> unitIcons = new List<UnitIconManager.UnitIcon>();

	private List<UnitIconManager.UnitIcon> displayedIcons = new List<UnitIconManager.UnitIcon>();

	private Transform tgpIconTf;

	private double worldToMap;

	private float heading;

	private bool isOffset;

	private Vector3 offsetFocus;

	public GameObject waypointDistObject;

	public Text waypointDistText;

	private MeasurementManager measurements;

	private FixedPoint tgpPosition;

	private MFDPortalPage portalPage;

	private WeaponManager wm;

	[Header("GPS")]
	public GameObject gpsIconTemplate;

	public Transform gpsSelectTf;

	public UILineRenderer gpsLine;

	private bool displayGps = true;

	private List<Transform> gpsIconTfs = new List<Transform>();

	private Vector2[] gpsLinePoints;

	private float gpsTargetAlt;

	private bool gpsDirty = true;

	[Header("Taxi")]
	public UILineRenderer taxiLine;

	[Header("Error Message")]
	public GameObject errorObject;

	public Text errorText;

	[Header("Bullseye")]
	public Transform bullseyeTf;

	public GameObject bullseyeTextObj;

	public Text bullseyeText;

	[Header("Height Display")]
	public DashMapHeightDisplay heightDisplay;

	[Header("Multicrew")]
	public MultiUserVehicleSync muvs;

	private Coroutine frameUpdateRoutine;

	private bool LAST_KNOWN_POSITION_LOGIC = true;

	private bool doGrouping = true;

	private bool doFrameCulling = true;

	private StringBuilder groupStringBuilder = new StringBuilder();

	private Coroutine setupTilesRoutine;

	private Coroutine tileDespawnerRoutine;

	private bool resetGenLoop;

	private float offsetFocusAmt = 0.2f;

	private Coroutine errorRoutine;

	public bool northUp { get; private set; }

	public MFDPage mfdPage { get; private set; }

	private void Awake()
	{
		if (!heightDisplay)
		{
			heightDisplay = GetComponentInChildren<DashMapHeightDisplay>();
		}
		measurements = GetComponentInParent<MeasurementManager>();
		mfdPage = GetComponent<MFDPage>();
		portalPage = GetComponent<MFDPortalPage>();
		wm = GetComponentInParent<WeaponManager>();
		errorObject.SetActive(value: false);
		int num = Mathf.CeilToInt(cameraOrthoSizes[cameraOrthoSizes.Length - 1] / dMapCam.worldTileSize);
		num *= num;
		for (int i = 0; i < num; i++)
		{
			MapGrid mapGrid = new MapGrid();
			GameObject gameObject = UnityEngine.Object.Instantiate(mapTileTemplate);
			gameObject.transform.SetParent(mapTileTemplate.transform.parent, worldPositionStays: false);
			gameObject.transform.localScale = mapTileTemplate.transform.localScale;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.SetActive(value: false);
			RenderTexture renderTexture = new RenderTexture(dMapCam.resolution, dMapCam.resolution, 16);
			renderTexture.filterMode = FilterMode.Point;
			gameObject.GetComponent<RawImage>().texture = renderTexture;
			mapGrid.tileObject = gameObject;
			mapGrid.texture = renderTexture;
			tilePool.Push(mapGrid);
		}
		gridTf.SetAsLastSibling();
		if ((bool)mfdPage)
		{
			mfdPage.OnInputAxis.AddListener(MoveOffset);
			mfdPage.OnActivatePage.AddListener(OnActivatePage);
		}
		if ((bool)portalPage)
		{
			portalPage.OnInputAxis.AddListener(MoveOffset);
			portalPage.OnSetPageStateEvent += PortalPage_OnSetPageStateEvent;
		}
		if ((bool)bullseyeTf)
		{
			bullseyeTf.gameObject.SetActive(value: false);
		}
		if ((bool)bullseyeTextObj)
		{
			bullseyeTextObj.SetActive(value: false);
		}
	}

	private void Start()
	{
		if (!UnitIconManager.instance)
		{
			base.enabled = false;
			return;
		}
		UnitIconManager.UnitIcon[] registeredIcons = UnitIconManager.instance.GetRegisteredIcons();
		foreach (UnitIconManager.UnitIcon icon in registeredIcons)
		{
			OnRegisterIcon(icon);
		}
		UnitIconManager instance = UnitIconManager.instance;
		instance.OnRegisterIcon = (UnitIconManager.IconEvent)Delegate.Combine(instance.OnRegisterIcon, new UnitIconManager.IconEvent(OnRegisterIcon));
		UnitIconManager instance2 = UnitIconManager.instance;
		instance2.OnUnregisterIcon = (UnitIconManager.IconEvent)Delegate.Combine(instance2.OnUnregisterIcon, new UnitIconManager.IconEvent(OnUnregisterIcon));
		tgpIconTf = UnityEngine.Object.Instantiate(UnitIconManager.instance.mapTgpIcon).transform;
		tgpIconTf.SetParent(mapDisplayTransform, worldPositionStays: false);
		tgpIconTf.localScale = Vector3.one;
		tgpIconTf.localRotation = Quaternion.identity;
		HideTGPIcon();
		SetOrthoSize();
		UpdateTargetAltLabel();
		wm.gpsSystem.onGPSTargetsChanged.AddListener(OnGPSTargetsChanged);
		OnGPSTargetsChanged();
		gpsSelectTf.gameObject.SetActive(value: false);
		gpsIconTemplate.gameObject.SetActive(value: false);
		gpsLine.gameObject.SetActive(value: false);
		measurements.OnChangedAltitudeMode += Measurements_OnChangedAltitudeMode;
	}

	private void PortalPage_OnSetPageStateEvent(MFDPortalPage.PageStates pageState)
	{
		Debug.Log("Setting map page state: " + pageState);
		if (pageState != MFDPortalPage.PageStates.SubSized && pageState != MFDPortalPage.PageStates.Minimized)
		{
			OnActivatePage();
			SetupTiles();
			if (frameUpdateRoutine != null)
			{
				StopCoroutine(frameUpdateRoutine);
			}
			frameUpdateRoutine = StartCoroutine(FrameUpdateRoutine());
			SetOrthoSize();
			return;
		}
		if (frameUpdateRoutine != null)
		{
			StopCoroutine(frameUpdateRoutine);
		}
		if (setupTilesRoutine != null)
		{
			StopCoroutine(setupTilesRoutine);
		}
		if (tileDespawnerRoutine != null)
		{
			StopCoroutine(tileDespawnerRoutine);
		}
	}

	private void Measurements_OnChangedAltitudeMode()
	{
		if (measurements.altitudeMode == MeasurementManager.AltitudeModes.Meters)
		{
			gpsTargetAlt = Mathf.Round(gpsTargetAlt / 100f) * 100f;
		}
		else
		{
			float num = MeasurementManager.DistToFeet(gpsTargetAlt);
			num = Mathf.Round(num / 250f) * 250f;
			gpsTargetAlt = num / MeasurementManager.DistToFeet(1f);
		}
		UpdateTargetAltLabel();
	}

	private void UpdateTargetAltLabel()
	{
		if ((bool)mfdPage)
		{
			mfdPage.SetText("tgtAltLabel", measurements.FormattedAltitude(gpsTargetAlt) + measurements.AltitudeLabel());
		}
		if ((bool)portalPage)
		{
			portalPage.SetText("tgtAltLabel", measurements.FormattedAltitude(gpsTargetAlt) + measurements.AltitudeLabel());
		}
	}

	private void OnActivatePage()
	{
		UpdateTargetAltLabel();
		OnGPSTargetsChanged();
	}

	private void OnEnable()
	{
		if ((bool)mfdPage)
		{
			SetupTiles();
			frameUpdateRoutine = StartCoroutine(FrameUpdateRoutine());
		}
	}

	private void OnGPSTargetsChanged()
	{
		gpsDirty = true;
		errorObject.SetActive(value: false);
	}

	private void UpdateTaxiPath()
	{
		if (!taxiLine)
		{
			return;
		}
		List<AirbaseNavNode> taxiNodes = WaypointManager.instance.taxiNodes;
		if (taxiNodes != null && taxiNodes.Count > 0)
		{
			taxiLine.gameObject.SetActive(value: true);
			Vector2[] array = new Vector2[taxiNodes.Count];
			int num = 0;
			foreach (AirbaseNavNode item in taxiNodes)
			{
				array[num] = taxiLine.transform.parent.InverseTransformPoint(mapTransform.TransformPoint(WorldToMapPoint(item.transform.position)));
				num++;
			}
			taxiLine.Points = array;
			taxiLine.SetVerticesDirty();
		}
		else
		{
			taxiLine.gameObject.SetActive(value: false);
		}
	}

	private void UpdateGPSIcons()
	{
		if (gpsDirty)
		{
			if (displayGps && !wm.gpsSystem.noGroups)
			{
				int count = wm.gpsSystem.currentGroup.targets.Count;
				if (count > gpsIconTfs.Count)
				{
					int num = count - gpsIconTfs.Count;
					for (int i = 0; i < num; i++)
					{
						Transform transform = UnityEngine.Object.Instantiate(gpsIconTemplate, gpsIconTemplate.transform.parent).transform;
						transform.localScale = gpsIconTemplate.transform.localScale;
						transform.localRotation = gpsIconTemplate.transform.localRotation;
						gpsIconTfs.Add(transform);
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
					gpsSelectTf.gameObject.SetActive(value: true);
					if (wm.gpsSystem.currentGroup.isPath)
					{
						gpsLine.gameObject.SetActive(value: true);
						gpsLinePoints = new Vector2[count];
					}
					else
					{
						gpsLine.gameObject.SetActive(value: false);
					}
				}
				else
				{
					gpsSelectTf.gameObject.SetActive(value: false);
					gpsLine.gameObject.SetActive(value: false);
				}
			}
			else
			{
				foreach (Transform gpsIconTf in gpsIconTfs)
				{
					if ((bool)gpsIconTf)
					{
						gpsIconTf.gameObject.SetActive(value: false);
					}
				}
				gpsSelectTf.gameObject.SetActive(value: false);
				gpsLine.gameObject.SetActive(value: false);
			}
			gpsDirty = false;
		}
		if (!displayGps || wm.gpsSystem.noGroups)
		{
			return;
		}
		for (int k = 0; k < wm.gpsSystem.currentGroup.targets.Count; k++)
		{
			gpsIconTfs[k].position = mapTransform.TransformPoint(WorldToMapPoint(wm.gpsSystem.currentGroup.targets[k].worldPosition));
			if (k == wm.gpsSystem.currentGroup.currentTargetIdx)
			{
				gpsSelectTf.position = gpsIconTfs[k].position;
			}
			if (wm.gpsSystem.currentGroup.isPath)
			{
				gpsLinePoints[k] = gpsLine.transform.parent.InverseTransformPoint(gpsIconTfs[k].position);
			}
		}
		if (wm.gpsSystem.currentGroup.isPath)
		{
			gpsLine.Points = gpsLinePoints;
			gpsLine.SetVerticesDirty();
		}
	}

	public void ToggleGPSDisplay()
	{
		displayGps = !displayGps;
		OnGPSTargetsChanged();
	}

	public void ToggleRotMode()
	{
		northUp = !northUp;
		UpdateMap();
	}

	private void UpdateMap()
	{
		UpdateMapTransform();
		UpdateIcons();
		UpdateWaypointLine();
		UpdateTaxiPath();
		if ((bool)heightDisplay)
		{
			heightDisplay.UpdateDisplay();
		}
	}

	private IEnumerator FrameUpdateRoutine()
	{
		yield return null;
		WaitForSeconds wait = new WaitForSeconds(1f / frameRate);
		while (base.enabled)
		{
			yield return wait;
			UpdateMap();
		}
	}

	private void OnDestroy()
	{
		if (activeMapGrids != null)
		{
			foreach (MapGrid activeMapGrid in activeMapGrids)
			{
				if (activeMapGrid != null && activeMapGrid.texture != null)
				{
					activeMapGrid.texture.Release();
					UnityEngine.Object.Destroy(activeMapGrid.texture);
				}
			}
		}
		activeMapGrids.Clear();
		mapTiles.Clear();
		if (tilePool != null)
		{
			while (tilePool.Count > 0)
			{
				MapGrid mapGrid = tilePool.Pop();
				if (mapGrid != null && mapGrid.texture != null)
				{
					mapGrid.texture.Release();
					UnityEngine.Object.Destroy(mapGrid.texture);
				}
			}
		}
		if ((bool)UnitIconManager.instance)
		{
			UnitIconManager instance = UnitIconManager.instance;
			instance.OnRegisterIcon = (UnitIconManager.IconEvent)Delegate.Remove(instance.OnRegisterIcon, new UnitIconManager.IconEvent(OnRegisterIcon));
		}
	}

	public void ZoomOut()
	{
		if (orthoIdx < cameraOrthoSizes.Length - 1)
		{
			orthoIdx++;
			SetOrthoSize();
			resetGenLoop = true;
		}
	}

	public void ZoomIn()
	{
		if (orthoIdx > 0)
		{
			orthoIdx--;
			SetOrthoSize();
			resetGenLoop = true;
		}
	}

	private void SetOrthoSize()
	{
		mapScale = camFactor / cameraOrthoSizes[orthoIdx];
	}

	private void OnRegisterIcon(UnitIconManager.UnitIcon icon)
	{
		unitIcons.Add(icon);
		icon.mapIconTransform.SetParent(mapDisplayTransform, worldPositionStays: false);
		icon.mapIconTransform.localScale = Vector3.one * unitIconScale;
		icon.mapIconTransform.localRotation = Quaternion.identity;
		icon.mapIconTransform.SetAsFirstSibling();
		unitIcons.Sort(PrioritySorter);
	}

	private int PrioritySorter(UnitIconManager.UnitIcon a, UnitIconManager.UnitIcon b)
	{
		if (a.priority == b.priority)
		{
			return a.actor.designation.CompareTo(b.actor.designation);
		}
		return a.priority.CompareTo(b.priority);
	}

	private void OnUnregisterIcon(UnitIconManager.UnitIcon icon)
	{
		unitIcons.Remove(icon);
	}

	private bool IsVisibleInFrame(UnitIconManager.UnitIcon icon, Vector3 newMapPos)
	{
		Rect rect = ((RectTransform)base.transform).rect;
		if (!rect.Contains(base.transform.InverseTransformPoint(mapTransform.TransformPoint(newMapPos))))
		{
			if (icon.mapIconTransform.gameObject.activeSelf)
			{
				return rect.Contains(base.transform.InverseTransformPoint(icon.mapIconTransform.position));
			}
			return false;
		}
		return true;
	}

	private void UpdateIcons()
	{
		float currTime = (VTOLMPUtils.IsMultiplayer() ? VTNetworkManager.GetNetworkTimestamp() : Time.time);
		int count = unitIcons.Count;
		for (int i = 0; i < count; i++)
		{
			ProcessIcon(unitIcons[i], currTime);
		}
		displayedIcons.Clear();
		compassTf.SetAsLastSibling();
		if (northUp)
		{
			compassTf.localRotation = Quaternion.identity;
		}
		else
		{
			compassTf.localRotation = Quaternion.Euler(0f, 0f, heading);
		}
		if ((bool)tgpIconTf && tgpIconTf.gameObject.activeInHierarchy)
		{
			tgpIconTf.SetAsLastSibling();
			tgpIconTf.position = mapTransform.TransformPoint(WorldToMapPoint(tgpPosition.point));
		}
		UpdateGPSIcons();
		playerIconTf.SetAsLastSibling();
		if (!bullseyeTf)
		{
			return;
		}
		if ((bool)WaypointManager.instance.bullseye)
		{
			bullseyeTf.gameObject.SetActive(value: true);
			bullseyeTf.position = mapTransform.TransformPoint(WorldToMapPoint(WaypointManager.instance.bullseye.position));
			if ((bool)bullseyeTextObj)
			{
				Vector3 target = VTMapManager.GlobalToWorldPoint(GetGlobalFocusPoint());
				WaypointManager.instance.GetBullsBRA(target, out var bearing, out var range, out var _);
				range = measurements.ConvertedDistance(range);
				if (measurements.distanceMode == MeasurementManager.DistanceModes.Meters)
				{
					range /= 1000f;
				}
				bullseyeTextObj.SetActive(value: true);
				bullseyeText.text = $"{Mathf.Round(bearing)}/{Mathf.Round(range)}";
			}
		}
		else
		{
			bullseyeTf.gameObject.SetActive(value: false);
			if ((bool)bullseyeTextObj)
			{
				bullseyeTextObj.SetActive(value: false);
			}
		}
	}

	private void ProcessIcon(UnitIconManager.UnitIcon icon, float currTime)
	{
		if (!icon.mapIconTransform)
		{
			return;
		}
		float num = 1f;
		bool flag = LAST_KNOWN_POSITION_LOGIC && icon.actor.team != wm.actor.team && !icon.stationary && icon.actor.detectionMode != AIUnitSpawn.InitialDetectionModes.Force_Detected;
		Vector3 vector;
		if (flag)
		{
			float num2 = currTime - icon.actor.LastSeenTime(wm.actor.team);
			float num3 = 20f;
			if (Time.time - icon.timeCreated < 1f || num2 > num3 + 60f)
			{
				num = 0f;
				icon.mapIconTransform.gameObject.SetActive(value: false);
				vector = Vector3.zero;
			}
			else
			{
				vector = WorldToMapPoint(icon.actor.LastKnownPosition(wm.actor.team));
				if (icon.actor.role == Actor.Roles.Air)
				{
					num3 = 5f;
				}
				num = Mathf.Clamp(1f - (num2 - num3) / 10f, 0.1f, 1f);
				Color color = icon.color;
				color.a = num;
				for (int i = 0; i < icon.images.Count; i++)
				{
					if ((bool)icon.images[i])
					{
						icon.images[i].color = color;
					}
				}
			}
		}
		else
		{
			vector = WorldToMapPoint(icon.unitTransform.position);
		}
		bool flag2 = false;
		if (doFrameCulling && !IsVisibleInFrame(icon, vector))
		{
			num = 0f;
			icon.mapIconTransform.gameObject.SetActive(value: false);
		}
		else
		{
			icon.mapIconTransform.position = mapTransform.TransformPoint(vector);
			bool flag3 = false;
			if (doGrouping)
			{
				groupStringBuilder.Clear();
				for (int j = 0; j < displayedIcons.Count; j++)
				{
					if (displayedIcons[j].actor.team != icon.actor.team || displayedIcons[j].actor.iconType != icon.actor.iconType || !((displayedIcons[j].mapIconTransform.localPosition - icon.mapIconTransform.localPosition).sqrMagnitude < 25f))
					{
						continue;
					}
					if (displayedIcons[j].priority > icon.priority)
					{
						flag3 = true;
						num = 0f;
						if ((bool)displayedIcons[j].mapIconSwapper)
						{
							displayedIcons[j].mapIconSwapper.SetGrouped(grouped: true);
						}
					}
					else
					{
						flag2 = true;
						displayedIcons[j].mapIconTransform.gameObject.SetActive(value: false);
						if ((bool)icon.callsignText && (bool)displayedIcons[j].callsignText)
						{
							groupStringBuilder.AppendLine(displayedIcons[j].callsignText.text.Trim());
						}
						displayedIcons.RemoveAt(j);
						j--;
					}
					if (flag3)
					{
						break;
					}
				}
			}
			if (flag3)
			{
				icon.mapIconTransform.gameObject.SetActive(value: false);
				if ((bool)icon.callsignText)
				{
					icon.callsignText.gameObject.SetActive(value: false);
				}
				return;
			}
			if ((bool)icon.callsignText)
			{
				if (icon.actor.team == wm.actor.team)
				{
					icon.callsignText.gameObject.SetActive(value: true);
					if (flag2)
					{
						Actor.Designation designation = icon.actor.designation;
						groupStringBuilder.AppendLine($"{designation.letter.ToString()[0]}{designation.num1}-{designation.num2}");
						icon.callsignText.text = groupStringBuilder.ToString();
					}
					else
					{
						Actor.Designation designation2 = icon.actor.designation;
						icon.callsignText.text = $"{designation2.letter.ToString()[0]}{designation2.num1}-{designation2.num2}";
					}
				}
				else
				{
					icon.callsignText.gameObject.SetActive(value: false);
				}
			}
		}
		if (!(num > 0.09f))
		{
			return;
		}
		displayedIcons.Add(icon);
		icon.mapIconTransform.gameObject.SetActive(value: true);
		if ((bool)icon.mapIconSwapper)
		{
			icon.mapIconSwapper.SetGrouped(flag2);
		}
		if (icon.actor.finalCombatRole == Actor.Roles.Air)
		{
			Vector3 vector2 = icon.actor.velocity;
			if (flag)
			{
				vector2 = icon.actor.LastKnownVelocity(wm.actor.team);
			}
			vector2.y = 0f;
			if (vector2.sqrMagnitude > 10000f)
			{
				icon.mapVelVectorTf.gameObject.SetActive(value: true);
				Vector3 upwards = mapTransform.TransformPoint(WorldToMapPoint(icon.unitTransform.position + vector2 * 10000f)) - icon.mapIconTransform.position;
				icon.mapVelVectorTf.rotation = Quaternion.LookRotation(icon.mapIconTransform.forward, upwards);
			}
			else
			{
				icon.mapVelVectorTf.gameObject.SetActive(value: false);
			}
		}
		if (icon.rotation < 360f && icon.rotation > -360f)
		{
			if (northUp)
			{
				icon.mapIconTransform.localRotation = Quaternion.Euler(0f, 0f, 0f - icon.rotation);
			}
			else
			{
				icon.mapIconTransform.localRotation = Quaternion.Euler(0f, 0f, 0f - icon.rotation + heading);
			}
		}
	}

	public void ShowTGPIcon(Vector3 worldPos)
	{
		if ((bool)tgpIconTf)
		{
			tgpPosition.point = worldPos;
			tgpIconTf.gameObject.SetActive(value: true);
		}
	}

	public void HideTGPIcon()
	{
		if ((bool)tgpIconTf)
		{
			tgpIconTf.gameObject.SetActive(value: false);
		}
	}

	private void UpdateWaypointLine()
	{
		if ((bool)WaypointManager.instance.currentWaypoint)
		{
			waypointLineTransform.gameObject.SetActive(value: true);
			Vector3 position = WaypointManager.instance.currentWaypoint.position;
			Vector3 vector = waypointLineTransform.parent.InverseTransformPoint(mapTransform.TransformPoint(WorldToMapPoint(position)));
			waypointLineTransform.transform.position = playerIconTf.transform.position;
			Vector3 upwards = vector - waypointLineTransform.localPosition;
			waypointLineTransform.localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
			float size = Vector3.Distance(waypointLineTransform.localPosition, vector);
			waypointLineTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
			waypointDistObject.SetActive(value: true);
			float distance = Vector3.Distance(base.transform.position, WaypointManager.instance.currentWaypoint.position);
			waypointDistText.text = measurements.FormattedDistance(distance);
		}
		else
		{
			waypointLineTransform.gameObject.SetActive(value: false);
			waypointDistObject.SetActive(value: false);
		}
	}

	private void SetupTiles()
	{
		if (setupTilesRoutine != null)
		{
			StopCoroutine(setupTilesRoutine);
		}
		setupTilesRoutine = StartCoroutine(SetupTilesRoutine());
		if (tileDespawnerRoutine != null)
		{
			StopCoroutine(tileDespawnerRoutine);
		}
		tileDespawnerRoutine = StartCoroutine(TileDespawnerRoutine());
	}

	private IEnumerator SetupTilesRoutine()
	{
		int num = 20;
		float num2 = dMapCam.worldTileSize * (float)num;
		float tileSize = mapTileTemplate.GetComponent<RectTransform>().rect.width;
		camFactor = dMapCam.worldTileSize / 2f;
		mapTileTemplate.SetActive(value: false);
		mapSize = tileSize * (float)num;
		worldToMap = (double)mapSize / (double)num2;
		WaitForEndOfFrame waitBeforeRender = new WaitForEndOfFrame();
		while (!WaterPhysics.instance)
		{
			yield return null;
		}
		while (base.enabled)
		{
			IntVector2 mapGrid = CurrentMapGrid();
			int gridViewSize = GridViewSize();
			for (int x = mapGrid.x - gridViewSize; x <= mapGrid.x + gridViewSize; x++)
			{
				if (resetGenLoop)
				{
					break;
				}
				for (int y = mapGrid.y - gridViewSize; y <= mapGrid.y + gridViewSize; y++)
				{
					if (resetGenLoop)
					{
						break;
					}
					IntVector2 intVector = new IntVector2(x, y);
					if (!mapTiles.ContainsKey(intVector))
					{
						MapGrid mg;
						if (tilePool.Count > 0)
						{
							mg = tilePool.Pop();
						}
						else
						{
							mg = new MapGrid();
							GameObject gameObject = UnityEngine.Object.Instantiate(mapTileTemplate);
							gameObject.transform.SetParent(mapTileTemplate.transform.parent, worldPositionStays: false);
							gameObject.transform.localScale = mapTileTemplate.transform.localScale;
							gameObject.transform.localRotation = Quaternion.identity;
							RenderTexture renderTexture = new RenderTexture(dMapCam.resolution, dMapCam.resolution, 16);
							renderTexture.filterMode = FilterMode.Point;
							gameObject.GetComponent<RawImage>().texture = renderTexture;
							mg.tileObject = gameObject;
							mg.texture = renderTexture;
							gridTf.SetAsLastSibling();
						}
						mg.grid = intVector;
						float num3 = (0f - mapSize) / 2f + tileSize / 2f;
						float x2 = num3 + (float)x * tileSize;
						float y2 = num3 + (float)y * tileSize;
						Vector3 pos = new Vector3(x2, y2, 0f);
						mg.tileObject.transform.localPosition = pos;
						yield return waitBeforeRender;
						dMapCam.GenerateTexture(MapToWorldPoint(pos), mg.texture);
						mg.tileObject.SetActive(value: true);
						mapTiles.Add(mg.grid, mg);
						activeMapGrids.Add(mg);
						yield return null;
					}
				}
			}
			resetGenLoop = false;
			yield return null;
		}
	}

	private IEnumerator TileDespawnerRoutine()
	{
		while (base.enabled)
		{
			bool flag = false;
			MapGrid mapGrid = null;
			for (int i = 0; i < activeMapGrids.Count; i++)
			{
				MapGrid mapGrid2 = activeMapGrids[i];
				if (!IsGridInView(mapGrid2.grid))
				{
					flag = true;
					mapGrid = mapGrid2;
					break;
				}
			}
			if (flag)
			{
				MapGrid mapGrid3 = mapGrid;
				mapGrid3.tileObject.SetActive(value: false);
				tilePool.Push(mapGrid3);
				mapTiles.Remove(mapGrid.grid);
				activeMapGrids.Remove(mapGrid3);
			}
			yield return null;
		}
	}

	private IntVector2 WorldPositionToGrid(Vector3 worldPosition)
	{
		Vector3 toVector = (FloatingOrigin.accumOffset + worldPosition).toVector3;
		toVector /= dMapCam.worldTileSize;
		return new IntVector2(Mathf.FloorToInt(toVector.x), Mathf.FloorToInt(toVector.z));
	}

	private bool IsGridInView(IntVector2 grid)
	{
		IntVector2 intVector = CurrentMapGrid();
		int num = GridViewSize() + 1;
		if (Mathf.Abs(grid.x - intVector.x) <= num)
		{
			return Mathf.Abs(grid.y - intVector.y) <= num;
		}
		return false;
	}

	private int GridViewSize()
	{
		return Mathf.CeilToInt(cameraOrthoSizes[orthoIdx] / dMapCam.worldTileSize);
	}

	private IntVector2 CurrentMapGrid()
	{
		return WorldPositionToGrid(MapToWorldPoint(mapTransform.InverseTransformPoint(mapDisplayTransform.position)));
	}

	private void UpdateMapTransform()
	{
		Vector3 forward = referenceTransform.forward;
		forward.y = 0f;
		heading = Vector3.Angle(Vector3.forward, forward);
		if (Vector3.Dot(forward, Vector3.right) < 0f)
		{
			heading *= -1f;
		}
		mapTransform.localRotation = Quaternion.identity;
		Vector3 localScale = mapScale * Vector3.one;
		localScale.z = 1f;
		mapTransform.localScale = localScale;
		Vector3 toVector = PlayerPosition3D().toVector3;
		if (isOffset)
		{
			toVector = offsetFocus;
		}
		toVector.x += dMapCam.worldTileSize / 2f;
		toVector.z += dMapCam.worldTileSize / 2f;
		toVector *= (float)worldToMap;
		toVector -= new Vector3(mapSize / 2f, 0f, mapSize / 2f);
		Vector3 localPosition = -toVector;
		localPosition.y = localPosition.z;
		localPosition *= mapScale;
		localPosition.z = 0f;
		mapTransform.localPosition = localPosition;
		if (northUp)
		{
			mapTransform.localRotation = Quaternion.identity;
			playerIconTf.localRotation = Quaternion.Euler(0f, 0f, 0f - heading);
		}
		else
		{
			mapTransform.RotateAround(mapTransform.parent.position, mapTransform.parent.forward, heading);
			playerIconTf.localRotation = Quaternion.identity;
		}
		if (isOffset)
		{
			playerIconTf.position = mapTransform.TransformPoint(WorldToMapPoint(referenceTransform.position));
			crosshairObject.SetActive(value: true);
		}
		else
		{
			playerIconTf.localPosition = Vector3.zero;
			crosshairObject.SetActive(value: false);
		}
	}

	public Vector3D GetGlobalFocusPoint()
	{
		if (isOffset)
		{
			return new Vector3D(offsetFocus);
		}
		return PlayerPosition3D();
	}

	public void MoveOffset(Vector3 input)
	{
		if (!isOffset)
		{
			isOffset = true;
			offsetFocus = PlayerPosition3D().toVector3;
		}
		Vector3 vector = new Vector3(input.x, 0f, input.y);
		if (!northUp)
		{
			vector = Quaternion.AngleAxis(heading, Vector3.up) * vector;
		}
		offsetFocus += offsetFocusAmt * cameraOrthoSizes[orthoIdx] * 8f * Time.deltaTime * vector;
		UpdateMap();
	}

	private Vector3 WorldToMapPoint(Vector3 worldPosition)
	{
		Vector3D vector3D = FloatingOrigin.accumOffset + worldPosition;
		vector3D.x += dMapCam.worldTileSize / 2f;
		vector3D.z += dMapCam.worldTileSize / 2f;
		vector3D *= worldToMap;
		Vector3 toVector = (vector3D - new Vector3D(mapSize / 2f, 0.0, mapSize / 2f)).toVector3;
		toVector.y = toVector.z;
		toVector.z = 0f;
		return toVector;
	}

	private Vector3 MapToWorldPoint(Vector3 mapPosition)
	{
		Vector3D vector3D = new Vector3D(mapPosition.x, 0.0, mapPosition.y);
		vector3D += new Vector3D(mapSize / 2f, 0.0, mapSize / 2f);
		vector3D /= worldToMap;
		vector3D.z -= dMapCam.worldTileSize / 2f;
		vector3D.x -= dMapCam.worldTileSize / 2f;
		return (vector3D - FloatingOrigin.accumOffset).toVector3;
	}

	public Vector3D PlayerPosition3D()
	{
		return FloatingOrigin.accumOffset + referenceTransform.position;
	}

	public void OffsetLeft()
	{
		if (!isOffset)
		{
			isOffset = true;
			offsetFocus = PlayerPosition3D().toVector3;
		}
		Vector3 vector = Vector3.left;
		if (!northUp)
		{
			vector = -Vector3.Cross(Vector3.up, FlightSceneManager.instance.playerActor.transform.forward).normalized;
		}
		offsetFocus += offsetFocusAmt * cameraOrthoSizes[orthoIdx] * vector;
	}

	public void OffsetRight()
	{
		if (!isOffset)
		{
			isOffset = true;
			offsetFocus = PlayerPosition3D().toVector3;
		}
		Vector3 vector = Vector3.right;
		if (!northUp)
		{
			vector = Vector3.Cross(Vector3.up, FlightSceneManager.instance.playerActor.transform.forward).normalized;
		}
		offsetFocus += offsetFocusAmt * cameraOrthoSizes[orthoIdx] * vector;
	}

	public void OffsetUp()
	{
		if (!isOffset)
		{
			isOffset = true;
			offsetFocus = PlayerPosition3D().toVector3;
		}
		Vector3 forward = Vector3.forward;
		if (!northUp)
		{
			forward = FlightSceneManager.instance.playerActor.transform.forward;
			forward.y = 0f;
			forward.Normalize();
		}
		offsetFocus += offsetFocusAmt * cameraOrthoSizes[orthoIdx] * forward;
	}

	public void OffsetDown()
	{
		if (!isOffset)
		{
			isOffset = true;
			offsetFocus = PlayerPosition3D().toVector3;
		}
		Vector3 vector = Vector3.back;
		if (!northUp)
		{
			vector = -FlightSceneManager.instance.playerActor.transform.forward;
			vector.y = 0f;
			vector.Normalize();
		}
		offsetFocus += offsetFocusAmt * cameraOrthoSizes[orthoIdx] * vector;
	}

	public void ResetOffset()
	{
		isOffset = false;
		resetGenLoop = true;
	}

	public void SetFuelWaypoint()
	{
		WaypointManager.instance.SetFuelWaypoint();
	}

	public void SetRTBWaypoint()
	{
		WaypointManager.instance.SetRTBWaypoint();
	}

	public void IncreaseTargetAlt()
	{
		if (measurements.altitudeMode == MeasurementManager.AltitudeModes.Meters)
		{
			gpsTargetAlt += 100f;
		}
		else
		{
			gpsTargetAlt += 250f / MeasurementManager.ConvertDistance(1f, MeasurementManager.DistanceModes.Feet);
		}
		UpdateTargetAltLabel();
	}

	public void DecreaseTargetAlt()
	{
		if (measurements.altitudeMode == MeasurementManager.AltitudeModes.Meters)
		{
			gpsTargetAlt -= 100f;
		}
		else
		{
			gpsTargetAlt -= 250f / MeasurementManager.ConvertDistance(1f, MeasurementManager.DistanceModes.Feet);
		}
		if (gpsTargetAlt < 0f)
		{
			gpsTargetAlt = 0f;
		}
		UpdateTargetAltLabel();
	}

	public void SendGPSTarget()
	{
		int num;
		if ((bool)muvs && VTOLMPUtils.IsMultiplayer())
		{
			num = ((!muvs.isMine) ? 1 : 0);
			if (num != 0)
			{
				goto IL_004b;
			}
		}
		else
		{
			num = 0;
		}
		if (wm.gpsSystem.noGroups)
		{
			wm.gpsSystem.CreateCustomGroup();
		}
		goto IL_004b;
		IL_004b:
		Vector3 vector = MapToWorldPoint(mapTransform.InverseTransformPoint(mapDisplayTransform.position));
		if ((bool)VTMapGenerator.fetch)
		{
			float terrainAltitude = VTMapGenerator.fetch.GetTerrainAltitude(vector);
			vector.y = WaterPhysics.instance.height + terrainAltitude + gpsTargetAlt;
		}
		else
		{
			Vector3 vector2 = vector;
			vector2.y = WaterPhysics.instance.height;
			if (Physics.Raycast(vector2 + new Vector3(0f, 10000f, 0f), Vector3.down, out var hitInfo, 10000f, 1))
			{
				vector = hitInfo.point + new Vector3(0f, gpsTargetAlt, 0f);
			}
			else
			{
				vector.y = WaterPhysics.instance.height + gpsTargetAlt;
			}
		}
		if (num != 0)
		{
			muvs.RemoteGPS_AddTarget("MAP", VTMapManager.WorldToGlobalPoint(vector));
		}
		else
		{
			wm.gpsSystem.AddTarget(vector, "MAP");
		}
	}

	private void DisplayErrorMessage(string message)
	{
		errorText.text = message;
		if (errorRoutine != null)
		{
			StopCoroutine(errorRoutine);
		}
		errorRoutine = StartCoroutine(ErrorRoutine());
	}

	private IEnumerator ErrorRoutine()
	{
		for (int i = 0; i < 5; i++)
		{
			errorObject.SetActive(value: true);
			yield return new WaitForSeconds(0.2f);
			errorObject.SetActive(value: false);
			yield return new WaitForSeconds(0.1f);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("DashMapDisplay");
		configNode.SetValue("orthoIdx", orthoIdx);
		configNode.SetValue("isOffset", isOffset);
		configNode.SetValue("offsetFocus", offsetFocus);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = "DashMapDisplay";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			orthoIdx = node.GetValue<int>("orthoIdx");
			SetOrthoSize();
			isOffset = node.GetValue<bool>("isOffset");
			offsetFocus = node.GetValue<Vector3>("offsetFocus");
			if (base.isActiveAndEnabled)
			{
				UpdateMap();
			}
		}
	}
}
