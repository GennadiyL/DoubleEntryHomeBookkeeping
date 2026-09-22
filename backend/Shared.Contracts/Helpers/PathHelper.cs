namespace Shared.Contracts.Helpers;

/// <summary>
/// Defines the path helper.
/// Provides project-independent, stable path manipulation helpers.
/// Callers use the stateless methods directly when the behavior does not need abstraction.
/// It belongs to Shared.Contracts because it has no business-domain dependency.
/// File-system access itself remains behind IFileService and IDirectoryService.
/// </summary>
public static class PathHelper
{
	public static bool IsValidDirectory(string? directoryPath)
	{
		if (string.IsNullOrEmpty(directoryPath))
		{
			return false;
		}

		char[] invalidChars = Path.GetInvalidPathChars();

		string[] directories = directoryPath.Split(Path.DirectorySeparatorChar);

		return directories.All(directory => directory.IndexOfAny(invalidChars) < 0);
	}

	public static bool IsValidFileName(string? fileName)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return false;
		}

		char[] invalidChars = Path.GetInvalidFileNameChars();

		return fileName.IndexOfAny(invalidChars) < 0;
	}

	public static bool IsValidFilePath(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			return false;
		}

		if (!IsValidFileName(Path.GetFileName(filePath)))
		{
			return false;
		}

		return IsValidDirectory(Path.GetDirectoryName(filePath));
	}

	public static bool IsFileExists(string filePath)
	{
		try
		{
			return File.Exists(filePath);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool IsDirectoryExists(string directoryPath)
	{
		try
		{
			return Directory.Exists(directoryPath);
		}
		catch (Exception)
		{
			return false;
		}
	}
}
