using UnityEngine;

public class CameraCullDistances : MonoBehaviour
{
	public float[] cullDists = new float[32];

	public bool spherical;

	private void Start()
	{
		Camera component = GetComponent<Camera>();
		component.layerCullDistances = cullDists;
		component.layerCullSpherical = spherical;
	}
}
