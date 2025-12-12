namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSPKHeader10 : ILSPKHeader
{
	public UInt32 Version;
	public UInt32 DataOffset;
	public UInt32 FileListSize;
	public UInt16 NumParts;
	public Byte Flags;
	public Byte Priority;
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
			Flags = (PackageFlags)Flags,
			Priority = Priority,
			Md5 = null
		};
	}

	public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
	{
		return new LSPKHeader10
		{
			Version = h.Version,
			DataOffset = h.DataOffset,
			FileListSize = h.FileListSize,
			NumParts = (UInt16)h.NumParts,
			Flags = (byte)h.Flags,
			Priority = h.Priority,
			NumFiles = h.NumFiles
		};
	}
}
