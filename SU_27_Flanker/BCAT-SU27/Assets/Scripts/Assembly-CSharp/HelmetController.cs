using System;
using System.Collections;
using UnityEngine;

public class HelmetController : ElectronicComponent, IQSVehicleComponent
{
	public GameObject displayQuad;

	public GameObject displayQuadParent;

	public GameObject gimbalLimitObj;

	public bool tgpDisplayEnabled;

	public Transform lockTransform;

	public HUDMaskToggler hudMaskToggler;

	public GameObject visorDisplayObject;

	public GameObject hmcsDisplayObject;

	public NightVisionGoggles nvg;

	public GameObject[] hideOnDisableHelmet;

	public Animator animator;

	public bool hmdRequiresVisorDown = true;

	public string visorAnimName;

	public float visorAnimSpeed = 1f;

	private int visorAnimID;

	private bool visorDown;

	private float visorTime = 0.999f;

	private bool hmcsEnabled;

	public float hmcsVewDotLimit;

	[Tooltip("Checks if this object is active to determine whether to hide the HMCS display when looking forward")]
	public GameObject hudPowerObject;

	private bool isPowered;

	private bool hasBattery;

	private bool nvgEnabled;

	private Coroutine visorRoutine;

	private Coroutine hmcsUpdateRoutine;

	public bool isVisorDown => visorDown;

	public event Action<int> OnVisorState;

	public void SetPower(int pow)
	{
		isPowered = pow > 0;
	}

	private void Awake()
	{
		if (!animator)
		{
			animator = GetComponent<Animator>();
		}
		visorAnimID = Animator.StringToHash(visorAnimName);
		tgpDisplayEnabled = false;
		displayQuad.SetActive(value: false);
		gimbalLimitObj.SetActive(value: false);
		hmcsDisplayObject.SetActive(!hmdRequiresVisorDown);
		visorDisplayObject.SetActive(value: false);
		animator.Play(visorAnimID, 0, visorTime);
		animator.speed = 0f;
	}

	private void Start()
	{
		if (GameSettings.CurrentSettings.GetBoolSetting("HIDE_HELMET"))
		{
			GameObject[] array = hideOnDisableHelmet;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].layer = 20;
			}
		}
		if (!hmdRequiresVisorDown)
		{
			hmcsEnabled = true;
			if (hmcsUpdateRoutine != null)
			{
				StopCoroutine(hmcsUpdateRoutine);
			}
			hmcsUpdateRoutine = StartCoroutine(HMCSUpdateRoutine());
		}
	}

	private void OnEnable()
	{
		TargetingMFDPage componentInChildren = base.transform.root.GetComponentInChildren<TargetingMFDPage>(includeInactive: true);
		if ((bool)componentInChildren)
		{
			componentInChildren.SetHelmet(this);
		}
		if (!hmdRequiresVisorDown)
		{
			hmcsEnabled = true;
			if (hmcsUpdateRoutine != null)
			{
				StopCoroutine(hmcsUpdateRoutine);
			}
			hmcsUpdateRoutine = StartCoroutine(HMCSUpdateRoutine());
		}
		ToggleVisor();
		ToggleVisor();
	}

	public void RefreshHMCSUpdate()
	{
		if (hmcsEnabled || !hmdRequiresVisorDown)
		{
			hmcsEnabled = true;
			if (hmcsUpdateRoutine != null)
			{
				StopCoroutine(hmcsUpdateRoutine);
			}
			hmcsUpdateRoutine = StartCoroutine(HMCSUpdateRoutine());
		}
	}

	public void ToggleDisplay()
	{
		if (tgpDisplayEnabled)
		{
			CloseDisplay();
		}
		else
		{
			OpenDisplay();
		}
	}

	public void ToggleNVG()
	{
		nvgEnabled = !nvgEnabled;
		if (nvgEnabled && !visorDown && hmdRequiresVisorDown)
		{
			ToggleVisor();
		}
		if (hmcsEnabled)
		{
			if (nvgEnabled)
			{
				nvg.EnableNVG();
			}
			else
			{
				nvg.DisableNVG();
			}
		}
	}

	private void CloseDisplay()
	{
		displayQuad.SetActive(value: false);
		gimbalLimitObj.SetActive(value: false);
		tgpDisplayEnabled = false;
	}

	private void OpenDisplay()
	{
		tgpDisplayEnabled = true;
		displayQuad.SetActive(value: true);
	}

	public void ToggleVisor()
	{
		visorDown = !visorDown;
		if (visorDown)
		{
			if (visorRoutine != null)
			{
				StopCoroutine(visorRoutine);
			}
			visorRoutine = StartCoroutine(VisorDeployRoutine());
		}
		else
		{
			if (visorRoutine != null)
			{
				StopCoroutine(visorRoutine);
			}
			visorRoutine = StartCoroutine(VisorRetractRoutine());
			if (hmdRequiresVisorDown)
			{
				hudMaskToggler.SetMask(maskEnabled: true);
				visorDisplayObject.SetActive(value: false);
				hmcsEnabled = false;
			}
		}
		this.OnVisorState?.Invoke(visorDown ? 1 : 0);
	}

	private IEnumerator VisorDeployRoutine()
	{
		float tgt = 0f;
		while (visorTime > tgt)
		{
			visorTime = Mathf.MoveTowards(visorTime, tgt, visorAnimSpeed * Time.deltaTime);
			animator.Play(visorAnimID, 0, visorTime);
			animator.speed = 0f;
			yield return null;
		}
		hmcsEnabled = true;
		if (nvgEnabled && hmdRequiresVisorDown)
		{
			nvg.EnableNVG();
		}
		if (hmcsUpdateRoutine != null)
		{
			StopCoroutine(hmcsUpdateRoutine);
		}
		hmcsUpdateRoutine = StartCoroutine(HMCSUpdateRoutine());
	}

	private IEnumerator VisorRetractRoutine()
	{
		if (hmdRequiresVisorDown)
		{
			nvg.DisableNVGImmediate();
		}
		float tgt = 0.999f;
		while (visorTime < tgt)
		{
			visorTime = Mathf.MoveTowards(visorTime, tgt, visorAnimSpeed * Time.deltaTime);
			animator.Play(visorAnimID, 0, visorTime);
			animator.speed = 0f;
			yield return null;
		}
	}

	private IEnumerator HMCSUpdateRoutine()
	{
		while (!visorDisplayObject || !hudMaskToggler)
		{
			yield return null;
		}
		bool battAndPow = hasBattery && isPowered;
		visorDisplayObject.SetActive(battAndPow);
		hudMaskToggler.SetMask(!battAndPow);
		while (hmcsEnabled)
		{
			hasBattery = battery.Drain(0.1f * Time.deltaTime);
			bool flag = hasBattery && isPowered;
			if (flag != battAndPow)
			{
				battAndPow = flag;
				visorDisplayObject.SetActive(battAndPow);
				hudMaskToggler.SetMask(!battAndPow);
			}
			if ((!hudPowerObject || hudPowerObject.activeSelf) && Vector3.Dot(hmcsDisplayObject.transform.forward, hudMaskToggler.transform.forward) > hmcsVewDotLimit)
			{
				hmcsDisplayObject.SetActive(value: false);
			}
			else
			{
				hmcsDisplayObject.SetActive(value: true);
			}
			yield return null;
		}
	}

	public void OnEject()
	{
		if (tgpDisplayEnabled)
		{
			ToggleDisplay();
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("HelmetController");
		configNode.SetValue("tgpDisplayEnabled", tgpDisplayEnabled);
		configNode.SetValue("visorDown", visorDown);
		configNode.SetValue("nvgEnabled", nvgEnabled);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = "HelmetController";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			bool value = node.GetValue<bool>("tgpDisplayEnabled");
			bool value2 = node.GetValue<bool>("visorDown");
			bool value3 = node.GetValue<bool>("nvgEnabled");
			if (value && !tgpDisplayEnabled)
			{
				ToggleDisplay();
			}
			if (value2 && !visorDown)
			{
				ToggleVisor();
			}
			if (value3 && !nvgEnabled)
			{
				ToggleNVG();
			}
		}
	}
}
