namespace LSLib.LS.Pak;

internal interface ILSPKFile
{
	public void ToCommon(PackagedFileInfoCommon info);
	abstract public static ILSPKFile FromCommon(PackagedFileInfoCommon info);
	public UInt16 ArchivePartNumber();
}
