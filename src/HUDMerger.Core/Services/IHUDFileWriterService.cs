using System;
using System.Text;
using HUDAnimations.Models;
using HUDMerger.Core.Models;
using VDF.Models;

namespace HUDMerger.Core.Services;

public interface IHUDFileWriterService
{
	public void Write(string relativePath, KeyValues keyValues, Encoding? encoding = default);
	public void Write(string relativePath, HUDAnimationsFile keyValues, Encoding? encoding = default);
	public void Copy(HUD source, string relativePath);
}
