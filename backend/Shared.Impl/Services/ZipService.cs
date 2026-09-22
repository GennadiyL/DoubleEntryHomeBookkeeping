using System.IO.Compression;
using Shared.Contracts;
using Shared.Contracts.Helpers;
using Shared.Contracts.Zip;

namespace Shared.Impl.Services;

/// <summary>
/// Defines the ZIP archive service.
/// Creates, reads, and extracts ZIP archives using platform compression APIs.
/// Consumers resolve IZipService and remain independent of the concrete ZIP library.
/// It maps shared compression-level values to implementation-specific options.
/// It does not apply business-specific archive layouts.
/// </summary>
internal class ZipService : IZipService
{
	private readonly ILogService _logService;

	public ZipService(ILogService logService) => _logService = logService;

	public virtual List<string> Zip(string zipFile, IList<string> files, ZipCompressionLevel compressionLevel)
	{
		if (!PathHelper.IsValidFilePath(zipFile))
		{
			string error = $"Invalid zip file path {zipFile}";
			_logService.Error(error);
			throw new ArgumentException(error);
		}

		List<string> zippedFiles = [];

		if (File.Exists(zipFile))
		{
			File.Delete(zipFile);
		}

		using (ZipArchive archive = ZipFile.Open(zipFile, ZipArchiveMode.Create))
		{
			foreach (string file in files)
			{
				if (!PathHelper.IsValidFilePath(file))
				{
					_logService.Info($"Invalid file to zip path {file}");
					continue;
				}

				if (!File.Exists(file))
				{
					_logService.Info($"File to zip doesn't exist {file}");
					continue;
				}

				_ = archive.CreateEntryFromFile(file, Path.GetFileName(file), ConvertCompressionLevel(compressionLevel));

				zippedFiles.Add(file);
				_logService.Info($"File added to zip {file}");
			}
		}

		_logService.Info($"Zip file created {zipFile}");
		return zippedFiles;
	}

	public List<string> Unzip(string zipFile, string destinationFolder)
	{
		if (!PathHelper.IsValidFilePath(zipFile))
		{
			throw new ArgumentException($"Invalid zip file path {zipFile}");
		}

		if (!File.Exists(zipFile))
		{
			throw new ArgumentException($"Zip file doesn't exist {zipFile}");
		}

		if (!PathHelper.IsValidDirectory(destinationFolder))
		{
			throw new ArgumentException($"Invalid destination folder path {destinationFolder}");
		}

		List<string> unzippedFiles = [];

		using ZipArchive archive = ZipFile.OpenRead(zipFile);
		foreach (ZipArchiveEntry entry in archive.Entries)
		{
			try
			{
				string outputPath = Path.Combine(destinationFolder, entry.FullName);
				entry.ExtractToFile(outputPath, overwrite: true);
				unzippedFiles.Add(outputPath);
				_logService.Info($"File restored from zip {outputPath}");
			}
			catch (Exception ex)
			{
				_logService.Error($"Cannot restore file from zip {entry.FullName}", ex);
			}
		}

		return unzippedFiles;
	}


	private static CompressionLevel ConvertCompressionLevel(ZipCompressionLevel compressionLevel)
	{
		switch (compressionLevel)
		{
			case ZipCompressionLevel.None:
				return CompressionLevel.NoCompression;
			case ZipCompressionLevel.BestSpeed:
				return CompressionLevel.Fastest;
			case ZipCompressionLevel.Default:
			case ZipCompressionLevel.BestCompression:
				return CompressionLevel.Optimal;
		}

		int compressionLevelValue = (int)compressionLevel;
		if (compressionLevelValue is <= 3 and >= 1)
		{
			return CompressionLevel.Fastest;
		}
		if (compressionLevelValue is >= 4 and <= 9)
		{
			return CompressionLevel.Optimal;
		}

		return CompressionLevel.NoCompression;
	}
}
