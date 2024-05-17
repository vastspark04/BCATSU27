using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtilleryUnit : MonoBehaviour, IArtilleryUnit, IEngageEnemies
{
	private class FireOrder
	{
		public FixedPoint fp;

		public int count;

		public int salvos;

		public virtual Vector3 point => fp.point;

		public FireOrder()
		{
		}

		public FireOrder(Vector3 worldPosition, int count, int salvos)
		{
			fp = new FixedPoint(worldPosition);
			this.salvos = salvos;
			this.count = count;
		}
	}

	private class ActorFireOrder : FireOrder
	{
		public float timeOrdered;

		public Vector3 aVel;

		public override Vector3 point => fp.point + aVel * (Time.time - timeOrdered);

		public ActorFireOrder(Actor a, int count, int salvos)
		{
			base.count = count;
			base.salvos = salvos;
			fp = new FixedPoint(a.position);
			timeOrdered = Time.time;
			aVel = a.velocity;
		}
	}

	public Gun gun;

	public ModuleTurret turret;

	public GroundUnitMover mover;

	public int burstCount = 1;

	public float burstInterval = 3f;

	public float fireInterval = 6f;

	public VisualTargetFinder targetFinder;

	private bool dead;

	private Vector3 targetPosition;

	private Queue<FireOrder> fireOrders = new Queue<FireOrder>();

	private Coroutine artyRoutine;

	private bool autoEngageEnemies;

	private void OnEnable()
	{
		if (!dead)
		{
			artyRoutine = StartCoroutine(ArtilleryRoutine());
		}
	}

	public void FireOnPosition(Vector3 position, int shotsPerSalvo, int salvos)
	{
		fireOrders.Enqueue(new FireOrder(position, shotsPerSalvo, salvos));
	}

	public void FireOnTransform(Transform tf)
	{
		fireOrders.Enqueue(new FireOrder(tf.position, burstCount, 1));
	}

	public void FireOnPosition(Vector3 position, int count)
	{
		fireOrders.Enqueue(new FireOrder(position, burstCount, count));
	}

	public void ClearFireOrders()
	{
		fireOrders.Clear();
	}

	public void Die()
	{
		dead = true;
		if (artyRoutine != null)
		{
			StopCoroutine(artyRoutine);
		}
	}

	private IEnumerator ArtilleryRoutine()
	{
		yield return null;
		while (base.enabled)
		{
			if (fireOrders.Count > 0)
			{
				if ((bool)mover)
				{
					mover.move = false;
				}
				FireOrder fireOrder = fireOrders.Dequeue();
				bool fireDirect = !Physics.Linecast(turret.referenceTransform.position, fireOrder.point + Vector3.up, 1);
				if (gun.GetBallisticDirection(fireOrder.point, Vector3.zero, out var ballisticDirection, fireDirect))
				{
					bool fired = false;
					while (!fired)
					{
						Vector3 vector = turret.pitchTransform.position + ballisticDirection * 5000f;
						turret.AimToTarget(vector);
						if (Vector3.Dot(gun.fireTransforms[0].forward, ballisticDirection) > 0.999f)
						{
							for (float t = 0f; t < 1f; t += Time.deltaTime)
							{
								if (fireOrder is ActorFireOrder)
								{
									gun.GetBallisticDirection(fireOrder.point, Vector3.zero, out ballisticDirection, fireDirect);
								}
								vector = turret.pitchTransform.position + ballisticDirection * 5000f;
								turret.AimToTarget(vector);
								yield return null;
							}
							for (int i = 0; i < fireOrder.salvos; i++)
							{
								for (int b = 0; b < fireOrder.count; b++)
								{
									gun.SetFire(fire: true);
									yield return null;
									gun.SetFire(fire: false);
									yield return new WaitForSeconds(burstInterval);
								}
								yield return new WaitForSeconds(fireInterval);
							}
							fired = true;
						}
						else
						{
							yield return null;
						}
					}
				}
				ballisticDirection = default(Vector3);
			}
			else if (autoEngageEnemies && (bool)targetFinder && (bool)targetFinder.attackingTarget)
			{
				FireOnActor(targetFinder.attackingTarget, burstCount, 1);
			}
			else
			{
				turret.ReturnTurret();
			}
			if ((bool)mover)
			{
				mover.move = true;
			}
			yield return null;
		}
	}

	public void FireOnPosition(FixedPoint targetPosition, Vector3 targetVelocity, int shotsPerSalvo, int salvos)
	{
		FireOnPosition(targetPosition.point, shotsPerSalvo, salvos);
	}

	public void FireOnPositionRadius(FixedPoint targetPosition, float radius, int shotsPerSalvo, int salvos)
	{
		Vector3 position = targetPosition.point + Vector3.ProjectOnPlane(Random.insideUnitSphere * radius, Vector3.up);
		FireOnPosition(position, shotsPerSalvo, salvos);
	}

	public void FireOnActor(Actor a, int shotsPerSalvo, int salvos)
	{
		fireOrders.Enqueue(new ActorFireOrder(a, shotsPerSalvo, salvos));
	}

	public void SetEngageEnemies(bool engage)
	{
		autoEngageEnemies = engage;
	}
}
