using System;

[Serializable]
public class AttitudePIDSet
{
	public PID pitchPID;

	public PID yawPID;

	public PID rollPID;

	public void SetUpdateMode(UpdateModes mode)
	{
		pitchPID.updateMode = (yawPID.updateMode = (rollPID.updateMode = mode));
	}
}
