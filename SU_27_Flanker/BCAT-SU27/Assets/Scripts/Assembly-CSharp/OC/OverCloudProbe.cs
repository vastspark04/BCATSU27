using UnityEngine;

namespace OC{

[ExecuteInEditMode]
public class OverCloudProbe : MonoBehaviour
{
	[Tooltip("How fast the probe should fade to the new sampled value.")]
	public float interpolationSpeed = 1f;

	[Tooltip("Draw a gizmo which visualizes the density at the probe position.")]
	[SerializeField]
	private bool m_Debug;

	public float density { get; private set; }

	public float rain { get; private set; }

	private void Update()
	{
		CloudDensity cloudDensity = OverCloud.GetDensity(base.transform.position);
		density = Mathf.Lerp(density, cloudDensity.density, interpolationSpeed * Time.deltaTime);
		rain = Mathf.Lerp(rain, cloudDensity.rain, interpolationSpeed * Time.deltaTime);
	}

	private void OnDrawGizmos()
	{
		if (m_Debug)
		{
			float t = OverCloud.GetDensity(base.transform.position).density;
			Gizmos.color = Color.Lerp(Color.red, Color.green, t);
			Gizmos.DrawSphere(base.transform.position, Mathf.Lerp(10f, 100f, t));
		}
	}
}
}