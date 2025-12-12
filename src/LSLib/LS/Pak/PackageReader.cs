using System.Buffers;
using System.IO.MemoryMappedFiles;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;

namespace LSLib.LS.Pak;

public class PackageReader
{
	private bool MetadataOnly;
	private Package Pak;

	private void ReadCompressedFileList<TFile>(MemoryMappedViewAccessor view, long offset)
		where TFile : struct, ILSPKFile
	{
		int numFiles = view.ReadInt32(offset);
		byte[] compressed;
		if (Pak.Metadata.Version > 13)
		{
			int compressedSize = view.ReadInt32(offset + 4);
			compressed = new byte[compressedSize];
			view.ReadArray(offset + 8, compressed, 0, compressedSize);
		}
		else
		{
			compressed = new byte[(int)Pak.Metadata.FileListSize - 4];
			view.ReadArray(offset + 4, compressed, 0, (int)Pak.Metadata.FileListSize - 4);
		}

		int fileBufferSize = Marshal.SizeOf(typeof(TFile)) * numFiles;
		var fileBuf = CompressionHelpers.Decompress(compressed, fileBufferSize, CompressionFlags.MethodLZ4);

		using var ms = new MemoryStream(fileBuf);
		using var msr = new BinaryReader(ms);

		var entries = new TFile[numFiles];
		BinUtils.ReadStructs(msr, entries);

		foreach (var entry in entries)
		{
			Pak.Files.Add(PackagedFileInfo.CreateFromEntry(Pak, entry, Pak.Parts[entry.ArchivePartNumber()], Pak.Views[entry.ArchivePartNumber()]));
		}
	}

	private void ReadFileList<TFile>(MemoryMappedViewAccessor view, long offset)
		where TFile : struct, ILSPKFile
	{
		var entries = new TFile[Pak.Metadata.NumFiles];
		BinUtils.ReadStructs(view, offset, entries);

		foreach (var entry in entries)
		{
			var file = PackagedFileInfo.CreateFromEntry(Pak, entry, Pak.Parts[entry.ArchivePartNumber()], Pak.Views[entry.ArchivePartNumber()]);
			if (file.ArchivePart == 0)
			{
				file.OffsetInFile += Pak.Metadata.DataOffset;
			}

			Pak.Files.Add(file);
		}
	}

	private Package ReadHeaderAndFileList<THeader, TFile>(MemoryMappedViewAccessor view, long offset)
		where THeader : struct, ILSPKHeader
		where TFile : struct, ILSPKFile
	{
		view.Read<THeader>(offset, out var header);

		Pak.Metadata = header.ToCommonHeader();

		if (MetadataOnly) return Pak;

		Pak.OpenStreams((int)Pak.Metadata.NumParts);

		if (Pak.Metadata.Version > 10)
		{
			Pak.Metadata.DataOffset = (uint)(offset + Marshal.SizeOf<THeader>());
			ReadCompressedFileList<TFile>(view, (long)Pak.Metadata.FileListOffset);
		}
		else
		{
			ReadFileList<TFile>(view, offset + Marshal.SizeOf<THeader>());
		}

		if (Pak.Metadata.Flags.HasFlag(PackageFlags.Solid) && Pak.Files.Count > 0)
		{
			UnpackSolidSegment(view);
		}

		return Pak;
	}

	private void UnpackSolidSegment(MemoryMappedViewAccessor view)
	{
		// Calculate compressed frame offset and bounds
		ulong totalUncompressedSize = 0;
		ulong totalSizeOnDisk = 0;
		ulong firstOffset = 0xffffffff;
		ulong lastOffset = 0;

		foreach (var entry in Pak.Files)
		{
			var file = entry;

			totalUncompressedSize += file.UncompressedSize;
			totalSizeOnDisk += file.SizeOnDisk;
			if (file.OffsetInFile < firstOffset)
			{
				firstOffset = file.OffsetInFile;
			}
			if (file.OffsetInFile + file.SizeOnDisk > lastOffset)
			{
				lastOffset = file.OffsetInFile + file.SizeOnDisk;
			}
		}

		if (firstOffset != Pak.Metadata.DataOffset + 7 || lastOffset - firstOffset != totalSizeOnDisk)
		{
			string msg = $"Incorrectly compressed solid archive; offsets {firstOffset}/{lastOffset}, bytes {totalSizeOnDisk}";
			throw new InvalidDataException(msg);
		}

		// Decompress all files as a single frame (solid)
		byte[] frame = new byte[lastOffset - Pak.Metadata.DataOffset];
		view.ReadArray(Pak.Metadata.DataOffset, frame, 0, (int)(lastOffset - Pak.Metadata.DataOffset));

		//byte[] decompressed = Native.LZ4FrameCompressor.Decompress(frame);
		//var decompressedStream = new MemoryStream(decompressed);

		var decoded = new byte[0];
		using var decodeStream = LZ4Frame.Decode(frame);

		var inputOffset = 0;
		var outputOffset = 0;
		while (inputOffset < frame.Length)
		{
			var outputFree = decoded.Length - outputOffset;

			// Always keep ~0x10000 bytes free in the decompression output array.
			if (outputFree < 0x10000)
			{
				Array.Resize(ref decoded, decoded.Length + (0x10000 - outputFree));
				outputFree = decoded.Length - outputOffset;
			}

			var inputAvailable = frame.Length - inputOffset;

			var readBytes = decodeStream.ReadManyBytes(decoded.AsSpan());

			if (readBytes == -1)
			{
				throw new InvalidDataException("Failed to create LZ4 decompression context");
			}

			inputOffset += inputAvailable;
			outputOffset += outputFree;

			if (inputAvailable == 0)
			{
				throw new InvalidDataException("LZ4 error: Not all input data was processed (input might be truncated or corrupted?)");
			}
		}

		var decompressedStream = new MemoryStream(decoded);

		//var decoded = LZ4Frame.Decode(frame.AsSpan(), new ArrayBufferWriter<byte>(frame.Length + 32)).WrittenMemory.ToArray();
		//var decompressedStream = new MemoryStream(decoded);

		// Update offsets to point to the decompressed chunk
		ulong offset = Pak.Metadata.DataOffset + 7;
		ulong compressedOffset = 0;
		foreach (var entry in Pak.Files)
		{
			var file = entry;

			//if (file.OffsetInFile != offset)
			//{
			//	throw new InvalidDataException("File list in solid archive not contiguous");
			//}

			file.MakeSolid(compressedOffset, decompressedStream);

			offset += file.SizeOnDisk;
			compressedOffset += file.UncompressedSize;
		}
	}

	public Package ReadInternal(Package pak)
	{
		Pak = pak;
		var view = Pak.MetadataView;

		// Check for v13 package headers
		var headerSize = view.ReadInt32(view.Capacity - 8);
		var signature = view.ReadUInt32(view.Capacity - 4);
		if (signature == PackageHeaderCommon.Signature)
		{
			return ReadHeaderAndFileList<LSPKHeader13, FileEntry10>(view, view.Capacity - headerSize);
		}

		// Check for v10 package headers
		signature = view.ReadUInt32(0);
		Int32 version;
		if (signature == PackageHeaderCommon.Signature)
		{
			version = view.ReadInt32(4);
			return version switch
			{
				10 => ReadHeaderAndFileList<LSPKHeader10, FileEntry10>(view, 4),
				15 => ReadHeaderAndFileList<LSPKHeader15, FileEntry15>(view, 4),
				16 => ReadHeaderAndFileList<LSPKHeader16, FileEntry15>(view, 4),
				18 => ReadHeaderAndFileList<LSPKHeader16, FileEntry18>(view, 4),
				_ => throw new InvalidDataException($"Package version v{version} not supported")
			};
		}

		// Check for v9 and v7 package headers
		version = view.ReadInt32(0);
		if (version == 7 || version == 9)
		{
			return ReadHeaderAndFileList<LSPKHeader7, FileEntry7>(view, 0);
		}

		throw new NotAPackageException("No valid signature found in package file");
	}

	public Package Read(string path, bool metadataOnly = false)
	{
		MetadataOnly = metadataOnly;

		try
		{
			return ReadInternal(new Package(path));
		}
		catch (Exception)
		{
			Pak?.Dispose();
			throw;
		}
	}

	public Package Read(string path, FileStream stream, bool metadataOnly = false)
	{
		MetadataOnly = metadataOnly;

		try
		{
			return ReadInternal(new Package(path, stream));
		}
		catch (Exception)
		{
			Pak?.Dispose();
			throw;
		}
	}
}
