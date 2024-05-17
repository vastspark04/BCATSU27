using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorBladeAnimator : MonoBehaviour
{
	public HelicopterRotor rotor;

	public Transform[] bladeTransforms;

	public Transform bladesCenter;

	public float discRotationMul;

	public float collectiveMul;

	private void Start()
	{
	}

	private void LateUpdate()
	{
		Vector3 vector = rotor.CurrentPYR();
		float num = rotor.CurrentCollective();
		float num2 = 10f;
		Quaternion identity = Quaternion.identity;
		identity = Quaternion.AngleAxis(vector.x * num2, Vector3.forward) * identity;
		identity = Quaternion.AngleAxis(vector.z * num2, Vector3.right) * identity;
		for (int i = 0; i < bladeTransforms.Length; i++)
		{
			Transform transform = bladeTransforms[i];
			Vector3 vector2 = bladesCenter.InverseTransformPoint(transform.position);
			vector2.Normalize();
			float z = (identity * vector2).y * discRotationMul + collectiveMul * num;
			transform.localRotation = Quaternion.Euler(0f, 0f, z);
		}
	}
}

}