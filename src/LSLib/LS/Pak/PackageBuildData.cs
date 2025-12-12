namespace LSLib.LS.Pak;

public class PackageBuildData
{
	public PackageVersion Version { get; set; } = PackageHeaderCommon.CurrentVersion;
	public CompressionMethod Compression { get; set; } = CompressionMethod.None;
	public LSCompressionLevel CompressionLevel { get; set; } = LSCompressionLevel.Default;
	public PackageFlags Flags { get; set; } = 0;
	// Calculate full archive checksum?
	public bool Hash { get; set; } = false;
	public List<PackageBuildInputFile> Files { get; set; } = [];
	public bool ExcludeHidden { get; set; } = true;
	public byte Priority { get; set; } = 0;

}
