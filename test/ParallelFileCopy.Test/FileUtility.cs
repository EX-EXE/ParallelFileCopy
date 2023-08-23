using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelFileCopy.Test;

public static class FileUtility
{

    public static string CreateRandomFile(long fileSize)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        CreateRandomFile(path, fileSize);
        return path;
    }

    public static void CreateRandomFile(string filePath, long fileSize)
    {
        var dir = System.IO.Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        using var fileHandle = System.IO.File.OpenHandle(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, FileOptions.RandomAccess | FileOptions.SequentialScan, 0);
        System.IO.RandomAccess.SetLength(fileHandle, fileSize);

        var rnd = new Random((int)DateTimeOffset.Now.Ticks);
        var bufferSize = 4096;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        var bufferSpan = buffer.AsSpan();
        try
        {
            var writeSize = 0L;
            while (writeSize < fileSize)
            {
                var writeSpan = bufferSpan.Slice(0, (int)(writeSize + bufferSize < fileSize ? bufferSize : fileSize - writeSize));
                rnd.NextBytes(writeSpan);
                System.IO.RandomAccess.Write(fileHandle, writeSpan, writeSize);
                writeSize += writeSpan.Length;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static (string rootDir, string[] files) CreateRandomSizeFiles(int fileNum, int dirDepth, long minFileSize, long maxFileSize)
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

        // Rand Dirs
        var randDirs = new List<string>();
        var tmpDir = root;
        randDirs.Add(tmpDir);
        foreach (var _ in Enumerable.Range(0, dirDepth))
        {
            tmpDir += "/" + System.IO.Path.GetRandomFileName();
            randDirs.Add(tmpDir);
        }

        // Rand Files
        var randFiles = new List<string>();
        var rnd = new Random((int)DateTime.Now.Ticks);
        foreach (var _ in Enumerable.Range(0, fileNum))
        {
            var dirIndex = rnd.Next(randDirs.Count);
            var filePath = randDirs[dirIndex] + "/" + System.IO.Path.GetRandomFileName();
            randFiles.Add(filePath);

            var size = rnd.NextInt64(minFileSize, maxFileSize);
            CreateRandomFile(filePath, size);
        }
        return (root, randFiles.ToArray());
    }

    public static bool CompareFile(string pathA, string pathB)
    {
        if (!System.IO.File.Exists(pathA))
        {
            throw new System.IO.FileNotFoundException(pathA);
        }
        if (!System.IO.File.Exists(pathB))
        {
            throw new System.IO.FileNotFoundException(pathB);
        }

        using var fileHandleA = System.IO.File.OpenHandle(pathA, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.RandomAccess | FileOptions.SequentialScan, 0);
        using var fileHandleB = System.IO.File.OpenHandle(pathB, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.RandomAccess | FileOptions.SequentialScan, 0);

        var fileSize = System.IO.RandomAccess.GetLength(fileHandleA);
        if (fileSize != System.IO.RandomAccess.GetLength(fileHandleB))
        {
            return false;
        }

        var bufferSize = 4096;
        var bufferA = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            var bufferB = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                var readSize = 0L;
                while(readSize < fileSize)
                {
                    var readA = System.IO.RandomAccess.Read(fileHandleA, bufferA, readSize);
                    var readB = System.IO.RandomAccess.Read(fileHandleB, bufferB, readSize);
                    if(readA != readB)
                    {
                        return false;
                    }
                    if (!bufferA.AsSpan(0, readA).SequenceEqual(bufferB.AsSpan(0, readB)))
                    {
                        return false;
                    }
                    readSize += readA;
                }
                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferB);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bufferA);
        }
    }

    public static bool CompareFiles(IEnumerable<string> fileListA, IEnumerable<string> fileListB)
    {
        var filesA = fileListA.ToArray();
        var filesB = fileListB.ToArray();
        if (filesA.Length != filesB.Length)
        {
            throw new ArgumentException($"SizeError : A:{filesA.Length} B:{filesB.Length}");
        }

        foreach (var index in Enumerable.Range(0, filesA.Length))
        {
            if (!CompareFile(filesA[index], filesB[index]))
            {
                return false;
            }
        }
        return true;
    }

    public static void DeleteFiles(params IEnumerable<string>[] files)
    {
        foreach (var fileEnumerable in files)
        {
            foreach (var file in fileEnumerable)
            {
                if (System.IO.File.Exists(file))
                {
                    System.IO.File.Delete(file);
                }
            }
        }
    }

}
