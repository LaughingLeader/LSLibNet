namespace LSLib.LS.Pak;

public class Packager
{
	public delegate void ProgressUpdateDelegate(string status, long numerator, long denominator);

	public ProgressUpdateDelegate ProgressUpdate = delegate { };

	private void WriteProgressUpdate(PackageBuildInputFile file, long numerator, long denominator)
	{
		ProgressUpdate(file.Path, numerator, denominator);
	}

	public void UncompressPackage(Package package, string outputPath, Func<PackagedFileInfo, bool> filter = null)
	{
		if (outputPath.Length > 0 && !outputPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
		{
			outputPath += Path.DirectorySeparatorChar;
		}

		List<PackagedFileInfo> files = package.Files;

		if (filter != null)
		{
			files = files.FindAll(obj => filter(obj));
		}

		long totalSize = files.Sum(p => (long)p.Size());
		long currentSize = 0;

		foreach (var file in files)
		{
			ProgressUpdate(file.Name, currentSize, totalSize);
			currentSize += (long)file.Size();

			if (file.IsDeletion()) continue;

			string outPath = Path.Join(outputPath, file.Name);

			FileManager.TryToCreateDirectory(outPath);

			using var inStream = file.CreateContentReader();
			using var outFile = File.Open(outPath, FileMode.Create, FileAccess.Write);
			inStream.CopyTo(outFile);
		}
	}

	public void UncompressPackage(string packagePath, string outputPath, Func<PackagedFileInfo, bool> filter = null)
	{
		ProgressUpdate("Reading package headers ...", 0, 1);
		var reader = new PackageReader();
		using var package = reader.Read(packagePath);
		UncompressPackage(package, outputPath, filter);
	}

	public static bool ShouldInclude(string file, PackageBuildData build)
	{
		if (build.ExcludeHidden)
		{
			var fileElements = file.Split(Path.DirectorySeparatorChar);

			return !Array.Exists(fileElements, element => element.StartsWith('.'));
		}
		return false;
	}

	private static void AddFilesFromPath(PackageBuildData build, string path)
	{
		if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.InvariantCultureIgnoreCase))
		{
			path += Path.DirectorySeparatorChar;
		}

		foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
		{
			var name = Path.GetRelativePath(path, file);

			if (ShouldInclude(file, build))
			{
				build.Files.Add(PackageBuildInputFile.CreateFromFilesystem(file, name));
			}
		}
	}

	public async Task CreatePackage(string packagePath, string inputPath, PackageBuildData build)
	{
		FileManager.TryToCreateDirectory(packagePath);

		ProgressUpdate("Enumerating files ...", 0, 1);
		AddFilesFromPath(build, inputPath);

		ProgressUpdate("Creating archive ...", 0, 1);
		using var writer = PackageWriterFactory.Create(build, packagePath);
		writer.WriteProgress += WriteProgressUpdate;
		writer.Write();
	}
}
