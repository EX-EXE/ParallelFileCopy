using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ParallelFileCopy;

public class BasicParallelFileCopyService : ParallelFileCopyService<BasicParallelFileCopyItem>
{
}