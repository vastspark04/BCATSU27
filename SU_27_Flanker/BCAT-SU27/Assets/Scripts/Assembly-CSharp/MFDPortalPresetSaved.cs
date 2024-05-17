using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MFDPortalPresetSaved : MonoBehaviour
{
	public Graphic img;

	private Color c;

	private Coroutine displayRoutine;

	private void Awake()
	{
		c = img.color;
	}

	public void Display()
	{
		if (displayRoutine != null)
		{
			StopCoroutine(displayRoutine);
		}
		base.gameObject.SetActive(value: true);
		displayRoutine = StartCoroutine(DisplayRoutine());
	}

	private IEnumerator DisplayRoutine()
	{
		yield return null;
		float a = 1f;
		while (a > 0f)
		{
			Color color = c;
			color.a = a;
			img.color = color;
			a = Mathf.MoveTowards(a, 0f, 0.5f * Time.deltaTime);
			yield return null;
		}
		base.gameObject.SetActive(value: false);
	}
}
