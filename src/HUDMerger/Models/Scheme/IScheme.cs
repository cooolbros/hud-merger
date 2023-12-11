using System;
using System.Collections.Generic;
using VDF.Models;

namespace HUDMerger.Models.Scheme;

public interface IScheme
{
	public IEnumerable<KeyValue> GetColour(string colourName);
	public IEnumerable<KeyValue> GetBorder(string borderName);
	public IEnumerable<KeyValue> GetFont(string fontName);
}
