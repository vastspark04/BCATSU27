using UnityEngine.UI;

public static class VTScenarioEditorExtensions
{
	public static void SetInteractable(this Button[] buttons, bool interactable)
	{
		int num = buttons.Length;
		for (int i = 0; i < num; i++)
		{
			if (buttons[i] != null)
			{
				buttons[i].interactable = interactable;
			}
		}
	}

	public static bool Contains<T>(this T[] stringArray, T s)
	{
		for (int i = 0; i < stringArray.Length; i++)
		{
			if (stringArray[i] != null && stringArray[i].Equals(s))
			{
				return true;
			}
		}
		return false;
	}
}
