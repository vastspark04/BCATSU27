using UnityEngine;

[ExecuteInEditMode]
public class NotEditable : MonoBehaviour
{
	private void OnEnable()
	{
		base.transform.hideFlags = HideFlags.HideInInspector;
		base.hideFlags = HideFlags.HideInInspector;
	}

	private void OnDrawGizmosSelected()
	{
		base.transform.position = Vector3.zero;
		base.transform.rotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
	}
}
