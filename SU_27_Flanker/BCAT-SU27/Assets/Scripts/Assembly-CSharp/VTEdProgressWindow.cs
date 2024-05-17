using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VTEdProgressWindow : MonoBehaviour
{
	public delegate float GetProgressDelegate();

	public Text titleText;

	public Text descriptText;

	public RectTransform loadBar;

	public Text progressText;

	private Action OnComplete;

	public float finalWidth;

	public float minWidth;

	private bool done = true;

	private event GetProgressDelegate OnGetProgress;

	public void Display(string title, string description, GetProgressDelegate OnGetProgress, Action OnComplete)
	{
		base.gameObject.SetActive(value: true);
		done = false;
		titleText.text = title;
		descriptText.text = description;
		this.OnGetProgress = OnGetProgress;
		this.OnComplete = OnComplete;
		SetProgress(0f);
		StartCoroutine(ProgressRoutine());
	}

	private void SetProgress(float p)
	{
		p = Mathf.Clamp01(p);
		loadBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(minWidth, finalWidth, p));
		progressText.text = $"{Mathf.RoundToInt(p * 100f)}%";
	}

	private IEnumerator ProgressRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(0.2f);
		float p = 0f;
		while (!done && p < 1f)
		{
			p = this.OnGetProgress();
			SetProgress(p);
			yield return wait;
		}
		if (!done)
		{
			SetDone();
		}
	}

	public void SetDone()
	{
		if (!done)
		{
			done = true;
			if (OnComplete != null)
			{
				OnComplete();
				OnComplete = null;
			}
			this.OnGetProgress = null;
			base.gameObject.SetActive(value: false);
		}
	}
}
