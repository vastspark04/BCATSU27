using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorShaker : MonoBehaviour
{
	public HelicopterRotor rotor;

	public AnimationCurve shakeTorqueCurve;

	public float shakeRpmRatio;

	public AnimationCurve bodyShakeCurve;

	private Actor a;

	private Vector3 torqueDir = Vector3.forward;

	private void Start()
	{
		a = GetComponentInParent<Actor>();
	}

	private void Update()
	{
		if (a.isPlayer)
		{
			float outputRPM = rotor.inputShaft.outputRPM;
			Vector3 vector = rotor.transform.TransformDirection(torqueDir);
			torqueDir = Quaternion.AngleAxis(shakeRpmRatio * outputRPM / 60f * 360f, Vector3.up) * torqueDir;
			rotor.rb.AddTorque(torqueDir * shakeTorqueCurve.Evaluate(outputRPM) * (1 + rotor.damageLevel));
			CamRigRotationInterpolator.ShakeAll(vector * bodyShakeCurve.Evaluate(outputRPM));
		}
	}
}

}