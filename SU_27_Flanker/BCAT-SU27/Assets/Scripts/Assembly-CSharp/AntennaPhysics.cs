using System;
using UnityEngine;

[RequireComponent(typeof(AntennaRenderer))]
[ExecuteInEditMode]
public class AntennaPhysics : MonoBehaviour
{
	public int vertexCount;

	public float length;

	public float mass;

	public float spring;

	public float damping;

	public float drag;

	public float startWidth;

	public float endWidth;

	public int meshResolution = 6;

	public Material material;

	private Vector3[] vertices;

	private Vector3[] defaultVertices;

	private Vector3 massPos;

	private Vector3 massVel;

	private Vector3 massCenter;

	private Vector3 lastParentPos;

	private Vector3 lastParentVel = Vector3.zero;

	public Vector3 wind;

	public bool dynamicUpdate;

	private AntennaRenderer ar;

	public GameObject endPiecePrefab;

	private Transform endPieceTransform;

	private Rigidbody rb;

	private void Awake()
	{
		if (!ar)
		{
			ar = GetComponent<AntennaRenderer>();
		}
		if (Application.isPlaying)
		{
			SetupRenderer();
		}
		else
		{
			ar.material = material;
		}
		rb = GetComponentInParent<Rigidbody>();
	}

	private void Start()
	{
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		lastParentPos += offset;
	}

	private void Update()
	{
		if (!Application.isPlaying)
		{
			ar.crossSegments = meshResolution;
			ar.startWidth = startWidth;
			ar.endWidth = endWidth;
			ar.SetPoints(new Vector3[2]
			{
				Vector3.zero,
				length * Vector3.up
			}, 0f, Color.white, worldSpace: false);
			if (!ar.material)
			{
				ar.material = material;
			}
		}
		if (Application.isPlaying && dynamicUpdate)
		{
			UpdatePhysics(Time.deltaTime);
			UpdateVertices();
			UpdateRenderer();
		}
	}

	private void FixedUpdate()
	{
		if (Application.isPlaying && !dynamicUpdate)
		{
			UpdatePhysics(Time.fixedDeltaTime);
			UpdateVertices();
			UpdateRenderer();
		}
	}

	private void UpdatePhysics(float deltaTime)
	{
		if (deltaTime != 0f)
		{
			Vector3 vector = (base.transform.position - lastParentPos) / deltaTime;
			if ((bool)rb)
			{
				vector = rb.GetPointVelocity(base.transform.position);
			}
			Vector3 vector2 = (vector - lastParentVel) / deltaTime;
			Vector3 vector3 = base.transform.InverseTransformVector(vector2);
			lastParentPos = base.transform.position;
			lastParentVel = vector;
			Vector3 vector4 = base.transform.InverseTransformVector(vector - wind) + massVel;
			Vector3 vector5 = drag * vector4;
			Vector3 vector6 = spring * (massCenter - massPos);
			Vector3 vector7 = damping * massVel;
			Vector3 vector8 = mass * base.transform.InverseTransformVector(Physics.gravity);
			Vector3 vector9 = (vector6 - vector5 + vector8 - vector7) / mass - vector3;
			massVel += vector9 * deltaTime;
			if (float.IsNaN(massVel.x))
			{
				massVel = Vector3.zero;
				massPos = massCenter;
			}
			massPos += massVel * deltaTime;
			float num = (1f - Vector3.Angle(massPos, Vector3.up) / 720f) * length;
			massPos = massPos.normalized * num;
		}
	}

	private void UpdateVertices()
	{
		float num = Vector3.Angle(massPos, Vector3.up);
		float num2 = length / ((float)vertexCount - 1f);
		for (int i = 1; i < vertexCount; i++)
		{
			float num3 = (float)i / (float)vertexCount;
			float maxRadiansDelta = 2f * num3 * num * ((float)Math.PI / 180f);
			vertices[i] = Vector3.RotateTowards(Vector3.up, massPos - vertices[i - 1], maxRadiansDelta, 0f);
			vertices[i] = vertices[i - 1] + vertices[i] * num2;
		}
	}

	private void SetupRenderer()
	{
		float num = length / ((float)vertexCount - 1f);
		vertices = new Vector3[vertexCount];
		defaultVertices = new Vector3[vertexCount];
		for (int i = 0; i < vertexCount; i++)
		{
			vertices[i] = Vector3.zero + (float)i * num * Vector3.up;
			defaultVertices[i] = vertices[i];
		}
		massCenter = length * Vector3.up;
		massPos = massCenter;
		lastParentPos = base.transform.position;
		ar.SetPoints(vertices, 0f, Color.white, worldSpace: false);
		if ((bool)endPiecePrefab)
		{
			endPieceTransform = UnityEngine.Object.Instantiate(endPiecePrefab, base.transform.TransformPoint(massPos), Quaternion.LookRotation(base.transform.forward, base.transform.up), base.transform).transform;
		}
	}

	private void UpdateRenderer()
	{
		ar.material = material;
		ar.startWidth = startWidth;
		ar.endWidth = endWidth;
		ar.crossSegments = Mathf.Max(2, meshResolution);
		ar.SetPoints(vertices, 0f, Color.white, worldSpace: false);
		if ((bool)endPieceTransform)
		{
			endPieceTransform.localPosition = vertices[vertices.Length - 1];
			Vector3 vector = vertices[vertices.Length - 1] - vertices[vertices.Length - 2];
			Vector3 forward = Vector3.Cross(vector, Vector3.left);
			endPieceTransform.localRotation = Quaternion.LookRotation(forward, vector);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
		if (!Application.isPlaying)
		{
			massPos = length * Vector3.up;
		}
		Gizmos.DrawSphere(base.transform.TransformPoint(massPos), 0.1f);
	}
}
