namespace LSLib.LS.Pak;

internal interface ILSPKHeader
{
	public PackageHeaderCommon ToCommonHeader();
	abstract public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h);
}
