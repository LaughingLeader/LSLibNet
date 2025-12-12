namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct LSPKHeader16 : ILSPKHeader
{
	public UInt32 Version;
	public UInt64 FileListOffset;
	public UInt32 FileListSize;
	public Byte Flags;
	public Byte Priority;
	public fixed byte Md5[16];

	public UInt16 NumParts;

	public readonly PackageHeaderCommon ToCommonHeader()
	{
		var header = new PackageHeaderCommon
		{
			Version = Version,
			FileListOffset = FileListOffset,
			FileListSize = FileListSize,
			NumParts = NumParts,
			Flags = (PackageFlags)Flags,
			Priority = Priority,
			Md5 = new byte[16]
		};

		fixed (byte* md = Md5)
		{
			Marshal.Copy(new IntPtr(md), header.Md5, 0, 0x10);
		}

		return header;
	}

	public static ILSPKHeader FromCommonHeader(PackageHeaderCommon h)
	{
		var header = new LSPKHeader16
		{
			Version = h.Version,
			FileListOffset = (UInt32)h.FileListOffset,
			FileListSize = h.FileListSize,
			Flags = (byte)h.Flags,
			Priority = h.Priority,
			NumParts = (UInt16)h.NumParts
		};

		Marshal.Copy(h.Md5, 0, new IntPtr(header.Md5), 0x10);
		return header;
	}
}
