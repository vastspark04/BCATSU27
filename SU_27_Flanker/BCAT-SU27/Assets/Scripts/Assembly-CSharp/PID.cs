using System;
using UnityEngine;

[Serializable]
public class PID
{
	[Serializable]
	public struct PIDConfig
	{
		public float kp;

		public float ki;

		public float kd;

		public float iMin;

		public float iMax;
	}

	public float kp;

	public float ki;

	public float kd;

	private float lastErr;

	private float acc;

	public float iMin;

	public float iMax;

	public bool circular;

	[HideInInspector]
	public UpdateModes updateMode;

	private float p;

	private float i;

	private float d;

	public float errorVel { get; private set; }

	public PID(PIDConfig config)
	{
		kp = config.kp;
		ki = config.ki;
		kd = config.kd;
		iMin = config.iMin;
		iMax = config.iMax;
	}

	public PID(float kp, float ki, float kd, float iMin, float iMax)
	{
		this.kp = kp;
		this.ki = ki;
		this.kd = kd;
		this.iMin = iMin;
		this.iMax = iMax;
	}

	public Vector3 Debug_GetPID()
	{
		return new Vector3(p, i, d);
	}

	public float Evaluate(float current, float target, bool _p = true, bool _i = true, bool _d = true)
	{
		float deltaTime = ((updateMode == UpdateModes.Fixed) ? Time.fixedDeltaTime : Time.deltaTime);
		return Evaluate(current, target, deltaTime, _p, _i, _d);
	}

	public float Evaluate(float current, float target, float deltaTime, bool _p = true, bool _i = true, bool _d = true)
	{
		float num;
		if (circular)
		{
			target = Mathf.Repeat(target, 360f);
			current = Mathf.Repeat(current, 360f);
			num = target - current;
			if (Mathf.Abs(num) > 180f)
			{
				if (target < current)
				{
					target += 360f;
				}
				else
				{
					current += 360f;
				}
				num = target - current;
			}
		}
		else
		{
			num = target - current;
		}
		p = 0f;
		if (_p)
		{
			p = kp * num;
		}
		d = 0f;
		if (_d)
		{
			if (deltaTime == 0f)
			{
				deltaTime = Time.fixedDeltaTime;
			}
			float num3 = (errorVel = (0f - (num - lastErr)) / deltaTime);
			d = kd * num3;
			lastErr = num;
		}
		i = 0f;
		if (_i)
		{
			i = ki * acc;
			acc = Mathf.Clamp(acc + num * deltaTime, iMin, iMax);
		}
		return p + i + d;
	}

	public ConfigNode SaveToNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.SetValue("acc", acc);
		configNode.SetValue("lastErr", lastErr);
		return configNode;
	}

	public void LoadFromNode(ConfigNode node)
	{
		acc = node.GetValue<float>("acc");
		lastErr = node.GetValue<float>("lastErr");
	}

	public void ResetIntegrator()
	{
		acc = 0f;
	}
}
