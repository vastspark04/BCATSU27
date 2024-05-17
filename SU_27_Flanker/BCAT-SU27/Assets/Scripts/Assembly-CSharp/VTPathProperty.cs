using UnityEngine.UI;

public class VTPathProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text valueText;

	private int currPathIdx;

	private FollowPath[] paths;

	private string[] pathOptions;

	public override void SetInitialValue(object value)
	{
		valueText.text = "None";
		paths = new FollowPath[editor.currentScenario.paths.paths.Count + 1];
		pathOptions = new string[paths.Length];
		currPathIdx = paths.Length - 1;
		int num = 0;
		foreach (FollowPath value2 in editor.currentScenario.paths.paths.Values)
		{
			paths[num] = value2;
			if (value2 == (FollowPath)value)
			{
				currPathIdx = num;
				valueText.text = value2.gameObject.name;
			}
			pathOptions[num] = value2.gameObject.name;
			num++;
		}
		paths[paths.Length - 1] = null;
		pathOptions[paths.Length - 1] = "None";
	}

	public override object GetValue()
	{
		if (currPathIdx >= 0)
		{
			return paths[currPathIdx];
		}
		return null;
	}

	public void EditButton()
	{
		editor.optionSelector.Display(fieldName, pathOptions, currPathIdx, OnSetPath);
	}

	private void OnSetPath(int idx)
	{
		currPathIdx = idx;
		FollowPath followPath = paths[idx];
		if ((bool)followPath)
		{
			valueText.text = followPath.gameObject.name;
		}
		else
		{
			valueText.text = "None";
		}
		ValueChanged();
	}
}
