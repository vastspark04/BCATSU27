using System.Collections;
using UnityEngine;

public class Cloud : MonoBehaviour
{
	public Color shadeColor;

	public Color lightColor;

	public Light sun;

	private ParticleSystem ps;

	private ParticleSystem.MainModule psMain;

	public bool spawnOnStart;

	public Vector3 rotationAxis;

	private bool ready;

	public float meshScale = 1f;

	public Mesh[] meshes;

	private Vector3[][] verts;

	private Vector3[][] normals;

	[Range(1f, 128f)]
	public int vertexDivisor = 1;

	public float maxDistance;

	public bool fixedRotation;

	private WaitForSeconds lodWait;

	private ParticleSystemRenderer psRenderer;

	private float[] maxLengths;

	private Vector3[][] normals2;

	private void Start()
	{
		if (spawnOnStart)
		{
			SpawnCloud2(base.transform.position);
		}
	}

	private void OnEnable()
	{
		StartCoroutine(LODRoutine());
	}

	private IEnumerator LODRoutine()
	{
		while (!ready)
		{
			yield return null;
		}
		float sqrMaxDist = maxDistance * maxDistance;
		if (!VRHead.instance)
		{
			yield break;
		}
		while (base.enabled)
		{
			yield return lodWait;
			if ((VRHead.instance.transform.position - base.transform.position).sqrMagnitude > sqrMaxDist)
			{
				if (psRenderer.enabled)
				{
					psRenderer.enabled = false;
				}
			}
			else if (!psRenderer.enabled)
			{
				psRenderer.enabled = true;
			}
		}
	}

	private void SetupMesh()
	{
		ps = GetComponent<ParticleSystem>();
		psMain = ps.main;
		psMain.maxParticles = 0;
		psRenderer = ps.GetComponent<ParticleSystemRenderer>();
		int num = meshes.Length;
		verts = new Vector3[num][];
		normals = new Vector3[num][];
		for (int i = 0; i < meshes.Length; i++)
		{
			Mesh mesh = meshes[i];
			verts[i] = mesh.vertices;
			normals[i] = mesh.normals;
			for (int j = 0; j < verts[i].Length; j++)
			{
				normals[i][j] = base.transform.TransformDirection(normals[i][j]);
				if (ps.main.simulationSpace == ParticleSystemSimulationSpace.World)
				{
					verts[i][j] = base.transform.TransformPoint(meshScale * verts[i][j]);
				}
				else
				{
					verts[i][j] = meshScale * verts[i][j];
				}
			}
		}
		lodWait = new WaitForSeconds(2f);
		ready = true;
	}

	public void SpawnCloud(Vector3 position)
	{
		if (!ready)
		{
			SetupMesh();
		}
		if ((bool)EnvironmentManager.instance)
		{
			EnvironmentManager.EnvironmentSetting currentEnvironment = EnvironmentManager.instance.GetCurrentEnvironment();
			sun = currentEnvironment.sun;
			if (currentEnvironment.useCloudColors)
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				materialPropertyBlock.SetColor("_TintColor", currentEnvironment.cloudLightColor);
				materialPropertyBlock.SetColor("_ShadowColor", currentEnvironment.cloudShadowColor);
				psRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
		Vector3 vector = base.transform.InverseTransformVector(position - base.transform.position);
		int num = Random.Range(0, meshes.Length);
		Quaternion quaternion = Quaternion.AngleAxis(Random.Range(0f, 360f), rotationAxis);
		if (fixedRotation)
		{
			psRenderer.alignment = ParticleSystemRenderSpace.Local;
		}
		for (int i = 0; i < verts[num].Length && i < verts[num].Length; i += vertexDivisor)
		{
			psMain.maxParticles++;
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			float t = (Vector3.Dot(sun.transform.forward, quaternion * -normals[num][i]) + 1f) / 2f;
			emitParams.startColor = Color.Lerp(shadeColor, lightColor, t);
			emitParams.position = vector + quaternion * verts[num][i] + Random.insideUnitSphere * 0.2f;
			emitParams.startLifetime = float.MaxValue;
			if (fixedRotation)
			{
				emitParams.rotation3D = Quaternion.LookRotation(normals[num][i]).eulerAngles;
			}
			ps.Emit(emitParams, 1);
		}
	}

	private void SetupMesh2()
	{
		ps = GetComponent<ParticleSystem>();
		psMain = ps.main;
		psMain.maxParticles = 0;
		psRenderer = ps.GetComponent<ParticleSystemRenderer>();
		int num = meshes.Length;
		verts = new Vector3[num][];
		normals = new Vector3[num][];
		maxLengths = new float[num];
		for (int i = 0; i < meshes.Length; i++)
		{
			Mesh mesh = meshes[i];
			verts[i] = mesh.vertices;
			normals[i] = mesh.normals;
			float num2 = 0f;
			for (int j = 0; j < verts[i].Length; j++)
			{
				normals[i][j] = base.transform.TransformDirection(normals[i][j]);
				if (ps.main.simulationSpace == ParticleSystemSimulationSpace.World)
				{
					verts[i][j] = base.transform.TransformPoint(meshScale * verts[i][j]);
				}
				else
				{
					verts[i][j] = meshScale * verts[i][j];
				}
				float magnitude = verts[i][j].magnitude;
				if (magnitude > num2)
				{
					num2 = magnitude;
				}
			}
			maxLengths[i] = num2;
		}
		lodWait = new WaitForSeconds(2f);
		ready = true;
	}

	public void SpawnCloud2(Vector3 position)
	{
		if (!ready)
		{
			SetupMesh2();
		}
		if ((bool)EnvironmentManager.instance)
		{
			EnvironmentManager.EnvironmentSetting currentEnvironment = EnvironmentManager.instance.GetCurrentEnvironment();
			sun = currentEnvironment.sun;
			if (currentEnvironment.useCloudColors)
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				materialPropertyBlock.SetColor("_TintColor", currentEnvironment.cloudLightColor);
				materialPropertyBlock.SetColor("_ShadowColor", currentEnvironment.cloudShadowColor);
				psRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
		Vector3 vector = base.transform.InverseTransformVector(position - base.transform.position);
		int num = Random.Range(0, meshes.Length);
		Quaternion quaternion = Quaternion.AngleAxis(Random.Range(0f, 360f), rotationAxis);
		if (fixedRotation)
		{
			psRenderer.alignment = ParticleSystemRenderSpace.Local;
		}
		for (int i = 0; i < verts[num].Length && i < verts[num].Length; i += vertexDivisor)
		{
			psMain.maxParticles++;
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			float num2 = (Vector3.Dot(sun.transform.forward, quaternion * -normals[num][i]) + 1f) / 2f;
			num2 *= num2;
			Vector3 b = verts[num][Random.Range(0, verts[num].Length)];
			float num3 = Random.Range(0f, 1f);
			Vector3 vector2 = quaternion * Vector3.Lerp(verts[num][i], b, num3);
			emitParams.position = vector2 + vector;
			Vector3 rhs = Vector3.Project(vector2, sun.transform.forward);
			Mathf.Sign(Vector3.Dot(-sun.transform.forward, rhs));
			_ = rhs.magnitude;
			float a = num3 * num2;
			Color color = Color.Lerp(shadeColor, lightColor, num2);
			color.a = a;
			emitParams.startColor = color;
			emitParams.startLifetime = float.MaxValue;
			if (fixedRotation)
			{
				emitParams.rotation3D = Quaternion.LookRotation(normals[num][i]).eulerAngles;
			}
			ps.Emit(emitParams, 1);
		}
	}
}
