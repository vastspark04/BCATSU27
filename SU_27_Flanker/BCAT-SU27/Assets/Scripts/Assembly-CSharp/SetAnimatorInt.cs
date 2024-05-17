using UnityEngine;

public class SetAnimatorInt : MonoBehaviour
{
	public string intName;

	public int integer;

	private void Awake()
	{
		GetComponent<Animator>().SetInteger(intName, integer);
	}
}
