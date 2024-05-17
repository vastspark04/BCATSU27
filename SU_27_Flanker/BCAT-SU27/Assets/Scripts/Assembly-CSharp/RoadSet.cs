using UnityEngine;

[CreateAssetMenu]
public class RoadSet : ScriptableObject
{
	public string displayName;

	public string description;

	public Texture2D thumbnail;

	public float radius = 7f;

	public RoadMeshProfile mainRoad;

	public RoadIntersectionProfile endPiece;

	public RoadMeshProfile bridge;

	public RoadIntersectionProfile bridgeToRoad;

	public RoadIntersectionProfile threeWay;

	public RoadIntersectionProfile fourWay;
}
