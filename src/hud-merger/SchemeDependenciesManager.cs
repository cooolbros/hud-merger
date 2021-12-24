using System;

namespace HUDMerger
{
	public class SchemeDependenciesManager
	{
		/// <summary>
		/// Client Scheme
		/// </summary>
		public ClientschemeDependencies ClientScheme { get; set; } = new();

		/// <summary>
		/// Source Scheme
		/// </summary>
		public ClientschemeDependencies SourceScheme { get; set; } = new();
	}
}
