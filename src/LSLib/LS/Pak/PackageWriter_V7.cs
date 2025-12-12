namespace LSLib.LS.Pak;

internal class PackageWriter_V7<THeader, TFile> : PackageWriter
	where THeader : ILSPKHeader
	where TFile : ILSPKFile
{
	public PackageWriter_V7(PackageBuildData build, string packagePath) : base(build, packagePath)
	{ }

	public override void Write()
	{
		// <= v9 packages don't support LZ4
		if ((Build.Version == PackageVersion.V7 || Build.Version == PackageVersion.V9) && Build.Compression == CompressionMethod.LZ4)
		{
			Build.Compression = CompressionMethod.Zlib;
		}

		Metadata.NumFiles = (uint)Build.Files.Count;
		Metadata.FileListSize = (UInt32)(Marshal.SizeOf(typeof(TFile)) * Build.Files.Count);

		using var writer = new BinaryWriter(MainStream, new UTF8Encoding(), true);

		Metadata.DataOffset = (UInt32)Marshal.SizeOf(typeof(THeader)) + Metadata.FileListSize;
		if (Metadata.Version >= 10)
		{
			Metadata.DataOffset += 4;
		}

		int paddingLength = Build.Version.PaddingSize();
		if (Metadata.DataOffset % paddingLength > 0)
		{
			Metadata.DataOffset += (UInt32)(paddingLength - Metadata.DataOffset % paddingLength);
		}

		// Write a placeholder instead of the actual headers; we'll write them after we
		// compressed and flushed all files to disk
		var placeholder = new byte[Metadata.DataOffset];
		writer.Write(placeholder);

		var writtenFiles = PackFiles();

		MainStream.Seek(0, SeekOrigin.Begin);
		if (Metadata.Version >= 10)
		{
			writer.Write(PackageHeaderCommon.Signature);
		}
		Metadata.NumParts = (UInt16)Streams.Count;
		Metadata.Md5 = ComputeArchiveHash();

		var header = (THeader)THeader.FromCommonHeader(Metadata);
		BinUtils.WriteStruct(writer, ref header);

		WriteFileList<TFile>(writer, writtenFiles);
	}
}
