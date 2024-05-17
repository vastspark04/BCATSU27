public struct GameVersion : IConfigValue
{
	public enum ReleaseTypes
	{
		Public,
		Testing,
		Modded
	}

	public int majorVersion;

	public int betaVersion;

	public int alphaVersion;

	public int buildVersion;

	public ReleaseTypes releaseType;

	private string releaseSymbol => releaseType switch
	{
		ReleaseTypes.Public => "f", 
		ReleaseTypes.Testing => "p", 
		ReleaseTypes.Modded => "m", 
		_ => "?", 
	};

	public GameVersion(int major, int beta, int alpha, int build, ReleaseTypes type)
	{
		majorVersion = major;
		betaVersion = beta;
		alphaVersion = alpha;
		buildVersion = build;
		releaseType = type;
	}

	public override string ToString()
	{
		return $"{majorVersion}.{betaVersion}.{alphaVersion}{releaseSymbol}{buildVersion}";
	}

	public string WriteValue()
	{
		return ToString();
	}

	public static GameVersion Parse(string s)
	{
		GameVersion result = default(GameVersion);
		result.ConstructFromValue(s);
		return result;
	}

	public void ConstructFromValue(string s)
	{
		string[] array = s.Split('.');
		majorVersion = ConfigNodeUtils.ParseInt(array[0]);
		betaVersion = ConfigNodeUtils.ParseInt(array[1]);
		char c = (array[2].Contains("f") ? 'f' : (array[2].Contains("p") ? 'p' : 'm'));
		string[] array2 = array[2].Split(c);
		alphaVersion = ConfigNodeUtils.ParseInt(array2[0]);
		buildVersion = ConfigNodeUtils.ParseInt(array2[1]);
		releaseType = ((c != 'f') ? ReleaseTypes.Testing : ReleaseTypes.Public);
	}

	public static bool operator >(GameVersion a, GameVersion b)
	{
		if (a.majorVersion == b.majorVersion)
		{
			if (a.betaVersion == b.betaVersion)
			{
				if (a.alphaVersion == b.alphaVersion)
				{
					if (a.releaseType == b.releaseType)
					{
						return a.buildVersion > b.buildVersion;
					}
					return a.releaseType == ReleaseTypes.Public;
				}
				return a.alphaVersion > b.alphaVersion;
			}
			return a.betaVersion > b.betaVersion;
		}
		return a.majorVersion > b.majorVersion;
	}

	public static bool operator <(GameVersion a, GameVersion b)
	{
		if (a.majorVersion == b.majorVersion)
		{
			if (a.betaVersion == b.betaVersion)
			{
				if (a.alphaVersion == b.alphaVersion)
				{
					if (a.releaseType == b.releaseType)
					{
						return a.buildVersion < b.buildVersion;
					}
					return a.releaseType == ReleaseTypes.Testing;
				}
				return a.alphaVersion < b.alphaVersion;
			}
			return a.betaVersion < b.betaVersion;
		}
		return a.majorVersion < b.majorVersion;
	}

	public static bool operator ==(GameVersion a, GameVersion b)
	{
		if (a.majorVersion == b.majorVersion && a.betaVersion == b.betaVersion && a.alphaVersion == b.alphaVersion && a.releaseType == b.releaseType)
		{
			return a.buildVersion == b.buildVersion;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (majorVersion * 10000000 + betaVersion * 100000 + alphaVersion * 1000 + buildVersion * 10 + ((releaseType == ReleaseTypes.Public) ? 1 : 0)).GetHashCode();
	}

	public static bool operator !=(GameVersion a, GameVersion b)
	{
		return !(a == b);
	}

	public override bool Equals(object obj)
	{
		if (obj is GameVersion)
		{
			return this == (GameVersion)obj;
		}
		return false;
	}
}
