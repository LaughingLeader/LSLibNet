namespace LSLib.LS.Pak;

public static class PackageWriterFactory
{
	public static PackageWriter Create(PackageBuildData build, string packagePath)
	{
		return build.Version switch
		{
			PackageVersion.V18 => new PackageWriter_V15<LSPKHeader16, FileEntry18>(build, packagePath),
			PackageVersion.V16 => new PackageWriter_V15<LSPKHeader16, FileEntry15>(build, packagePath),
			PackageVersion.V15 => new PackageWriter_V15<LSPKHeader15, FileEntry15>(build, packagePath),
			PackageVersion.V13 => new PackageWriter_V13<LSPKHeader13, FileEntry10>(build, packagePath),
			PackageVersion.V10 => new PackageWriter_V7<LSPKHeader10, FileEntry10>(build, packagePath),
			PackageVersion.V9 or PackageVersion.V7 => new PackageWriter_V7<LSPKHeader7, FileEntry7>(build, packagePath),
			_ => throw new ArgumentException($"Cannot write version {build.Version} packages"),
		};
	}
}
