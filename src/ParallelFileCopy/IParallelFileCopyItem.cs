namespace ParallelFileCopy;

public interface IParallelFileCopyItem
{
    public string SrcFile { get; set; }
    public string DstFile { get; set; }
    public long SrcFileSize { get; set; }
    public long CopyedSize { get; set; }
    public ParallelFileCopyStatus Status { get; set; }
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public Exception? Exception { get; set; }
}