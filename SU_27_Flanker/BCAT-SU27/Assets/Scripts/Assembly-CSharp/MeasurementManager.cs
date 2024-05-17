using System;
using UnityEngine;

public class MeasurementManager : MonoBehaviour, IPersistentDataSaver, IPersistentVehicleData
{
	public enum AltitudeModes
	{
		Meters,
		Feet
	}

	public enum DistanceModes
	{
		Meters,
		NautMiles,
		Feet,
		Miles
	}

	public enum SpeedModes
	{
		MetersPerSecond,
		KilometersPerHour,
		Knots,
		MilesPerHour,
		FeetPerSecond,
		Mach
	}

	private AltitudeModes _altitudeMode;

	private DistanceModes _distanceMode;

	private SpeedModes _airspeedMode;

	private FlightInfo flightInfo;

	private static AnimationCurve machAtAltCurve;

	public AltitudeModes altitudeMode
	{
		get
		{
			return _altitudeMode;
		}
		set
		{
			if (_altitudeMode != value && this.OnChangedAltitudeMode != null)
			{
				_altitudeMode = value;
				this.OnChangedAltitudeMode();
			}
			else
			{
				_altitudeMode = value;
			}
		}
	}

	public DistanceModes distanceMode
	{
		get
		{
			return _distanceMode;
		}
		set
		{
			if (_distanceMode != value && this.OnChangedDistanceMode != null)
			{
				_distanceMode = value;
				this.OnChangedDistanceMode();
			}
			else
			{
				_distanceMode = value;
			}
		}
	}

	public SpeedModes airspeedMode
	{
		get
		{
			return _airspeedMode;
		}
		set
		{
			if (_airspeedMode != value && this.OnChangedSpeedMode != null)
			{
				_airspeedMode = value;
				this.OnChangedSpeedMode();
			}
			else
			{
				_airspeedMode = value;
			}
		}
	}

	public static MeasurementManager instance { get; private set; }

	public event Action OnChangedDistanceMode;

	public event Action OnChangedSpeedMode;

	public event Action OnChangedAltitudeMode;

	public void SavePersistentData()
	{
		if (PilotSaveManager.current != null && PilotSaveManager.currentVehicle != null)
		{
			VehicleSave vehicleSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName);
			vehicleSave.altitudeMode = altitudeMode;
			vehicleSave.distanceMode = distanceMode;
			vehicleSave.airspeedMode = airspeedMode;
		}
	}

	private void Awake()
	{
		if (PilotSaveManager.current != null && PilotSaveManager.currentVehicle != null)
		{
			VehicleSave vehicleSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName);
			altitudeMode = vehicleSave.altitudeMode;
			distanceMode = vehicleSave.distanceMode;
			airspeedMode = vehicleSave.airspeedMode;
		}
		flightInfo = GetComponentInParent<FlightInfo>();
		instance = this;
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		SavePersistentData();
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (PilotSaveManager.current != null && PilotSaveManager.currentVehicle != null)
		{
			VehicleSave vehicleSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName);
			vehicleSave.altitudeMode = altitudeMode;
			vehicleSave.distanceMode = distanceMode;
			vehicleSave.airspeedMode = airspeedMode;
		}
	}

	public string FormattedAltitude(float altitude)
	{
		return $"{ConvertedAltitude(altitude):n0}";
	}

	public float ConvertedAltitude(float altitude)
	{
		return altitudeMode switch
		{
			AltitudeModes.Meters => altitude, 
			AltitudeModes.Feet => DistToFeet(altitude), 
			_ => 0f, 
		};
	}

	public static float ConvertDistance(float distMeters, DistanceModes targetUnit)
	{
		return targetUnit switch
		{
			DistanceModes.Feet => DistToFeet(distMeters), 
			DistanceModes.Miles => DistToMiles(distMeters), 
			DistanceModes.NautMiles => DistToNauticalMile(distMeters), 
			_ => distMeters, 
		};
	}

	public string AltitudeLabel()
	{
		return altitudeMode switch
		{
			AltitudeModes.Meters => "m", 
			AltitudeModes.Feet => "ft", 
			_ => "?", 
		};
	}

	public float TrueSpeedToIndicatedSpeed(float trueSpeed)
	{
		return trueSpeed - DistToFeet(flightInfo.altitudeASL) / 1000f * (trueSpeed * 0.02f);
	}

	public string FormattedDistance(float distance)
	{
		switch (distanceMode)
		{
		case DistanceModes.Meters:
			if (distance > 5000f)
			{
				return string.Format("{0:n}{1}", distance / 1000f, "km");
			}
			return string.Format("{0:n0}{1}", distance, "m");
		case DistanceModes.NautMiles:
		{
			float num = DistToNauticalMile(distance);
			return string.Format("{0:n}{1}", num, "nm");
		}
		case DistanceModes.Feet:
			return string.Format("{0:n0}{1}", DistToFeet(distance), "ft");
		case DistanceModes.Miles:
			if (DistToMiles(distance) > 1f)
			{
				return string.Format("{0:n}{1}", DistToMiles(distance), "mi");
			}
			return string.Format("{0:n0}{1}", DistToFeet(distance), "ft");
		default:
			return "Unhandled unit";
		}
	}

	public string DistanceLabel()
	{
		return distanceMode switch
		{
			DistanceModes.Meters => "km", 
			DistanceModes.NautMiles => "nm", 
			DistanceModes.Feet => "ft", 
			DistanceModes.Miles => "mi", 
			_ => "Unhandled unit", 
		};
	}

	public float ConvertedDistance(float distance)
	{
		return distanceMode switch
		{
			DistanceModes.Meters => distance, 
			DistanceModes.Feet => DistToFeet(distance), 
			DistanceModes.Miles => DistToMiles(distance), 
			DistanceModes.NautMiles => DistToNauticalMile(distance), 
			_ => distance, 
		};
	}

	public string FormattedSpeed(float speed)
	{
		return airspeedMode switch
		{
			SpeedModes.MetersPerSecond => $"{speed:n0}", 
			SpeedModes.KilometersPerHour => $"{SpeedToKMH(speed):n0}", 
			SpeedModes.Knots => $"{SpeedToKnot(speed):n0}", 
			SpeedModes.MilesPerHour => $"{SpeedToMPH(speed):n0}", 
			SpeedModes.FeetPerSecond => $"{SpeedToFPS(speed):n0}", 
			SpeedModes.Mach => $"{SpeedToMach(speed, flightInfo.altitudeASL):n}", 
			_ => "Unhandled unit", 
		};
	}

	public float ConvertedSpeed(float speed)
	{
		return airspeedMode switch
		{
			SpeedModes.MetersPerSecond => speed, 
			SpeedModes.KilometersPerHour => SpeedToKMH(speed), 
			SpeedModes.Knots => SpeedToKnot(speed), 
			SpeedModes.MilesPerHour => SpeedToMPH(speed), 
			SpeedModes.FeetPerSecond => SpeedToFPS(speed), 
			SpeedModes.Mach => SpeedToMach(speed, flightInfo.altitudeASL), 
			_ => -1f, 
		};
	}

	public float ConvertedVerticalSpeed(float speed)
	{
		return altitudeMode switch
		{
			AltitudeModes.Feet => DistToFeet(speed) * 60f, 
			AltitudeModes.Meters => speed, 
			_ => -1f, 
		};
	}

	public string SpeedLabel()
	{
		return airspeedMode switch
		{
			SpeedModes.MetersPerSecond => "m/s", 
			SpeedModes.KilometersPerHour => "KPH", 
			SpeedModes.Knots => "kt", 
			SpeedModes.MilesPerHour => "MPH", 
			SpeedModes.FeetPerSecond => "ft/s", 
			SpeedModes.Mach => "M", 
			_ => "Unhandled unit", 
		};
	}

	private static AnimationCurve CreateMachAtAltitudeCurve()
	{
		AnimationCurve animationCurve = new AnimationCurve();
		animationCurve.AddKey(0f, 340.3f);
		animationCurve.AddKey(4572f, 322.2f);
		animationCurve.AddKey(10668f, 295.4f);
		return animationCurve;
	}

	public static float DistToFeet(float meters)
	{
		return meters * 3.28084f;
	}

	public static float DistToMiles(float meters)
	{
		return meters * 0.000621371f;
	}

	public static float DistToNauticalMile(float meters)
	{
		return meters * 0.000539957f;
	}

	public static float SpeedToMPH(float ms)
	{
		return ms * 2.23694f;
	}

	public static float SpeedToKMH(float ms)
	{
		return ms * 3.6f;
	}

	public static float SpeedToKnot(float ms)
	{
		return ms * 1.94384f;
	}

	public static float SpeedToFPS(float ms)
	{
		return ms * 3.28084f;
	}

	public static float SpeedToMach(float ms, float altitude)
	{
		if (machAtAltCurve == null)
		{
			machAtAltCurve = CreateMachAtAltitudeCurve();
		}
		float num = machAtAltCurve.Evaluate(altitude);
		return ms / num;
	}
}
