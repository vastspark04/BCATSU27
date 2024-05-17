using System.Collections.Generic;
using UnityEngine;

namespace VTOLVR.Testing{

public class AttackVectorsTest : MonoBehaviour
{
	public List<Actor> enemies;

	public Transform attackerTf;

	public Transform targetTf;

	public float attackRange;

	private void OnDrawGizmos()
	{
		if (enemies != null && (bool)attackerTf && (bool)targetTf)
		{
			AIWing.GetGroundAttackVectors(attackerTf.position, targetTf.position, attackRange, new List<Actor>[1] { enemies }, out var ingress, out var egress);
			float num = (targetTf.position - attackerTf.position).magnitude - attackRange;
			Vector3 vector = targetTf.position - attackRange * ingress;
			Gizmos.color = Color.green;
			Gizmos.DrawLine(vector, vector - num * ingress);
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(vector, vector + 5000f * egress);
		}
	}
}

}