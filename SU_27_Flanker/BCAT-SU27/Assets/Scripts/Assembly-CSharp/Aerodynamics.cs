using UnityEngine;

[CreateAssetMenu]
public class Aerodynamics : ScriptableObject
{
	public AnimationCurve liftCurve;

	public AnimationCurve dragCurve;

	public AnimationCurve buffetCurve;

	public float buffetMagnitude;

	public float buffetTimeFactor;

	public bool perpLiftVector;

	public bool mirroredCurves = true;
}
