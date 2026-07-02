using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.Generated;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.Generated", "AppSemanticTypeModel")]

namespace SemanticTypeModel.Samples.OrderFulfillment.Domain;

public static class OrderFulfillmentSemanticModel
{
    public static TypeSchemaModel Create() => AppSemanticTypeModel.Create();
}

[SemanticType(SemanticTypeRole.Entity, Name = "Customer")]
[SemanticDescription("Customer account shared by fulfillment projections.")]
public sealed class Customer
{
    [SemanticKey]
    public required string CustomerId { get; init; }
    public required string DisplayName { get; init; }
    public required string EmailAddress { get; init; }
    public string? LoyaltyTier { get; init; }
    [SemanticOwned]
    public Address BillingAddress { get; init; } = new() { Line1 = string.Empty, City = string.Empty, CountryCode = string.Empty };
    public DateTimeOffset? LastContactedAt { get; init; }
}

[SemanticType(SemanticTypeRole.Entity, Name = "Product")]
public sealed class Product
{
    [SemanticKey]
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal? PromotionalPrice { get; init; }
    public bool IsHazardous { get; init; }
}

[SemanticType(SemanticTypeRole.Entity, Name = "Order")]
public sealed class Order
{
    [SemanticKey]
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public OrderStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public DateTimeOffset? PromisedAt { get; init; }
    public Guid? PromotionId { get; init; }
}

[SemanticType(SemanticTypeRole.Entity, Name = "OrderLine")]
public sealed class OrderLine
{
    [SemanticKey(Name = "PK_OrderLine", Order = 0)]
    public required string OrderId { get; init; }
    [SemanticKey(Name = "PK_OrderLine", Order = 1)]
    public int LineNumber { get; init; }
    public required string Sku { get; init; }
    public int Quantity { get; init; }
    public int? BackorderedQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal? LineDiscount { get; init; }
}

[SemanticType(SemanticTypeRole.Entity, Name = "Warehouse")]
public sealed class Warehouse
{
    [SemanticKey]
    public required string WarehouseId { get; init; }
    public required string Name { get; init; }
    [SemanticOwned]
    public Address Location { get; init; } = new() { Line1 = string.Empty, City = string.Empty, CountryCode = string.Empty };
    public bool? SupportsColdStorage { get; init; }
}

[SemanticType(SemanticTypeRole.Entity, Name = "Shipment")]
public sealed class Shipment
{
    [SemanticKey]
    public required string ShipmentId { get; init; }
    public required string OrderId { get; init; }
    public ShipmentStatus Status { get; init; }
    public DateTime? PackedOn { get; init; }
    public long? CarrierTrackingNumber { get; init; }
}

[SemanticType(SemanticTypeRole.ValueObject, Name = "Address")]
public sealed class Address
{
    public required string Line1 { get; init; }
    public string? Line2 { get; init; }
    public required string City { get; init; }
    public required string CountryCode { get; init; }
    public string? PostalCode { get; init; }
}

public enum OrderStatus { Draft, Submitted, Fulfilled, Cancelled }
public enum ShipmentStatus { Pending, Packed, Shipped, Delivered, Exception }
public enum FulfillmentMode { Standard, Expedited, ColdChain }

[SemanticEnvelope("order-submitted")]
[SemanticType(SemanticTypeRole.Entity, Name = "OrderSubmitted")]
public sealed class OrderSubmitted
{
    [SemanticKey]
    public Guid EventId { get; init; }
    [SemanticEnvelopeMetadata]
    public DateTimeOffset OccurredAt { get; init; }
    [SemanticEnvelopePayload]
    public required OrderSubmittedPayload Payload { get; init; }
}

public sealed class OrderSubmittedPayload
{
    public required string OrderId { get; init; }
    public required string CustomerId { get; init; }
    public decimal? InitialDiscount { get; init; }
}

[SemanticConfigurationSection("orderProcessing", Presence = SemanticConfigurationSectionPresence.Required)]
[SemanticValidateDataAnnotations]
[SemanticValidateOnStart]
[SemanticType(SemanticTypeRole.Configuration, Name = "OrderProcessingOptions")]
public sealed class OrderProcessingOptions
{
    public required string QueueName { get; init; }
    public int MaxConcurrentOrders { get; init; }
    public FulfillmentMode Mode { get; init; }
    public decimal? ExpeditedSurcharge { get; init; }
    [SemanticRequiredWhen(nameof(Mode), nameof(FulfillmentMode.ColdChain))]
    public string? ColdChainProvider { get; init; }
}

[SemanticConfigurationSection("coldStorage")]
[SemanticType(SemanticTypeRole.Configuration, Name = "ColdStorageOptions")]
public sealed class ColdStorageOptions
{
    public bool Enabled { get; init; }
    public decimal? MinimumTemperatureCelsius { get; init; }
}

[SemanticConfigurationSection("notifications")]
[SemanticType(SemanticTypeRole.Configuration, Name = "NotificationOptions")]
public sealed class NotificationOptions
{
    public string? SenderEmail { get; init; }
}

[SemanticType(SemanticTypeRole.Entity, Name = "ProjectionProbe")]
public sealed class ProjectionProbe
{
    public int? OptionalInt { get; init; }
    public long? OptionalLong { get; init; }
    public decimal? OptionalDecimal { get; init; }
    public bool? OptionalBool { get; init; }
    public DateTime? OptionalDateTime { get; init; }
    public DateTimeOffset? OptionalDateTimeOffset { get; init; }
    public Guid? OptionalGuid { get; init; }
    [SemanticAnnotation("efCore.enumStorage", "Numeric")]
    public OrderStatus? OptionalStatus { get; init; }
    public int RequiredInt { get; init; }
    public required string RequiredText { get; init; }
    public string? OptionalText { get; init; }
}
