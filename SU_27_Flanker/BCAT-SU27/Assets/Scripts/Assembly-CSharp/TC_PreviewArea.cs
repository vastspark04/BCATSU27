using UnityEngine;

[ExecuteInEditMode]
public class TC_PreviewArea : MonoBehaviour
{
	[HideInInspector]
	public Transform t;

	public bool manual;

	public Rect area;

	public float positionY;

	private void Awake()
	{
		t = base.transform;
	}

	private void Update()
	{
		area.center = new Vector2(t.position.x, t.position.z);
	}

	private void OnDrawGizmos()
	{
		if (manual)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(new Vector3(area.center.x, positionY, area.center.y), new Vector3(area.width, 500f, area.height));
			Gizmos.color = Color.white;
		}
	}
}
