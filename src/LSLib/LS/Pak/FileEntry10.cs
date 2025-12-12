namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry10 : ILSPKFile
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	public byte[] Name;

	public UInt32 OffsetInFile;
	public UInt32 SizeOnDisk;
	public UInt32 UncompressedSize;
	public UInt32 ArchivePart;
	public UInt32 Flags;
	public UInt32 Crc;

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
		return new FileEntry10
		{
			Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
			OffsetInFile = (uint)info.OffsetInFile,
			SizeOnDisk = (uint)info.SizeOnDisk,
			UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize,
			ArchivePart = info.ArchivePart,
			Flags = (byte)info.Flags,
			Crc = info.Crc
		};
	}

	public readonly UInt16 ArchivePartNumber() => (UInt16)ArchivePart;
}
