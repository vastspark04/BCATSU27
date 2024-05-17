using UnityEngine;

public class LookAtWorldUp : MonoBehaviour
{
	public Transform target;

	private void LateUpdate()
	{
		base.transform.rotation = Quaternion.LookRotation(target.position - base.transform.position, Vector3.up);
	}
}
