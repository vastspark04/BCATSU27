using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VTOLVR.Multiplayer{

public class MPPlayerDistanceChecker : MonoBehaviour
{
	private float _cDist;

	public float closestDistance => _cDist;

	private void OnEnable()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			base.enabled = false;
		}
		else
		{
			StartCoroutine(UpdateRoutine());
		}
	}

	private IEnumerator UpdateRoutine()
	{
		yield return null;
		while (base.enabled)
		{
			float cSqr = float.MaxValue;
			List<PlayerInfo> players = VTOLMPLobbyManager.instance.connectedPlayers;
			for (int i = 0; i < players.Count; i++)
			{
				PlayerInfo playerInfo = players[i];
				if ((bool)playerInfo.vehicleActor)
				{
					float sqrMagnitude = (playerInfo.vehicleActor.position - base.transform.position).sqrMagnitude;
					if (sqrMagnitude < cSqr)
					{
						cSqr = sqrMagnitude;
					}
				}
				yield return null;
			}
			_cDist = Mathf.Sqrt(cSqr);
			yield return null;
		}
	}
}

}