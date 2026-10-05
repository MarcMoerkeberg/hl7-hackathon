namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// Indicates the purpose of a bundle - how it is intended to be used.
/// </summary>
/// <remarks>Value set: <see href="https://hl7.org/fhir/R4/valueset-bundle-type.html">bundle-type (R4)</see>.</remarks>
public sealed record BundleType : FhirCode<BundleType>
{
    /// <summary>The bundle is a document. The first resource is a Composition.</summary>
    public static readonly BundleType Document = new("document");

    /// <summary>The bundle is a message. The first resource is a MessageHeader.</summary>
    public static readonly BundleType Message = new("message");

    /// <summary>The bundle is a transaction - intended to be processed by a server as an atomic commit.</summary>
    public static readonly BundleType Transaction = new("transaction");

    /// <summary>The bundle is a transaction response.</summary>
    public static readonly BundleType TransactionResponse = new("transaction-response");

    /// <summary>The bundle is a set of actions - intended to be processed by a server as a group of independent actions.</summary>
    public static readonly BundleType Batch = new("batch");

    /// <summary>The bundle is a batch response. Note that as a batch, some responses may indicate failure and others success.</summary>
    public static readonly BundleType BatchResponse = new("batch-response");

    /// <summary>The bundle is a list of resources from a history interaction on a server.</summary>
    public static readonly BundleType History = new("history");

    /// <summary>The bundle is a list of resources returned as a result of a search/query interaction, operation, or message.</summary>
    public static readonly BundleType Searchset = new("searchset");

    /// <summary>The bundle is a set of resources collected into a single package for ease of distribution.</summary>
    public static readonly BundleType Collection = new("collection");

    private BundleType(string code)
        : base(code)
    {
    }
}
