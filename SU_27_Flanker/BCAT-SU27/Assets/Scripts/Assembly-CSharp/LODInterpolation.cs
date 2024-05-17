using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LODInterpolation : MonoBehaviour
{
	public LODBase lodBase;

	public Rigidbody rb;

	public float interpolationDistance;

	private float sqrInterpDist;

	private void Start()
	{
		if (!lodBase)
		{
			lodBase = GetComponentInParent<LODBase>();
		}
		if (!rb)
		{
			rb = GetComponent<Rigidbody>();
		}
		if ((bool)lodBase)
		{
			lodBase.AddListener(OnUpdateDist);
			sqrInterpDist = interpolationDistance * interpolationDistance;
		}
		else
		{
			base.enabled = false;
			Debug.LogError("LODInterpolation doesn't have an LODBase!");
		}
	}

	private void OnUpdateDist(float sqrDist)
	{
		if (sqrDist < sqrInterpDist)
		{
			rb.interpolation = RigidbodyInterpolation.Interpolate;
		}
		else
		{
			rb.interpolation = RigidbodyInterpolation.None;
		}
	}
}
