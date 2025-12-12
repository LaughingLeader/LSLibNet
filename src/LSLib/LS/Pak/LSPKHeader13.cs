namespace LSLib.LS.Pak;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct LSPKHeader13 : ILSPKHeader
{
	public UInt32 Version;
	public UInt32 FileListOffset;
	public UInt32 FileListSize;
	public UInt16 NumParts;
	public Byte Flags;
	public Byte Priority;
	public fixed byte Md5[16];

	public readonly PackageHeaderCommon ToCommonHeader()
	{
		var header = new PackageHeaderCommon
		{
			Version = Version,
			DataOffset = 0,
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
		var header = new LSPKHeader13
		{
			Version = h.Version,
			FileListOffset = (UInt32)h.FileListOffset,
			FileListSize = h.FileListSize,
			NumParts = (UInt16)h.NumParts,
			Flags = (byte)h.Flags,
			Priority = h.Priority
		};

		Marshal.Copy(h.Md5, 0, new IntPtr(header.Md5), 0x10);
		return header;
	}
}
