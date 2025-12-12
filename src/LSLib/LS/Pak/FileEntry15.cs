namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry15 : ILSPKFile
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	public byte[] Name;

	public UInt64 OffsetInFile;
	public UInt64 SizeOnDisk;
	public UInt64 UncompressedSize;
	public UInt32 ArchivePart;
	public UInt32 Flags;
	public UInt32 Crc;
	public UInt32 Unknown2;

	public readonly void ToCommon(PackagedFileInfoCommon info)
	{
		info.Name = BinUtils.NullTerminatedBytesToString(Name);
		info.ArchivePart = ArchivePart;
		info.Crc = Crc;
		info.Flags = (CompressionFlags)Flags;
		info.OffsetInFile = OffsetInFile;
		info.SizeOnDisk = SizeOnDisk;
		info.UncompressedSize = UncompressedSize;
	}

	public static ILSPKFile FromCommon(PackagedFileInfoCommon info)
	{
		return new FileEntry15
		{
			Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
			OffsetInFile = (uint)info.OffsetInFile,
			SizeOnDisk = (uint)info.SizeOnDisk,
			UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize,
			ArchivePart = info.ArchivePart,
			Flags = (Byte)info.Flags,
			Crc = info.Crc,
			Unknown2 = 0
		};
	}

	public readonly UInt16 ArchivePartNumber() => (UInt16)ArchivePart;
}
