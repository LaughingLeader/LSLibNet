namespace LSLib.LS.Pak;

public class PackageBuildInputFile
{
	public string Path;
	public string FilesystemPath;
	public byte[] Body;

	public Stream MakeInputStream()
	{
		if (Body != null)
		{
			return new MemoryStream(Body);
		}
		else
		{
			return new FileStream(FilesystemPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}
	}

	public long Size()
	{
		if (Body != null)
		{
			return Body.Length;
		}
		else
		{
			return new FileInfo(FilesystemPath).Length;
		}
	}

	public static PackageBuildInputFile CreateFromBlob(byte[] body, string path)
	{
		return new PackageBuildInputFile
		{
			Path = path,
			Body = body
		};
	}

	public static PackageBuildInputFile CreateFromFilesystem(string filesystemPath, string path)
	{
		return new PackageBuildInputFile
		{
			Path = path,
			FilesystemPath = filesystemPath
		};
	}
}
