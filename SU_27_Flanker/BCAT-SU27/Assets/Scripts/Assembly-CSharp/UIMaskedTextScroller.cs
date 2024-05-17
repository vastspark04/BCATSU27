using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIMaskedTextScroller : MonoBehaviour
{
	private RectTransform maskTransform;

	public Text text;

	private ContentSizeFitter sizeFitter;

	public float speed = 30f;

	public float pauseTime = 3f;

	public bool onlyOnHover;

	private Coroutine updateRoutine;

	private Coroutine htRoutine;

	private bool pointerIn;

	private void Awake()
	{
		maskTransform = (RectTransform)text.transform.parent;
		sizeFitter = text.GetComponent<ContentSizeFitter>();
	}

	private void OnEnable()
	{
		ResetTf();
		if (!onlyOnHover)
		{
			Refresh(pauseTime);
		}
		if (htRoutine != null)
		{
			StopCoroutine(htRoutine);
		}
		if (onlyOnHover)
		{
			htRoutine = StartCoroutine(HoverTestRoutine());
		}
	}

	private IEnumerator UpdateRoutine(float initialWait)
	{
		WaitForSeconds pauseWait = new WaitForSeconds(pauseTime);
		yield return null;
		bool first = true;
		while (base.enabled)
		{
			text.transform.localPosition = Vector3.zero;
			if (first)
			{
				yield return new WaitForSeconds(initialWait);
				first = false;
			}
			else
			{
				yield return pauseWait;
			}
			sizeFitter.SetLayoutHorizontal();
			float width = maskTransform.rect.width;
			float num = text.rectTransform.rect.width;
			float num2 = 0f - text.rectTransform.pivot.x;
			if (num2 > 0f)
			{
				num *= 1f + 2f * num2;
			}
			float tgtX = width - num;
			if (num > width)
			{
				float x = 0f;
				while (x > tgtX)
				{
					x = Mathf.MoveTowards(x, tgtX, speed * Time.deltaTime);
					text.transform.localPosition = new Vector3(x, 0f, 0f);
					yield return null;
				}
			}
			yield return pauseWait;
		}
	}

	public void Refresh(float initialWait = -1f)
	{
		if (initialWait < 0f)
		{
			initialWait = pauseTime;
		}
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
		if (!base.gameObject.activeInHierarchy || !base.enabled || (onlyOnHover && !pointerIn))
		{
			ResetTf();
		}
		else
		{
			updateRoutine = StartCoroutine(UpdateRoutine(onlyOnHover ? 0.5f : pauseTime));
		}
	}

	private IEnumerator HoverTestRoutine()
	{
		while (base.enabled)
		{
			Vector3 point = base.transform.InverseTransformPoint(Input.mousePosition);
			if (((RectTransform)base.transform).rect.Contains(point))
			{
				if (!pointerIn)
				{
					OnPointerEnter();
				}
			}
			else if (pointerIn)
			{
				OnPointerExit();
			}
			yield return null;
		}
	}

	private void OnPointerEnter()
	{
		pointerIn = true;
		if (onlyOnHover)
		{
			if (updateRoutine != null)
			{
				StopCoroutine(updateRoutine);
			}
			updateRoutine = StartCoroutine(UpdateRoutine(0.5f));
		}
	}

	private void OnPointerExit()
	{
		pointerIn = false;
		if (onlyOnHover)
		{
			if (updateRoutine != null)
			{
				StopCoroutine(updateRoutine);
			}
			ResetTf();
		}
	}

	private void ResetTf()
	{
		sizeFitter.SetLayoutHorizontal();
		text.transform.localPosition = Vector3.zero;
	}
}
