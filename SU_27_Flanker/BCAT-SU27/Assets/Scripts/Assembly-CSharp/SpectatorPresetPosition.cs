using System.Collections.Generic;
using UnityEngine;

public class SpectatorPresetPosition : MonoBehaviour
{
	public static List<SpectatorPresetPosition> presetPositions = new List<SpectatorPresetPosition>();

	public bool fixedView;

	private void OnEnable()
	{
		presetPositions.Add(this);
	}

	private void OnDisable()
	{
		presetPositions.Remove(this);
	}
}
