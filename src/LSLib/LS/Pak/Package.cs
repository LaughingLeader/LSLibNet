using System.IO.MemoryMappedFiles;

namespace LSLib.LS.Pak;

public class Package : IDisposable
{
	internal MemoryMappedFile MetadataFile { get; }
	internal MemoryMappedViewAccessor MetadataView { get; }
	internal MemoryMappedFile[]? Parts { get; private set; }
	internal MemoryMappedViewAccessor[]? Views { get; private set; }

	public string PackagePath { get; }
	public PackageHeaderCommon? Metadata { get; internal set; }
	public List<PackagedFileInfo> Files { get; }

	public PackageVersion? Version => (PackageVersion?)(Metadata?.Version);

	internal Package(string path, FileStream? stream = null)
	{
		Files = [];
		PackagePath = path;
		var file = stream ?? File.OpenRead(PackagePath);
		MetadataFile = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
		MetadataView = MetadataFile.CreateViewAccessor(0, file.Length, MemoryMappedFileAccess.Read);
	}

	public void OpenStreams(int numParts)
	{
		// Open a stream for each file chunk
		Parts = new MemoryMappedFile[numParts];
		Views = new MemoryMappedViewAccessor[numParts];

		Parts[0] = MetadataFile;
		Views[0] = MetadataView;

		for (var part = 1; part < numParts; part++)
		{
			var partPath = Package.MakePartFilename(PackagePath, part);
			OpenPart(part, partPath);
		}
	}

	public void OpenPart(int index, string path)
	{
		if (Parts == null || Views == null) throw new IndexOutOfRangeException("Call OpenStreams first.", new NullReferenceException(nameof(Parts)));
		var file = File.OpenRead(path);
		Parts[index] = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
		Views[index] = Parts[index].CreateViewAccessor(0, file.Length, MemoryMappedFileAccess.Read);
	}

	public void Dispose()
	{
		MetadataView?.Dispose();
		MetadataFile?.Dispose();

		foreach (var view in Views ?? [])
		{
			view?.Dispose();
		}

		foreach (var file in Parts ?? [])
		{
			file?.Dispose();
		}

		GC.SuppressFinalize(this);
	}

	public static string MakePartFilename(string path, int part)
	{
		var dirName = Path.GetDirectoryName(path);
		var baseName = Path.GetFileNameWithoutExtension(path);
		var extension = Path.GetExtension(path);
		return Path.Join(dirName, $"{baseName}_{part}{extension}");
	}
}
