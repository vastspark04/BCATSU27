using UnityEngine;
using UnityEngine.UI;

public class VTEditorInfoBar : MonoBehaviour
{
	public VTScenarioEditor editor;

	public InputField altitudeValue;

	public InputField altitudeAGLValue;

	public Text altLabelText;

	public Text northValue;

	public Text eastValue;

	public InputField headingInputField;

	public Text cursorLocation;

	private MeasurementManager.DistanceModes[] altModes = new MeasurementManager.DistanceModes[2]
	{
		MeasurementManager.DistanceModes.Meters,
		MeasurementManager.DistanceModes.Feet
	};

	private string[] altLabels = new string[2] { "m", "ft" };

	private int altIdx;

	private VTMapManager mapManager;

	private Transform cursorTransform;

	public void ToggleAltIdx()
	{
		altIdx = (altIdx + 1) % altModes.Length;
		altLabelText.text = "Altitude(" + altLabels[altIdx] + ")";
	}

	private void Start()
	{
		mapManager = VTMapManager.fetch;
		cursorTransform = editor.editorCamera.focusTransform;
		headingInputField.onEndEdit.AddListener(OnEndEditHeading);
		altLabelText.text = "Altitude(m)";
	}

	private void Update()
	{
		Vector3D vector3D = mapManager.WorldPositionToGPSCoords(cursorTransform.position);
		if (!altitudeValue.isFocused)
		{
			altitudeValue.text = MeasurementManager.ConvertDistance((float)vector3D.z, altModes[altIdx]).ToString("0");
		}
		if (!altitudeAGLValue.isFocused)
		{
			altitudeAGLValue.text = MeasurementManager.ConvertDistance(editor.editorCamera.cursorAGL, altModes[altIdx]).ToString("0");
		}
		northValue.text = VTMapManager.FormattedCoordsMinSec(vector3D.x);
		eastValue.text = VTMapManager.FormattedCoordsMinSec(vector3D.y);
		cursorLocation.text = editor.editorCamera.cursorLocation.ToString();
		if (!headingInputField.isFocused)
		{
			headingInputField.text = Mathf.Round(editor.editorCamera.cursorHeading).ToString();
		}
	}

	private void OnEndEditHeading(string s)
	{
		if (float.TryParse(s, out var result))
		{
			editor.editorCamera.SetCursorHeading(result);
		}
	}

	public void EndEditMSL(string s)
	{
		if (float.TryParse(s, out var result))
		{
			if (altIdx == 1)
			{
				result /= MeasurementManager.DistToFeet(1f);
			}
			SetAltitude(result);
		}
	}

	public void EndEditAGL(string s)
	{
		if (float.TryParse(s, out var result))
		{
			if (altIdx == 1)
			{
				result /= MeasurementManager.DistToFeet(1f);
			}
			result += editor.editorCamera.surfaceAlt;
			SetAltitude(result);
		}
	}

	private void SetAltitude(float alt)
	{
		editor.editorCamera.SetCursorAlt(alt);
	}
}
