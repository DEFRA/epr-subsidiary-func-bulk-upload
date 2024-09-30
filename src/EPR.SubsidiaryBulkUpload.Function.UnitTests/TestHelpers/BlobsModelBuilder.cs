using Azure.Storage.Blobs.Models;

namespace EPR.SubsidiaryBulkUpload.Function.UnitTests.TestHelpers;

public static class BlobsModelBuilder
{
    public static BlobDownloadDetails CreateBlobDownloadDetails(int contentLength = 0) =>
        CreateBlobDownloadDetails(contentLength, new Dictionary<string, string>());

    public static BlobDownloadDetails CreateBlobDownloadDetails(int contentLength, IDictionary<string, string> metadata) =>
        BlobsModelFactory.BlobDownloadDetails(
            BlobType.Block,             // BlobType blobType,
            contentLength,              // long contentLength,
            "application/vnd.ms-excel", // string contentType,
            [],                         // byte[] contentHash,
            DateTime.UtcNow,            // DateTimeOffset lastModified,
            metadata,                   // IDictionary<string,string> metadata,
            null,                       // string contentRange,
            null,                       // string contentEncoding,
            null,                       // string cacheControl,
            null,                       // string contentDisposition,
            null,                       // string contentLanguage,
            0,                          // long blobSequenceNumber,
            DateTimeOffset.MinValue,    // DateTimeOffset copyCompletedOn,
            null,                       // string copyStatusDescription,
            null,                       // string copyId,
            null,                       // string copyProgress,
            null,                       // Uri copySource,
            CopyStatus.Pending,         // CopyStatus copyStatus,
            LeaseDurationType.Infinite, // LeaseDurationType leaseDuration,
            LeaseState.Available,       // LeaseState leaseState,
            LeaseStatus.Unlocked,       // LeaseStatus leaseStatus,
            "bytes",                    // string acceptRanges,
            0,                          // int blobCommittedBlockCount,
            true,                       // bool isServerEncrypted,
            null,                       // string encryptionKeySha256,
            null,                       // string encryptionScope,
            [],                         // byte[] blobContentHash,
            0,                          // long tagCount,
            null,                       // string versionId,
            false,                      // bool isSealed,
            null,                       // IList<ObjectReplicationPolicy> objectReplicationSourceProperties,
            null,                       // string objectReplicationDestinationPolicy,
            false,                      // bool hasLegalHold,
            DateTimeOffset.UtcNow);     // DateTimeOffset createdOn
}
