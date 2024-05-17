using UnityEngine;
using System.Collections.Generic;

public class DebugUIBuilder : MonoBehaviour
{
	[SerializeField]
	private RectTransform buttonPrefab;
	[SerializeField]
	private RectTransform labelPrefab;
	[SerializeField]
	private RectTransform sliderPrefab;
	[SerializeField]
	private RectTransform dividerPrefab;
	[SerializeField]
	private RectTransform togglePrefab;
	[SerializeField]
	private RectTransform radioPrefab;
	[SerializeField]
	private GameObject uiHelpersToInstantiate;
	[SerializeField]
	private Transform[] targetContentPanels;
	[SerializeField]
	private bool manuallyResizeContentPanels;
	[SerializeField]
	private List<GameObject> toEnable;
	[SerializeField]
	private List<GameObject> toDisable;
	public LaserPointer.LaserBeamBehavior laserBeamBehavior;
}
