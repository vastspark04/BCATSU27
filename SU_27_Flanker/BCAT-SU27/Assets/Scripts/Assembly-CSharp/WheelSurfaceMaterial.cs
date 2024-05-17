using UnityEngine;

[CreateAssetMenu(fileName = "New Wheel Surface Material", menuName = "Wheel Physics/Surface Material", order = 0)]
public class WheelSurfaceMaterial : ScriptableObject
{
	public float bumpiness;

	public float bumpScale;

	public float traction;

	public float resistance;

	public AudioClip rollingAudio;

	public Color dustColor = Color.clear;
}
