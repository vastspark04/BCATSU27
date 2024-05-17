using System.Collections;
using UnityEngine;

namespace VTOLVR.Multiplayer.Test{

public class TestGroundUnitMoverSync : MonoBehaviour
{
	public GroundUnitMover mover;

	public GroundUnitMover remoteMover;

	public FollowPath path;

	public MinMax latency = new MinMax(0.02f, 0.05f);

	public float updateInterval = 0.5f;

	private float origMoveSpeed = -1f;

	[ContextMenu("Begin Test")]
	public void BeginTest()
	{
		StartCoroutine(TestRoutine());
	}

	private IEnumerator TestRoutine()
	{
		mover.gameObject.GetComponentImplementing<GroundUnitSpawn>().SetPath(path);
		float seconds = updateInterval;
		WaitForSeconds wait = new WaitForSeconds(seconds);
		while (base.enabled)
		{
			FixedPoint pos = new FixedPoint(mover.transform.position);
			Vector3 vel = mover.velocity;
			float timestamp = Time.time;
			yield return new WaitForSeconds(latency.Random());
			SendRemote(pos.point, vel, timestamp);
			yield return wait;
		}
	}

	private void SendRemote(Vector3 pos, Vector3 vel, float timestamp)
	{
		float num = Time.time - timestamp;
		remoteMover.SetRemoteTarget(pos + vel * (updateInterval + num));
		if (origMoveSpeed < 0f)
		{
			origMoveSpeed = remoteMover.moveSpeed;
		}
		remoteMover.moveSpeed = origMoveSpeed * 1.2f;
	}
}

}