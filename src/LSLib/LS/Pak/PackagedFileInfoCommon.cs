namespace LSLib.LS.Pak;

abstract public class PackagedFileInfoCommon
{
	public string Name;
	public UInt32 ArchivePart;
	public UInt32 Crc;
	public CompressionFlags Flags;
	public UInt64 OffsetInFile;
	public UInt64 SizeOnDisk;
	public UInt64 UncompressedSize;
}
