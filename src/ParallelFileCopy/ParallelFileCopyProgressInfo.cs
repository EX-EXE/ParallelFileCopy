namespace ParallelFileCopy;

public class ParallelFileCopyProgressInfo<TItem>
    where TItem : IParallelFileCopyItem
{
    public TItem[] AllItems { get; internal set; } = Array.Empty<TItem>();
    public TItem? EventItem { get; internal set; } = default;
}
