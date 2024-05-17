using UnityEngine;

public class MaterialSwitcher : MonoBehaviour
{
	public Renderer targetRenderer;

	public Material[] materialsA;

	public Material[] materialsB;

	public bool startWithA = true;

	private bool isA;

	private void Start()
	{
		if (startWithA)
		{
			SwitchToA();
		}
		else
		{
			SwitchToB();
		}
	}

	public void Toggle()
	{
		if (isA)
		{
			SwitchToB();
		}
		else
		{
			SwitchToA();
		}
	}

	public void SwitchToA()
	{
		targetRenderer.materials = materialsA;
		isA = true;
	}

	public void SwitchToB()
	{
		targetRenderer.materials = materialsB;
		isA = false;
	}
}
