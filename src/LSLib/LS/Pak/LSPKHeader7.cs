namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSPKHeader7 : ILSPKHeader
{
	public UInt32 Version;
	public UInt32 DataOffset;
	public UInt32 NumParts;
	public UInt32 FileListSize;
	public Byte LittleEndian;
	public UInt32 NumFiles;

	public readonly PackageHeaderCommon ToCommonHeader()
	{
		return new PackageHeaderCommon
		{
			Version = Version,
			DataOffset = DataOffset,
			FileListOffset = (ulong)Marshal.SizeOf(typeof(LSPKHeader7)),
			FileListSize = FileListSize,
			NumFiles = NumFiles,
			NumParts = NumParts,
			Flags = 0,
			Priority = 0,
			Md5 = null
		};
	}

	public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
	{
		return new LSPKHeader7
		{
			Version = h.Version,
			DataOffset = h.DataOffset,
			NumParts = h.NumParts,
			FileListSize = h.FileListSize,
			LittleEndian = 0,
			NumFiles = h.NumFiles
		};
	}
}
