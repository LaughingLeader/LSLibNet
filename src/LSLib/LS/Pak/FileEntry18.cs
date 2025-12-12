namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FileEntry18 : ILSPKFile
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
	public byte[] Name;

	public UInt32 OffsetInFile1;
	public UInt16 OffsetInFile2;
	public Byte ArchivePart;
	public Byte Flags;
	public UInt32 SizeOnDisk;
	public UInt32 UncompressedSize;

	public readonly void ToCommon(PackagedFileInfoCommon info)
	{
		info.Name = BinUtils.NullTerminatedBytesToString(Name);
		info.ArchivePart = ArchivePart;
		info.Crc = 0;
		info.Flags = (CompressionFlags)Flags;
		info.OffsetInFile = OffsetInFile1 | ((ulong)OffsetInFile2 << 32);
		info.SizeOnDisk = SizeOnDisk;
		info.UncompressedSize = UncompressedSize;
	}

	public static ILSPKFile FromCommon(PackagedFileInfoCommon info)
	{
		return new FileEntry18
		{
			Name = BinUtils.StringToNullTerminatedBytes(info.Name, 256),
			OffsetInFile1 = (uint)(info.OffsetInFile & 0xffffffff),
			OffsetInFile2 = (ushort)((info.OffsetInFile >> 32) & 0xffff),
			ArchivePart = (byte)info.ArchivePart,
			Flags = (byte)info.Flags,
			SizeOnDisk = (uint)info.SizeOnDisk,
			UncompressedSize = info.Flags.Method() == CompressionMethod.None ? 0 : (uint)info.UncompressedSize
		};
	}

	public readonly UInt16 ArchivePartNumber() => ArchivePart;
}
