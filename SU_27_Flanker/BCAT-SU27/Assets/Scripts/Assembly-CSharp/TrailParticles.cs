using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class TrailParticles : MonoBehaviour
{
	public enum EmissionModes
	{
		Rate,
		Distance
	}

	private class LineParticle
	{
		public bool alive;

		public float time;

		public Vector3 position;

		public Vector3 velocity;

		public LineParticle(Vector3 position, Vector3 velocity)
		{
			alive = false;
			this.position = position;
			this.velocity = velocity;
			time = 0f;
		}
	}

	public EmissionModes mode;

	public Vector3 startSpeed;

	public ParticleSystemSimulationSpace space;

	public int maxParticles = 100;

	public float lifeTime = 3f;

	public float emissionRate = 10f;

	public float minDistance = 0.1f;

	private Vector3 lastPos;

	public Vector3 forceOverLifetime;

	public bool localForce;

	public float drag;

	[Header("Color")]
	public Color startColor;

	public Gradient colorOverTime;

	private GradientColorKey[] colorKeys;

	private GradientAlphaKey[] alphaKeys;

	private Stack<Color>[] colorStack = new Stack<Color>[8];

	private int numAlive;

	private int lastMaxParticles;

	private LineRenderer lr;

	private Gradient lrGradient;

	private LineParticle[] particles;

	private Vector3[] positions;

	private int newestIdx = -1;

	private float lastSpawnTime;

	private const int colorFrameInterval = 8;

	private int frameIdx;

	private Vector3 zeroPosition
	{
		get
		{
			if (space != ParticleSystemSimulationSpace.World)
			{
				return Vector3.zero;
			}
			return base.transform.position;
		}
	}

	private Vector3 forward
	{
		get
		{
			if (space != ParticleSystemSimulationSpace.World)
			{
				return Vector3.forward;
			}
			return base.transform.forward;
		}
	}

	private Vector3 right
	{
		get
		{
			if (space != ParticleSystemSimulationSpace.World)
			{
				return Vector3.right;
			}
			return base.transform.right;
		}
	}

	private Vector3 up
	{
		get
		{
			if (space != ParticleSystemSimulationSpace.World)
			{
				return Vector3.up;
			}
			return base.transform.up;
		}
	}

	private void OnEnable()
	{
		if (!lr)
		{
			lr = GetComponent<LineRenderer>();
			if (!lr)
			{
				lr = base.gameObject.AddComponent<LineRenderer>();
			}
		}
		lastMaxParticles = maxParticles;
		if (particles == null)
		{
			CreateParticleArray();
		}
		lastSpawnTime = Time.time;
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		}
		frameIdx = Random.Range(0, 8);
	}

	private void Start()
	{
		lr.useWorldSpace = space == ParticleSystemSimulationSpace.World;
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		if (space == ParticleSystemSimulationSpace.World && particles != null && particles.Length != 0)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i].position += offset;
			}
			lastPos += offset;
		}
	}

	private void OnDisable()
	{
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift -= FloatingOrigin_instance_OnOriginShift;
		}
	}

	private void OnValidate()
	{
		if (maxParticles < 3)
		{
			maxParticles = 3;
		}
		if (emissionRate <= 0f)
		{
			emissionRate = 0.01f;
		}
		if (lifeTime < 0.01f)
		{
			lifeTime = 0.01f;
		}
		if (!lr)
		{
			lr = GetComponent<LineRenderer>();
			if (!lr)
			{
				lr = base.gameObject.AddComponent<LineRenderer>();
			}
		}
		if (lr.useWorldSpace != (space == ParticleSystemSimulationSpace.World))
		{
			lr.useWorldSpace = space == ParticleSystemSimulationSpace.World;
		}
	}

	private void LateUpdate()
	{
		if (maxParticles < 3)
		{
			return;
		}
		frameIdx++;
		bool flag = frameIdx >= 7;
		if (flag)
		{
			frameIdx = 0;
		}
		if (lastMaxParticles != maxParticles || particles == null)
		{
			CreateParticleArray();
		}
		if (mode == EmissionModes.Distance)
		{
			if ((lastPos - base.transform.position).sqrMagnitude > minDistance * minDistance)
			{
				SpawnNewParticle();
				lastPos = base.transform.position;
			}
		}
		else if (mode == EmissionModes.Rate && Time.time - lastSpawnTime > 1f / emissionRate)
		{
			SpawnNewParticle();
		}
		if (lr.positionCount != numAlive)
		{
			lr.positionCount = numAlive;
		}
		int num = newestIdx;
		int num2 = 0;
		while (newestIdx >= 0 && num2 < maxParticles)
		{
			LineParticle lineParticle = particles[num];
			if (lineParticle.alive)
			{
				lineParticle.time += Time.deltaTime;
				if (lineParticle.time > lifeTime)
				{
					lineParticle.alive = false;
					numAlive--;
				}
				lineParticle.position += lineParticle.velocity * Time.deltaTime;
				if (num == newestIdx)
				{
					positions[num2] = zeroPosition;
				}
				else
				{
					positions[num2] = lineParticle.position;
				}
				if (flag)
				{
					float num3 = lineParticle.time / lifeTime;
					Color item = startColor * colorOverTime.Evaluate(num3);
					int num4 = Mathf.Min(Mathf.FloorToInt(num3 * 8f), 7);
					colorStack[num4].Push(item);
				}
			}
			num--;
			if (num < 0)
			{
				num = maxParticles - 1;
			}
			num2++;
		}
		lr.SetPositions(positions);
		if (!flag)
		{
			return;
		}
		for (int i = 0; i < 8; i++)
		{
			int num5 = 0;
			Color color = new Color(0f, 0f, 0f, 0f);
			while (colorStack[i].Count > 0)
			{
				color += colorStack[i].Pop();
				num5++;
			}
			if (num5 > 0)
			{
				color /= (float)num5;
				colorKeys[i].color = color;
				alphaKeys[i].alpha = color.a;
			}
			else
			{
				colorKeys[i].color = Color.white;
				alphaKeys[i].alpha = 0f;
			}
		}
		lrGradient.SetKeys(colorKeys, alphaKeys);
		lr.colorGradient = lrGradient;
	}

	private void SpawnNewParticle()
	{
		lastSpawnTime = Time.time;
		int num = (newestIdx + 1) % maxParticles;
		if (!particles[num].alive)
		{
			newestIdx = num;
			particles[newestIdx].alive = true;
			particles[newestIdx].position = zeroPosition;
			particles[newestIdx].velocity = startSpeed.x * right + startSpeed.y * up + startSpeed.z * forward;
			particles[newestIdx].time = 0f;
			numAlive++;
		}
	}

	[ContextMenu("Reset Particles")]
	public void ResetParticles()
	{
		if (particles != null)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i].alive = false;
				particles[i].position = zeroPosition;
				particles[i].velocity = Vector3.zero;
			}
			lastSpawnTime = 0f;
			newestIdx = -1;
			numAlive = 0;
		}
	}

	private void CreateParticleArray()
	{
		lastMaxParticles = maxParticles;
		particles = new LineParticle[maxParticles];
		for (int i = 0; i < maxParticles; i++)
		{
			particles[i] = new LineParticle(Vector3.zero, Vector3.zero);
		}
		positions = new Vector3[maxParticles];
		newestIdx = -1;
		numAlive = 0;
		lrGradient = new Gradient();
		colorKeys = new GradientColorKey[8];
		alphaKeys = new GradientAlphaKey[8];
		lrGradient.SetKeys(colorKeys, alphaKeys);
		lr.colorGradient = lrGradient;
		for (int j = 0; j < 8; j++)
		{
			colorKeys[j].time = (alphaKeys[j].time = (float)j / 7f);
			colorStack[j] = new Stack<Color>();
		}
		lr.positionCount = maxParticles;
	}
}
