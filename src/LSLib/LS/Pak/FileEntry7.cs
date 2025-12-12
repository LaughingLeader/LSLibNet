namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry7 : ILSPKFile
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	public byte[] Name;

	public UInt32 OffsetInFile;
	public UInt32 SizeOnDisk;
	public UInt32 UncompressedSize;
	public UInt32 ArchivePart;

	public readonly void ToCommon(PackagedFileInfoCommon info)
	{
		info.Name = BinUtils.NullTerminatedBytesToString(Name);
		info.ArchivePart = ArchivePart;
		info.Crc = 0;
		info.Flags = UncompressedSize > 0 ? CompressionHelpers.MakeCompressionFlags(CompressionMethod.Zlib, LSCompressionLevel.Default) : 0;
		info.OffsetInFile = OffsetInFile;
		info.SizeOnDisk = SizeOnDisk;
		info.UncompressedSize = UncompressedSize;
	}

	public static ILSPKFile FromCommon(PackagedFileInfoCommon info)
	{
		return new FileEntry7
		{
			Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
			OffsetInFile = (uint)info.OffsetInFile,
			SizeOnDisk = (uint)info.SizeOnDisk,
			UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize,
			ArchivePart = info.ArchivePart
		};
	}

	public readonly UInt16 ArchivePartNumber() => (UInt16)ArchivePart;
}
