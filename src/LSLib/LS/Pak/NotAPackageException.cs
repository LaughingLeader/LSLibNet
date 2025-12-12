namespace LSLib.LS.Pak;

public class NotAPackageException : Exception
{
	public NotAPackageException()
	{
	}

	public NotAPackageException(string message) : base(message)
	{
	}

	public NotAPackageException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
