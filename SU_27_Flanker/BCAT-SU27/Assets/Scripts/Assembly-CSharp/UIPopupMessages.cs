using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupMessages : MonoBehaviour
{
	public GameObject textTemplate;

	private ObjectPool textPool;

	private List<RectTransform> textTfs = new List<RectTransform>();

	private Dictionary<string, RectTransform> persistentTextTfs = new Dictionary<string, RectTransform>();

	public static UIPopupMessages fetch { get; private set; }

	private void Awake()
	{
		fetch = this;
		textPool = ObjectPool.CreateObjectPool(textTemplate, 5, canGrow: true, destroyOnLoad: true);
	}

	public void DisplayMessage(string message, float time, Color color)
	{
		GameObject pooledObject = textPool.GetPooledObject();
		pooledObject.transform.SetParent(base.transform);
		pooledObject.SetActive(value: true);
		Text component = pooledObject.GetComponent<Text>();
		component.text = message;
		component.color = color;
		component.GetComponent<ContentSizeFitter>().SetLayoutVertical();
		textTfs.Add(component.rectTransform);
		UpdateTextPositions();
		StartCoroutine(MessageRoutine(component, time));
	}

	private void UpdateTextPositions()
	{
		float num = 0f;
		for (int i = 0; i < textTfs.Count; i++)
		{
			RectTransform rectTransform = textTfs[i];
			rectTransform.localPosition = new Vector3(0f, num, 0f);
			num -= rectTransform.rect.height + 5f;
		}
	}

	public void DisplayPersistentMessage(string message, Color color, string id)
	{
		Text component;
		if (persistentTextTfs.TryGetValue(id, out var value))
		{
			component = value.GetComponent<Text>();
		}
		else
		{
			GameObject pooledObject = textPool.GetPooledObject();
			pooledObject.transform.SetParent(base.transform);
			pooledObject.SetActive(value: true);
			component = pooledObject.GetComponent<Text>();
			textTfs.Add(component.rectTransform);
			persistentTextTfs.Add(id, component.rectTransform);
		}
		component.text = message;
		component.color = color;
		component.GetComponent<ContentSizeFitter>().SetLayoutVertical();
		UpdateTextPositions();
	}

	public void RemovePersistentMessage(string id)
	{
		if (persistentTextTfs.TryGetValue(id, out var value))
		{
			textTfs.Remove(value);
			value.gameObject.SetActive(value: false);
			persistentTextTfs.Remove(id);
			UpdateTextPositions();
		}
	}

	private IEnumerator MessageRoutine(Text text, float time)
	{
		yield return new WaitForSeconds(time);
		Color color = text.color;
		while (color.a > 0f)
		{
			color.a = Mathf.MoveTowards(color.a, 0f, Time.unscaledDeltaTime);
			text.color = color;
			yield return null;
		}
		text.gameObject.SetActive(value: false);
		textTfs.Remove(text.rectTransform);
		UpdateTextPositions();
	}
}
