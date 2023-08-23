namespace ParallelFileCopy.Test;

public class CopyFileTest
{
    private static readonly long[] Nums = { 0, 1, 64, 128, 256, 500, 1000, 5000 };
    private static readonly long[] Units = { 1, 1024, 1024 * 1024 };

    public static List<object[]> FileSizeArray()
    {
        var ret = new List<object[]>();
        foreach (var unit in Units)
        {
            foreach (var num in Nums)
            {
                ret.Add(new object[] { num * unit });
            }
        }
        return ret;
    }

    private BasicParallelFileCopyService copyService = new BasicParallelFileCopyService();

    [Theory]
    [MemberData(nameof(FileSizeArray))]
    public async Task CopyFile(long fileSize)
    {
        var srcFile = FileUtility.CreateRandomFile(fileSize);
        var dstFile = System.IO.Path.GetTempFileName();

        var item = new BasicParallelFileCopyItem()
        {
            SrcFile = srcFile,
            DstFile = dstFile,
        };

        await copyService.CopyFilesAsync(new[] { item }).ConfigureAwait(false);
        Assert.True(FileUtility.CompareFile(srcFile, dstFile));
        System.IO.File.Delete(dstFile);
        System.IO.File.Delete(srcFile);
    }

    [Fact]
    public async Task CopyFiles()
    {
        var itemList = new List<BasicParallelFileCopyItem>();
        foreach (var fileSize in new[] { 1024, 1024 * 1024, 1024 * 2 })
        {
            foreach (var _ in Enumerable.Range(0, 1024))
            {
                var srcFile = FileUtility.CreateRandomFile(fileSize);
                var dstFile = System.IO.Path.GetTempFileName();

                var item = new BasicParallelFileCopyItem()
                {
                    SrcFile = srcFile,
                    DstFile = dstFile,
                };
                itemList.Add(item);
            }
        }
        await copyService.CopyFilesAsync(itemList).ConfigureAwait(false);
        foreach(var item in itemList)
        {
            Assert.True(FileUtility.CompareFile(item.SrcFile, item.DstFile));
            System.IO.File.Delete(item.SrcFile);
            System.IO.File.Delete(item.DstFile);
        }
    }



    [Fact]
    public async Task CopyOverride()
    {
        var dstFile = System.IO.Path.GetTempFileName();
        foreach (var fileSize in new[] { 1024, 1024 * 1024, 1024 * 2 })
        {
            var srcFile = FileUtility.CreateRandomFile(fileSize);
            var item = new BasicParallelFileCopyItem()
            {
                SrcFile = srcFile,
                DstFile = dstFile,
            };
            await copyService.CopyFilesAsync(new[] { item }).ConfigureAwait(false);
            Assert.True(FileUtility.CompareFile(srcFile, dstFile));
            System.IO.File.Delete(srcFile);
        }
        System.IO.File.Delete(dstFile);
    }
}