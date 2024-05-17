using UnityEngine;

public class OculusSpatializerUnity : MonoBehaviour
{
	public LayerMask layerMask;
	public bool visualizeRoom;
	public int raysPerSecond;
	public float roomInterpSpeed;
	public float maxWallDistance;
	public int rayCacheSize;
	public bool dynamicReflectionsEnabled;
}
