# ParallelFileCopy
Copy files within multiple tasks.  

# Quick Start
Install by nuget
PM> Install-Package [ParallelFileCopy](https://www.nuget.org/packages/ParallelFileCopy)

```csharp
var copyItems = new[]
{
    new BasicParallelFileCopyItem() { SrcFile = srcFile, DstFile = dstFile },
    new BasicParallelFileCopyItem() { SrcFile = srcFile2, DstFile = dstFile2 },
    new BasicParallelFileCopyItem() { SrcFile = srcFile3, DstFile = dstFile3 },
};
var copyService = new BasicParallelFileCopyService()
{
    ParallelCount = 1,
};
await copyService.CopyFilesAsync(copyItems).ConfigureAwait(false);
```
