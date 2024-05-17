using System;
using System.Collections.Generic;
using UnityEngine;

public class LAI_Pilot
{
	public class TrainingLibrary
	{
		public DecisionFrame[,,] frames;

		public TrainingLibrary()
		{
			frames = new DecisionFrame[6, 6, 5];
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					for (int k = 0; k < 5; k++)
					{
						DecisionFrame decisionFrame = new DecisionFrame
						{
							dt = (Directions)i,
							tvd = (Directions)j,
							decision = (Decisions)k,
							score = 0f
						};
						frames[i, j, k] = decisionFrame;
					}
				}
			}
		}
	}

	public enum Directions
	{
		Front,
		Back,
		Left,
		Right,
		Up,
		Down
	}

	public enum Decisions
	{
		FlyToTarget,
		FlyAwayFromTarget,
		LagPursuitTarget,
		FlyAboveTarget,
		FlyBelowTarget
	}

	public struct DecisionFrame
	{
		public Directions dt;

		public Directions tvd;

		public Decisions decision;

		public float score;
	}

	private const float MAX_DECISION_TIME = 8f;

	private const float DECISION_REWARD_FACTOR = 2f;

	private const float DECISION_REWARD_POWER = 2f;

	public LAI_Pilot opponent;

	public Vector3 position;

	public Vector3 velocity;

	public Quaternion rotation;

	public float mass;

	public float thrust;

	public float liftFactor;

	public float dragFactor;

	public float maxG = 9f;

	public float maxAoA = 25f;

	public float rollRate;

	public bool useBestDecision;

	private List<DecisionFrame> history = new List<DecisionFrame>();

	private Directions currDirToTarget;

	private Directions currTargetVelDir;

	private TrainingLibrary trainingLibrary;

	private float time;

	private float lastDecisionTime;

	private Vector3 intentDirection;

	public const int DIR_COUNT = 6;

	public const int DECISIONS_COUNT = 5;

	public Vector3 forward => rotation * Vector3.forward;

	public Vector3 up => rotation * Vector3.up;

	public float speed { get; private set; }

	public Decisions currentDecision { get; private set; }

	public LAI_Pilot(TrainingLibrary tl)
	{
		trainingLibrary = tl;
	}

	public void ClearHistory()
	{
		history.Clear();
	}

	public bool Update(float deltaTime)
	{
		time += deltaTime;
		UpdateDecision();
		UpdateControl(deltaTime);
		UpdatePhysics(deltaTime);
		return IsKill();
	}

	public void RewardKill()
	{
		if (trainingLibrary != null)
		{
			float num = 2f;
			int num2 = history.Count - 1;
			while (num2 >= 0 && num > 0.001f)
			{
				DecisionFrame decisionFrame = history[num2];
				DecisionFrame decisionFrame2 = trainingLibrary.frames[(int)decisionFrame.dt, (int)decisionFrame.tvd, (int)decisionFrame.decision];
				float num3 = num / 2f;
				decisionFrame2.score += num3;
				num -= num3;
				trainingLibrary.frames[(int)decisionFrame.dt, (int)decisionFrame.tvd, (int)decisionFrame.decision] = decisionFrame2;
				num2--;
			}
		}
	}

	public void PunishDeath()
	{
		if (trainingLibrary != null)
		{
			float num = 2f;
			int num2 = history.Count - 1;
			while (num2 >= 0 && num > 0.001f)
			{
				DecisionFrame decisionFrame = history[num2];
				DecisionFrame decisionFrame2 = trainingLibrary.frames[(int)decisionFrame.dt, (int)decisionFrame.tvd, (int)decisionFrame.decision];
				float num3 = num / 2f;
				decisionFrame2.score -= num3;
				num -= num3;
				trainingLibrary.frames[(int)decisionFrame.dt, (int)decisionFrame.tvd, (int)decisionFrame.decision] = decisionFrame2;
				num2--;
			}
		}
	}

	private bool IsKill()
	{
		if ((opponent.position - position).sqrMagnitude < 640000f)
		{
			return Vector3.Dot(forward, (opponent.position - position).normalized) > 0.97f;
		}
		return false;
	}

	private void UpdateDecision()
	{
		Directions direction = GetDirection(opponent.position - position, forward, up);
		Directions direction2 = GetDirection(position - opponent.position, opponent.forward, opponent.up);
		if (direction != currDirToTarget || direction2 != currTargetVelDir || time - lastDecisionTime > 8f)
		{
			lastDecisionTime = time;
			currDirToTarget = direction;
			currTargetVelDir = direction2;
			if (useBestDecision)
			{
				NewBestDecision();
			}
			else
			{
				NewRandomDecision();
			}
		}
		intentDirection = (currentDecision switch
		{
			Decisions.FlyAwayFromTarget => position - (opponent.position - position), 
			Decisions.LagPursuitTarget => opponent.position - opponent.velocity, 
			Decisions.FlyAboveTarget => opponent.position + Vector3.up * opponent.velocity.magnitude, 
			Decisions.FlyBelowTarget => opponent.position - Vector3.up * opponent.velocity.magnitude, 
			_ => opponent.position, 
		} - position).normalized;
	}

	private void NewRandomDecision()
	{
		DecisionFrame decisionFrame = default(DecisionFrame);
		decisionFrame.dt = currDirToTarget;
		decisionFrame.tvd = currTargetVelDir;
		decisionFrame.decision = (Decisions)UnityEngine.Random.Range(0, 5);
		DecisionFrame item = decisionFrame;
		currentDecision = item.decision;
		history.Add(item);
	}

	private void NewBestDecision()
	{
		float num = 0f;
		Decisions decisions = Decisions.FlyToTarget;
		for (int i = 0; i < 5; i++)
		{
			DecisionFrame decisionFrame = trainingLibrary.frames[(int)currDirToTarget, (int)currDirToTarget, i];
			if (decisionFrame.score > num)
			{
				num = decisionFrame.score;
				decisions = decisionFrame.decision;
			}
		}
		currentDecision = decisions;
		DecisionFrame decisionFrame2 = default(DecisionFrame);
		decisionFrame2.dt = currDirToTarget;
		decisionFrame2.tvd = currTargetVelDir;
		decisionFrame2.decision = currentDecision;
		DecisionFrame item = decisionFrame2;
		history.Add(item);
	}

	private Directions GetDirection(Vector3 vector, Vector3 refFwd, Vector3 refUp)
	{
		Vector3 rhs = Vector3.Cross(refUp, refFwd);
		float num = Vector3.Dot(vector, refFwd);
		float num2 = Vector3.Dot(vector, refUp);
		float num3 = Vector3.Dot(vector, rhs);
		float num4 = num;
		Directions result = Directions.Front;
		if (0f - num > num4)
		{
			num4 = 0f - num;
			result = Directions.Back;
		}
		if (num2 > num4)
		{
			num4 = num2;
			result = Directions.Up;
		}
		if (0f - num2 > num4)
		{
			num4 = 0f - num2;
			result = Directions.Down;
		}
		if (num3 > num4)
		{
			num4 = num3;
			result = Directions.Right;
		}
		if (0f - num3 > num4)
		{
			num4 = 0f - num3;
			result = Directions.Left;
		}
		return result;
	}

	private void UpdateControl(float deltaTime)
	{
		Vector3 vector = Vector3.ProjectOnPlane(intentDirection, velocity).normalized + 0.2f * Vector3.up;
		if (vector == Vector3.zero)
		{
			vector = Vector3.up;
		}
		Vector3 upwards = Vector3.RotateTowards(up, vector, rollRate * ((float)Math.PI / 180f) * deltaTime, 0f);
		float num = (maxG - Vector3.Dot(up, Vector3.up)) * 9.81f / speed;
		num *= Mathf.Clamp01(speed / 70f);
		Vector3 target = Vector3.RotateTowards(forward, intentDirection, num * deltaTime, 0f);
		target = Vector3.RotateTowards(velocity, target, maxAoA, float.MaxValue);
		rotation = Quaternion.LookRotation(target, upwards);
	}

	private void UpdatePhysics(float deltaTime)
	{
		velocity += thrust / mass * deltaTime * forward;
		velocity += 9.81f * deltaTime * Vector3.down;
		float num = Vector3.Angle(velocity, forward);
		velocity += liftFactor / mass * speed * speed * num * deltaTime * (forward - velocity.normalized).normalized;
		velocity -= dragFactor / mass * speed * deltaTime * velocity;
		position += velocity * deltaTime;
		speed = velocity.magnitude;
	}
}
