using System;
using UnityEngine;

[Serializable]
public class PID3
{
	public float kp;

	public float ki;

	public float kd;

	private Vector3 lastErr;

	private Vector3 acc = Vector3.zero;

	public float iMax;

	public UpdateModes updateMode;

	private Vector3 lastEval;

	private bool first = true;

	public Vector3 lastEvaluatedValue => lastEval;

	public PID3(PID.PIDConfig config)
	{
		kp = config.kp;
		ki = config.ki;
		kd = config.kd;
		iMax = config.iMax;
	}

	public PID3(float kp, float ki, float kd, float iMax)
	{
		this.kp = kp;
		this.ki = ki;
		this.kd = kd;
		this.iMax = iMax;
	}

	public Vector3 Evaluate(Vector3 current, Vector3 target)
	{
		float num = Time.deltaTime;
		if (updateMode == UpdateModes.Fixed || num == 0f)
		{
			num = Time.fixedDeltaTime;
		}
		Vector3 vector = target - current;
		Vector3 vector2 = kp * vector;
		Vector3 vector3;
		if (first)
		{
			first = false;
			vector3 = Vector3.zero;
		}
		else
		{
			vector3 = (vector - lastErr) / num;
		}
		Vector3 vector4 = kd * -vector3;
		lastErr = vector;
		Vector3 vector5 = ki * acc;
		acc = Vector3.ClampMagnitude(acc + vector * num, iMax);
		lastEval = vector2 + vector5 - vector4;
		return lastEval;
	}

	public void SetStartError(Vector3 err)
	{
		lastErr = err;
	}

	public void ResetIntegrator()
	{
		acc = Vector3.zero;
		first = true;
	}
}
