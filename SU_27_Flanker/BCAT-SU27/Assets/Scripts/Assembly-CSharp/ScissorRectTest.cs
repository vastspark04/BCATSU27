using System.Collections;
using UnityEngine;

public class ScissorRectTest : MonoBehaviour
{
	public int numDivisions = 100;

	private Camera cam;

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	private void Start()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		float size = 1f / (float)numDivisions;
		while (base.enabled)
		{
			for (int x = 0; x < numDivisions; x++)
			{
				for (int y = 0; y < numDivisions; y++)
				{
					float x2 = (float)x / (float)numDivisions;
					float y2 = (float)y / (float)numDivisions;
					SetScissorRect(r: new Rect(x2, y2, size, size), cam: cam);
					cam.Render();
					yield return null;
				}
			}
		}
	}

	public static void SetScissorRect(Camera cam, Rect r)
	{
		if (r.x < 0f)
		{
			r.width += r.x;
			r.x = 0f;
		}
		if (r.y < 0f)
		{
			r.height += r.y;
			r.y = 0f;
		}
		r.width = Mathf.Min(1f - r.x, r.width);
		r.height = Mathf.Min(1f - r.y, r.height);
		cam.rect = new Rect(0f, 0f, 1f, 1f);
		cam.ResetProjectionMatrix();
		Matrix4x4 projectionMatrix = cam.projectionMatrix;
		cam.rect = r;
		Matrix4x4.TRS(new Vector3(r.x, r.y, 0f), Quaternion.identity, new Vector3(r.width, r.height, 1f));
		Matrix4x4 matrix4x = Matrix4x4.TRS(new Vector3(1f / r.width - 1f, 1f / r.height - 1f, 0f), Quaternion.identity, new Vector3(1f / r.width, 1f / r.height, 1f));
		Matrix4x4 matrix4x2 = Matrix4x4.TRS(new Vector3((0f - r.x) * 2f / r.width, (0f - r.y) * 2f / r.height, 0f), Quaternion.identity, Vector3.one);
		cam.projectionMatrix = matrix4x2 * matrix4x * projectionMatrix;
	}
}
