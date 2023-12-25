using System;
using System.Collections.Generic;
using VDF.Models;

namespace HUDAnimations.Models;

public class HUDAnimationsFile : List<KeyValue>
{
	public HUDAnimationsFile() { }
	public HUDAnimationsFile(IEnumerable<KeyValue> collection) : base(collection) { }
}
