using System.Collections.Generic;
using UnityEngine;

public class BzMapRoadTest : MonoBehaviour
{
	public List<BezierTest> testCurves;

	public bool constantUpdate;

	[ContextMenu("Test")]
	public void Test()
	{
	}

	private void Update()
	{
		if (constantUpdate)
		{
			Test();
		}
	}
}
