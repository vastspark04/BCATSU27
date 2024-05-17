using UnityEngine;
using UnityEngine.UI;

public class HUDAltitudeLadder : MonoBehaviour, IQSVehicleComponent
{
	public GameObject tickTemplate;

	public float interval;

	public float spacing;

	public float maxAlt = 500f;

	public float maxRadarAlt = 500f;

	public float offset;

	public Text altDisplayText;

	public Text altUnitsText;

	private Rigidbody rb;

	private FlightInfo flightInfo;

	private MeasurementManager measurements;

	public Text altText;

	public GameObject radarModeObject;

	public GameObject aslModeObject;

	public GameObject[] declutterObjects;

	public bool forceMode;

	public bool useRadar = true;

	private VehicleMaster vm;

	private float smoothRate = 4f;

	private float smoothAlt;

	private void Awake()
	{
		vm = GetComponentInParent<VehicleMaster>();
		vm.OnSetRadarAltMode += Vm_OnSetRadarAltMode;
		if (!forceMode)
		{
			useRadar = vm.useRadarAlt;
		}
		UpdateModeObjects();
		rb = GetComponentInParent<Rigidbody>();
		flightInfo = rb.GetComponentInChildren<FlightInfo>();
		measurements = rb.GetComponentInChildren<MeasurementManager>();
		HUDDeclutter componentInParent = GetComponentInParent<HUDDeclutter>();
		if ((bool)componentInParent)
		{
			componentInParent.OnSetDeclutter += Decl_OnSetDeclutter;
		}
	}

	private void Vm_OnSetRadarAltMode(bool useRadarAlt)
	{
		if (!forceMode)
		{
			useRadar = useRadarAlt;
		}
		UpdateModeObjects();
	}

	private void Decl_OnSetDeclutter(int declutterLevel)
	{
		bool active = declutterLevel == 0;
		if ((bool)altUnitsText)
		{
			altUnitsText.gameObject.SetActive(active);
		}
		if ((bool)altText)
		{
			altText.gameObject.SetActive(active);
		}
		if (declutterObjects != null)
		{
			declutterObjects.SetActive(active);
		}
	}

	private void Start()
	{
		ConstructLadder();
		UpdateModeObjects();
	}

	private void ConstructLadder()
	{
		if ((bool)altText)
		{
			string text = string.Empty;
			for (float num = maxAlt; num >= 0f; num -= interval)
			{
				text = text + "\n" + num;
			}
			altText.text = text;
		}
	}

	public void ToggleAltMode()
	{
		vm.ToggleRadarAltMode();
		UpdateModeObjects();
	}

	private void UpdateModeObjects()
	{
		if ((bool)radarModeObject)
		{
			radarModeObject.SetActive(useRadar);
		}
		if ((bool)aslModeObject)
		{
			aslModeObject.SetActive(!useRadar);
		}
	}

	private void LateUpdate()
	{
		float num = flightInfo.radarAltitude;
		if (!useRadar)
		{
			num = flightInfo.altitudeASL;
		}
		num += offset;
		smoothAlt = Mathf.Lerp(smoothAlt, num, smoothRate * Time.deltaTime);
		float num2 = measurements.ConvertedAltitude(useRadar ? smoothAlt : num) * (spacing / interval);
		if (float.IsNaN(num2))
		{
			Debug.Log("ladderPos is NaN");
		}
		Vector3 localPosition = base.transform.localPosition;
		localPosition.y = 0f - num2;
		base.transform.localPosition = localPosition;
		if ((bool)altDisplayText)
		{
			altDisplayText.text = measurements.FormattedAltitude(num);
		}
		if ((bool)altUnitsText)
		{
			altUnitsText.text = measurements.AltitudeLabel();
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode("HUDAltitudeLadder_" + base.gameObject.name).SetValue("useRadar", useRadar);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("HUDAltitudeLadder_" + base.gameObject.name);
		if (node != null)
		{
			useRadar = node.GetValue<bool>("useRadar");
			UpdateModeObjects();
		}
	}
}
