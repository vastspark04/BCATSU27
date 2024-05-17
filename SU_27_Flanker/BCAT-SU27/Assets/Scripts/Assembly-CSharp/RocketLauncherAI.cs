using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncherAI : MonoBehaviour, IEngageEnemies, IArtilleryUnit, IQSVehicleComponent
{
	[Serializable]
	private struct LaunchResult
	{
		public Vector3 correction;

		public Vector3 hitOffset;
	}

	public Actor actor;

	public RocketLauncher rocketLauncher;

	public VisualTargetFinder targetFinder;

	public ModuleTurret turret;

	public MinMax attackRange;

	public RocketRangeProfile rangeProfile;

	public int shotsPerSalvo;

	public float salvoInterval;

	public float rippleRate = 60f;

	public bool allowReload;

	public float reloadTime;

	private bool died;

	private Coroutine attackRoutine;

	private bool engageEnemies = true;

	public bool useCorrectiveAiming;

	public float correctionAdjust = 0.1f;

	public float correctionMaxDist = 4000f;

	public Vector3 defaultCorrection;

	private int maxLaunchResults = 8;

	private Actor correctiveTarget;

	private Queue<LaunchResult> cLaunchResults = new Queue<LaunchResult>();

	private Vector3 correction = Vector3.zero;

	private float timeEmptied;

	private bool qs_firingSalvo;

	private int qs_shotsPerSalvo;

	private int qs_salvos;

	private FixedPoint qs_targetPos;

	private Vector3 qs_targetVel;

	private float qs_radius;

	private bool qs_resumeAttack;

	private int qs_numFired;

	private Coroutine fireSalvoRoutine;

	private string qsNodeName => base.gameObject.name + "_RocketLauncherAI";

	private void Awake()
	{
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
		Health componentInParent = GetComponentInParent<Health>();
		if ((bool)componentInParent)
		{
			componentInParent.OnDeath.AddListener(OnDeath);
		}
	}

	private void OnDeath()
	{
		died = true;
		engageEnemies = false;
		if (attackRoutine != null)
		{
			StopCoroutine(attackRoutine);
		}
		if (fireSalvoRoutine != null)
		{
			StopCoroutine(fireSalvoRoutine);
		}
		base.enabled = false;
	}

	private void OnEnable()
	{
		if (engageEnemies && !died)
		{
			attackRoutine = StartCoroutine(AttackRoutine());
		}
	}

	private void OnDisable()
	{
		if (attackRoutine != null)
		{
			StopCoroutine(attackRoutine);
		}
		attackRoutine = null;
	}

	private IEnumerator CorrectionRoutine(Rocket r, Vector3D targetGpoint, Vector3 currCorrection)
	{
		if (!r)
		{
			yield break;
		}
		Vector3D gPos = VTMapManager.WorldToGlobalPoint(r.transform.position);
		bool detonated = false;
		r.OnDetonated += delegate(Rocket detRocket)
		{
			detonated = true;
			gPos = VTMapManager.WorldToGlobalPoint(detRocket.transform.position);
		};
		while (!detonated)
		{
			yield return null;
		}
		Vector3 vector = VTMapManager.GlobalToWorldPoint(gPos);
		Vector3 lhs = vector - VTMapManager.GlobalToWorldPoint(targetGpoint);
		Vector3 normalized = Vector3.ProjectOnPlane(vector - base.transform.position, Vector3.up).normalized;
		Vector3 rhs = Vector3.Cross(Vector3.up, normalized);
		Vector3 hitOffset = new Vector3(Vector3.Dot(lhs, rhs), Vector3.Dot(lhs, Vector3.up), Vector3.Dot(lhs, normalized));
		if (lhs.sqrMagnitude < correctionMaxDist * correctionMaxDist)
		{
			if (cLaunchResults.Count == maxLaunchResults)
			{
				cLaunchResults.Dequeue();
			}
			cLaunchResults.Enqueue(new LaunchResult
			{
				correction = currCorrection,
				hitOffset = hitOffset
			});
		}
	}

	private void CalculateCorrection()
	{
		correction = Vector3.zero;
		if (cLaunchResults.Count <= 0)
		{
			return;
		}
		foreach (LaunchResult cLaunchResult in cLaunchResults)
		{
			correction += -cLaunchResult.hitOffset * correctionAdjust + cLaunchResult.correction;
		}
		correction /= (float)cLaunchResults.Count;
	}

	private IEnumerator AttackRoutine()
	{
		yield return null;
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, salvoInterval));
		while (!engageEnemies)
		{
			yield return null;
		}
		while (base.enabled)
		{
			if (rocketLauncher.GetCount() == 0)
			{
				if (!allowReload)
				{
					base.enabled = false;
					break;
				}
				while (Time.time - timeEmptied < reloadTime)
				{
					yield return null;
				}
				rocketLauncher.ReloadAll();
			}
			Actor tgt = null;
			while (!tgt)
			{
				if (useCorrectiveAiming && (bool)correctiveTarget && correctiveTarget.alive && targetFinder.targetsSeen.Contains(correctiveTarget))
				{
					tgt = correctiveTarget;
				}
				else
				{
					correctiveTarget = null;
				}
				if (!tgt && (bool)targetFinder.attackingTarget)
				{
					float sqrMagnitude = (targetFinder.attackingTarget.position - base.transform.position).sqrMagnitude;
					if (sqrMagnitude > attackRange.min * attackRange.min && sqrMagnitude < attackRange.max * attackRange.max)
					{
						tgt = targetFinder.attackingTarget;
					}
				}
				yield return null;
			}
			FireSalvo(shotsPerSalvo, 1, rippleRate, tgt.position, tgt.velocity);
			yield return fireSalvoRoutine;
			if (rocketLauncher.GetCount() == 0)
			{
			}
			yield return new WaitForSeconds(salvoInterval);
			if (useCorrectiveAiming)
			{
				while (rocketLauncher.liveRocketsCount > 0)
				{
					yield return null;
				}
			}
			turret.ReturnTurretOneshot();
			yield return null;
		}
	}

	public void CommandFireSalvo(int shotsPerSalvo, int salvos, float rippleRate, Vector3 targetPosition, Vector3 targetVelocity, float radius)
	{
		if (!died)
		{
			if (attackRoutine != null)
			{
				StopCoroutine(attackRoutine);
			}
			FireSalvo(shotsPerSalvo, salvos, rippleRate, targetPosition, targetVelocity, radius, resumeAttackRoutine: true);
		}
	}

	private void FireSalvo(int shotsPerSalvo, int salvos, float rippleRate, Vector3 targetPosition, Vector3 targetVelocity, float radius = 0f, bool resumeAttackRoutine = false)
	{
		if (!died)
		{
			if (fireSalvoRoutine != null)
			{
				StopCoroutine(fireSalvoRoutine);
			}
			fireSalvoRoutine = StartCoroutine(FireSalvoRoutine(shotsPerSalvo, salvos, rippleRate, new FixedPoint(targetPosition), targetVelocity, radius, resumeAttackRoutine));
		}
	}

	private IEnumerator FireSalvoRoutine(int shotsPerSalvo, int salvos, float rippleRate, FixedPoint targetPosition, Vector3 targetVelocity, float radius = 0f, bool resumeAttackRoutine = false)
	{
		qs_firingSalvo = true;
		qs_shotsPerSalvo = shotsPerSalvo;
		qs_salvos = salvos;
		qs_targetPos = targetPosition;
		qs_targetVel = targetVelocity;
		qs_radius = radius;
		qs_resumeAttack = resumeAttackRoutine;
		qs_numFired = 0;
		if (rocketLauncher.GetCount() == 0)
		{
			if (!allowReload)
			{
				qs_firingSalvo = false;
				turret.ReturnTurretOneshot();
				yield break;
			}
			while (Time.time - timeEmptied < reloadTime)
			{
				yield return null;
			}
			rocketLauncher.ReloadAll();
		}
		rocketLauncher.autoCalcImpact = false;
		rocketLauncher.OnEnableWeapon();
		rocketLauncher.SetParentActor(actor);
		Transform fireTf = rocketLauncher.fireTransforms[0];
		for (int i = 0; i < salvos; i++)
		{
			int numFired = 0;
			while (numFired < shotsPerSalvo)
			{
				float aimT = 0f;
				if (numFired > 0)
				{
					float num = 60f / rippleRate;
					aimT = 1f - num;
				}
				Vector3 radialOffset = Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, Vector3.up) * radius;
				while (aimT < 1f)
				{
					Vector3 vector4;
					bool flag;
					if ((bool)rangeProfile)
					{
						Vector3 vector = targetPosition.point + radialOffset - rocketLauncher.fireTransforms[0].position;
						if (targetVelocity.sqrMagnitude > 25f)
						{
							float magnitude = vector.magnitude;
							Vector3 fromDirection = vector;
							fromDirection.y = 0f;
							float targetElevation = VectorUtils.SignedAngle(fromDirection, vector, Vector3.up);
							float time = rangeProfile.GetTime(targetElevation, magnitude);
							vector += targetVelocity * time;
						}
						Vector3 vector2 = vector;
						vector2.y = 0f;
						float targetElevation2 = VectorUtils.SignedAngle(vector2, vector, Vector3.up);
						float magnitude2 = vector.magnitude;
						float angle = rangeProfile.GetAngle(targetElevation2, magnitude2);
						Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
						vector4 = turret.yawTransform.position + Quaternion.AngleAxis(angle, -vector3) * vector2;
						flag = true;
						useCorrectiveAiming = false;
					}
					else
					{
						flag = rocketLauncher.CalculateImpactWithTarget(targetPosition.point, 0.5f, 32f, out var time2, out var tgtDist, out var hitPos);
						Vector3 vector5 = hitPos - (fireTf.position + tgtDist * fireTf.forward);
						vector5 = Vector3.ProjectOnPlane(vector5, fireTf.forward);
						vector4 = targetPosition.point + radialOffset - vector5 + targetVelocity * time2;
						if (useCorrectiveAiming)
						{
							vector4 = base.transform.position + (vector4 - base.transform.position).normalized * 1000f;
							CalculateCorrection();
							Vector3 vector6 = defaultCorrection + correction;
							vector4.y += vector6.z;
						}
					}
					turret.AimToTarget(vector4);
					if (flag && turret.TargetInRange(vector4, attackRange.max))
					{
						aimT += Time.deltaTime;
					}
					targetPosition.point += targetVelocity * Time.deltaTime;
					yield return null;
				}
				if (useCorrectiveAiming)
				{
					if (rocketLauncher.GetCount() > 0 && !died)
					{
						if (rocketLauncher.FireRocket())
						{
							StartCoroutine(CorrectionRoutine(rocketLauncher.lastFiredRocket, VTMapManager.WorldToGlobalPoint(targetPosition.point + radialOffset), correction));
						}
						if (rocketLauncher.GetCount() == 0)
						{
							timeEmptied = Time.time;
						}
					}
				}
				else if (rocketLauncher.GetCount() > 0 && !died)
				{
					rocketLauncher.FireRocket();
					if (rocketLauncher.GetCount() == 0)
					{
						timeEmptied = Time.time;
					}
				}
				numFired++;
				if (rocketLauncher.GetCount() == 0)
				{
					if (!allowReload)
					{
						qs_firingSalvo = false;
						turret.ReturnTurretOneshot();
						yield break;
					}
					while (Time.time - timeEmptied < reloadTime)
					{
						yield return null;
					}
					rocketLauncher.ReloadAll();
				}
				qs_numFired = numFired;
				targetPosition.point += targetVelocity * Time.deltaTime;
				qs_targetPos = targetPosition;
				yield return null;
			}
			if (salvos <= 1)
			{
				continue;
			}
			yield return new WaitForSeconds(salvoInterval);
			if (useCorrectiveAiming)
			{
				while (rocketLauncher.liveRocketsCount > 0)
				{
					yield return null;
				}
			}
		}
		rocketLauncher.OnDisableWeapon();
		qs_firingSalvo = false;
		if (!resumeAttackRoutine)
		{
			yield break;
		}
		if (engageEnemies)
		{
			if (attackRoutine != null)
			{
				StopCoroutine(attackRoutine);
			}
			attackRoutine = StartCoroutine(AttackRoutine());
		}
		turret.ReturnTurretOneshot();
	}

	public void SetEngageEnemies(bool engage)
	{
		if (died && engage)
		{
			return;
		}
		engageEnemies = engage;
		if (!engage)
		{
			if (attackRoutine != null)
			{
				StopCoroutine(attackRoutine);
				attackRoutine = null;
			}
		}
		else if (base.gameObject.activeInHierarchy && attackRoutine == null)
		{
			attackRoutine = StartCoroutine(AttackRoutine());
		}
	}

	public void FireOnPosition(FixedPoint targetPosition, Vector3 targetVelocity, int shotsPerSalvo, int salvos)
	{
		CommandFireSalvo(shotsPerSalvo, salvos, rippleRate, targetPosition.point, targetVelocity, 0f);
	}

	public void FireOnPositionRadius(FixedPoint targetPosition, float radius, int shotsPerSalvo, int salvos)
	{
		CommandFireSalvo(shotsPerSalvo, salvos, rippleRate, targetPosition.point, Vector3.zero, radius);
	}

	public void FireOnActor(Actor a, int shotsPerSalvo, int salvos)
	{
		CommandFireSalvo(shotsPerSalvo, salvos, rippleRate, a.position, a.velocity, 0f);
	}

	public void ClearFireOrders()
	{
		if (fireSalvoRoutine != null)
		{
			StopCoroutine(fireSalvoRoutine);
		}
		qs_firingSalvo = false;
		if (engageEnemies && attackRoutine == null)
		{
			attackRoutine = StartCoroutine(AttackRoutine());
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(qsNodeName);
		configNode.SetValue("timeSinceEmpty", Time.time - timeEmptied);
		configNode.SetValue("rippleRate", rippleRate);
		configNode.SetValue("allowReload", allowReload);
		configNode.SetValue("reloadTime", reloadTime);
		configNode.SetValue("died", died);
		configNode.SetValue("qs_firingSalvo", qs_firingSalvo);
		if (qs_firingSalvo)
		{
			configNode.SetValue("qs_shotsPerSalvo", qs_shotsPerSalvo);
			configNode.SetValue("qs_salvos", qs_salvos);
			configNode.SetValue("qs_targetPos", qs_targetPos);
			configNode.SetValue("qs_targetVel", qs_targetVel);
			configNode.SetValue("qs_radius", qs_radius);
			configNode.SetValue("qs_resumeAttack", qs_resumeAttack);
			configNode.SetValue("qs_numFired", qs_numFired);
		}
		ConfigNode eqNode = configNode.AddNode("rocketLauncher");
		rocketLauncher.OnQuicksaveEquip(eqNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(qsNodeName);
		if (node != null)
		{
			timeEmptied = Time.time - node.GetValue<float>("timeSinceEmpty");
			rippleRate = node.GetValue<float>("rippleRate");
			allowReload = node.GetValue<bool>("allowReload");
			reloadTime = node.GetValue<float>("reloadTime");
			died = node.GetValue<bool>("died");
			if (died)
			{
				engageEnemies = false;
				base.enabled = false;
			}
			qs_firingSalvo = node.GetValue<bool>("qs_firingSalvo");
			if (qs_firingSalvo)
			{
				FireSalvo(node.GetValue<int>("qs_shotsPerSalvo") - node.GetValue<int>("qs_numFired"), node.GetValue<int>("qs_salvos"), rippleRate, node.GetValue<FixedPoint>("qs_targetPos").point, node.GetValue<Vector3>("qs_targetVel"), node.GetValue<float>("qs_radius"), node.GetValue<bool>("qs_resumeAttack"));
			}
			ConfigNode node2 = node.GetNode("rocketLauncher");
			if (node2 != null)
			{
				rocketLauncher.OnQuickloadEquip(node2);
			}
		}
	}
}
