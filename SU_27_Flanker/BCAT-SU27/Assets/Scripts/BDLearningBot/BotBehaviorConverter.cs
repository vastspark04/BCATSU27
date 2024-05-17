using System;

public class BotBehaviorConverter
{
	private int max;

	private int[] behaviorFactors;

	public int BehaviorCount => max;

	public BotBehaviorConverter(int[] behaviorFactors)
	{
		this.behaviorFactors = behaviorFactors;
		max = 1;
		for (int i = 0; i < behaviorFactors.Length; i++)
		{
			max *= behaviorFactors[i];
		}
	}

	public void Convert(int behaviorCode, int[] output)
	{
		if (output.Length != behaviorFactors.Length)
		{
			throw new ArgumentException("Output array length does not match the original behavior factors!");
		}
		if (behaviorCode < 0 || behaviorCode >= max)
		{
			throw new ArgumentException($"behaviorCode {behaviorCode} is out of bounds! range:0->{max - 1}");
		}
		for (int i = 0; i < output.Length; i++)
		{
			output[i] = behaviorCode % behaviorFactors[i];
			behaviorCode /= behaviorFactors[i];
		}
	}

	public int Convert(int[] input)
	{
		if (input.Length != behaviorFactors.Length)
		{
			throw new ArgumentException("Output array length does not match the original behavior factors!");
		}
		int num = 0;
		num += input[0];
		for (int i = 1; i < input.Length; i++)
		{
			if (input[i] < 0 || input[i] >= behaviorFactors[i])
			{
				throw new ArgumentException($"Input sub-behavior idx:{i} is out of bounds! input:{input[i]} subCount:{behaviorFactors[i]}");
			}
			int num2 = 1;
			for (int num3 = i - 1; num3 >= 0; num3--)
			{
				num2 *= behaviorFactors[num3];
			}
			num += num2 * input[i];
		}
		return num;
	}
}
