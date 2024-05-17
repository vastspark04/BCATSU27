using UnityEngine;

public class VTMapEdCursorTerrainCircle : MonoBehaviour
{
	public int verts = 32;

	public int vertUpdatesPerFrame = 8;

	private int currUpdatePos;

	private LineRenderer lr;

	private float radius = 500f;

	public void SetRadius(float r)
	{
		radius = r;
		if (base.gameObject.activeInHierarchy)
		{
			UpdateAll();
		}
	}

	public void SetThickness(float t)
	{
		if (!lr)
		{
			lr = GetComponent<LineRenderer>();
		}
		float num3 = (lr.startWidth = (lr.endWidth = t));
	}

	private void Awake()
	{
		lr = GetComponent<LineRenderer>();
		lr.positionCount = 32;
		lr.loop = true;
		float num3 = (lr.startWidth = (lr.endWidth = 1f));
		lr.useWorldSpace = false;
	}

	private void Update()
	{
		UpdateLine(currUpdatePos, vertUpdatesPerFrame);
		currUpdatePos = (currUpdatePos + vertUpdatesPerFrame) % verts;
	}

	public void UpdateAll()
	{
		UpdateLine(0, verts);
	}

	private void UpdateLine(int start, int count)
	{
		float y = 0f;
		if ((bool)WaterPhysics.instance)
		{
			y = WaterPhysics.instance.height - 1000f;
		}
		Vector3 vector = new Vector3(0f, 12000f, 0f);
		float num = 360f / (float)verts;
		for (int i = 0; i < count; i++)
		{
			int num2 = (start + i) % verts;
			Vector3 position = Quaternion.AngleAxis((float)num2 * num, Vector3.forward) * new Vector3(0f, radius, 0f);
			Vector3 vector2 = base.transform.TransformPoint(position);
			vector2.y = y;
			if (Physics.Linecast(vector2 + vector, vector2, out var hitInfo, 1))
			{
				vector2 = hitInfo.point;
			}
			position = base.transform.InverseTransformPoint(vector2);
			lr.SetPosition(num2, position);
		}
	}
}
