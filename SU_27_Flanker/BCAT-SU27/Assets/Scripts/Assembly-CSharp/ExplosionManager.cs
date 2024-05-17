using System;
using System.Collections;
using UnityEngine;
using VTOLVR.Multiplayer;

public class ExplosionManager : MonoBehaviour
{
	public enum ExplosionTypes
	{
		Small,
		Medium,
		Massive,
		Aerial,
		DebrisPoof,
		MediumAerial
	}

	private struct DmgPair
	{
		public Health health;

		public Vector3 position;

		public float damage;
	}

	public GameObject debrisPoofPrefab;

	public GameObject explosionPrefab;

	public GameObject airExplosionPrefab;

	public GameObject mediumAirExplosionPrefab;

	public GameObject mediumExplosionPrefab;

	public GameObject massiveExplosionPrefab;

	private const int DMG_BUFFER_SIZE = 32;

	private static DmgPair[] dmgPairBuffer = new DmgPair[32];

	private static Collider[] explosionColliderBuffer = new Collider[64];

	public static ExplosionManager instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	public void CreateExplosionEffect(ExplosionTypes type, Vector3 position, Vector3 normal)
	{
		GameObject original;
		float num;
		switch (type)
		{
		case ExplosionTypes.Aerial:
			original = airExplosionPrefab;
			num = 70f;
			break;
		case ExplosionTypes.MediumAerial:
			original = mediumAirExplosionPrefab;
			num = 120f;
			break;
		case ExplosionTypes.Massive:
			original = massiveExplosionPrefab;
			num = 450f;
			break;
		case ExplosionTypes.Medium:
			original = mediumExplosionPrefab;
			num = 175f;
			break;
		case ExplosionTypes.DebrisPoof:
			original = debrisPoofPrefab;
			num = 10f;
			break;
		default:
			original = explosionPrefab;
			num = 40f;
			break;
		}
		UnityEngine.Object.Instantiate(original, position, Quaternion.LookRotation(normal));
		if ((bool)FlybyCameraMFDPage.instance && FlybyCameraMFDPage.instance.isCamEnabled && (bool)FlybyCameraMFDPage.instance.flybyCam)
		{
			try
			{
				float num2 = num * num / (FlybyCameraMFDPage.instance.flybyCam.transform.position - position).sqrMagnitude;
				FlybyCameraMFDPage.instance.ShakeCamera(num2 * 2f);
			}
			catch (NullReferenceException arg)
			{
				Debug.LogError($"Got an NRE creating an explosion effect despite checking for references first! Likely after a NetDestroy command.\n{arg}");
			}
		}
	}

	private static bool PointIsInside(Vector3 position, Collider col)
	{
		if (col is SphereCollider)
		{
			SphereCollider sphereCollider = (SphereCollider)col;
			if ((position - sphereCollider.transform.position).sqrMagnitude < sphereCollider.radius * sphereCollider.radius)
			{
				return true;
			}
		}
		else if (col is BoxCollider)
		{
			BoxCollider boxCollider = (BoxCollider)col;
			if (new Bounds(boxCollider.center, boxCollider.size).Contains(boxCollider.transform.InverseTransformPoint(position)))
			{
				return true;
			}
		}
		return false;
	}

	public void CreateDamageExplosion(Vector3 position, float radius, float damage, Actor sourceActor, Vector3 sourceVelocity, Collider directHit = null, bool debugMode = false, PlayerInfo sourcePlayer = null)
	{
		if (!(damage > 0f) || !(radius > 0f))
		{
			return;
		}
		Vector3 vector = sourceVelocity * Time.fixedDeltaTime;
		float magnitude = vector.magnitude;
		int layerMask = 9473;
		Vector3 vector2 = position + vector;
		int num = Physics.OverlapCapsuleNonAlloc(position, vector2, radius, explosionColliderBuffer, layerMask);
		if (debugMode)
		{
			Debug.LogFormat("Debugging explosion at {0} from {1}", position, sourceActor.DebugName());
		}
		if (num == 0)
		{
			return;
		}
		Collider[] array = explosionColliderBuffer;
		int num2 = 0;
		if ((bool)directHit)
		{
			Hitbox component = directHit.GetComponent<Hitbox>();
			if ((bool)component && (bool)component.health)
			{
				dmgPairBuffer[0] = new DmgPair
				{
					health = component.health,
					position = position,
					damage = damage
				};
				num2 = 1;
				if (debugMode)
				{
					Debug.LogFormat("- Direct hit: {0}", UIUtils.GetHierarchyString(component.gameObject));
				}
			}
		}
		float num3 = radius * radius;
		Vector3 vector3 = Vector3.Lerp(position, vector2, 0.5f);
		for (int i = 0; i < num && num2 < 32; i++)
		{
			float num4 = -1f;
			Hitbox hitbox = array[i].GetComponent<Hitbox>();
			if (!hitbox)
			{
				continue;
			}
			if (debugMode)
			{
				Debug.Log("- Explosion found hitbox: " + UIUtils.GetHierarchyString(hitbox.gameObject));
			}
			if ((bool)hitbox.actor && (bool)sourceActor && (bool)hitbox.actor.GetMissile() && hitbox.actor.team == sourceActor.team)
			{
				if (debugMode)
				{
					Debug.Log(" - - hitbox is a missile on the same team! Ignoring.");
				}
				continue;
			}
			if (hitbox.hitProbability < 1f && UnityEngine.Random.Range(0f, 1f) > hitbox.hitProbability)
			{
				continue;
			}
			int num5;
			Vector3 vector4;
			if (array[i] is MeshCollider)
			{
				num5 = (((MeshCollider)array[i]).convex ? 1 : 0);
				if (num5 == 0)
				{
					vector4 = array[i].transform.position;
					goto IL_0216;
				}
			}
			else
			{
				num5 = 1;
			}
			vector4 = array[i].ClosestPoint(vector3);
			goto IL_0216;
			IL_0216:
			Vector3 vector5 = vector4;
			Vector3 vector6 = vector3 + Vector3.ClampMagnitude(Vector3.Project(vector5 - vector3, sourceVelocity), magnitude / 2f);
			Vector3 vector7 = vector6;
			Vector3 vector8 = ((num5 != 0) ? array[i].ClosestPoint(vector6) : array[i].transform.position);
			Vector3 position2 = position;
			if (Physics.Raycast(new Ray(vector7, vector8 - vector7), out var hitInfo, radius * 2f, layerMask))
			{
				if (hitInfo.collider == array[i])
				{
					num4 = (hitInfo.point - vector7).sqrMagnitude;
					position2 = hitInfo.point;
					if (debugMode)
					{
						Debug.Log(" - - raycast hit the hitbox, dist: " + Mathf.Sqrt(num4));
					}
				}
				else
				{
					if (debugMode)
					{
						Debug.Log(" - - raycast did not hit the detected target. Instead: " + UIUtils.GetHierarchyString(hitInfo.collider.gameObject));
					}
					Hitbox component2 = hitInfo.collider.GetComponent<Hitbox>();
					if ((bool)component2 && (bool)component2.actor && !component2.actor.GetMissile())
					{
						hitbox = component2;
						num4 = (hitInfo.point - vector7).sqrMagnitude;
						position2 = hitInfo.point;
						if (debugMode)
						{
							Debug.Log(" - - it has a hitbox; damaging it instead.");
						}
					}
					else if (PointIsInside(position, array[i]))
					{
						num4 = 0.01f;
						position2 = position;
						if (debugMode)
						{
							Debug.Log(" - - detonated inside the collider!");
						}
					}
				}
			}
			else if (PointIsInside(vector7, array[i]))
			{
				num4 = 0.01f;
				position2 = position;
				if (debugMode)
				{
					Debug.Log(" - - no raycast hit but origin was inside the collider!");
				}
			}
			else if (debugMode)
			{
				Debug.Log(" - - did not pass raycast test");
			}
			if (!(num4 > 0f) || !(num4 < num3))
			{
				continue;
			}
			float num6 = Mathf.Sqrt(num4);
			float num7 = (radius - num6) / radius;
			float num8 = damage * num7 * num7;
			num8 -= hitbox.subtractiveArmor;
			if (!(num8 > 0f))
			{
				continue;
			}
			for (int j = 0; j < 32; j++)
			{
				if (dmgPairBuffer[j].health == null)
				{
					dmgPairBuffer[j] = new DmgPair
					{
						health = hitbox.health,
						position = position2,
						damage = num8
					};
					num2++;
					j = 32;
				}
				else if (dmgPairBuffer[j].health == hitbox.health)
				{
					if (dmgPairBuffer[j].damage < num8)
					{
						dmgPairBuffer[j] = new DmgPair
						{
							health = hitbox.health,
							position = position2,
							damage = num8
						};
					}
					j = 32;
				}
			}
		}
		for (int k = 0; k < num2; k++)
		{
			DmgPair dmgPair = dmgPairBuffer[k];
			dmgPair.health.Damage(dmgPair.damage, dmgPair.position, Health.DamageTypes.Impact, sourceActor, "explosion", rpcIfRemote: true, sourcePlayer);
			if (debugMode)
			{
				Debug.Log("Explosion is damaging " + UIUtils.GetHierarchyString(dmgPair.health.gameObject) + " for " + dmgPair.damage);
			}
			dmgPairBuffer[k] = default(DmgPair);
		}
		DoWaterSplashes(position, sourceVelocity, 4f * radius);
	}

	private Vector3 ClosestPointSphereColliderToLineSegment(Vector3 seg1, Vector3 seg2, SphereCollider sc, out Vector3 onSegPt)
	{
		Vector3 vector = sc.transform.TransformPoint(sc.center);
		Vector3 lossyScale = sc.transform.lossyScale;
		float num = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z) * sc.radius;
		Vector3 vector2 = Vector3.Lerp(seg1, seg2, 0.5f);
		Vector3 onNormal = seg2 - seg1;
		float magnitude = onNormal.magnitude;
		onSegPt = vector2 + Vector3.ClampMagnitude(Vector3.Project(vector - vector2, onNormal), magnitude / 2f);
		return (onSegPt - vector).normalized * num;
	}

	private float DoWaterSplashes(Vector3 position, Vector3 velocity, float radius)
	{
		float num = 0f;
		if (WaterPhysics.GetAltitude(position) < radius && !Physics.Raycast(position, Vector3.down, Mathf.Min(radius, WaterPhysics.GetAltitude(position)), 1))
		{
			int num2 = Mathf.CeilToInt(UnityEngine.Random.Range(25f, 50f) * (radius / 200f));
			float num3 = 300f;
			for (int i = 0; i < num2; i++)
			{
				Vector3 direction = UnityEngine.Random.onUnitSphere * num3;
				direction.y *= 0f - Mathf.Sign(direction.y);
				direction += velocity;
				Ray ray = new Ray(position, direction);
				if (WaterPhysics.instance.waterPlane.Raycast(ray, out var enter) && enter < radius)
				{
					float num4 = enter / num3;
					num = Mathf.Max(num, num4);
					StartCoroutine(WaterSplashDelayed(num4, new FixedPoint(ray.GetPoint(enter))));
				}
			}
		}
		return num;
	}

	private IEnumerator WaterSplashDelayed(float t, FixedPoint pt)
	{
		yield return new WaitForSeconds(t);
		BulletHitManager.instance.CreateSplash(pt.point);
	}
}
