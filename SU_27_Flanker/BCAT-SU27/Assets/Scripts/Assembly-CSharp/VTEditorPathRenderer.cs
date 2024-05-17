using System.Collections;
using UnityEngine;

[RequireComponent(typeof(FollowPath))]
public class VTEditorPathRenderer : MonoBehaviour
{
	public Transform cameraTransform;

	public float distanceInterval = 150f;

	public Mesh arrowMesh;

	public Material arrowMaterial;

	public float moveSpeed = 55f;

	public float lineWidth = 0.005f;

	private LineRenderer lr;

	private FollowPath path;

	private float lastLength = 1f;

	private Matrix4x4[] matrices;

	private Vector3[] linePositions;

	private float t;

	private int arrowCount;

	private int lineVertCount;

	private int widthAnimKeyCount = 100;

	private Keyframe[] widthKeys;

	private AnimationCurve widthCurve;

	private bool isEditing;

	private static Mesh sphereMesh;

	private MaterialPropertyBlock defaultSphereProps;

	private MaterialPropertyBlock highlightedPropBlock;

	private MaterialPropertyBlock selectedPropBlock;

	private int highlightedIdx = -1;

	private int selectedIdx = -1;

	private bool started;

	public void SelectPoint(int idx)
	{
		selectedIdx = idx;
	}

	public void HighlightPoint(int idx)
	{
		highlightedIdx = idx;
	}

	public void UnHighlightPoint(int idx)
	{
		if (highlightedIdx == idx)
		{
			highlightedIdx = -1;
		}
	}

	private void OnDestroy()
	{
		if (path != null)
		{
			path.OnPathChanged -= OnPathChanged;
		}
	}

	private void OnEnable()
	{
		if (sphereMesh == null)
		{
			GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphereMesh = obj.GetComponent<MeshFilter>().sharedMesh;
			Object.Destroy(obj);
		}
		StartCoroutine(UpdateRoutine());
	}

	public void StartEditing()
	{
		isEditing = true;
	}

	public void StopEditing()
	{
		isEditing = false;
		lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f));
		float num3 = (lr.startWidth = (lr.endWidth = 0.5f));
	}

	private IEnumerator UpdateRoutine()
	{
		yield return null;
		if (!started)
		{
			path = GetComponent<FollowPath>();
			path.OnPathChanged += OnPathChanged;
			lr = base.gameObject.AddComponent<LineRenderer>();
			lr.material = arrowMaterial;
			lr.useWorldSpace = false;
			lastLength = -10f;
			SetupArrays();
			OnPathChanged();
			if (isEditing)
			{
				UpdateLineWidth();
			}
			else
			{
				lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f));
				float num3 = (lr.startWidth = (lr.endWidth = 0.5f));
			}
			defaultSphereProps = new MaterialPropertyBlock();
			Color color = arrowMaterial.GetColor("_TintColor");
			defaultSphereProps.SetColor("_TintColor", color * 0.5f);
			highlightedPropBlock = new MaterialPropertyBlock();
			highlightedPropBlock.SetColor("_TintColor", color);
			selectedPropBlock = new MaterialPropertyBlock();
			selectedPropBlock.SetColor("_TintColor", color * 1.5f);
			started = true;
		}
		StartCoroutine(ArrowRenderRoutine());
		StartCoroutine(WidthUpdateRoutine());
	}

	private IEnumerator WidthUpdateRoutine()
	{
		yield return new WaitForSeconds(Random.Range(0f, 0.08f));
		WaitForSeconds waitInterval = new WaitForSeconds(0.08f);
		while (base.enabled)
		{
			if (isEditing)
			{
				UpdateLineWidth();
			}
			yield return waitInterval;
		}
	}

	private IEnumerator ArrowRenderRoutine()
	{
		while (base.enabled)
		{
			if (path.IsCurveReady())
			{
				if (isEditing)
				{
					lr.enabled = true;
					float approximateLength = path.GetApproximateLength();
					if (Mathf.Abs(approximateLength - lastLength) > 1f)
					{
						lastLength = approximateLength;
						SetupArrays();
					}
					for (int i = 0; i < arrowCount; i++)
					{
						float num = t + (float)i * (1f / (float)arrowCount);
						num = Mathf.Repeat(num, 1f);
						Vector3 worldPoint = path.GetWorldPoint(num);
						Quaternion q = Quaternion.LookRotation(path.GetWorldTangent(num));
						float num2 = widthCurve.Evaluate(num);
						Matrix4x4 matrix4x = Matrix4x4.TRS(worldPoint, q, 3f * num2 * Vector3.one);
						matrices[i] = matrix4x;
					}
					float num3 = moveSpeed / approximateLength;
					t = Mathf.Repeat(t + num3 * Time.unscaledDeltaTime, 1f);
					Graphics.DrawMeshInstanced(arrowMesh, 0, arrowMaterial, matrices);
				}
			}
			else
			{
				lr.enabled = false;
			}
			if (isEditing && path.pointTransforms != null)
			{
				for (int j = 0; j < path.pointTransforms.Length; j++)
				{
					float value = Vector3.Distance(path.pointTransforms[j].position, cameraTransform.position);
					value = Mathf.Clamp(value, 10f, 8000f);
					Vector3 s = 6f * lineWidth * value * Vector3.one;
					Matrix4x4 matrix = Matrix4x4.TRS(path.pointTransforms[j].position, Quaternion.identity, s);
					Graphics.DrawMesh(properties: (j != selectedIdx) ? ((j != highlightedIdx) ? defaultSphereProps : highlightedPropBlock) : selectedPropBlock, mesh: sphereMesh, matrix: matrix, material: arrowMaterial, layer: 0, camera: null, submeshIndex: 0);
				}
			}
			yield return null;
		}
	}

	private void UpdateLineWidth()
	{
		if (path.IsCurveReady())
		{
			if (widthKeys == null)
			{
				widthKeys = new Keyframe[widthAnimKeyCount];
			}
			for (int i = 0; i < widthAnimKeyCount; i++)
			{
				float time = (float)i * (1f / (float)(widthAnimKeyCount - 1));
				float value = Mathf.Clamp(Vector3.Distance(cameraTransform.position, path.GetWorldPoint(time)), 1f, 8000f) * lineWidth;
				widthKeys[i] = new Keyframe(time, value);
			}
			if (widthCurve == null)
			{
				widthCurve = new AnimationCurve(widthKeys);
			}
			else
			{
				widthCurve.keys = widthKeys;
			}
			lr.widthCurve = widthCurve;
		}
	}

	private void OnPathChanged()
	{
		if (path.IsCurveReady())
		{
			lastLength = path.GetApproximateLength();
			SetupArrays();
			UpdateLineWidth();
		}
		if (!lr)
		{
			return;
		}
		if (path.IsCurveReady())
		{
			lr.enabled = true;
			for (int i = 0; i < lineVertCount; i++)
			{
				linePositions[i] = path.GetPoint((float)i * (1f / (float)(lineVertCount - 1)));
			}
			lr.positionCount = lineVertCount;
			lr.SetPositions(linePositions);
		}
		else
		{
			lr.enabled = false;
		}
	}

	private void SetupArrays()
	{
		if (path.IsCurveReady())
		{
			if (lastLength < distanceInterval)
			{
				matrices = new Matrix4x4[1];
			}
			else
			{
				int a = Mathf.CeilToInt(lastLength / distanceInterval);
				a = Mathf.Min(a, 1023);
				matrices = new Matrix4x4[a];
			}
			arrowCount = matrices.Length;
			lineVertCount = Mathf.Clamp(Mathf.CeilToInt(lastLength / 2f), 10, 200);
			linePositions = new Vector3[lineVertCount];
		}
	}
}
