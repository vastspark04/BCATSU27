using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreditsScroller : MonoBehaviour
{
	public GameObject displayObj;

	public RectTransform maskRect;

	public GameObject textTemplate;

	public float scrollSpeed;

	public Font normalFont;

	public Font boldFont;

	public SOText creditsText;

	private List<Coroutine> creditRoutines = new List<Coroutine>();

	private bool playingCredits;

	[Header("Test")]
	public bool doTest;

	[TextArea]
	public string testText;

	public TextAsset creditsTextAsset;

	private Action OnCreditsFinish;

	private Queue<Text> textPool;

	private void Start()
	{
		if (doTest)
		{
			CreatePool();
			Test();
		}
	}

	public void FinishCredits()
	{
		ClearCredits();
		OnCreditsFinish?.Invoke();
	}

	public void StartCredits(Action onCreditsFinish)
	{
		OnCreditsFinish = onCreditsFinish;
		ClearCredits();
		displayObj.SetActive(value: true);
		CreatePool();
		string text = (creditsTextAsset ? creditsTextAsset.text : creditsText.text);
		Queue<string> lines = GenerateQueue(text);
		Coroutine item = StartCoroutine(CreditsRoutine(lines));
		creditRoutines.Add(item);
	}

	private void ClearCredits()
	{
		if (textPool != null)
		{
			while (textPool.Count > 0)
			{
				UnityEngine.Object.Destroy(textPool.Dequeue().gameObject);
			}
		}
		foreach (Coroutine creditRoutine in creditRoutines)
		{
			if (creditRoutine != null)
			{
				StopCoroutine(creditRoutine);
			}
		}
		creditRoutines.Clear();
		displayObj.SetActive(value: false);
		playingCredits = false;
	}

	private void Test()
	{
		Queue<string> lines = GenerateQueue(testText);
		StartCoroutine(CreditsRoutine(lines));
	}

	private Queue<string> GenerateQueue(string text)
	{
		text = TranslateCredits(text);
		string[] array = text.Split('\n');
		Queue<string> queue = new Queue<string>();
		for (int i = 0; i < array.Length; i++)
		{
			queue.Enqueue(array[i]);
		}
		return queue;
	}

	private string TranslateCredits(string text)
	{
		text = text.Replace("<b>Credits", $"<b>{VTLStaticStrings.credits_credits}");
		text = text.Replace("<b>Developer", $"<b>{VTLStaticStrings.credits_developer}");
		text = text.Replace("<b>Programming", $"<b>{VTLStaticStrings.credits_programming}");
		text = text.Replace("<b>Modelling", $"<b>{VTLStaticStrings.credits_modelling}");
		text = text.Replace("<b>Music", $"<b>{VTLStaticStrings.credits_music}");
		text = text.Replace("<b>Voices", $"<b>{VTLStaticStrings.credits_voices}");
		text = text.Replace("<b>Wingmen", $"<b>{VTLStaticStrings.credits_wingmen}");
		text = text.Replace("<b>Ground Crew", $"<b>{VTLStaticStrings.credits_groundCrew}");
		text = text.Replace("<b>AWACS", $"<b>{VTLStaticStrings.credits_awacs}");
		text = text.Replace("<b>ATC", $"<b>{VTLStaticStrings.credits_atc}");
		text = text.Replace("<b>Wingmen", $"<b>{VTLStaticStrings.credits_credits}");
		text = text.Replace("<b>Private Testing Volunteers", $"<b>{VTLStaticStrings.credits_pvtTesting}");
		text = text.Replace("<b>Translation", $"<b>{VTLStaticStrings.credits_translation}");
		text = text.Replace("<b>Special Thanks", $"<b>{VTLStaticStrings.credits_thanks}");
		Debug.Log(text);
		return text;
	}

	private void CreatePool()
	{
		textPool = new Queue<Text>();
		float num = ((RectTransform)textTemplate.transform).rect.height * textTemplate.transform.localScale.y;
		int num2 = Mathf.CeilToInt(maskRect.rect.height / num) + 1;
		textTemplate.SetActive(value: false);
		for (int i = 0; i < num2; i++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(textTemplate);
			Text component = obj.GetComponent<Text>();
			component.transform.SetParent(textTemplate.transform.parent);
			component.transform.localScale = textTemplate.transform.localScale;
			component.transform.localRotation = textTemplate.transform.localRotation;
			obj.SetActive(value: false);
			textPool.Enqueue(component);
		}
	}

	private IEnumerator CreditsRoutine(Queue<string> lines)
	{
		playingCredits = true;
		float tHeight = ((RectTransform)textTemplate.transform).rect.height * textTemplate.transform.localScale.y;
		Text previousText = null;
		int tpCount = textPool.Count;
		while (lines.Count > 0)
		{
			Text text = textPool.Dequeue();
			string text2 = lines.Dequeue();
			if (text2.StartsWith("<b>"))
			{
				text2 = text2.Substring(3, text2.Length - 3);
				text.font = boldFont;
			}
			else
			{
				text.font = normalFont;
			}
			text.text = text2;
			if ((bool)previousText)
			{
				text.transform.localPosition = previousText.transform.localPosition + new Vector3(0f, 0f - tHeight, 0f);
			}
			else
			{
				text.transform.localPosition = new Vector3(0f, 0f - maskRect.rect.height, 0f);
			}
			text.gameObject.SetActive(value: true);
			StartCoroutine(ScrollRoutine(text));
			previousText = text;
			while (textPool.Count < 1)
			{
				yield return null;
			}
		}
		while (textPool.Count < tpCount)
		{
			yield return null;
		}
		FinishCredits();
	}

	private IEnumerator ScrollRoutine(Text txt)
	{
		RectTransform rtf = txt.rectTransform;
		float tHeight = rtf.rect.height * rtf.localScale.y;
		while ((bool)rtf && rtf.localPosition.y < tHeight && playingCredits)
		{
			float num = scrollSpeed;
			if (Input.GetKey(KeyCode.RightShift))
			{
				num *= 4f;
			}
			rtf.localPosition += new Vector3(0f, num * Time.deltaTime, 0f);
			yield return null;
		}
		txt.gameObject.SetActive(value: false);
		if (playingCredits)
		{
			textPool.Enqueue(txt);
		}
		else
		{
			UnityEngine.Object.Destroy(rtf.gameObject);
		}
	}
}
