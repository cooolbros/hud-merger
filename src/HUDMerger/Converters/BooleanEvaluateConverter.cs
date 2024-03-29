﻿using System;
using System.Globalization;

namespace HUDMerger.Converters;

/// <summary>
/// Evaluate a boolean and return the ConverterParameter or TargetNullValue
/// </summary>
public class BooleanEvaluateConverter : ConverterBase
{
	public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
#pragma warning disable CS8603
		return (bool)value ? parameter : null;
#pragma warning restore CS8603
	}

	public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
