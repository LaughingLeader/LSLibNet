namespace LSLib.LS.Pak;

internal class PackageWriter_V13<THeader, TFile> : PackageWriter
	where THeader : ILSPKHeader
	where TFile : ILSPKFile
{
	public PackageWriter_V13(PackageBuildData build, string packagePath) : base(build, packagePath)
	{ }

	public override void Write()
	{
		var writtenFiles = PackFiles();

		using var writer = new BinaryWriter(MainStream, new UTF8Encoding(), true);

		Metadata.FileListOffset = (UInt64)MainStream.Position;
		WriteCompressedFileList<TFile>(writer, writtenFiles);

		Metadata.FileListSize = (UInt32)(MainStream.Position - (long)Metadata.FileListOffset);
		Metadata.Md5 = ComputeArchiveHash();
		Metadata.NumParts = (UInt16)Streams.Count;

		var header = (THeader)THeader.FromCommonHeader(Metadata);
		BinUtils.WriteStruct(writer, ref header);

		writer.Write((UInt32)(8 + Marshal.SizeOf(typeof(THeader))));
		writer.Write(PackageHeaderCommon.Signature);
	}
}
