using UnityEngine;

[CreateAssetMenu]
public class GroundCrewVoiceProfile : ScriptableObject
{
	public enum GroundCrewMessages
	{
		NotAvailable,
		IsAirborne,
		TaxiToStation,
		EnteredStation,
		TurnOffEngines,
		DisarmWeapons,
		Success,
		ReturnedToVehicle
	}

	public AudioClip[] rearmingNotAvailableClips;

	public AudioClip[] isAirborneClips;

	public AudioClip[] taxiToStationClips;

	public AudioClip[] enteredRearmingStationClips;

	public AudioClip[] turnOffEnginesClips;

	public AudioClip[] disarmWeaponsClips;

	public AudioClip[] successClips;

	public AudioClip[] returnedToVehicleClips;

	public void PlayMessage(GroundCrewMessages m)
	{
		CommRadioManager.instance.PlayMessage(GetClip(m));
	}

	public AudioClip[] GetAllClips(GroundCrewMessages m)
	{
		return m switch
		{
			GroundCrewMessages.NotAvailable => rearmingNotAvailableClips, 
			GroundCrewMessages.IsAirborne => isAirborneClips, 
			GroundCrewMessages.TaxiToStation => taxiToStationClips, 
			GroundCrewMessages.EnteredStation => enteredRearmingStationClips, 
			GroundCrewMessages.TurnOffEngines => turnOffEnginesClips, 
			GroundCrewMessages.DisarmWeapons => disarmWeaponsClips, 
			GroundCrewMessages.Success => successClips, 
			GroundCrewMessages.ReturnedToVehicle => returnedToVehicleClips, 
			_ => null, 
		};
	}

	private AudioClip GetClip(GroundCrewMessages m)
	{
		return GetAllClips(m)?.Random();
	}
}
