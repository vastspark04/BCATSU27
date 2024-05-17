using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ATCVoiceProfile : ScriptableObject
{
	public AudioClip[] numberClips;

	public AudioClip[] letterClips;

	public List<AudioClip> towerClips;

	public List<AudioClip> landingRequestFlyHeadingClips;

	public List<AudioClip> expectRunwayClips;

	public List<AudioClip> parallelDesignationClips;

	public List<AudioClip> landingClearedAtRunwayClips;

	public List<AudioClip> landingPatternFullClips;

	public List<AudioClip> taxiToParkingZoneClips;

	public List<AudioClip> taxiToRunwayClips;

	public List<AudioClip> holdShortAtRunwayClips;

	public List<AudioClip> clearedForTakeoffRunwayClips;

	public List<AudioClip> cancelRequestClips;

	public List<AudioClip> unableClips;

	public List<AudioClip> landedElsewhereClips;

	public List<AudioClip> landedBeforeClearanceClips;

	public List<AudioClip> clearedVerticalTakeoffClips;

	public List<AudioClip> clearedVerticalLandingClips;

	public List<AudioClip> contactedWrongTowerClips;

	[Header("Carrier")]
	public List<AudioClip> landingClearedAtCarrierClips;

	public List<AudioClip> waitForCatapultClearanceClips;

	public List<AudioClip> taxiToCatapultClips;

	public List<AudioClip> preCatapultClips;

	public List<AudioClip> catapultReadyClips;

	public List<AudioClip> callTheBallClips;

	public List<AudioClip> rogerBallClips;

	[Header("Carrier LSO")]
	public List<AudioClip> linedUpClips;

	public List<AudioClip> comeLeftClips;

	public List<AudioClip> rightLineupClips;

	public List<AudioClip> youreHighClips;

	public List<AudioClip> powerLowClips;

	public List<AudioClip> waveOffClips;

	public List<AudioClip> foulDeckClips;

	public List<AudioClip> bolterClips;

	public List<AudioClip> xWireClips;

	public List<AudioClip> returnToHoldingClips;

	[ContextMenu("Clear number clips")]
	public void ClearNumberClips()
	{
		numberClips = null;
	}

	[ContextMenu("Clear Letter Clips")]
	public void ClearLetterClips()
	{
		letterClips = null;
	}

	public void PlayLandedBeforeClearanceMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(landedBeforeClearanceClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayLandedElseWhereMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(landedElsewhereClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayWaitForCatapultClearanceMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(waitForCatapultClearanceClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayTaxiToCatapultMsg(CarrierCatapult c)
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(taxiToCatapultClips);
		AppendNumbers(array, 4, c.catapultDesignation);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayPreCatapultMsg()
	{
		AudioClip[] array = new AudioClip[4];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(preCatapultClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayRunUpEnginesCatapultMsg()
	{
		AudioClip[] array = new AudioClip[4];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(catapultReadyClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayCancelledRequestMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(cancelRequestClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayClearedToLandCarrierMsg()
	{
		AudioClip[] array = new AudioClip[4];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(landingClearedAtCarrierClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayTaxiToRunwayMsg(float heading, Runway.ParallelDesignations pDes = Runway.ParallelDesignations.None)
	{
		AudioClip[] array = new AudioClip[(pDes == Runway.ParallelDesignations.None) ? 3 : 4];
		array[0] = GetRandomClip(taxiToRunwayClips);
		AppendRunwayDigits(heading, array, 1);
		if (pDes != 0)
		{
			array[3] = GetParallelDesignationClip(pDes);
		}
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayRequestedWrongATCMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(contactedWrongTowerClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayUnableMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(unableClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayHoldShortAtRunwayMsg(float heading)
	{
		AudioClip[] array = new AudioClip[6];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(holdShortAtRunwayClips);
		AppendRunwayDigits(heading, array, 4);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayClearForTakeoffRunwayMsg(float heading)
	{
		AudioClip[] array = new AudioClip[7];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(clearedForTakeoffRunwayClips);
		AppendRunwayDigits(heading, array, 5);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayClearedVerticalTakeoffMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(clearedVerticalTakeoffClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayClearedVerticalLandingMsg(int padNumber)
	{
		AudioClip[] array = new AudioClip[7];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(clearedVerticalLandingClips);
		int num = 0;
		int num2 = 0;
		if (padNumber > 9)
		{
			num = padNumber / 10;
			num2 = padNumber - num;
		}
		else
		{
			num2 = padNumber;
		}
		AppendNumbers(array, 5, num, num2);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayTaxiToParkingMsg()
	{
		CommRadioManager.instance.PlayMessage(GetRandomClip(taxiToParkingZoneClips));
	}

	public void PlayCallTheBallMsg()
	{
		AudioClip[] array = new AudioClip[4];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(callTheBallClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayRogerBallMsg()
	{
		CommRadioManager.instance.PlayMessage(GetRandomClip(rogerBallClips), duckBGM: false, queueBehindLiveRadio: false);
	}

	public void PlayLandingFlyHeadingMsg(float heading, Runway runway)
	{
		Runway.ParallelDesignations parallelDesignation = runway.parallelDesignation;
		bool flag = parallelDesignation != Runway.ParallelDesignations.None;
		AudioClip[] array = new AudioClip[flag ? 12 : 11];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(landingRequestFlyHeadingClips);
		int num = Mathf.RoundToInt(heading) % 360;
		int num2 = num / 100;
		int num3 = (num - num2 * 100) / 10;
		int num4 = num - num2 * 100 - num3 * 10;
		AppendNumbers(array, 5, num2, num3, num4);
		array[8] = GetRandomClip(expectRunwayClips);
		AppendRunwayDigits(runway, array, 9);
		if (flag)
		{
			array[11] = GetParallelDesignationClip(parallelDesignation);
		}
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayVerticalLandingFlyHeadingMsg(float heading)
	{
		AudioClip[] array = new AudioClip[8];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(landingRequestFlyHeadingClips);
		int num = Mathf.RoundToInt(heading) % 360;
		int num2 = num / 100;
		int num3 = (num - num2 * 100) / 10;
		int num4 = num - num2 * 100 - num3 * 10;
		AppendNumbers(array, 5, num2, num3, num4);
		CommRadioManager.instance.PlayMessageString(array);
	}

	public void PlayLandingPatternFullMsg()
	{
		AudioClip[] array = new AudioClip[5];
		AppendPlayerDesignation(array, 0);
		array[3] = GetRandomClip(towerClips);
		array[4] = GetRandomClip(landingPatternFullClips);
		CommRadioManager.instance.PlayMessageString(array);
	}

	private AudioClip GetRandomClip(List<AudioClip> list)
	{
		if (list == null || list.Count == 0)
		{
			return null;
		}
		int index = Random.Range(0, list.Count);
		return list[index];
	}

	public void PlayLandingClearedForRunwayMsg(float heading, Runway.ParallelDesignations parallelDesignation = Runway.ParallelDesignations.None)
	{
		AudioClip[] array;
		if (parallelDesignation == Runway.ParallelDesignations.None)
		{
			array = new AudioClip[7];
			AppendPlayerDesignation(array, 0);
			array[3] = GetRandomClip(towerClips);
			array[4] = GetRandomClip(landingClearedAtRunwayClips);
			AppendRunwayDigits(heading, array, 5);
		}
		else
		{
			array = new AudioClip[8];
			AppendPlayerDesignation(array, 0);
			array[3] = GetRandomClip(towerClips);
			array[4] = GetRandomClip(landingClearedAtRunwayClips);
			AppendRunwayDigits(heading, array, 5);
			array[7] = GetParallelDesignationClip(parallelDesignation);
		}
		CommRadioManager.instance.PlayMessageString(array);
	}

	private AudioClip GetParallelDesignationClip(Runway.ParallelDesignations d)
	{
		return d switch
		{
			Runway.ParallelDesignations.Left => parallelDesignationClips[0], 
			Runway.ParallelDesignations.Center => parallelDesignationClips[1], 
			Runway.ParallelDesignations.Right => parallelDesignationClips[2], 
			_ => null, 
		};
	}

	private void AppendNumbers(AudioClip[] audioString, int idx, params int[] nums)
	{
		for (int i = 0; i < nums.Length; i++)
		{
			int num = ((i == nums.Length - 1) ? (nums[i] * 2 + 1) : (nums[i] * 2));
			audioString[idx + i] = numberClips[num];
		}
	}

	private void AppendRunwayDigits(Runway runway, AudioClip[] audioString, int idx)
	{
		float heading = VectorUtils.Bearing(runway.transform.position, runway.transform.position + runway.transform.forward);
		AppendRunwayDigits(heading, audioString, idx);
	}

	private void AppendRunwayDigits(float heading, AudioClip[] audioString, int idx)
	{
		heading /= 10f;
		int num = Mathf.RoundToInt(heading);
		if (num == 0)
		{
			num = 36;
		}
		int num2 = Mathf.FloorToInt((float)num / 10f);
		int num3 = num - num2 * 10;
		AppendNumbers(audioString, idx, num2, num3);
	}

	private void AppendPlayerDesignation(AudioClip[] audioString, int idx)
	{
		AppendActorDesignation(FlightSceneManager.instance.playerActor, audioString, idx);
	}

	private void AppendActorDesignation(Actor a, AudioClip[] audioString, int idx)
	{
		audioString[idx] = letterClips[(int)a.designation.letter];
		AppendNumbers(audioString, idx + 1, a.designation.num1, a.designation.num2);
	}

	public void PlayLSOLinedUp()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(linedUpClips));
	}

	public void PlayLSOComeLeft()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(comeLeftClips));
	}

	public void PlayLSORightForLineup()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(rightLineupClips));
	}

	public void PlayLSOYoureHigh()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(youreHighClips));
	}

	public void PlayLSOPowerLow()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(powerLowClips));
	}

	public void PlayLSOHighLeft()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(youreHighClips), GetRandomClip(comeLeftClips));
	}

	public void PlayLSOHighRight()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(youreHighClips), GetRandomClip(rightLineupClips));
	}

	public void PlayLSOLowLeft()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(powerLowClips), GetRandomClip(comeLeftClips));
	}

	public void PlayLSOLowRight()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(powerLowClips), GetRandomClip(rightLineupClips));
	}

	public void PlayLSOWaveOff()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(waveOffClips));
	}

	public void PlayLSOFoulDeck()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(foulDeckClips));
	}

	public void PlayLSOBolter()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(bolterClips));
	}

	public void PlayLSOXwire(int idx)
	{
		if (idx >= 0 && idx < xWireClips.Count)
		{
			CommRadioManager.instance.PlayMessage(xWireClips[idx]);
		}
	}

	public void PlayLSOReturnToHolding()
	{
		CommRadioManager.instance.PlayLiveRadioMessage(GetRandomClip(returnToHoldingClips));
	}

	public static ATCVoiceProfile ImportFromFolder(string folderPath)
	{
		return null;
	}
}
