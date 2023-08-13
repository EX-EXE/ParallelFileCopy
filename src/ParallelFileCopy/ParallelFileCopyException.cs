using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelFileCopy;

public class ParallelFileCopyException<TItem> : System.Exception
    where TItem : IParallelFileCopyItem
{
    public TItem Item { get; internal init; }
    public ParallelFileCopyException(string message, Exception ex, TItem item)
        : base(message, ex)
    {
        Item = item;
    }
}
