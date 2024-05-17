using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class VTOLQuickStart : MonoBehaviour
{
	[Serializable]
	public class QuickStartComponents
	{
		[Serializable]
		public class QSLever
		{
			public VRLever lever;

			public int state = 1;
		}

		[Serializable]
		public class QSKnobInt
		{
			public VRTwistKnobInt knob;

			public int state;
		}

		[Serializable]
		public class QSEngine
		{
			public ModuleEngine engine;

			public int state;
		}

		public QSEngine[] engines;

		public QSLever[] levers;

		public QSKnobInt[] knobs;

		public UnityEvent OnApplySettings;

		public void ApplySettings()
		{
			QSLever[] array = levers;
			foreach (QSLever qSLever in array)
			{
				qSLever.lever.RemoteSetState(qSLever.state);
			}
			QSEngine[] array2 = engines;
			foreach (QSEngine qSEngine in array2)
			{
				qSEngine.engine.SetPowerImmediate(qSEngine.state);
			}
			QSKnobInt[] array3 = knobs;
			foreach (QSKnobInt qSKnobInt in array3)
			{
				qSKnobInt.knob.RemoteSetState(qSKnobInt.state);
			}
			if (OnApplySettings != null)
			{
				OnApplySettings.Invoke();
			}
		}
	}

	public Rigidbody rb;

	public QuickStartComponents quickStartComponents;

	public QuickStartComponents quickStopComponents;

	public VRLever gearLever;

	public VRLever brakeLockLever;

	public VRThrottle throttle;

	private TiltController tc;

	public UnityEvent OnStartFlying;

	private bool startFlew;

	private float timeStartFly;

	private Coroutine startFlyRtn;

	public void FireStartFlyingEvents()
	{
		if (OnStartFlying != null)
		{
			OnStartFlying.Invoke();
		}
	}

	private void Start()
	{
		tc = rb.GetComponentInChildren<TiltController>();
	}

	public void StartFly()
	{
		float num = 1000f;
		float num2 = -4000f;
		float num3 = 1000f;
		float spd = 130f;
		if (startFlew)
		{
			if (!(Time.time - timeStartFly < 0.5f))
			{
				return;
			}
			if (startFlyRtn != null)
			{
				StopCoroutine(startFlyRtn);
				num = 8000f;
				num2 = 90000f;
				num3 = -90000f;
				spd = 400f;
			}
		}
		timeStartFly = Time.time;
		startFlew = true;
		rb.transform.position += num * Vector3.up;
		rb.transform.position += rb.transform.forward * num2 + rb.transform.right * num3;
		QuickStart();
		if ((bool)tc)
		{
			tc.SetTiltImmediate(90f);
		}
		if ((bool)gearLever)
		{
			gearLever.RemoteSetState(1);
		}
		startFlyRtn = StartCoroutine(StartFlyRoutine(spd));
	}

	public void QuickStart()
	{
		quickStartComponents.ApplySettings();
	}

	private IEnumerator StartFlyRoutine(float spd)
	{
		while (rb.velocity.magnitude < spd)
		{
			rb.AddForce(50f * rb.transform.forward, ForceMode.Acceleration);
			yield return new WaitForFixedUpdate();
		}
	}
}
