using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationToggle : MonoBehaviour, IQSMissileComponent, IQSVehicleComponent
{
	[Serializable]
	public class RotationToggleTransform
	{
		public Transform transform;

		public float angle;

		public Vector3 axis;

		public float speed;

		public bool smooth;

		public float smoothFactor;

		private Quaternion defaultAngle;

		private Quaternion deployedAngle;

		private float tRate;

		private float smoothT;

		private float _currentT;

		private bool init;

		public float currentT
		{
			get
			{
				if (smooth)
				{
					return smoothT;
				}
				return _currentT;
			}
		}

		public void Init()
		{
			if (!init)
			{
				init = true;
				defaultAngle = transform.localRotation;
				Vector3 vector = transform.parent.InverseTransformDirection(transform.TransformDirection(axis));
				deployedAngle = Quaternion.AngleAxis(angle, vector) * defaultAngle;
				tRate = speed / Mathf.Abs(angle);
			}
		}

		public bool UpdateNormalizedRotation(float t)
		{
			_currentT = Mathf.MoveTowards(_currentT, t, tRate * Time.deltaTime);
			if (smooth)
			{
				smoothT = Mathf.Lerp(smoothT, _currentT, smoothFactor * Time.deltaTime);
				transform.localRotation = Quaternion.Lerp(defaultAngle, deployedAngle, smoothT);
				return smoothT == t;
			}
			transform.localRotation = Quaternion.Lerp(defaultAngle, deployedAngle, _currentT);
			return _currentT == t;
		}

		public void SetNormalizedRotationImmediate(float t)
		{
			if (!init)
			{
				Init();
			}
			Quaternion localRotation = Quaternion.Lerp(defaultAngle, deployedAngle, t);
			transform.localRotation = localRotation;
			smoothT = (_currentT = t);
		}

		public void SaveToConfigNode(ConfigNode tfNode)
		{
			tfNode.SetValue("defaultAngle", defaultAngle.eulerAngles);
			tfNode.SetValue("deployedAngle", deployedAngle.eulerAngles);
			tfNode.SetValue("_currentT", _currentT);
			tfNode.SetValue("smoothT", smoothT);
			tfNode.SetValue("tRate", tRate);
		}

		public void LoadFromConfigNode(ConfigNode tfNode)
		{
			defaultAngle = Quaternion.Euler(tfNode.GetValue<Vector3>("defaultAngle"));
			deployedAngle = Quaternion.Euler(tfNode.GetValue<Vector3>("deployedAngle"));
			_currentT = tfNode.GetValue<float>("_currentT");
			smoothT = tfNode.GetValue<float>("smoothT");
			tRate = tfNode.GetValue<float>("tRate");
		}
	}

	public RotationToggleTransform[] transforms;

	public bool startDeployed;

	public bool manual;

	private float manualTarget;

	private bool warned;

	public bool qsVehiclePersistent;

	[Header("Optional Battery")]
	public Battery battery;

	private float battDrain = 0.1f;

	private bool useBatt;

	private Coroutine rotateCoroutine;

	public bool deployed { get; private set; }

	public bool remoteOnly { get; set; }

	public event Action OnFinishRetract;

	public event Action OnStartDeploy;

	public event Action OnStartRetract;

	public event Action<float> OnStateSetImmediate;

	private bool CheckHasPower()
	{
		if (useBatt)
		{
			return battery.Drain(battDrain * Time.deltaTime);
		}
		return true;
	}

	private void Awake()
	{
		useBatt = battery != null;
		for (int i = 0; i < transforms.Length; i++)
		{
			transforms[i].Init();
			if (startDeployed && !remoteOnly)
			{
				transforms[i].SetNormalizedRotationImmediate(1f);
			}
		}
		if (startDeployed && !remoteOnly)
		{
			deployed = true;
		}
	}

	private void OnEnable()
	{
		if (manual)
		{
			StartCoroutine(ManualUpdateRoutine());
		}
	}

	private IEnumerator ManualUpdateRoutine()
	{
		while (base.enabled)
		{
			if (CheckHasPower())
			{
				for (int i = 0; i < transforms.Length; i++)
				{
					transforms[i].UpdateNormalizedRotation(manualTarget);
				}
			}
			yield return null;
		}
	}

	public void Toggle()
	{
		if (deployed)
		{
			SetDefault();
		}
		else
		{
			SetDeployed();
		}
	}

	public void SetState(int st)
	{
		if (st > 0)
		{
			SetDeployed();
		}
		else
		{
			SetDefault();
		}
	}

	public void SetNormalizedRotationImmediate(float t)
	{
		if (!remoteOnly)
		{
			if (rotateCoroutine != null)
			{
				StopCoroutine(rotateCoroutine);
			}
			for (int i = 0; i < transforms.Length; i++)
			{
				transforms[i].SetNormalizedRotationImmediate(t);
			}
			this.OnStateSetImmediate?.Invoke(t);
		}
	}

	public void SetDeployed()
	{
		if (remoteOnly)
		{
			return;
		}
		deployed = true;
		if (!base.gameObject.activeInHierarchy)
		{
			SetNormalizedRotationImmediate(1f);
			return;
		}
		if (rotateCoroutine != null)
		{
			StopCoroutine(rotateCoroutine);
		}
		rotateCoroutine = StartCoroutine(RotateRoutine(1f));
	}

	public void SetDefault()
	{
		if (remoteOnly)
		{
			return;
		}
		deployed = false;
		if (!base.gameObject.activeInHierarchy)
		{
			SetNormalizedRotationImmediate(0f);
			return;
		}
		if (rotateCoroutine != null)
		{
			StopCoroutine(rotateCoroutine);
		}
		rotateCoroutine = StartCoroutine(RotateRoutine(0f));
	}

	public void SetNormalizedRotation(float t)
	{
		if (!remoteOnly)
		{
			manualTarget = Mathf.Clamp01(t);
		}
	}

	private IEnumerator RotateRoutine(float target)
	{
		if (target > 0.001f)
		{
			if (this.OnStartDeploy != null)
			{
				this.OnStartDeploy();
			}
		}
		else
		{
			this.OnStartRetract?.Invoke();
		}
		bool done = false;
		while (!done)
		{
			if (CheckHasPower())
			{
				done = true;
				for (int i = 0; i < transforms.Length; i++)
				{
					if (!transforms[i].UpdateNormalizedRotation(target))
					{
						done = false;
					}
				}
			}
			yield return null;
		}
		if (target < 0.001f && this.OnFinishRetract != null)
		{
			this.OnFinishRetract();
		}
	}

	public void OnQuicksavedMissile(ConfigNode qsNode, float elapsedTime)
	{
		ConfigNode configNode = qsNode.AddNode(base.gameObject.name + "_RotationToggle");
		configNode.SetValue("deployed", deployed);
		for (int i = 0; i < transforms.Length; i++)
		{
			ConfigNode tfNode = configNode.AddNode("tf");
			transforms[i].SaveToConfigNode(tfNode);
		}
	}

	public void OnQuickloadedMissile(ConfigNode qsNode, float elapsedTime)
	{
		ConfigNode node = qsNode.GetNode(base.gameObject.name + "_RotationToggle");
		if (node != null)
		{
			List<ConfigNode> nodes = node.GetNodes("tf");
			for (int i = 0; i < nodes.Count; i++)
			{
				transforms[i].LoadFromConfigNode(nodes[i]);
			}
			if (node.GetValue<bool>("deployed"))
			{
				SetDeployed();
				SetNormalizedRotationImmediate(1f);
			}
			else
			{
				SetDefault();
				SetNormalizedRotationImmediate(0f);
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (qsVehiclePersistent)
		{
			OnQuicksavedMissile(qsNode, 0f);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (qsVehiclePersistent)
		{
			OnQuickloadedMissile(qsNode, 0f);
		}
	}
}
