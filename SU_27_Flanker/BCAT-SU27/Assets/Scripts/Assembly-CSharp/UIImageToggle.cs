using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIImageToggle : MonoBehaviour
{
	private bool _imgEnabled;

	public float alphaRate;

	public Image image;

	private Color color;

	private bool gotStartColor;

	private Coroutine tRoutine;

	public bool imageEnabled
	{
		get
		{
			return _imgEnabled;
		}
		set
		{
			if (_imgEnabled != value)
			{
				GetStartColor();
				_imgEnabled = value;
				if (tRoutine != null)
				{
					StopCoroutine(tRoutine);
				}
				if (base.gameObject.activeInHierarchy)
				{
					tRoutine = StartCoroutine(ToggleRoutine());
					return;
				}
				Color color = this.color;
				color.a = (_imgEnabled ? 1 : 0);
				image.color = color;
			}
		}
	}

	private void GetStartColor()
	{
		if (!gotStartColor)
		{
			color = image.color;
			gotStartColor = true;
		}
	}

	private void Start()
	{
		GetStartColor();
		if (tRoutine != null)
		{
			StopCoroutine(tRoutine);
		}
		Color color = this.color;
		color.a = (_imgEnabled ? 1 : 0);
		image.color = color;
	}

	private void OnEnable()
	{
		GetStartColor();
		color = image.color;
		if (tRoutine != null)
		{
			StopCoroutine(tRoutine);
		}
		tRoutine = StartCoroutine(ToggleRoutine());
	}

	public void Toggle()
	{
		imageEnabled = !imageEnabled;
	}

	private IEnumerator ToggleRoutine()
	{
		Color tCol = color;
		tCol.a = (_imgEnabled ? 1 : 0);
		while (image.color.a != tCol.a)
		{
			image.color = Color.Lerp(image.color, tCol, alphaRate * Time.deltaTime);
			yield return null;
		}
	}
}
