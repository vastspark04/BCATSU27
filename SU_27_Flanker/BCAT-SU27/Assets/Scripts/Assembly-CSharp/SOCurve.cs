using UnityEngine;

[CreateAssetMenu]
public class SOCurve : ScriptableObject
{
	public AnimationCurve curve;

	public float Evaluate(float t)
	{
		return curve.Evaluate(t);
	}
}
