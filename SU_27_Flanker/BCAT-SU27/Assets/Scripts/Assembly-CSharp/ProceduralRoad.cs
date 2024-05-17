using System.Collections.Generic;
using UnityEngine;

public class ProceduralRoad : MonoBehaviour
{
	[Tooltip("The template model of the road segment.  Must be a single object with a mesh. Z forward, Y up.")]
	public GameObject roadTemplate;

	[Tooltip("The path to follow")]
	public FollowPath path;

	[Tooltip("Approximate distance between each segment.  Lower number = higher resolution")]
	public float segmentInterval = 5f;

	[Tooltip("Height above the ground the road surface will sit")]
	public float roadHeight;

	[Tooltip("Depth below the ground the foundation will sit")]
	public float baseDepth;

	[Tooltip("Overall scale of each road segment. For making the road wider or narrower")]
	public float roadScale = 1f;

	[Tooltip("This will split the road into objects containing this amount of segments")]
	public int splitInterval = 10;

	[Tooltip("Use this to override terrain height and use curve height instead.")]
	public bool useCurveHeight;

	public bool createColliders = true;

	public ProceduralIntersection startIntersection;

	public ProceduralIntersection endIntersection;

	private GameObject roadObject;

	public void GenerateRoad()
	{
		if (!path)
		{
			return;
		}
		path.SetupCurve();
		if (!roadObject)
		{
			Transform transform = base.transform.Find("RoadObject");
			if ((bool)transform)
			{
				roadObject = transform.gameObject;
			}
		}
		if ((bool)roadObject)
		{
			Object.DestroyImmediate(roadObject);
		}
		if ((bool)startIntersection)
		{
			startIntersection.UpdateNodeVerts();
		}
		if ((bool)endIntersection)
		{
			endIntersection.UpdateNodeVerts();
		}
		roadObject = new GameObject("RoadObject");
		roadObject.transform.parent = base.transform;
		roadObject.transform.localPosition = Vector3.zero;
		roadObject.transform.localRotation = Quaternion.identity;
		roadObject.transform.localScale = Vector3.one;
		List<GameObject> list = new List<GameObject>();
		List<int> list2 = new List<int>();
		List<int> list3 = new List<int>();
		List<int> list4 = new List<int>();
		List<int> list5 = new List<int>();
		Vector3[] vertices = roadTemplate.GetComponent<MeshFilter>().sharedMesh.vertices;
		roadTemplate.SetActive(value: false);
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] *= roadScale;
			if (vertices[i].z > 0f)
			{
				list2.Add(i);
			}
			else
			{
				list3.Add(i);
			}
			if (vertices[i].y > 0f)
			{
				list4.Add(i);
			}
			else
			{
				list5.Add(i);
			}
		}
		List<Vector3> list6 = new List<Vector3>();
		foreach (int item in list2)
		{
			if (vertices[item].y > 0f)
			{
				list6.Add(vertices[item]);
			}
		}
		List<MeshFilter> list7 = new List<MeshFilter>();
		float approximateLength = path.GetApproximateLength();
		int num = Mathf.RoundToInt(approximateLength / segmentInterval);
		float[] array = new float[num + 1];
		float num2 = 0f;
		float num3 = 1f / approximateLength;
		for (int j = 0; j < num; j++)
		{
			array[j] = num2;
			float num4 = 0f;
			Vector3 b = path.GetPoint(num2);
			while (num4 < segmentInterval && num2 < 1f)
			{
				num2 += num3 * 0.1f;
				Vector3 point = path.GetPoint(num2);
				num4 += Vector3.Distance(point, b);
				b = point;
			}
		}
		array[num] = 1f;
		int num5 = splitInterval;
		for (int k = 0; k < num; k++)
		{
			float t = array[k];
			Vector3 vector = path.gameObject.transform.TransformPoint(path.GetPoint(t));
			GameObject gameObject = Object.Instantiate(roadTemplate);
			gameObject.transform.position = vector;
			if (num5 >= splitInterval)
			{
				num5 = 0;
				GameObject gameObject2 = new GameObject("RoadSegment");
				gameObject2.transform.position = vector;
				gameObject2.transform.parent = roadObject.transform;
				gameObject.transform.parent = gameObject2.transform;
				list.Add(gameObject2);
			}
			else
			{
				num5++;
				gameObject.transform.parent = list[list.Count - 1].transform;
			}
			Vector3 forward = path.transform.TransformDirection(path.GetTangent(t));
			if (k < num)
			{
				forward = path.gameObject.transform.TransformPoint(path.GetPoint(array[k + 1])) - vector;
			}
			forward.y = 0f;
			gameObject.transform.rotation = Quaternion.LookRotation(forward);
			gameObject.SetActive(value: true);
			MeshFilter component = gameObject.GetComponent<MeshFilter>();
			Mesh mesh = Object.Instantiate(component.sharedMesh);
			Vector3[] array2 = new Vector3[vertices.Length];
			float num6 = float.MinValue;
			foreach (Vector3 item2 in list6)
			{
				if (Physics.Raycast(gameObject.transform.TransformPoint(item2) + 1000f * Vector3.up, Vector3.down, out var hitInfo, 2000f, 1))
				{
					num6 = Mathf.Max(num6, hitInfo.point.y + roadHeight);
				}
			}
			if (useCurveHeight)
			{
				num6 = Mathf.Max(num6, vector.y);
			}
			float num7 = float.MinValue;
			if (k == 0)
			{
				for (int l = 0; l < vertices.Length; l++)
				{
					if (vertices[l].z < 0f && vertices[l].y > 0f && Physics.Raycast(gameObject.transform.TransformPoint(vertices[l]) + 1000f * Vector3.up, Vector3.down, out var hitInfo2, 2000f, 1))
					{
						num7 = Mathf.Max(num7, hitInfo2.point.y + roadHeight);
					}
				}
			}
			if (useCurveHeight)
			{
				num7 = Mathf.Max(num6, vector.y);
			}
			for (int m = 0; m < array2.Length; m++)
			{
				Vector3 vector2 = gameObject.transform.TransformPoint(vertices[m]);
				bool num8 = list4.Contains(m);
				bool flag = list2.Contains(m);
				RaycastHit hitInfo3;
				if (num8)
				{
					if (flag)
					{
						vector2.y = num6;
					}
					else if (k == 0)
					{
						vector2.y = num7;
					}
				}
				else if (Physics.Raycast(vector2 + 1000f * Vector3.up, Vector3.down, out hitInfo3, 2000f, 1))
				{
					vector2.y = hitInfo3.point.y - baseDepth;
				}
				if (!flag && k == 0 && (bool)startIntersection)
				{
					ProceduralIntersection.IntersectionNode intersectionNode = null;
					float num9 = float.MaxValue;
					foreach (ProceduralIntersection.IntersectionNode node in startIntersection.nodes)
					{
						float sqrMagnitude = (vector - node.nodeTransform.position).sqrMagnitude;
						if (sqrMagnitude < num9)
						{
							num9 = sqrMagnitude;
							intersectionNode = node;
						}
					}
					if (intersectionNode != null)
					{
						Vector3 vector3 = vector2;
						float num10 = float.MaxValue;
						Vector3[] verts = intersectionNode.verts;
						foreach (Vector3 vector4 in verts)
						{
							float sqrMagnitude2 = (vector4 - vector3).sqrMagnitude;
							if (sqrMagnitude2 < num10)
							{
								num10 = sqrMagnitude2;
								vector2 = vector4;
							}
						}
					}
				}
				array2[m] = gameObject.transform.InverseTransformPoint(vector2);
			}
			if (k > 0)
			{
				for (int num11 = 0; num11 < list2.Count; num11++)
				{
					Vector3 position = list7[k - 1].sharedMesh.vertices[list2[num11]];
					position = gameObject.transform.InverseTransformPoint(list7[k - 1].transform.TransformPoint(position));
					array2[list3[num11]] = position;
				}
			}
			if (k == num - 1)
			{
				Vector3 vector5 = path.transform.TransformPoint(path.GetPoint(1f));
				float magnitude = (vector5 - vector).magnitude;
				for (int num12 = 0; num12 < list2.Count; num12++)
				{
					array2[list2[num12]].z = magnitude;
				}
				if ((bool)endIntersection)
				{
					ProceduralIntersection.IntersectionNode intersectionNode2 = null;
					float num13 = float.MaxValue;
					foreach (ProceduralIntersection.IntersectionNode node2 in endIntersection.nodes)
					{
						float sqrMagnitude3 = (vector5 - node2.nodeTransform.position).sqrMagnitude;
						if (sqrMagnitude3 < num13)
						{
							num13 = sqrMagnitude3;
							intersectionNode2 = node2;
						}
					}
					if (intersectionNode2 != null)
					{
						for (int num14 = 0; num14 < list2.Count; num14++)
						{
							Vector3 vector6 = gameObject.transform.TransformPoint(array2[list2[num14]]);
							Vector3 vector7 = vector6;
							float num15 = float.MaxValue;
							Vector3[] verts = intersectionNode2.verts;
							foreach (Vector3 vector8 in verts)
							{
								float sqrMagnitude4 = (vector8 - vector7).sqrMagnitude;
								if (sqrMagnitude4 < num15)
								{
									num15 = sqrMagnitude4;
									vector6 = vector8;
								}
							}
							array2[list2[num14]] = gameObject.transform.InverseTransformPoint(vector6);
						}
					}
				}
			}
			mesh.vertices = array2;
			mesh.RecalculateNormals();
			component.sharedMesh = mesh;
			list7.Add(component);
		}
		foreach (GameObject item3 in list)
		{
			MeshCombiner2 meshCombiner = item3.AddComponent<MeshCombiner2>();
			meshCombiner.destroyNonColliders = true;
			meshCombiner.autoSmoothing = true;
			meshCombiner.autoWeldBucketStep = 25f;
			meshCombiner.createMeshColliders = createColliders;
			meshCombiner.CombineMeshes();
			Object.DestroyImmediate(meshCombiner);
		}
	}
}
