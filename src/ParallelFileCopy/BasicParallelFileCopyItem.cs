namespace ParallelFileCopy;

public class BasicParallelFileCopyItem : IParallelFileCopyItem
{
    /// <summary>
    /// Invalid FileSize
    /// </summary>
    public static readonly long InvalidSize = -1;
    /// <summary>
    /// Invalid DateTime
    /// </summary>
    public static readonly DateTimeOffset InvalidDateTime = DateTimeOffset.MinValue;

    /// <summary>
    /// SrcFile
    /// </summary>
    public string SrcFile { get; set; } = string.Empty;
    /// <summary>
    /// DstFile
    /// </summary>
    public string DstFile { get; set; } = string.Empty;

    /// <summary>
    /// Status
    /// </summary>
    public ParallelFileCopyStatus Status { get; set; } = ParallelFileCopyStatus.Init;

    /// <summary>
    /// SrcFileSize
    /// </summary>
    public long SrcFileSize { get; set; } = InvalidSize;
    /// <summary>
    /// CopyedSize
    /// </summary>
    public long CopyedSize { get; set; } = InvalidSize;
    /// <summary>
    /// Exception
    /// </summary>
    public Exception? Exception { get; set; } = null;

    /// <summary>
    /// StartDateTime
    /// </summary>
    public DateTimeOffset StartDateTime { get; set; } = InvalidDateTime;
    /// <summary>
    /// EndDateTime
    /// </summary>
    public DateTimeOffset EndDateTime { get; set; } = InvalidDateTime;

}
