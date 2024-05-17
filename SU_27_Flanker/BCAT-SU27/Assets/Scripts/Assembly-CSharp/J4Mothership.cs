using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class J4Mothership : MonoBehaviour
{
	public class J4Fighter : MonoBehaviour
	{
		public J4Mothership mother;

		public AIWing wing;

		public AIPilot aiPilot;

		private void Start()
		{
			if ((bool)mother)
			{
				mother.currFighterCount++;
			}
		}

		private void OnDestroy()
		{
			if ((bool)mother)
			{
				mother.currFighterCount--;
				mother.fighters.Remove(this);
			}
			if ((bool)wing)
			{
				wing.pilots.Remove(aiPilot);
				if (wing.pilots.Count > 0)
				{
					wing.UpdateLeader();
				}
				else
				{
					Object.Destroy(wing.gameObject);
				}
			}
		}

		public void ReturnToMother()
		{
			aiPilot.SetEngageEnemies(engage: false);
			aiPilot.StopCombat();
			FollowPath followPath = null;
			float num = float.MaxValue;
			for (int i = 0; i < mother.returnPaths.Length; i++)
			{
				FollowPath followPath2 = mother.returnPaths[i];
				float sqrMagnitude = (followPath2.pointTransforms[0].position - base.transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					followPath = followPath2;
				}
			}
			aiPilot.FlyNavPath(followPath);
			StartCoroutine(DestroyOnReturnRoutine(followPath.pointTransforms[followPath.pointTransforms.Length - 1]));
		}

		public void OrbitMother(float radius, float altitude)
		{
			aiPilot.SetEngageEnemies(engage: false);
			aiPilot.StopCombat();
			aiPilot.defaultAltitude = altitude;
			aiPilot.orbitRadius = radius;
			aiPilot.OrbitTransform(mother.transform);
		}

		private IEnumerator DestroyOnReturnRoutine(Transform tgtTf)
		{
			while ((base.transform.position - tgtTf.position).sqrMagnitude > 900f)
			{
				yield return null;
			}
			Object.Destroy(base.gameObject);
		}
	}

	public GameObject fighterPrefab;

	public Transform[] spawnTfs;

	private int spawnIdx;

	public float wingSpawnInterval;

	public float fighterSpawnInterval;

	public MinMax altRange;

	public MinMax radiusRange;

	public int wingCount;

	public int maxFighterCount;

	[HideInInspector]
	public int currFighterCount;

	[Header("MegaBeam")]
	public Animator beamAnimator;

	public float explosionDelay;

	public GameObject explosionObject;

	public Transform explodeSphereTf;

	public FollowPath[] returnPaths;

	public Transform shipTransform;

	public float startAltitude;

	public float targetAltitude;

	public float entranceLerpRate;

	public float exitSpeed;

	public float exitAccel;

	public float exitDelay;

	[HideInInspector]
	public List<J4Fighter> fighters = new List<J4Fighter>();

	private Coroutine enterRoutine;

	private Coroutine spawnRoutine;

	private bool beamFired;

	private void Awake()
	{
		shipTransform.localPosition = new Vector3(0f, startAltitude, 0f);
	}

	public void Enter()
	{
		enterRoutine = StartCoroutine(EnterRoutine());
	}

	private IEnumerator EnterRoutine()
	{
		bool startedSpawning = false;
		Vector3 tgtPos = new Vector3(0f, targetAltitude, 0f);
		while (shipTransform.localPosition.y > targetAltitude + 5f)
		{
			shipTransform.localPosition = Vector3.Lerp(shipTransform.localPosition, tgtPos, entranceLerpRate * Time.deltaTime);
			if (shipTransform.localPosition.y < targetAltitude + 1500f && !startedSpawning)
			{
				startedSpawning = true;
				spawnRoutine = StartCoroutine(SpawnRoutine());
			}
			yield return null;
		}
	}

	private IEnumerator SpawnRoutine()
	{
		while (base.enabled)
		{
			while (currFighterCount + wingCount > maxFighterCount)
			{
				yield return null;
			}
			Transform spawnTf = spawnTfs[spawnIdx];
			AIWing wing = new GameObject("j4Wing").AddComponent<AIWing>();
			float alt = altRange.Random();
			float orbitRadius = radiusRange.Random();
			for (int i = 0; i < wingCount; i++)
			{
				GameObject obj = Object.Instantiate(fighterPrefab, spawnTf.position, spawnTf.rotation, null);
				AIPilot component = obj.GetComponent<AIPilot>();
				obj.SetActive(value: true);
				component.kPlane.SetToKinematic();
				component.kPlane.SetVelocity(spawnTf.forward * 200f);
				component.OrbitTransform(base.transform);
				component.orbitRadius = orbitRadius;
				component.defaultAltitude = alt;
				wing.pilots.Add(component);
				J4Fighter j4Fighter = obj.AddComponent<J4Fighter>();
				j4Fighter.wing = wing;
				j4Fighter.mother = this;
				j4Fighter.aiPilot = component;
				fighters.Add(j4Fighter);
				yield return new WaitForSeconds(fighterSpawnInterval);
			}
			spawnIdx = (spawnIdx + 1) % spawnTfs.Length;
			yield return new WaitForSeconds(wingSpawnInterval);
			yield return null;
		}
	}

	public void FireBeam()
	{
		if (!beamFired)
		{
			beamFired = true;
			beamAnimator.SetTrigger("fire");
			StartCoroutine(ExplosionDamageRoutine());
			StartCoroutine(ExitRoutine());
		}
	}

	private void RecallFighters()
	{
		foreach (J4Fighter fighter in fighters)
		{
			if (fighter.aiPilot.actor.alive)
			{
				fighter.ReturnToMother();
			}
		}
	}

	private IEnumerator ExitRoutine()
	{
		RecallFighters();
		if (spawnRoutine != null)
		{
			StopCoroutine(spawnRoutine);
		}
		if (enterRoutine != null)
		{
			StopCoroutine(enterRoutine);
		}
		yield return new WaitForSeconds(exitDelay);
		float speed = exitSpeed;
		while (shipTransform.localPosition.y < startAltitude)
		{
			shipTransform.localPosition += new Vector3(0f, speed * Time.deltaTime, 0f);
			speed += exitAccel * Time.deltaTime;
			yield return null;
		}
	}

	private IEnumerator ExplosionDamageRoutine()
	{
		yield return new WaitForSeconds(explosionDelay);
		explosionObject.SetActive(value: true);
		explosionObject.transform.parent = null;
		float r = 0f;
		while (r < 20000f)
		{
			float num = r * r;
			for (int i = 0; i < TargetManager.instance.allActors.Count; i++)
			{
				Actor actor = TargetManager.instance.allActors[i];
				if (actor.alive && (actor.position - base.transform.position).sqrMagnitude < num && actor.transform != base.transform)
				{
					Health component = actor.GetComponent<Health>();
					if ((bool)component)
					{
						component.Kill();
					}
				}
			}
			explodeSphereTf.localScale = 2f * r * Vector3.one;
			r += 343f * Time.deltaTime;
			yield return null;
		}
		explodeSphereTf.gameObject.SetActive(value: false);
	}
}
