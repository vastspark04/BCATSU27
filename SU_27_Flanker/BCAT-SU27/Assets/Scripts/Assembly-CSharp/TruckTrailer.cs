using UnityEngine;

[ExecuteInEditMode]
public class TruckTrailer : MonoBehaviour
{
	public Transform trailerHinge;

	public Transform truckHinge;

	public bool flatten;

	private void LateUpdate()
	{
		if ((bool)trailerHinge && (bool)truckHinge)
		{
			if (flatten)
			{
				Vector3 position = base.transform.position;
				position.y = truckHinge.position.y;
				base.transform.position = position;
			}
			base.transform.LookAt(truckHinge);
			Vector3 vector = trailerHinge.InverseTransformPoint(truckHinge.position);
			base.transform.position += vector.z * trailerHinge.transform.forward;
		}
	}
}
