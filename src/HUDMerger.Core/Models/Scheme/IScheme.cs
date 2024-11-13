using System;
using System.Collections.Generic;
using VDF.Models;

namespace HUDMerger.Core.Models.Scheme;

public interface IScheme
{
	public IEnumerable<KeyValuePair<KeyValue, string>> GetColour(string colourName);
	public void SetColour(IEnumerable<KeyValuePair<KeyValue, string>> colourValue);

	public IEnumerable<KeyValuePair<KeyValue, dynamic>> GetBorder(string borderName);
	public void SetBorder(IEnumerable<KeyValuePair<KeyValue, dynamic>> borderValue);

	public IEnumerable<KeyValuePair<KeyValue, HashSet<KeyValue>>> GetFont(string fontName);
	public void SetFont(IEnumerable<KeyValuePair<KeyValue, HashSet<KeyValue>>> fontValue);
}
