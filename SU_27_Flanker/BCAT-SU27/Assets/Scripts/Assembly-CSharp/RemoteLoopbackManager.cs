using UnityEngine;
using System;

public class RemoteLoopbackManager : MonoBehaviour
{
	[Serializable]
	public class SimulatedLatencySettings
	{
		public float FakeLatencyMax;
		public float FakeLatencyMin;
		public float LatencyWeight;
		public int MaxSamples;
	}

	public OvrAvatar LocalAvatar;
	public OvrAvatar LoopbackAvatar;
	public SimulatedLatencySettings LatencySettings;
}
