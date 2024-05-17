using UnityEngine;
using UnityEngine.UI;

public class VTEdEventsWindow : VTEdUITab
{
	public VTScenarioEditor editor;

	public VTEdTimedEventsWindow timedEventsWindow;

	public VTEdTriggerEventsWindow triggerEventsWindow;

	public VTEdSequencedEventsWindow sequencedEventsWindow;

	public Image timedButtonImage;

	public Image triggerButtonImage;

	public Image sequencedEventsButton;

	private Color activeColor;

	private Color inactiveColor;

	private void Start()
	{
		editor.OnScenarioLoaded += Editor_OnScenarioLoaded;
		activeColor = timedButtonImage.color;
		inactiveColor = triggerButtonImage.color;
	}

	private void Editor_OnScenarioLoaded()
	{
		if (base.isOpen)
		{
			if (timedEventsWindow.isOpen)
			{
				timedEventsWindow.Open();
			}
			if (triggerEventsWindow.isOpen)
			{
				triggerEventsWindow.Open();
			}
			if ((bool)sequencedEventsWindow && sequencedEventsWindow.isOpen)
			{
				sequencedEventsWindow.Open();
			}
		}
	}

	public override void OnOpenedTab()
	{
		base.OnOpenedTab();
		if (timedEventsWindow.isOpen)
		{
			timedEventsWindow.Open();
		}
		else if (triggerEventsWindow.isOpen)
		{
			triggerEventsWindow.Open();
		}
		else if ((bool)sequencedEventsWindow && sequencedEventsWindow.isOpen)
		{
			sequencedEventsWindow.Open();
		}
		else
		{
			timedEventsWindow.Open();
		}
	}

	public override void OnClosedTab()
	{
		base.OnClosedTab();
		if (timedEventsWindow.isOpen)
		{
			timedEventsWindow.groupEditor.Close();
		}
		if (triggerEventsWindow.isOpen && triggerEventsWindow.eventEditor.gameObject.activeSelf)
		{
			triggerEventsWindow.eventEditor.Close();
		}
		if ((bool)sequencedEventsWindow && sequencedEventsWindow.isOpen && sequencedEventsWindow.sequenceEditor.gameObject.activeSelf)
		{
			sequencedEventsWindow.sequenceEditor.CloseButton();
		}
	}

	public void TimedEventsButton()
	{
		if (triggerEventsWindow.isOpen)
		{
			triggerEventsWindow.CloseWindow();
		}
		if ((bool)sequencedEventsWindow && sequencedEventsWindow.isOpen)
		{
			sequencedEventsWindow.CloseWindow();
		}
		if (!timedEventsWindow.isOpen)
		{
			timedEventsWindow.Open();
		}
		timedButtonImage.color = activeColor;
		triggerButtonImage.color = inactiveColor;
		if ((bool)sequencedEventsButton)
		{
			sequencedEventsButton.color = inactiveColor;
		}
	}

	public void TriggerEventsButton()
	{
		if (!triggerEventsWindow.isOpen)
		{
			triggerEventsWindow.Open();
		}
		if ((bool)sequencedEventsWindow && sequencedEventsWindow.isOpen)
		{
			sequencedEventsWindow.CloseWindow();
		}
		if (timedEventsWindow.isOpen)
		{
			timedEventsWindow.Close();
		}
		timedButtonImage.color = inactiveColor;
		triggerButtonImage.color = activeColor;
		if ((bool)sequencedEventsButton)
		{
			sequencedEventsButton.color = inactiveColor;
		}
	}

	public void SequencedEventsButton()
	{
		if (!sequencedEventsWindow.isOpen)
		{
			sequencedEventsWindow.Open();
		}
		if (triggerEventsWindow.isOpen)
		{
			triggerEventsWindow.CloseWindow();
		}
		if (timedEventsWindow.isOpen)
		{
			timedEventsWindow.Close();
		}
		timedButtonImage.color = inactiveColor;
		triggerButtonImage.color = inactiveColor;
		if ((bool)sequencedEventsButton)
		{
			sequencedEventsButton.color = activeColor;
		}
	}
}
