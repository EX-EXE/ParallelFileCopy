using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ParallelFileCopy;

public class ParallelFileCopyService<TItem>
    where TItem : IParallelFileCopyItem
{
    private static readonly int DefaultCacheSize = 1024 * 1024;
    public int CacheSize { get; set; } = DefaultCacheSize;
    public int ParallelCount { get; set; } = Environment.ProcessorCount;


    public async ValueTask CopyFilesAsync(
        IEnumerable<TItem> items,
        Action<ParallelFileCopyProgressInfo<TItem>>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ParallelCount <= 0)
        {
            ParallelCount = Environment.ProcessorCount;
        }
        if (CacheSize <= 0)
        {
            CacheSize = DefaultCacheSize;
        }

        // Fetch FileInfo
        var allItemList = new List<TItem>();
        var copyQueue = Channel.CreateUnbounded<TItem>(new UnboundedChannelOptions()
        {
            AllowSynchronousContinuations = false,
            SingleReader = false,
            SingleWriter = true,
        });
        foreach (var item in items)
        {
            var fileInfo = new FileInfo(item.SrcFile);
            item.SrcFileSize = fileInfo.Length;

            allItemList.Add(item);
            if (!copyQueue.Writer.TryWrite(item))
            {
                throw new InvalidOperationException($"Error WriteQueue. : {item.SrcFile}");
            }
        }

        // Progress
        var allItems = allItemList.ToArray();
        var invokeProgress = (TItem item) =>
        {
            progress?.Invoke(new ParallelFileCopyProgressInfo<TItem>()
            {
                AllItems = allItems,
                EventItem = item,
            });
        };

        // Copy
        var copyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var copyCancellationToken = copyCancellationTokenSource.Token;
        var exceptionList = new ConcurrentBag<Exception>();
        var tasks = Enumerable.Range(0, ParallelCount)
            .Select(_ =>
            {
                return Task.Run(() =>
                {
                    var cache = ArrayPool<byte>.Shared.Rent(CacheSize);
                    try
                    {
                        var parallelCopyItem = default(TItem);
                        while (copyQueue.Reader.TryRead(out parallelCopyItem))
                        {
                            try
                            {
                                parallelCopyItem.StartDateTime = DateTimeOffset.Now;
                                parallelCopyItem.Status = ParallelFileCopyStatus.Init;
                                invokeProgress.Invoke(parallelCopyItem);

                                copyCancellationToken.ThrowIfCancellationRequested();
                                if (!CompareFile(parallelCopyItem, copyCancellationToken))
                                {
                                    // Init
                                    DeleteFile(parallelCopyItem.DstFile);
                                    CreateDirectory(parallelCopyItem.DstFile);

                                    // Start
                                    parallelCopyItem.Status = ParallelFileCopyStatus.Copying;
                                    invokeProgress.Invoke(parallelCopyItem);

                                    // Copy
                                    var copyException = default(Exception);
                                    try
                                    {
                                        OnBeforeFileCopy(parallelCopyItem, copyCancellationToken);
                                        RunFileCopy(parallelCopyItem, cache, copyCancellationToken);
                                    }
                                    catch (Exception ex)
                                    {
                                        copyException = ex;
                                        // Delete ErrorFile
                                        try
                                        {
                                            DeleteFile(parallelCopyItem.DstFile);
                                        }
                                        catch { } // Nothing
                                        // ReThrow
                                        throw;
                                    }
                                    finally
                                    {
                                        OnAfterFileCopy(parallelCopyItem, copyException, copyCancellationToken);
                                    }
                                }
                                else
                                {
                                    OnSkipFileCopy(parallelCopyItem, copyCancellationToken);
                                }

                                // End
                                parallelCopyItem.Status = ParallelFileCopyStatus.Success;
                                invokeProgress.Invoke(parallelCopyItem);
                            }
                            catch (Exception ex)
                            {
                                parallelCopyItem.Exception = ex;
                                if (ex is OperationCanceledException)
                                {
                                    // Cancel
                                    parallelCopyItem.Status = ParallelFileCopyStatus.Cancel;
                                    var exception = new ParallelFileCopyException<TItem>(
                                        $"Cancel FileCopy : {parallelCopyItem.SrcFile} => {parallelCopyItem.DstFile}",
                                        ex,
                                        parallelCopyItem);
                                    exceptionList.Add(exception);
                                    OnCancelFileCopy(parallelCopyItem);
                                }
                                else
                                {
                                    // Error
                                    parallelCopyItem.Status = ParallelFileCopyStatus.Fail;
                                    var exception = new ParallelFileCopyException<TItem>(
                                        $"Error FileCopy : {parallelCopyItem.SrcFile} => {parallelCopyItem.DstFile}",
                                        ex,
                                        parallelCopyItem);
                                    exceptionList.Add(exception);
                                    copyCancellationTokenSource.Cancel();
                                    OnErrorFileCopy(parallelCopyItem, ex);
                                }
                            }
                            finally
                            {
                                parallelCopyItem.EndDateTime = DateTimeOffset.Now;
                                invokeProgress.Invoke(parallelCopyItem);
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(cache);
                    }
                }, copyCancellationToken);
            });
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Exception
        if (!exceptionList.IsEmpty)
        {
            throw new AggregateException(exceptionList);
        }
    }

    protected virtual bool CompareFile(TItem item, CancellationToken cancellationToken)
    {
        return false;
    }
    protected virtual void OnBeforeFileCopy(TItem item, CancellationToken cancellationToken)
    {
    }
    protected virtual void OnAfterFileCopy(TItem item, Exception? exception, CancellationToken cancellationToken)
    {
    }
    protected virtual void OnSkipFileCopy(TItem item, CancellationToken cancellationToken)
    {
    }
    protected virtual void OnSuccessFileCopy(TItem item, CancellationToken cancellationToken)
    {
    }
    protected virtual void OnCancelFileCopy(TItem item)
    {
    }
    protected virtual void OnErrorFileCopy(TItem item, Exception exception)
    {
    }

    /// <summary>
    /// RunFileCopy
    /// </summary>
    /// <param name="parallelCopyItem"></param>
    /// <param name="cache"></param>
    /// <param name="cancellationToken"></param>
    protected virtual void RunFileCopy(TItem parallelCopyItem, Span<byte> cache, CancellationToken cancellationToken)
    {
        using (var srcStream = new FileStream(
            parallelCopyItem.SrcFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            1,
            false))
        {
            using (var dstStream = new FileStream(
                parallelCopyItem.DstFile,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                1,
                false))
            {
                dstStream.SetLength(srcStream.Length);
                while (srcStream.Position < srcStream.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var readSize = srcStream.Read(cache);
                    dstStream.Write(cache.Slice(0, readSize));
                    parallelCopyItem.CopyedSize += readSize;
                }
            }
        }
    }

    /// <summary>
    /// CreateDirectory
    /// </summary>
    /// <param name="path"></param>
    private static void CreateDirectory(ReadOnlySpan<char> path)
    {
        var filePath = path.ToString();
        var parentDir = Path.GetDirectoryName(filePath);
        if (parentDir != null && !Directory.Exists(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }
    }

    /// <summary>
    /// DeleteFile
    /// </summary>
    /// <param name="path"></param>
    private static void DeleteFile(ReadOnlySpan<char> path)
    {
        var filePath = path.ToString();
        if (File.Exists(filePath))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }
    }
}