using System.IO.MemoryMappedFiles;

namespace LSLib.LS.Pak;

public class Package : IDisposable
{
	public readonly string PackagePath;
	internal readonly MemoryMappedFile MetadataFile;
	internal readonly MemoryMappedViewAccessor MetadataView;

	internal MemoryMappedFile[] Parts;
	internal MemoryMappedViewAccessor[] Views;

	public PackageHeaderCommon Metadata;
	public List<PackagedFileInfo> Files = [];

	public PackageVersion Version
	{
		get { return (PackageVersion)Metadata.Version; }
	}

	public void OpenPart(int index, string path)
	{
		var file = File.OpenRead(path);
		Parts[index] = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
		Views[index] = Parts[index].CreateViewAccessor(0, file.Length, MemoryMappedFileAccess.Read);
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
			string partPath = Package.MakePartFilename(PackagePath, part);
			OpenPart(part, partPath);
		}
	}

	internal Package(string path, FileStream stream = null)
	{
		PackagePath = path;
		var file = stream ?? File.OpenRead(PackagePath);
		MetadataFile = MemoryMappedFile.CreateFromFile(file, null, file.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
		MetadataView = MetadataFile.CreateViewAccessor(0, file.Length, MemoryMappedFileAccess.Read);
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
	}

	public static string MakePartFilename(string path, int part)
	{
		string dirName = Path.GetDirectoryName(path);
		string baseName = Path.GetFileNameWithoutExtension(path);
		string extension = Path.GetExtension(path);
		return Path.Join(dirName, $"{baseName}_{part}{extension}");
	}
}
