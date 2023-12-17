using System;
using System.Linq;
using System.Collections.Generic;

namespace VDF.Models;

public class KeyValues : List<KeyValue>
{
	public KeyValues() { }
	public KeyValues(IEnumerable<KeyValue> collection) : base(collection) { }
}
