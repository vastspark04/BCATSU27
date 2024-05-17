using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ApplyColorToAllUI : MonoBehaviour
{
	public Color color;

	public bool apply;

	private void Start()
	{
	}

	private void Update()
	{
		if (apply)
		{
			apply = false;
			Apply();
		}
	}

	private void Apply()
	{
		Image[] componentsInChildren = GetComponentsInChildren<Image>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].color = color;
		}
		Text[] componentsInChildren2 = GetComponentsInChildren<Text>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].color = color;
		}
		SpriteRenderer[] componentsInChildren3 = GetComponentsInChildren<SpriteRenderer>();
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			componentsInChildren3[i].color = color;
		}
	}
}
