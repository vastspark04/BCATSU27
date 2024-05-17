using UnityEngine;
using UnityEngine.UI;

public class DataGraphButton : MonoBehaviour
{
	[HideInInspector]
	public Vector2 val;

	[HideInInspector]
	public DataGraph graph;

	private void Awake()
	{
		GetComponent<Button>().onClick.AddListener(OnClick);
	}

	private void OnClick()
	{
		graph.DisplayValue(val);
	}
}
