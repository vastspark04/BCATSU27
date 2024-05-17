using UnityEngine;

[ExecuteAlways]
public class TelescopePiston : MonoBehaviour
{
	public Transform piston;

	public Transform cylinder;

	public Transform midSection;

	public float midSectionPosition = 0.5f;

	public Transform upReference;

	private void LateUpdate()
	{
		Vector3 vector = (upReference ? upReference.up : Vector3.up);
		piston.rotation = Quaternion.LookRotation(cylinder.position - piston.position, vector);
		cylinder.LookAt(piston, vector);
		if ((bool)midSection)
		{
			midSection.position = Vector3.Lerp(piston.position, cylinder.position, midSectionPosition);
			midSection.LookAt(piston, vector);
		}
	}
}
