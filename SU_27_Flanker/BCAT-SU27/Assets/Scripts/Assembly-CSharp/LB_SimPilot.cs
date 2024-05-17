using System;
using System.Collections.Generic;
using BDLearningBot;
using UnityEngine;

public class LB_SimPilot
{
	private enum GeneralBehaviors
	{
		Gunsight,
		Lead,
		Direct,
		Lag,
		High,
		Low,
		Climb,
		Dive,
		JinkLeft,
		JinkRight
	}

	private enum SpeedBehaviors
	{
		Accelerate,
		Deccelerate,
		Maintain
	}

	public LB_SimPilot opponent;

	public Vector3 position;

	public Quaternion rotation = Quaternion.identity;

	public Vector3 velocity;

	public float maxG = 9f;

	public float thrust;

	public float mass;

	public float liftFactor;

	public float dragFactor;

	public float rollRate = 180f;

	public float maxAoA = 20f;

	public float minTargetSpeed = 100f;

	public float maxTargetSpeed = 450f;

	public Vector3[] directions;

	public bool learn = true;

	private LearningBot bot;

	private bool _isStandby;

	private int[] nonLearningBCodes = new int[2] { 0, 2 };

	private int adjustment = 50;

	private float speed;

	private float time;

	private int lastSituationCode = -1;

	private float behaviorCertainty;

	private const float bulletSpeed = 1100f;

	private int behaviorCode;

	private BotBehaviorConverter bConverter;

	private int[] subBehaviors;

	private GeneralBehaviors genBehavior;

	private SpeedBehaviors speedBehavior;

	private float minAlt = 300f;

	private float minAltThresh = 50f;

	private bool climbing;

	private Vector3 targetDirection;

	private float targetSpeed;

	private PID throttlePid = new PID(0.2f, 1f, -0.12f, 0f, 0.22f);

	private Vector3 accel;

	public Vector3 forward => rotation * Vector3.forward;

	public Vector3 up => rotation * Vector3.up;

	public LearningBot learningBot => bot;

	public bool isStandby => _isStandby;

	public event Action OnWin;

	public string GetDebugString()
	{
		float f = MeasurementManager.SpeedToKnot(speed);
		return string.Format("{0}\n{1}\ncertainty: {2}%\n{3}kts\nG:{4}", genBehavior.ToString(), speedBehavior.ToString(), (behaviorCertainty * 100f).ToString("0.0"), Mathf.Round(f), (accel.magnitude / 9.81f).ToString("0.0"));
	}

	public void CreateBot()
	{
		if (bot == null && learn)
		{
			bot = new LearningBot(5000, 30);
			bot.diminishingAdjustment = true;
		}
	}

	public void BeginSim()
	{
		CreateBot();
		if (bConverter == null)
		{
			bConverter = new BotBehaviorConverter(new int[2]
			{
				EnumCount<GeneralBehaviors>(),
				EnumCount<SpeedBehaviors>()
			});
			subBehaviors = new int[2];
		}
		_isStandby = false;
		if (learn)
		{
			bot.BeginSession();
		}
	}

	public void Kill()
	{
		if (!_isStandby)
		{
			if (learn)
			{
				bot.EndDefeat(adjustment);
			}
			_isStandby = true;
		}
	}

	public void Win()
	{
		if (learn)
		{
			bot.EndVictory(adjustment);
		}
		_isStandby = true;
	}

	public void WinNeutral()
	{
		if (learn)
		{
			bot.EndVictory(0);
		}
		_isStandby = true;
	}

	public void Update(float deltaTime)
	{
		if (!_isStandby)
		{
			time += deltaTime;
			UpdateLearningBot();
			UpdateBehavior();
			UpdateControl(deltaTime);
			UpdatePhysics(deltaTime);
			if (position.y < 0f)
			{
				opponent.WinNeutral();
				Kill();
			}
			else if (Vector3.Dot(forward, opponent.position - position) < 0f && (Mathf.Abs(position.x) > 50000f || Mathf.Abs(position.y) > 50000f || Mathf.Abs(position.z) > 50000f))
			{
				Kill();
				opponent.WinNeutral();
			}
			else if (IsMutualKill())
			{
				opponent.Kill();
				Kill();
			}
			else if (IsKill())
			{
				opponent.Kill();
				Win();
				this.OnWin?.Invoke();
			}
		}
	}

	private bool IsKill()
	{
		float num = speed + 1100f;
		float num2 = (opponent.position - position).magnitude / num;
		Vector3 vector = opponent.position + opponent.velocity * num2;
		if ((vector - position).sqrMagnitude < 640000f)
		{
			return Vector3.Dot(forward, (vector - position).normalized) > 0.9995f;
		}
		return false;
	}

	public bool IsMutualKill()
	{
		if ((opponent.position - position).sqrMagnitude < 810000f && Vector3.Dot(forward, (opponent.position - position).normalized) > 0.995f)
		{
			return Vector3.Dot(opponent.forward, (position - opponent.position).normalized) > 0.995f;
		}
		return false;
	}

	private void UpdateLearningBot()
	{
		if (learn)
		{
			int num = DetermineSituation();
			if (num != lastSituationCode)
			{
				lastSituationCode = num;
				behaviorCode = bot.GetBehavior(num, out behaviorCertainty);
			}
		}
		else
		{
			behaviorCode = bConverter.Convert(nonLearningBCodes);
			behaviorCertainty = 1f;
		}
	}

	private int GetDirectionIndex(Vector3 direction, Vector3 relativeTo)
	{
		direction = Quaternion.FromToRotation(relativeTo, Vector3.forward) * direction;
		float num = -1f;
		int result = 0;
		for (int i = 0; i < directions.Length; i++)
		{
			float num2 = Vector3.Dot(direction, directions[i]);
			if (num2 > num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	private int DetermineSituation()
	{
		Vector3 vector = opponent.position - position;
		Vector3 normalized = vector.normalized;
		Vector3 vector2 = vector;
		vector2.y = 0f;
		vector2.Normalize();
		Vector3 relativeTo = forward;
		relativeTo.y = 0f;
		relativeTo.Normalize();
		int directionIndex = GetDirectionIndex(normalized, relativeTo);
		int directionIndex2 = GetDirectionIndex(opponent.forward, -vector2);
		int num = Mathf.RoundToInt(Mathf.Clamp(position.y / 152f, 0f, 15f));
		int num2 = 0;
		if (opponent.speed > speed + 25f)
		{
			num2 = 1;
		}
		else if (opponent.speed < speed - 25f)
		{
			num2 = 2;
		}
		int num3 = 0;
		if (speed > 100f)
		{
			num3 = 1;
		}
		if (speed > 140f)
		{
			num3 = 2;
		}
		if (speed > 180f)
		{
			num3 = 3;
		}
		if (speed > 220f)
		{
			num3 = 4;
		}
		if (speed > 260f)
		{
			num3 = 5;
		}
		if (speed > 300f)
		{
			num3 = 6;
		}
		if (speed > 340f)
		{
			num3 = 7;
		}
		float magnitude = vector.magnitude;
		int num4 = 0;
		if (magnitude > 800f)
		{
			num4 = 1;
		}
		if (magnitude > 1600f)
		{
			num4 = 2;
		}
		if (magnitude > 2400f)
		{
			num4 = 3;
		}
		return directionIndex | (directionIndex2 << 4) | (num << 8) | (num2 << 10) | (num3 << 13) | (num4 << 15);
	}

	private void UpdateBehavior()
	{
		bConverter.Convert(behaviorCode, subBehaviors);
		genBehavior = (GeneralBehaviors)subBehaviors[0];
		speedBehavior = (SpeedBehaviors)subBehaviors[1];
		if (climbing)
		{
			genBehavior = GeneralBehaviors.Climb;
			speedBehavior = SpeedBehaviors.Accelerate;
			if (position.y > minAlt + minAltThresh)
			{
				climbing = false;
			}
		}
		else if (position.y < minAlt)
		{
			climbing = true;
		}
		Vector3 vector = opponent.position;
		switch (genBehavior)
		{
		case GeneralBehaviors.Gunsight:
		{
			float num = speed + 1100f;
			float num2 = (opponent.position - position).magnitude / num;
			vector = opponent.position + opponent.velocity * num2;
			break;
		}
		case GeneralBehaviors.Lead:
			vector = opponent.position + opponent.velocity * 2f;
			break;
		case GeneralBehaviors.Direct:
			vector = opponent.position;
			break;
		case GeneralBehaviors.Lag:
			vector = opponent.position - opponent.velocity * 1f;
			break;
		case GeneralBehaviors.High:
			vector = opponent.position + 100f * Vector3.up;
			break;
		case GeneralBehaviors.Low:
			vector = opponent.position - 100f * Vector3.up;
			break;
		case GeneralBehaviors.Climb:
			vector = position + Vector3.ProjectOnPlane(forward, Vector3.up).normalized * 100f + Vector3.up * 100f;
			break;
		case GeneralBehaviors.Dive:
			vector = position + Vector3.ProjectOnPlane(forward, Vector3.up).normalized * 100f - Vector3.up * 100f;
			break;
		case GeneralBehaviors.JinkLeft:
			vector = position + Vector3.ProjectOnPlane(forward, Vector3.up).normalized * 100f - Vector3.Cross(Vector3.up, forward).normalized * 100f;
			break;
		case GeneralBehaviors.JinkRight:
			vector = position + Vector3.ProjectOnPlane(forward, Vector3.up).normalized * 100f + Vector3.Cross(Vector3.up, forward).normalized * 100f;
			break;
		}
		targetDirection = (vector - position).normalized;
		switch (speedBehavior)
		{
		case SpeedBehaviors.Accelerate:
			targetSpeed = Mathf.Min(maxTargetSpeed, speed + 50f);
			break;
		case SpeedBehaviors.Deccelerate:
			targetSpeed = Mathf.Max(minTargetSpeed, speed - 50f);
			break;
		case SpeedBehaviors.Maintain:
			break;
		}
	}

	private int EnumCount<T>()
	{
		return Enum.GetValues(typeof(T)).Length;
	}

	public void SetTargetSpeed(float s)
	{
		targetSpeed = s;
	}

	private void UpdateControl(float deltaTime)
	{
		Vector3 vector = Vector3.ProjectOnPlane(targetDirection, velocity).normalized + 0.2f * Vector3.up;
		if (vector == Vector3.zero)
		{
			vector = Vector3.up;
		}
		Vector3 upwards = Vector3.RotateTowards(up, vector, rollRate * ((float)Math.PI / 180f) * deltaTime, 0f);
		float num = Mathf.Clamp01(speed / 120f);
		float num2 = (maxG * num - Vector3.Dot(up, Vector3.up)) * 9.81f / speed;
		num2 *= Mathf.Clamp01(speed / 70f);
		Vector3 target = Vector3.RotateTowards(forward, targetDirection, num2 * deltaTime, 0f);
		target = Vector3.RotateTowards(velocity, target, maxAoA, float.MaxValue);
		rotation = Quaternion.LookRotation(target, upwards);
	}

	private void UpdatePhysics(float deltaTime)
	{
		if (deltaTime < float.Epsilon)
		{
			Debug.LogError("SimPilot delta time was too small!");
			return;
		}
		Vector3 vector = velocity;
		float num = Mathf.Clamp01(throttlePid.Evaluate(speed, targetSpeed, deltaTime));
		velocity += num * thrust / mass * deltaTime * forward;
		velocity += 9.81f * deltaTime * Vector3.down;
		float num2 = Vector3.Angle(velocity, forward);
		Vector3 normalized = (forward - velocity.normalized).normalized;
		float num3 = liftFactor / mass * speed * speed * num2;
		velocity += num3 * deltaTime * normalized;
		float num4 = dragFactor / mass * speed;
		velocity -= num4 * deltaTime * velocity;
		if (velocity.sqrMagnitude > 1000000f)
		{
			Debug.LogError($"We had a high velocity incident! {velocity.magnitude} m/s");
		}
		position += velocity * deltaTime;
		if (Mathf.Abs(position.x) > 50000f || Mathf.Abs(position.y) > 50000f || Mathf.Abs(position.z) > 50000f)
		{
			Debug.LogError($"We had a large position incident! pos={position.ToString()}, velocity={velocity.ToString()}, aoa={num2}, deltaTime={deltaTime}, liftAccel={num3}, dragAccel={num4}");
		}
		speed = velocity.magnitude;
		accel = (velocity - vector) / deltaTime;
	}

	public Texture2D GetCertaintyMap(Texture2D existing = null)
	{
		List<float> certaintyMap = bot.GetCertaintyMap();
		Debug.Log("f_cMap count: " + certaintyMap.Count);
		int num = Mathf.CeilToInt(Mathf.Sqrt(certaintyMap.Count));
		Texture2D texture2D = existing;
		if (existing == null || num > existing.width)
		{
			if ((bool)existing)
			{
				UnityEngine.Object.DestroyImmediate(existing);
			}
			texture2D = new Texture2D(num, num);
			texture2D.filterMode = FilterMode.Point;
		}
		bool flag = false;
		int num2 = 0;
		for (int i = 0; i < texture2D.width; i++)
		{
			for (int j = 0; j < texture2D.height; j++)
			{
				if (flag)
				{
					texture2D.SetPixel(i, j, Color.black);
					continue;
				}
				float num3 = certaintyMap[num2];
				Color color = new Color(num3, num3, num3, 1f);
				texture2D.SetPixel(i, j, color);
				num2++;
				if (num2 >= certaintyMap.Count)
				{
					flag = true;
				}
			}
		}
		texture2D.Apply();
		return texture2D;
	}
}
