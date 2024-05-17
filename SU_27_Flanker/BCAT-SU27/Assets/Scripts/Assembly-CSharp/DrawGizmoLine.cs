using UnityEngine;

public class DrawGizmoLine : MonoBehaviour
{
	public Transform start;

	public Transform end;

	public Color color;

	public bool onlyOnSelected;

	private void OnDrawGizmos()
	{
		if (!onlyOnSelected)
		{
			Draw();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (onlyOnSelected)
		{
			Draw();
		}
	}

	private void Draw()
	{
		if ((bool)start && (bool)end)
		{
			Gizmos.color = color;
			Gizmos.DrawLine(start.position, end.position);
		}
	}
}
