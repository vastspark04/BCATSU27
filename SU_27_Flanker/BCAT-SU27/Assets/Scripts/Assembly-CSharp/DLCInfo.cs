using UnityEngine;

public class DLCInfo : MonoBehaviour
{
	public string dlcName;

	public string versionString;

	public GameVersion dlcVersion => GameVersion.Parse(versionString);
}
