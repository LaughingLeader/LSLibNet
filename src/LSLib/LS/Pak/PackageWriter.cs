using System.IO.Hashing;
using System.Security.Cryptography;

namespace LSLib.LS.Pak;

abstract public class PackageWriter : IDisposable
{
	public delegate void WriteProgressDelegate(PackageBuildInputFile file, long numerator, long denominator);

	protected readonly PackageHeaderCommon Metadata = new();
	protected readonly List<Stream> Streams = [];
	protected readonly PackageBuildData Build;
	protected readonly string PackagePath;
	protected readonly Stream MainStream;
	public WriteProgressDelegate WriteProgress = delegate { };

	public PackageWriter(PackageBuildData build, string packagePath)
	{
		Build = build;
		PackagePath = packagePath;

		MainStream = File.Open(PackagePath, FileMode.Create, FileAccess.Write);
		Streams.Add(MainStream);

		Metadata.Version = (UInt32)Build.Version;
		Metadata.Flags = Build.Flags;
		Metadata.Priority = Build.Priority;
		Metadata.Md5 = new byte[16];
	}


	public void Dispose()
	{
		foreach (Stream stream in Streams)
		{
			stream.Dispose();
		}
	}

	protected bool CanCompressFile(PackageBuildInputFile file, Stream inputStream)
	{
		var extension = Path.GetExtension(file.Path).ToLowerInvariant();
		return extension != ".gts"
			&& extension != ".gtp"
			&& extension != ".wem"
			&& extension != ".bnk"
			&& inputStream.Length > 0;
	}

	protected void WritePadding(Stream stream)
	{
		int padLength = Build.Version.PaddingSize();
		long alignTo;
		if (Build.Version >= PackageVersion.V16)
		{
			alignTo = stream.Position - Marshal.SizeOf(typeof(LSPKHeader16)) - 4;
		}
		else
		{
			alignTo = stream.Position;
		}

		// Pad the file to a multiple of 64 bytes
		var padBytes = (padLength - alignTo % padLength) % padLength;
		var pad = new byte[padBytes];
		for (var i = 0; i < pad.Length; i++)
		{
			pad[i] = 0xAD;
		}

		stream.Write(pad, 0, pad.Length);
	}

	protected PackageBuildTransientFile WriteFile(PackageBuildInputFile input)
	{
		using var inputStream = input.MakeInputStream();

		var compression = Build.Compression;
		var compressionLevel = Build.CompressionLevel;

		if (!CanCompressFile(input, inputStream))
		{
			compression = CompressionMethod.None;
			compressionLevel = LSCompressionLevel.Fast;
		}

		var uncompressed = new byte[inputStream.Length];
		inputStream.ReadExactly(uncompressed, 0, uncompressed.Length);
		var compressed = CompressionHelpers.Compress(uncompressed, compression, compressionLevel);

		if (Streams.Last().Position + compressed.Length > Build.Version.MaxPackageSize())
		{
			// Start a new package file if the current one is full.
			string partPath = Package.MakePartFilename(PackagePath, Streams.Count);
			var nextPart = File.Open(partPath, FileMode.Create, FileAccess.Write);
			Streams.Add(nextPart);
		}

		Stream stream = Streams.Last();
		var packaged = new PackageBuildTransientFile
		{
			Name = input.Path.Replace('\\', '/'),
			UncompressedSize = (ulong)uncompressed.Length,
			SizeOnDisk = (ulong)compressed.Length,
			ArchivePart = (UInt32)(Streams.Count - 1),
			OffsetInFile = (ulong)stream.Position,
			Flags = CompressionHelpers.MakeCompressionFlags(compression, compressionLevel)
		};

		stream.Write(compressed, 0, compressed.Length);

		if (Build.Version.HasCrc())
		{
			packaged.Crc = Crc32.HashToUInt32(compressed);
		}
		else
		{
			packaged.Crc = 0;
		}

		if (!Build.Flags.HasFlag(PackageFlags.Solid))
		{
			WritePadding(stream);
		}

		return packaged;
	}

	protected List<PackageBuildTransientFile> PackFiles()
	{
		long totalSize = Build.Files.Sum(p => p.Size());
		long currentSize = 0;

		var writtenFiles = new List<PackageBuildTransientFile>();
		foreach (var file in Build.Files)
		{
			WriteProgress(file, currentSize, totalSize);
			writtenFiles.Add(WriteFile(file));
			currentSize += file.Size();
		}

		return writtenFiles;
	}

	internal void WriteFileList<TFile>(BinaryWriter metadataWriter, List<PackageBuildTransientFile> files)
		where TFile : ILSPKFile
	{
		foreach (var file in files)
		{
			if (file.ArchivePart == 0)
			{
				file.OffsetInFile -= Metadata.DataOffset;
			}

			// <= v10 packages don't support compression level in the flags field
			file.Flags = (CompressionFlags)((byte)file.Flags & 0x0f);

			var entry = (TFile)TFile.FromCommon(file);
			BinUtils.WriteStruct(metadataWriter, ref entry);
		}
	}

	internal void WriteCompressedFileList<TFile>(BinaryWriter metadataWriter, List<PackageBuildTransientFile> files)
		where TFile : ILSPKFile
	{
		byte[] fileListBuf;
		using (var fileList = new MemoryStream())
		using (var fileListWriter = new BinaryWriter(fileList))
		{
			foreach (var file in files)
			{
				var entry = (TFile)TFile.FromCommon(file);
				BinUtils.WriteStruct(fileListWriter, ref entry);
			}

			fileListBuf = fileList.ToArray();
		}

		byte[] compressedFileList = CompressionHelpers.Compress(fileListBuf, CompressionMethod.LZ4, LSCompressionLevel.Default);

		metadataWriter.Write((UInt32)files.Count);

		if (Build.Version > PackageVersion.V13)
		{
			metadataWriter.Write((UInt32)compressedFileList.Length);
		}
		else
		{
			Metadata.FileListSize = (uint)compressedFileList.Length + 4;
		}

		metadataWriter.Write(compressedFileList);
	}

	protected byte[] ComputeArchiveHash()
	{
		// MD5 is computed over the contents of all files in an alphabetically sorted order
		var orderedFileList = Build.Files.Select(item => item).ToList();
		if (Build.Version < PackageVersion.V15)
		{
			orderedFileList.Sort((a, b) => String.CompareOrdinal(a.Path, b.Path));
		}

		using MD5 md5 = MD5.Create();
		foreach (var file in orderedFileList)
		{
			using var packagedStream = file.MakeInputStream();
			using var reader = new BinaryReader(packagedStream);

			byte[] uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
			md5.TransformBlock(uncompressed, 0, uncompressed.Length, uncompressed, 0);
		}

		md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
		byte[] hash = md5.Hash;

		// All hash bytes are incremented by 1
		for (var i = 0; i < hash.Length; i++)
		{
			hash[i] += 1;
		}

		return hash;
	}

	abstract public void Write();
}
