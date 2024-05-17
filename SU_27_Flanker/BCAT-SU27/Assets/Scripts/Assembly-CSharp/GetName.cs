using UnityEngine;
using UnityEngine.UI;

public class GetName : MonoBehaviour
{
	public int m_Index;

	private void Start()
	{
		Text component = GetComponent<Text>();
		ExamplesController component2 = base.transform.parent.parent.GetComponent<ExamplesController>();
		if ((bool)component2)
		{
			component.text = component2.m_Examples[m_Index].Name;
		}
	}
}
