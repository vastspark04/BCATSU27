using UnityEngine;

public class NameAppender : MonoBehaviour
{
	public bool includeSelf;

	public string label;

	[ContextMenu("Append Label")]
	public void Append()
	{
		Transform[] componentsInChildren = GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (!(transform == base.transform) || includeSelf)
			{
				transform.name = transform.name + " (" + label + ")";
			}
		}
	}
}
