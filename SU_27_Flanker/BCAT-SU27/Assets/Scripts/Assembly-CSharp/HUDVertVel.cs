using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class HUDVertVel : MonoBehaviour
{
	public Image velBar;

	public Text numberText;

	public float barScale = 5f;

	public Transform caretTransform;

	public float maxCaretVel;

	public bool showNumberTextInRange;

	public MinMax numberTextRange;

	public bool fpmImperial;

	private Rigidbody rb;

	private MeasurementManager measurements;

	private StringBuilder sb;

	private void Awake()
	{
		sb = new StringBuilder();
	}

	private void Start()
	{
		rb = GetComponentInParent<Rigidbody>();
		measurements = GetComponentInParent<MeasurementManager>();
	}

	private void Update()
	{
		float num = measurements.ConvertedVerticalSpeed(rb.velocity.y);
		float y = rb.velocity.y;
		num = Mathf.Round(num * 10f) / 10f;
		if ((bool)numberText)
		{
			sb.Clear();
		}
		if (y >= 0f)
		{
			if ((bool)numberText)
			{
				sb.Append('+');
			}
			if ((bool)velBar)
			{
				velBar.rectTransform.localRotation = Quaternion.identity;
			}
		}
		else if ((bool)velBar)
		{
			velBar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
		}
		if ((bool)velBar)
		{
			velBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barScale * Mathf.Clamp(Mathf.Abs(y), 0f, 30f));
		}
		if ((bool)caretTransform)
		{
			caretTransform.localPosition = new Vector3(0f, Mathf.Clamp(y, 0f - maxCaretVel, maxCaretVel) * barScale, 0f);
		}
		if ((bool)numberText)
		{
			if (fpmImperial && measurements.altitudeMode == MeasurementManager.AltitudeModes.Feet)
			{
				float num2 = y * 196.85f;
				num2 = Mathf.RoundToInt(num2 / 10f) * 10;
				sb.Append(num2);
			}
			else
			{
				sb.Append((Mathf.Abs(num) >= 10f) ? Mathf.RoundToInt(num).ToString() : num.ToString("0.0"));
			}
			numberText.text = sb.ToString();
			if (showNumberTextInRange)
			{
				numberText.gameObject.SetActive(y > numberTextRange.min && y < numberTextRange.max);
			}
		}
	}
}
