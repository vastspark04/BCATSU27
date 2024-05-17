using UnityEngine;

public class Origin : MonoBehaviour
{
	private void LateUpdate()
	{
		base.transform.position = Vector3.zero;
	}
}
