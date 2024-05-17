using UnityEngine;

[CreateAssetMenu]
public class SOText : ScriptableObject
{
	[TextArea(5, 30)]
	public string text;
}
