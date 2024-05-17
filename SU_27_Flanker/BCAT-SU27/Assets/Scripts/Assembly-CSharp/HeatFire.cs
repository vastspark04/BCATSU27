using UnityEngine;

public class HeatFire : MonoBehaviour
{
	public Transform target;

	public Vector3 localPosition;

	public HeatEmitter heatEmitter;

	private ParticleSystem[] ps;

	private bool dead;

	public bool useSpeedEmissionCurve;

	public AnimationCurve speedEmissionCurve;

	private FixedPoint lastPos;

	private float lastTime = -1f;

	private float[] baseMuls;

	private bool shiftTransformOnOriginShift = true;

	private VehiclePart vp;

	private void Awake()
	{
		ps = GetComponentsInChildren<ParticleSystem>();
		baseMuls = new float[ps.Length];
		for (int i = 0; i < ps.Length; i++)
		{
			baseMuls[i] = ps[i].emission.rateOverTimeMultiplier;
		}
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += OnExitScene;
		}
	}

	private void Start()
	{
		if ((bool)target)
		{
			vp = target.GetComponentInParent<VehiclePart>();
			if ((bool)vp)
			{
				vp.OnRepair.AddListener(OnRepair);
			}
		}
		if (shiftTransformOnOriginShift && (bool)FloatingOrigin.instance && !GetComponent<FloatingOriginTransform>())
		{
			FloatingOrigin.instance.AddTransform(base.transform);
		}
	}

	private void OnRepair()
	{
		if ((bool)vp)
		{
			vp.OnRepair.RemoveListener(OnRepair);
		}
		Object.Destroy(base.gameObject);
	}

	private void OnExitScene()
	{
		Object.Destroy(base.gameObject);
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
		if (shiftTransformOnOriginShift && (bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.RemoveTransform(base.transform);
		}
	}

	private void LateUpdate()
	{
		if ((bool)target && target.gameObject.activeInHierarchy)
		{
			base.transform.position = target.TransformPoint(localPosition);
			if (!useSpeedEmissionCurve)
			{
				return;
			}
			if (lastTime < 0f)
			{
				lastTime = Time.time;
				lastPos.point = base.transform.position;
			}
			else if (Time.time - lastTime > 0.1f)
			{
				float num = Time.time - lastTime;
				float time = (base.transform.position - lastPos.point).magnitude / num;
				float num2 = speedEmissionCurve.Evaluate(time);
				for (int i = 0; i < ps.Length; i++)
				{
					ps[i].SetEmissionRateMultiplier(num2 * baseMuls[i]);
				}
				lastPos.point = base.transform.position;
				lastTime = Time.time;
			}
		}
		else if (!dead)
		{
			OnParentDestroy();
			dead = true;
		}
	}

	private void OnParentDestroy()
	{
		base.transform.parent = null;
		ps.SetEmission(emit: false);
		if ((bool)heatEmitter)
		{
			heatEmitter.enabled = false;
		}
		Object.Destroy(base.gameObject, ps.GetLongestLife());
	}
}
