using System.Threading.Channels;
using System.Xml.Linq;

namespace ParallelFileCopy.App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var root = args[0];
            var files = System.IO.Directory.GetFiles(root, "*", SearchOption.AllDirectories);

            var dst = files.Select(x => System.IO.Path.Combine(args[1], System.IO.Path.GetRelativePath(root, x)));
            //var channel = Channel.CreateUnbounded<ParallelFileCopyItem>(new UnboundedChannelOptions()
            //{
            //    SingleReader = true,
            //    SingleWriter = false,
            //    AllowSynchronousContinuations = false,
            //});
            //var cancellationTokenSource = new CancellationTokenSource();

            // Run
            await new BasicParallelFileCopyService().CopyFilesAsync(files.Zip(dst).Select(info =>
            {
                return new BasicParallelFileCopyItem()
                {
                    SrcFile = info.First,
                    DstFile = info.Second,
                };
            }),
            progress: info =>
            {
                if (info.EventItem != null)
                {
                    Console.WriteLine($"[{info.EventItem.Status.ToString()}] {info.EventItem.SrcFile}");
                }
            },
            cancellationToken: default);
        }
    }
}