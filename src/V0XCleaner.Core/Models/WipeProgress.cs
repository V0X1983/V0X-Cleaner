namespace V0XCleaner.Core.Models;

public sealed record WipeProgress(long BytesWritten, long TotalBytesEstimate, int CurrentPass, int TotalPasses);
