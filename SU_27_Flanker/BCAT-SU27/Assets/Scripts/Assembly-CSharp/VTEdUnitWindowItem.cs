using UnityEngine;
using UnityEngine.UI;

public class VTEdUnitWindowItem : MonoBehaviour
{
	public VTEdUnitsWindow unitsWindow;

	public Text unitNameText;

	public GameObject selectionBoxImage;

	public int characterLimit = 21;

	private float lastClickTime = -1f;

	private UnitSpawner unitSpawner;

	private bool isSelected;

	public UnitSpawner GetUnitSpawner()
	{
		return unitSpawner;
	}

	public void SetUnit(UnitSpawner us)
	{
		unitNameText.text = us.GetShortDisplayName(characterLimit);
		unitSpawner = us;
		bool active = (isSelected = unitsWindow.IsUnitSelected(us));
		selectionBoxImage.SetActive(active);
		unitsWindow.OnSelectedUnit += OnSelected;
	}

	private void OnDestroy()
	{
		if (unitsWindow != null)
		{
			unitsWindow.OnSelectedUnit -= OnSelected;
		}
	}

	private void OnSelected(UnitSpawner us)
	{
		if (!this || !base.gameObject)
		{
			Debug.LogError("OnSelected was invoked on an destroyed game object!");
			if ((bool)unitsWindow)
			{
				unitsWindow.OnSelectedUnit -= OnSelected;
				Debug.LogError(" - Removed event listener.");
			}
		}
		else
		{
			if ((bool)us && us.unitInstanceID == unitSpawner.unitInstanceID)
			{
				isSelected = true;
			}
			else
			{
				isSelected = false;
			}
			selectionBoxImage.SetActive(isSelected);
		}
	}

	public void Select()
	{
		if (!isSelected)
		{
			unitsWindow.unitOptionsWindow.Close();
			unitsWindow.SelectUnit(unitSpawner);
		}
	}

	public void Deselect()
	{
		if (isSelected)
		{
			unitsWindow.unitOptionsWindow.Close();
			unitsWindow.DeselectUnit(unitSpawner);
		}
	}

	public void UnitToolsButton()
	{
		if (!isSelected)
		{
			Select();
		}
		unitsWindow.OpenToolsForUnit(unitSpawner);
	}

	private void GoToUnit()
	{
		if (!isSelected)
		{
			Select();
		}
		unitsWindow.editor.editorCamera.FocusOnPoint(unitSpawner.transform.position);
	}

	public void OnClick()
	{
		if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
		{
			GoToUnit();
			return;
		}
		if (isSelected)
		{
			Deselect();
		}
		else
		{
			Select();
		}
		lastClickTime = Time.unscaledTime;
	}
}
