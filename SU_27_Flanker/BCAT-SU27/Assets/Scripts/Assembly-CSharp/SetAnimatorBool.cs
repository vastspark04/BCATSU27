using UnityEngine;

public class SetAnimatorBool : MonoBehaviour
{
	public string boolName;

	public bool val;

	private void Start()
	{
		GetComponent<Animator>().SetBool(boolName, val);
	}
}
