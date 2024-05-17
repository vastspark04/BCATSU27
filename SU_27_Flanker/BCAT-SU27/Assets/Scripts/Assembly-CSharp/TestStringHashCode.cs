using UnityEngine;

public class TestStringHashCode : MonoBehaviour
{
	public string s;

	[ContextMenu("Output Hash Code")]
	public void Test()
	{
		Debug.Log(s.GetHashCode());
	}
}
