public static class VTLStaticStrings
{
	private const string s_endMission_minsAgo_en = "mins ago";

	public static string s_endMission_minsAgo;

	private const string mission_completed_en = "completed.";

	public static string mission_completed;

	private const string mission_failed_en = "failed.";

	public static string mission_failed;

	private const string mission_hasReached_en = "{0} has reached {1}.";

	public static string mission_hasReached;

	private const string mission_vehicleDestroyed_en = "Vehicle was destroyed.";

	public static string mission_vehicleDestroyed;

	private const string mission_ejected_en = "Pilot has ejected.";

	public static string mission_ejected;

	private const string mission_killedGForces_en = "Killed by G-Forces";

	public static string mission_killedGForces;

	private const string mission_minimum_en = "Min:";

	public static string mission_minimum;

	private const string mission_targetDestroyed_en = "target destroyed";

	public static string mission_targetDestroyed;

	private const string mission_failedToDefend_en = "Failed to defend {0}";

	public static string mission_failedToDefend;

	private const string credits_credits_en = "Credits";

	public static string credits_credits;

	private const string credits_developer_en = "Developer";

	public static string credits_developer;

	private const string credits_programming_en = "Programming";

	public static string credits_programming;

	private const string credits_modelling_en = "Modelling";

	public static string credits_modelling;

	private const string credits_music_en = "Music";

	public static string credits_music;

	private const string credits_voices_en = "Voices";

	public static string credits_voices;

	private const string credits_wingmen_en = "Wingmen";

	public static string credits_wingmen;

	private const string credits_groundCrew_en = "Ground Crew";

	public static string credits_groundCrew;

	private const string credits_awacs_en = "AWACS";

	public static string credits_awacs;

	private const string credits_atc_en = "ATC";

	public static string credits_atc;

	private const string credits_pvtTesting_en = "Private Testing Volunteers";

	public static string credits_pvtTesting;

	private const string credits_translation_en = "Translation";

	public static string credits_translation;

	private const string credits_thanks_en = "Special Thanks\nto everyone participating in\nthe public_testing branch!";

	public static string credits_thanks;

	private const string mfd_home_en = "Home";

	public static string mfd_home;

	private const string briefingUI_configure_en = "Configure";

	public static string briefingUI_configure;

	private const string briefingUI_fly_en = "Fly";

	public static string briefingUI_fly;

	private const string ps_timeLabel_en = "Total Flight Time";

	public static string ps_timeLabel;

	private const string ps_lvLabel_en = "Last Vehicle";

	public static string ps_lvLabel;

	private const string setting_enabled_en = "ON";

	public static string setting_enabled;

	private const string setting_disabled_en = "OFF";

	public static string setting_disabled;

	private const string s_scenarios_en = "Scenarios";

	public static string s_scenarios;

	private const string s_campaigns_en = "Campaigns";

	public static string s_campaigns;

	private const string s_maps_en = "Maps";

	public static string s_maps;

	private const string err_logSteam_en = "Must be logged into Steam!";

	public static string err_logSteam;

	private const string err_wsConnect_en = "Failed to connect to Steam Workshop!";

	public static string err_wsConnect;

	private const string err_scenarioNotFound_en = "Scenario not found!";

	public static string err_scenarioNotFound;

	private const string err_version_en = "Incompatible game version!";

	public static string err_version;

	private const string vehicleConfig_denyOverBudget_en = "OVER BUDGET";

	public static string vehicleConfig_denyOverBudget;

	private const string vehicleConfig_repairOverBudget_en = "Insufficient funds to repair.";

	public static string vehicleConfig_repairOverBudget;

	private const string env_day_en = "Day";

	public static string env_day;

	private const string env_night_en = "Night";

	public static string env_night;

	private const string env_morning_en = "Morning";

	public static string env_morning;

	public static void ApplyLocalization()
	{
		ps_timeLabel = VTLocalizationManager.GetString("pilotSelect_totalFlightTime", "Total Flight Time", "Title for label showing the player's total flight time.");
		ps_lvLabel = VTLocalizationManager.GetString("pilotSelect_lastVehicle", "Last Vehicle", "Title for label showing the vehicle the player has most recently flown.");
		setting_enabled = VTLocalizationManager.GetString("setting_enabled", "ON", "Label for an enabled option in settings UI");
		setting_disabled = VTLocalizationManager.GetString("setting_disabled", "OFF", "Label for a disabled option in settings UI");
		s_scenarios = VTLocalizationManager.GetString("s_scenarios", "Scenarios");
		s_campaigns = VTLocalizationManager.GetString("s_campaigns", "Campaigns");
		s_maps = VTLocalizationManager.GetString("s_maps", "Maps");
		err_logSteam = VTLocalizationManager.GetString("err_LogSteam", "Must be logged into Steam!", "Error message for workshop browser");
		err_wsConnect = VTLocalizationManager.GetString("err_wsConnect", "Failed to connect to Steam Workshop!", "Error message for workshop browser");
		err_scenarioNotFound = VTLocalizationManager.GetString("err_scenarioNotFound", "Scenario not found!", "Error message for workshop browser");
		err_version = VTLocalizationManager.GetString("err_version", "Incompatible game version!", "Error message for workshop browser");
		vehicleConfig_denyOverBudget = VTLocalizationManager.GetString("vehicleConfig_denyOverBudget", "OVER BUDGET", "Denial message for vehicle configurator");
		vehicleConfig_repairOverBudget = VTLocalizationManager.GetString("vehicleConfig_repairOverBudget", "Insufficient funds to repair.", "Denial message for vehicle configurator");
		briefingUI_configure = VTLocalizationManager.GetString("briefingUI_configure", "Configure", "Start button in mission briefing");
		briefingUI_fly = VTLocalizationManager.GetString("briefingUI_fly", "Fly", "Start button in mission briefing");
		mfd_home = VTLocalizationManager.GetString("mfd_home", "Home", "MFD home page button");
		env_morning = VTLocalizationManager.GetString("env_morning", "Morning", "Environment setting");
		env_day = VTLocalizationManager.GetString("env_day", "Day", "Environment setting");
		env_night = VTLocalizationManager.GetString("env_night", "Night", "Environment setting");
		credits_credits = VTLocalizationManager.GetString("credits_credits", "Credits", "Credits string");
		credits_developer = VTLocalizationManager.GetString("credits_developer", "Developer", "Credits string");
		credits_programming = VTLocalizationManager.GetString("credits_programming", "Programming", "Credits string");
		credits_modelling = VTLocalizationManager.GetString("credits_modelling", "Modelling", "Credits string");
		credits_music = VTLocalizationManager.GetString("credits_music", "Music", "Credits string");
		credits_voices = VTLocalizationManager.GetString("credits_voices", "Voices", "Credits string");
		credits_wingmen = VTLocalizationManager.GetString("credits_wingmen", "Wingmen", "Credits string");
		credits_groundCrew = VTLocalizationManager.GetString("credits_groundCrew", "Ground Crew", "Credits string");
		credits_awacs = VTLocalizationManager.GetString("credits_awacs", "AWACS", "Credits string");
		credits_atc = VTLocalizationManager.GetString("credits_atc", credits_atc, "Credits string");
		credits_pvtTesting = VTLocalizationManager.GetString("credits_pvtTesting", "Private Testing Volunteers", "Credits string");
		credits_translation = VTLocalizationManager.GetString("credits_translation", "Translation", "Credits string");
		credits_thanks = VTLocalizationManager.GetString("credits_thanks", "Special Thanks\nto everyone participating in\nthe public_testing branch!", "Credits string");
		mission_completed = VTLocalizationManager.GetString("mission_completed", "completed.", "'x completed.' end-mission entry.");
		mission_failed = VTLocalizationManager.GetString("mission_failed", "failed.", "'x failed.' end-mission entry.");
		mission_hasReached = VTLocalizationManager.GetString("mission_hasReached", "{0} has reached {1}.", "'{unit} has reached {waypoint}' end-mission entry for protect objective.  Must contain {0} and {1} variable indices.");
		mission_vehicleDestroyed = VTLocalizationManager.GetString("mission_vehicleDestroyed", "Vehicle was destroyed.", "End-mission entry for when vehicle was destroyed.");
		mission_ejected = VTLocalizationManager.GetString("mission_ejected", "Pilot has ejected.", "End-mission entry for when pilot ejects.");
		mission_killedGForces = VTLocalizationManager.GetString("mission_killedGForces", "Killed by G-Forces", "End-mission entry for when killed by g-forces");
		mission_minimum = VTLocalizationManager.GetString("mission_minimum", "Min:", "Short for 'minimum' in objective description with multiple targets.");
		mission_targetDestroyed = VTLocalizationManager.GetString("mission_targetDestroyed", "target destroyed", "Reason for failing a Join mission (target waypoint unit was killed)");
		mission_failedToDefend = VTLocalizationManager.GetString("mission_failedToDefend", "Failed to defend {0}", "Reason for failing a Protect objective. Must contain variable index {0}");
		s_endMission_minsAgo = VTLocalizationManager.GetString("s_endMission_minsAgo", "mins ago", "Label for quickload button 'minutes ago' in end-mission display 'Quick Load (x mins ago)'");
	}
}
