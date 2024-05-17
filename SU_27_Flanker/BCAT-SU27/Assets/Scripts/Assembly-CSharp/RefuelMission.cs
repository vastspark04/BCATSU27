using UnityEngine;

public class RefuelMission : MissionObjective
{
	public FuelTank fuelTank;

	public FuelTank[] fuelTanks;

	public MultiplayerSpawn[] mpSpawnTargets;

	[Range(0f, 1f)]
	public float completionThreshold = 0.98f;

	private void Update()
	{
		if (!base.started || base.completed)
		{
			return;
		}
		bool flag = true;
		if ((bool)fuelTank && fuelTank.fuelFraction < completionThreshold)
		{
			flag = false;
		}
		if (fuelTanks != null)
		{
			int num = 0;
			while (flag && num < fuelTanks.Length)
			{
				if (fuelTanks[num].fuelFraction < completionThreshold)
				{
					flag = false;
				}
				num++;
			}
		}
		if (mpSpawnTargets != null)
		{
			int num2 = 0;
			while (flag && num2 < mpSpawnTargets.Length)
			{
				MultiplayerSpawn multiplayerSpawn = mpSpawnTargets[num2];
				if ((bool)multiplayerSpawn.actor)
				{
					if (multiplayerSpawn.actor.GetComponent<FuelTank>().fuelFraction < completionThreshold)
					{
						flag = false;
					}
				}
				else
				{
					flag = false;
				}
				num2++;
			}
		}
		if (flag)
		{
			CompleteObjective();
			if (base.isPlayersMission)
			{
				EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_completed}", red: false);
			}
		}
	}
}
