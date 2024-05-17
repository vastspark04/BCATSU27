using UnityEngine;

public class SetAllCollidersScale : MonoBehaviour
{
	[ContextMenu("Apply")]
	public void Apply()
	{
		Collider[] array = Object.FindObjectsOfType<Collider>();
		foreach (Collider collider in array)
		{
			if (collider.transform.lossyScale != Vector3.one)
			{
				Debug.LogFormat("{0} does not have scale of one. ({1})", collider.gameObject.name, collider.transform.lossyScale);
			}
		}
	}
}
