using UnityEngine;

[ExecuteAlways]
public class CenterOfMass : MonoBehaviour
{
	private void OnEnable()
	{
		Rigidbody componentInParent = GetComponentInParent<Rigidbody>();
		componentInParent.centerOfMass = componentInParent.transform.InverseTransformPoint(base.transform.position);
	}
}
