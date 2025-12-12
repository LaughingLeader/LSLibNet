namespace LSLib.LS.Pak;

internal class PackageWriter_V15<THeader, TFile> : PackageWriter
	where THeader : ILSPKHeader
	where TFile : ILSPKFile
{
	public PackageWriter_V15(PackageBuildData build, string packagePath) : base(build, packagePath)
	{ }

	public override void Write()
	{
		using (var writer = new BinaryWriter(MainStream, new UTF8Encoding(), true))
		{
			writer.Write(PackageHeaderCommon.Signature);
			var header = (THeader)THeader.FromCommonHeader(Metadata);
			BinUtils.WriteStruct(writer, ref header);
		}

		var writtenFiles = PackFiles();

		using (var writer = new BinaryWriter(MainStream, new UTF8Encoding(), true))
		{
			Metadata.FileListOffset = (UInt64)MainStream.Position;
			WriteCompressedFileList<TFile>(writer, writtenFiles);

			Metadata.FileListSize = (UInt32)(MainStream.Position - (long)Metadata.FileListOffset);
			if (Build.Hash)
			{
				Metadata.Md5 = ComputeArchiveHash();
			}
			else
			{
				Metadata.Md5 = new byte[0x10];
			}

			Metadata.NumParts = (UInt16)Streams.Count;

			MainStream.Seek(4, SeekOrigin.Begin);
			var header = (THeader)THeader.FromCommonHeader(Metadata);
			BinUtils.WriteStruct(writer, ref header);
		}
	}
}
