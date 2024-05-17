using UnityEngine;
using UnityEngine.UI;

public class MFDFlightLog : MonoBehaviour
{
	public Transform logParent;

	public GameObject messageTemplate;

	public RectTransform windowRect;

	private float lineHeight;

	private float lineHeightLocal;

	private int lineCharCount;

	private float windowHeight;

	private float currPos;

	private float maxHeight;

	private float scrollAmt;

	private RectTransform lastRect;

	private void Awake()
	{
		messageTemplate.SetActive(value: false);
		RectTransform rectTransform = (RectTransform)messageTemplate.transform;
		lineHeightLocal = rectTransform.rect.height;
		lineHeight = rectTransform.rect.height * rectTransform.localScale.y;
		lineCharCount = messageTemplate.GetComponent<Text>().text.Length;
		windowHeight = messageTemplate.transform.parent.InverseTransformVector(windowRect.TransformVector(new Vector3(windowRect.rect.height, 0f, 0f))).magnitude;
		scrollAmt = lineHeight * 2f;
		FlightLogger.AddLogEntryListener(AddMessage);
		foreach (FlightLogger.LogEntry item in FlightLogger.GetLog())
		{
			AddMessage(item);
		}
		MFDPortalPage componentInParent = GetComponentInParent<MFDPortalPage>();
		if ((bool)componentInParent)
		{
			componentInParent.OnSetPageStateEvent += Mfdp_OnSetPageStateEvent;
		}
	}

	private void Mfdp_OnSetPageStateEvent(MFDPortalPage.PageStates pageState)
	{
		if (pageState != MFDPortalPage.PageStates.Minimized && pageState != MFDPortalPage.PageStates.SubSized)
		{
			windowHeight = messageTemplate.transform.parent.InverseTransformVector(windowRect.TransformVector(new Vector3(windowRect.rect.height, 0f, 0f))).magnitude;
		}
	}

	private void OnDestroy()
	{
		FlightLogger.RemoveLogEntryListener(AddMessage);
	}

	private void AddMessage(FlightLogger.LogEntry e)
	{
		string timestampedMessage = e.timestampedMessage;
		int num = timestampedMessage.Length / lineCharCount;
		float num2 = (float)num * lineHeightLocal;
		GameObject gameObject = Object.Instantiate(messageTemplate);
		gameObject.transform.SetParent(messageTemplate.transform.parent, worldPositionStays: false);
		gameObject.transform.localScale = messageTemplate.transform.localScale;
		gameObject.transform.localRotation = messageTemplate.transform.localRotation;
		gameObject.SetActive(value: true);
		Vector3 localPosition = messageTemplate.transform.localPosition;
		if ((bool)lastRect)
		{
			localPosition.y = lastRect.localPosition.y - lastRect.rect.height * lastRect.localScale.y;
		}
		RectTransform rectTransform = (RectTransform)gameObject.transform;
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeightLocal + num2);
		rectTransform.localPosition = localPosition;
		lastRect = rectTransform;
		bool num3 = Mathf.Abs(currPos - BottomPos()) < 1f;
		float num4 = lineHeight + (float)num * lineHeight;
		maxHeight += num4;
		gameObject.GetComponent<Text>().text = timestampedMessage;
		if (num3)
		{
			ScrollToBottom();
		}
	}

	private void Update()
	{
		Vector3 localPosition = messageTemplate.transform.localPosition;
		localPosition.y += currPos;
		logParent.localPosition = Vector3.Lerp(logParent.localPosition, localPosition, 10f * Time.deltaTime);
	}

	public void ScrollUp()
	{
		currPos = Mathf.Max(0f, currPos - scrollAmt);
	}

	public void ScrollDown()
	{
		currPos = Mathf.Min(currPos + scrollAmt, BottomPos());
	}

	public void ScrollToBottom()
	{
		currPos = BottomPos();
	}

	private float BottomPos()
	{
		return Mathf.Max(0f, maxHeight - windowHeight);
	}

	public void DumpLog()
	{
		FlightLogger.DumpLog();
	}
}
