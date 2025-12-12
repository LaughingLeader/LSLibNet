namespace LSLib.LS.Pak;

public class PackageHeaderCommon
{
	public const PackageVersion CurrentVersion = PackageVersion.V18;
	public const UInt32 Signature = 0x4B50534C;

	public UInt32 Version;
	public UInt64 FileListOffset;
	// Size of file list; used for legacy (<= v10) packages only
	public UInt32 FileListSize;
	// Number of packed files; used for legacy (<= v10) packages only
	public UInt32 NumFiles;
	public UInt32 NumParts;
	// Offset of packed data in archive part 0; used for legacy (<= v10) packages only
	public UInt32 DataOffset;
	public PackageFlags Flags;
	public Byte Priority;
	public byte[] Md5;
}
