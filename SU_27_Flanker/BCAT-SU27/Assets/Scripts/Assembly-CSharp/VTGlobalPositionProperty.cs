using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTGlobalPositionProperty : VTPropertyField, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public VTScenarioEditor editor;

	public Text posText;

	public Button goToPosButton;

	public Image colorImg;

	private FixedPoint val;

	private Color color;

	private static float clrCycle;

	private float gizmosSizeMult = 1f;

	private void Awake()
	{
		color = Color.HSVToRGB(clrCycle, 1f, 0.5f);
		colorImg.color = color;
		clrCycle = Mathf.Repeat(clrCycle + 0.3f, 1f);
	}

	public override void SetInitialValue(object value)
	{
		val = (FixedPoint)value;
		if (val.globalPoint.sqrMagnitude > 0.0001)
		{
			Vector3D vector3D = VTMapManager.fetch.WorldPositionToGPSCoords(val.point);
			posText.text = string.Format("N{0}E{1}A{2}", vector3D.x.ToString("0.000"), vector3D.y.ToString("0.000"), vector3D.z.ToString("0"));
			goToPosButton.interactable = true;
		}
		else
		{
			posText.text = "Not set";
			goToPosButton.interactable = false;
		}
	}

	public override object GetValue()
	{
		return val;
	}

	public void SetPositionButton()
	{
		SetInitialValue(new FixedPoint(editor.editorCamera.focusTransform.position));
		ValueChanged();
	}

	public void GoToPositionButton()
	{
		editor.editorCamera.FocusOnPoint(val.point);
	}

	private void Update()
	{
		if ((bool)editor)
		{
			float radius = (val.point - editor.editorCamera.transform.position).magnitude * gizmosSizeMult * 0.015f;
			editor.editorCamera.DrawWireSphere(val.point, radius, color);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		gizmosSizeMult = 2f;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		gizmosSizeMult = 1f;
	}
}
