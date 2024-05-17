using UnityEngine;

public class FollowPathTest : MonoBehaviour
{
	private FollowPath fp;

	public Transform testObject;

	public float speed = 0.5f;

	private float t;

	private void Start()
	{
		fp = GetComponent<FollowPath>();
	}

	private void Update()
	{
		t = Mathf.Repeat(t + speed * Time.deltaTime, 1f);
		testObject.localPosition = fp.GetPoint(t);
	}
}
