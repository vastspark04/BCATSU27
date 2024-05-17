using UnityEngine;
using UnityEngine.UI;

public class HUDWaypoint : MonoBehaviour
{
	public GameObject tunnelTemplate;

	public RectTransform waypointIcon;

	public float startDepth;

	public float depthInterval;

	public int tunnelCount;

	public Text distanceText;

	private RectTransform[] tunnelRects;

	private Transform[] tunnelRoots;

	private Image[] tunnelImages;

	private float tunnelA;

	private Transform iconRoot;

	private Vector3 parentLocalPos;

	public Transform waypointTransform;

	private bool tunnelEnabled = true;

	private MeasurementManager measurements;

	private Transform actorTransform;

	private void Awake()
	{
		measurements = GetComponentInParent<MeasurementManager>();
	}

	private void Start()
	{
		ConstructTunnel();
		actorTransform = GetComponentInParent<Actor>().transform;
		if (!WaypointManager.instance)
		{
			DisableTunnel();
			base.enabled = false;
		}
	}

	private void ConstructTunnel()
	{
		parentLocalPos = tunnelTemplate.transform.parent.localPosition;
		tunnelRects = new RectTransform[tunnelCount];
		tunnelRoots = new Transform[tunnelCount];
		tunnelImages = new Image[tunnelCount];
		for (int i = 0; i < tunnelCount; i++)
		{
			GameObject gameObject = Object.Instantiate(tunnelTemplate);
			GameObject gameObject2 = new GameObject("TunnelRoot " + i);
			tunnelRoots[i] = gameObject2.transform;
			gameObject2.transform.parent = tunnelTemplate.transform.parent;
			gameObject2.transform.localPosition = Vector3.zero;
			gameObject2.transform.localRotation = Quaternion.identity;
			gameObject2.transform.localScale = Vector3.one;
			gameObject.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			tunnelRects[i] = (RectTransform)gameObject.transform;
			tunnelRects[i].localPosition = new Vector3(0f, 0f, startDepth + (float)i * depthInterval);
			tunnelImages[i] = gameObject.GetComponent<Image>();
			tunnelA = tunnelImages[i].color.a;
		}
		GameObject gameObject3 = new GameObject("IconRoot");
		gameObject3.transform.parent = tunnelTemplate.transform.parent;
		gameObject3.transform.localPosition = Vector3.zero;
		gameObject3.transform.localRotation = Quaternion.identity;
		gameObject3.transform.localScale = Vector3.one;
		waypointIcon.SetParent(gameObject3.transform, worldPositionStays: false);
		waypointIcon.localPosition = new Vector3(0f, 0f, startDepth + (float)tunnelCount * depthInterval);
		iconRoot = gameObject3.transform;
		tunnelTemplate.SetActive(value: false);
	}

	private void Update()
	{
		waypointTransform = WaypointManager.instance.currentWaypoint;
		if ((bool)waypointTransform)
		{
			if (!tunnelEnabled)
			{
				EnableTunnel();
			}
			float value = Vector3.Angle(tunnelTemplate.transform.parent.forward, waypointTransform.position - VRHead.position);
			value = Mathf.Clamp(value, 0f, 60f);
			tunnelTemplate.transform.parent.position = Vector3.Lerp(VRHead.position, tunnelTemplate.transform.parent.parent.TransformPoint(parentLocalPos), Mathf.Clamp01((value - 10f) / 10f));
			Vector3 forward = tunnelTemplate.transform.parent.InverseTransformDirection(waypointTransform.position - tunnelTemplate.transform.parent.position);
			float a = Mathf.Clamp01((value - 12f) / 4f) * tunnelA;
			for (int i = 0; i < tunnelCount; i++)
			{
				float t = ((float)i + 1f) / (float)(tunnelCount + 1) / 2f;
				tunnelRoots[i].localRotation = Quaternion.Slerp(Quaternion.identity, Quaternion.LookRotation(forward), t);
				Color color = tunnelImages[i].color;
				color.a = a;
				tunnelImages[i].color = color;
			}
			iconRoot.position = VRHead.position;
			iconRoot.localRotation = Quaternion.LookRotation(tunnelTemplate.transform.parent.InverseTransformDirection(waypointTransform.position - VRHead.position));
			float distance = Vector3.Distance(waypointTransform.position, actorTransform.position);
			distanceText.text = measurements.FormattedDistance(distance);
		}
		else if (tunnelEnabled)
		{
			DisableTunnel();
		}
	}

	private void EnableTunnel()
	{
		tunnelEnabled = true;
		for (int i = 0; i < tunnelCount; i++)
		{
			tunnelRects[i].gameObject.SetActive(value: true);
		}
		waypointIcon.gameObject.SetActive(value: true);
	}

	private void DisableTunnel()
	{
		tunnelEnabled = false;
		if (tunnelRects != null)
		{
			for (int i = 0; i < tunnelCount; i++)
			{
				tunnelRects[i].gameObject.SetActive(value: false);
			}
		}
		if ((bool)waypointIcon)
		{
			waypointIcon.gameObject.SetActive(value: false);
		}
	}

	public void ClearWaypoint()
	{
		WaypointManager.instance.ClearWaypoint();
	}
}
