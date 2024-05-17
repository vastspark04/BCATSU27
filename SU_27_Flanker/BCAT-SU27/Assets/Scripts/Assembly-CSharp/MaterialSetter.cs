using UnityEngine;

public class MaterialSetter : MonoBehaviour
{
	public Material[] materials;

	private Renderer r;

	private void Awake()
	{
		r = base.gameObject.GetComponentImplementing<Renderer>();
	}

	public void Apply()
	{
		r.materials = materials;
	}
}
