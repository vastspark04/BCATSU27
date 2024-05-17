using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AWACSVoiceProfile : ScriptableObject
{
	public AudioClip[] phoneticAlphabet;

	public AudioClip[] compoundDigits;

	public AudioClip[] rangeClips;

	public AudioClip[] highAltitudeClips;

	public AudioClip[] thousandClips;

	public AudioClip[] overlordClips;

	public AudioClip[] hostileBraaClips;

	public AudioClip[] groupBraaClips;

	public AudioClip[] hostileBullseyeClips;

	public AudioClip[] groupBullseyeClips;

	public AudioClip[] homeplateBraaClips;

	public AudioClip[] hotClips;

	public AudioClip[] coldClips;

	public AudioClip[] cardinalClips;

	public AudioClip[] mergedClips;

	public AudioClip[] lowClips;

	public AudioClip[] fastClips;

	public AudioClip[] veryFastClips;

	public AudioClip[] cleanClips;

	public AudioClip[] grandSlamClips;

	public AudioClip[] popupClips;

	public AudioClip[] pictureClips;

	public AudioClip[] rtbClips;

	public AudioClip[] goingDownClips;

	public AudioClip[] unableClips;

	public AudioClip[] leansOnClips;

	private List<AudioClip> audioString = new List<AudioClip>();

	private Vector3 playerPos
	{
		get
		{
			if ((bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor)
			{
				return FlightSceneManager.instance.playerActor.position;
			}
			return Vector3.zero;
		}
	}

	private MeasurementManager.DistanceModes distanceMode
	{
		get
		{
			if ((bool)MeasurementManager.instance)
			{
				return MeasurementManager.instance.distanceMode;
			}
			return MeasurementManager.DistanceModes.Meters;
		}
	}

	[ContextMenu("Test")]
	public void PlayRandomHostileOrGroupReport()
	{
		Vector3 vector = Vector3.ProjectOnPlane(Random.onUnitSphere, Vector3.up).normalized * Random.Range(0, 4000);
		float magnitude = vector.magnitude;
		Vector3 vector2 = playerPos + vector;
		float num = VectorUtils.Bearing(playerPos, vector2);
		float num2 = Random.Range(0f, 10000f);
		vector2.y = WaterPhysics.waterHeight + num2;
		Vector3 vector3 = Random.onUnitSphere * Random.Range(120f, 620f);
		float num3 = VectorUtils.Bearing(Vector3.zero, vector3);
		Debug.LogFormat("Test reporting group BRAA {0}, {1}, {2}, {3} ({4}m/s)", num, magnitude, num2, num3, vector3.magnitude);
		if (Random.Range(0, 100) < 50)
		{
			ReportHostile(vector2, vector3, braaOnly: false);
		}
		else
		{
			ReportGroup(vector2, vector3, braaOnly: false);
		}
	}

	public void ReportGroups(List<AIAWACSSpawn.ContactGroup> groups, int offset, int count)
	{
		Debug.Log("ReportGroups");
		audioString.Clear();
		AppendClips(audioString, RandomClip(overlordClips), RandomClip(pictureClips));
		bool flag = (bool)WaypointManager.instance && (bool)WaypointManager.instance.bullseye;
		int num = offset;
		int num2 = 0;
		while (num < groups.Count && num2 < count)
		{
			AIAWACSSpawn.ContactGroup contactGroup = groups[num];
			if (contactGroup.count > 1)
			{
				if (flag)
				{
					AppendClips(audioString, RandomClip(groupBullseyeClips));
				}
				else
				{
					AppendClips(audioString, RandomClip(groupBraaClips));
				}
			}
			else if (flag)
			{
				AppendClips(audioString, RandomClip(hostileBullseyeClips));
			}
			else
			{
				AppendClips(audioString, RandomClip(hostileBraaClips));
			}
			Vector3 point = contactGroup.globalPos.point;
			Vector3 velocity = contactGroup.velocity;
			if (flag)
			{
				AppendBearing(audioString, WaypointManager.instance.bullseye.position, point);
				AppendRange(audioString, WaypointManager.instance.bullseye.position, point);
			}
			else
			{
				AppendBearing(audioString, playerPos, point);
				AppendRange(audioString, playerPos, point);
			}
			AppendAltitude(audioString, point);
			AppendAzimuth(audioString, point, velocity);
			num++;
			num2++;
		}
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportPopups(List<AIAWACSSpawn.ContactGroup> groups, int offset, int count)
	{
		Debug.Log("ReportPopups");
		audioString.Clear();
		AppendCallsigns(audioString);
		AppendClips(audioString, RandomClip(popupClips));
		int num = offset;
		int num2 = 0;
		while (num < groups.Count && num2 < count)
		{
			AIAWACSSpawn.ContactGroup contactGroup = groups[num];
			AppendPopup(audioString, contactGroup.count > 1, contactGroup.globalPos.globalPoint.toVector3, contactGroup.velocity);
			num++;
			num2++;
		}
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportPopups(bool grpA, Vector3 gPosA, Vector3 velA, bool grpB, Vector3 gPosB, Vector3 velB, bool grpC, Vector3 gPosC, Vector3 velC, int count)
	{
		if (count > 0)
		{
			audioString.Clear();
			AppendCallsigns(audioString);
			AppendClips(audioString, RandomClip(popupClips));
			if (count >= 1)
			{
				AppendPopup(audioString, grpA, gPosA, velA);
			}
			if (count >= 2)
			{
				AppendPopup(audioString, grpB, gPosB, velB);
			}
			if (count >= 3)
			{
				AppendPopup(audioString, grpC, gPosC, velC);
			}
			CommRadioManager.instance.PlayMessageString(audioString);
		}
	}

	private void AppendPopup(List<AudioClip> audioString, bool grp, Vector3 gPos, Vector3 vel)
	{
		bool flag = (bool)WaypointManager.instance && (bool)WaypointManager.instance.bullseye;
		if (grp)
		{
			if (flag)
			{
				AppendClips(audioString, RandomClip(groupBullseyeClips));
			}
			else
			{
				AppendClips(audioString, RandomClip(groupBraaClips));
			}
		}
		else if (flag)
		{
			AppendClips(audioString, RandomClip(hostileBullseyeClips));
		}
		else
		{
			AppendClips(audioString, RandomClip(hostileBraaClips));
		}
		Vector3 vector = VTMapManager.GlobalToWorldPoint(new Vector3D(gPos));
		if (flag)
		{
			AppendBearing(audioString, WaypointManager.instance.bullseye.position, vector);
			AppendRange(audioString, WaypointManager.instance.bullseye.position, vector);
		}
		else
		{
			AppendBearing(audioString, playerPos, vector);
			AppendRange(audioString, playerPos, vector);
		}
		AppendAltitude(audioString, vector);
		AppendAzimuth(audioString, vector, vel);
	}

	public void ReportGroup(Vector3 pos, Vector3 velocity, bool braaOnly)
	{
		Debug.Log("ReportGroup");
		audioString.Clear();
		AppendCallsigns(audioString);
		if (!braaOnly && (bool)WaypointManager.instance && (bool)WaypointManager.instance.bullseye)
		{
			AppendClips(audioString, RandomClip(groupBullseyeClips));
			AppendBearing(audioString, WaypointManager.instance.bullseye.position, pos);
			AppendRange(audioString, WaypointManager.instance.bullseye.position, pos);
		}
		else
		{
			AppendClips(audioString, RandomClip(groupBraaClips));
			AppendBearing(audioString, playerPos, pos);
			AppendRange(audioString, playerPos, pos);
		}
		AppendAltitude(audioString, pos);
		AppendAzimuth(audioString, pos, velocity);
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportHostile(Vector3 pos, Vector3 velocity, bool braaOnly)
	{
		audioString.Clear();
		AppendCallsigns(audioString);
		if (!braaOnly && (bool)WaypointManager.instance && (bool)WaypointManager.instance.bullseye)
		{
			AppendClips(audioString, RandomClip(hostileBullseyeClips));
			AppendBearing(audioString, WaypointManager.instance.bullseye.position, pos);
			AppendRange(audioString, WaypointManager.instance.bullseye.position, pos);
		}
		else
		{
			AppendClips(audioString, RandomClip(hostileBraaClips));
			AppendBearing(audioString, playerPos, pos);
			AppendRange(audioString, playerPos, pos);
		}
		AppendAltitude(audioString, pos);
		AppendAzimuth(audioString, pos, velocity);
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportPictureClean()
	{
		audioString.Clear();
		AppendCallsigns(audioString);
		AppendClips(audioString, RandomClip(cleanClips));
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportGrandSlam()
	{
		audioString.Clear();
		AppendCallsigns(audioString);
		AppendClips(audioString, RandomClip(grandSlamClips));
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportHomeplateBra(Vector3 homePos)
	{
		audioString.Clear();
		AppendCallsigns(audioString);
		AppendClips(audioString, RandomClip(homeplateBraaClips));
		AppendBearing(audioString, playerPos, homePos);
		AppendRange(audioString, playerPos, homePos, sayMerged: false);
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportRTB()
	{
		CommRadioManager.instance.PlayMessage(RandomClip(rtbClips), duckBGM: false, queueBehindLiveRadio: false);
	}

	public void ReportGoingDown()
	{
		CommRadioManager.instance.PlayMessage(RandomClip(goingDownClips), duckBGM: false, queueBehindLiveRadio: false);
	}

	public void ReportUnable()
	{
		audioString.Clear();
		AppendCallsigns(audioString);
		AppendClips(audioString, RandomClip(unableClips));
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	public void ReportThreatToAwacs(int count, Vector3 pos, Vector3 velocity)
	{
		audioString.Clear();
		AppendCallsigns(audioString);
		if (count > 1)
		{
			AppendClips(audioString, RandomClip(groupBraaClips));
		}
		else
		{
			AppendClips(audioString, RandomClip(hostileBraaClips));
		}
		AppendBearing(audioString, playerPos, pos);
		AppendRange(audioString, playerPos, pos);
		AppendAltitude(audioString, pos);
		AppendAzimuth(audioString, pos, velocity);
		AppendClips(audioString, RandomClip(leansOnClips));
		CommRadioManager.instance.PlayMessageString(audioString);
	}

	private void AppendBearing(List<AudioClip> audioString, Vector3 fromPt, Vector3 toPt)
	{
		int num = Mathf.RoundToInt(VectorUtils.Bearing(fromPt, toPt));
		int num2 = num / 100 % 10;
		int num3 = num / 10 % 10;
		int num4 = num % 10;
		AppendNumbers(audioString, num2, num3, num4);
	}

	private float ConvertedDistance(float f_range)
	{
		if ((bool)MeasurementManager.instance)
		{
			return MeasurementManager.instance.ConvertedDistance(f_range);
		}
		return f_range;
	}

	private void AppendRange(List<AudioClip> audioString, Vector3 fromPt, Vector3 toPt, bool sayMerged = true)
	{
		float magnitude = Vector3.ProjectOnPlane(fromPt - toPt, Vector3.up).magnitude;
		if (sayMerged && magnitude < 2000f)
		{
			AppendClips(audioString, RandomClip(mergedClips));
			return;
		}
		magnitude = ConvertedDistance(magnitude);
		if (distanceMode == MeasurementManager.DistanceModes.Meters)
		{
			magnitude /= 1000f;
		}
		float value = magnitude;
		value = Mathf.Clamp(value, 0f, 60f);
		float f = ((value <= 10f) ? value : ((value <= 20f) ? (10f + (value - 10f) / 2f) : ((!(value <= 40f)) ? (19f + (value - 40f) / 10f) : (15f + (value - 20f) / 5f))));
		int num = Mathf.Clamp(Mathf.RoundToInt(f), 0, rangeClips.Length - 1);
		AppendClips(audioString, rangeClips[num]);
	}

	private void AppendAltitude(List<AudioClip> audioString, Vector3 pt)
	{
		float altitude = WaterPhysics.GetAltitude(pt);
		if (altitude < 1500f)
		{
			AppendClips(audioString, RandomClip(lowClips));
			return;
		}
		altitude = ConvertedAltitude(altitude);
		altitude /= 1000f;
		int b = Mathf.RoundToInt(altitude);
		b = Mathf.Max(1, b);
		AudioClip audioClip;
		if (b < 9)
		{
			audioClip = compoundDigits[b * 2];
		}
		else if (b <= 20)
		{
			audioClip = highAltitudeClips[(b - 9) / 2];
		}
		else
		{
			int value = 5 + (b - 15) / 5;
			value = Mathf.Clamp(value, 0, highAltitudeClips.Length - 1);
			audioClip = highAltitudeClips[value];
		}
		AppendClips(audioString, audioClip, RandomClip(thousandClips));
	}

	private void AppendAzimuth(List<AudioClip> audioString, Vector3 pos, Vector3 vel)
	{
		Vector3 rhs = pos - playerPos;
		rhs.Normalize();
		float num = Vector3.Dot(vel.normalized, rhs);
		if (num < -0.8f)
		{
			AppendClips(audioString, RandomClip(hotClips));
		}
		else if (num > 0.5f)
		{
			AppendClips(audioString, RandomClip(coldClips));
		}
		else
		{
			float num2 = VectorUtils.Bearing(Vector3.zero, vel);
			if (num2 > 45f && num2 <= 135f)
			{
				AppendClips(audioString, cardinalClips[1]);
			}
			else if (num2 > 135f && num2 <= 225f)
			{
				AppendClips(audioString, cardinalClips[2]);
			}
			else if (num2 > 225f && num2 <= 315f)
			{
				AppendClips(audioString, cardinalClips[3]);
			}
			else
			{
				AppendClips(audioString, cardinalClips[0]);
			}
		}
		if (vel.magnitude > 340f)
		{
			AppendClips(audioString, RandomClip(fastClips));
		}
	}

	private void AppendCallsigns(List<AudioClip> audioString)
	{
		if ((bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor)
		{
			AppendActorDesignation(audioString, FlightSceneManager.instance.playerActor.designation);
		}
		else
		{
			PhoneticLetters letter = (PhoneticLetters)Random.Range(0, 25);
			int num = 1;
			int num2 = Random.Range(1, 10);
			AppendActorDesignation(audioString, new Actor.Designation(letter, num, num2));
		}
		AppendClips(audioString, RandomClip(overlordClips));
	}

	private void AppendActorDesignation(List<AudioClip> audioString, Actor.Designation designation)
	{
		audioString.Add(phoneticAlphabet[(int)designation.letter]);
		AppendNumbers(audioString, designation.num1, designation.num2);
	}

	private void AppendNumbers(List<AudioClip> audioString, params int[] nums)
	{
		for (int i = 0; i < nums.Length; i++)
		{
			int num = ((i == nums.Length - 1) ? (nums[i] * 2 + 1) : (nums[i] * 2));
			audioString.Add(compoundDigits[num]);
		}
	}

	private void AppendClips(List<AudioClip> audioString, params AudioClip[] clips)
	{
		for (int i = 0; i < clips.Length; i++)
		{
			audioString.Add(clips[i]);
		}
	}

	private AudioClip RandomClip(AudioClip[] clips)
	{
		if (clips != null && clips.Length != 0)
		{
			return clips[Random.Range(0, clips.Length)];
		}
		return null;
	}

	private float ConvertedAltitude(float f_alt)
	{
		if ((bool)MeasurementManager.instance)
		{
			return MeasurementManager.instance.ConvertedAltitude(f_alt);
		}
		return f_alt;
	}

	public void PlayRandomMessage()
	{
		int num = Random.Range(-4, 5);
		if (num <= 0)
		{
			PlayRandomHostileOrGroupReport();
			return;
		}
		switch (num)
		{
		case 1:
			ReportGoingDown();
			break;
		case 2:
			ReportPictureClean();
			break;
		case 3:
			ReportRTB();
			break;
		case 4:
			ReportUnable();
			break;
		case 5:
			ReportGrandSlam();
			break;
		}
	}
}
