using System;
using System.Collections.Generic;

namespace VTOLVR.SteamWorkshop{

public class WorkshopItemUpdate : IProgress<float>
{
	public string Name;

	public string Description;

	public string ContentPath;

	public string ChangeNote;

	public string IconPath;

	public List<string> Tags;

	public ulong PublishedFileId;

	private float progress;

	public float GetUploadProgress()
	{
		return progress;
	}

	public void Report(float value)
	{
		progress = value;
	}
}

}