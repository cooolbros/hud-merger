using System;
using System.Globalization;

namespace HUDAnimations.Models;

public abstract class InterpolatorBase
{
	public abstract override string ToString();

	protected static string Print(float num)
	{
		return num.ToString(CultureInfo.InvariantCulture);
	}
}
