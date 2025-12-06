using Sunfire.Logging;

namespace Sunfire.FSUtils;

internal static class FSHelpers
{
    public static bool TryReadHeader(string path, Span<byte> buffer, out int bytesRead)
    {
        bytesRead = 0;
        try
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);

            bytesRead = fs.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            return true;
        }
        catch (FileNotFoundException) { /* File vanished between listing and reading */ }
        catch (DirectoryNotFoundException) { /* Parent moved */ }
        catch (UnauthorizedAccessException) { /* Permission denied */ }
        catch (IOException) { /* File strictly locked by another process (rare with FileShare.ReadWrite) */ }
        catch (System.Security.SecurityException) { /* ACL issues */ }

        return false;
    }

    public static bool TryReadSegment(string path, long offset, int length, Span<byte> buffer)
    {
        if (buffer.Length < length)
        {
            Task.Run(async () => await Logger.Error(nameof(FSUtils), "Media scanner passed too small of a buffer for request segment read"));
            return false;
        }

        try
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);

            if (fs.Length < length + offset)
            {
                return false;
            }

            fs.Seek(offset, SeekOrigin.Begin);

            int bytesRead = fs.ReadAtLeast(buffer[..length], length, throwOnEndOfStream: false);

            return bytesRead == length;
        }
        catch (FileNotFoundException) { /* File vanished between listing and reading */ }
        catch (DirectoryNotFoundException) { /* Parent moved */ }
        catch (UnauthorizedAccessException) { /* Permission denied */ }
        catch (IOException) { /* File strictly locked by another process */ }
        catch (System.Security.SecurityException) { /* ACL issues */ }
        catch (ArgumentOutOfRangeException) { /* Invalid offset passed */ }

        return false;
    }

    public static bool TryReadTail(string path, Span<byte> buffer, out int bytesRead)
    {
        bytesRead = 0;
        try
        {
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);

            fs.Seek(-Math.Min(fs.Length, buffer.Length), SeekOrigin.End);

            bytesRead = fs.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            return true;
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        catch (System.Security.SecurityException) { }

        return false;
    }
}
