using UnityEngine;

public class GazeTargetSpawner : MonoBehaviour
{
	public GameObject GazeTargetPrefab;
	public int NumberOfDummyTargets;
	public int RadiusMultiplier;
	[SerializeField]
	private bool isVisible;
}
