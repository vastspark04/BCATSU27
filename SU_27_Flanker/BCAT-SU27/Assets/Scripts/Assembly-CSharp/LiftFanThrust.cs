using System;
using UnityEngine;

public class LiftFanThrust : FlightControlComponent
{
	public FlightInfo flightInfo;

	public ModuleEngine engine;

	public TiltController tiltController;

	public ModuleEngine liftFanEngine;

	public float maxLiftFanTilt = 15f;

	public float throttleDiv = 0.74f;

	private float myThrottle;

	[Header("Yaw Animation")]
	public Transform yawTf;

	public Vector3 yawAxis;

	public float maxYawAngle;

	public float yawControlRate;

	private Quaternion origYawRot;

	private Vector3 pyr;

	private void Awake()
	{
		if ((bool)yawTf)
		{
			origYawRot = yawTf.localRotation;
		}
	}

	public override void SetThrottle(float t)
	{
		myThrottle = t / throttleDiv;
	}

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		pyr = pitchYawRoll;
	}

	private void FixedUpdate()
	{
		if (tiltController.currentTilt < 89f && engine.startedUp)
		{
			float num = engine.maxThrust * engine.abThrustMult;
			if (engine.useAtmosCurve)
			{
				num *= engine.atmosCurve.Evaluate(AerodynamicsController.fetch.AtmosPressureAtPosition(engine.thrustTransform.position));
			}
			if (engine.useSpeedCurve)
			{
				num *= engine.speedCurve.Evaluate(flightInfo.airspeed);
			}
			float maxThrust = num - throttleDiv * engine.maxThrust;
			liftFanEngine.maxThrust = maxThrust;
			if (!liftFanEngine.engineEnabled)
			{
				liftFanEngine.SetPower(1);
			}
			float num2 = Mathf.Min(tiltController.currentTilt, maxLiftFanTilt);
			liftFanEngine.transform.localRotation = Quaternion.Euler(num2, 0f, 0f);
			float num3 = Mathf.Cos((tiltController.currentTilt - num2) * ((float)Math.PI / 180f));
			liftFanEngine.SetThrottle(myThrottle * num3);
			if ((bool)yawTf)
			{
				Quaternion to = Quaternion.AngleAxis(maxYawAngle * pyr.y * num3, yawAxis) * origYawRot;
				yawTf.localRotation = Quaternion.RotateTowards(yawTf.localRotation, to, yawControlRate * Time.fixedDeltaTime);
			}
		}
		else if (liftFanEngine.engineEnabled)
		{
			liftFanEngine.SetThrottle(0f);
			liftFanEngine.SetPower(0);
		}
	}
}
