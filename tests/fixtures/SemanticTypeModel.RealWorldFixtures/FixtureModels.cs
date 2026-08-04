using SemanticTypeModel.Abstractions.Model;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;
using RunState = SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

namespace SemanticTypeModel.RealWorldFixtures;

#pragma warning disable CS1591
public static class FixtureModels
{
    public static TypeSchemaModel CreateIntake()
    {
        ScalarTypeDefinition guid = Scalar<Guid>(ScalarKind.Guid);
        ScalarTypeDefinition text = Scalar<string>(ScalarKind.String);
        ScalarTypeDefinition number = Scalar<int>(ScalarKind.Integer);
        ScalarTypeDefinition boolean = Scalar<bool>(ScalarKind.Boolean);
        ScalarTypeDefinition character = Scalar<char>(ScalarKind.String);
        ScalarTypeDefinition date = Scalar<DateOnly>(ScalarKind.Date);
        ScalarTypeDefinition time = Scalar<TimeOnly>(ScalarKind.Time);
        ScalarTypeDefinition duration = Scalar<TimeSpan>(ScalarKind.Duration);
        ScalarTypeDefinition timestamp = Scalar<DateTimeOffset>(ScalarKind.DateTimeOffset);
        ScalarTypeDefinition uri = Scalar<Uri>(ScalarKind.String);
        EnumTypeDefinition importType = Enum<Intake.ImportType>();

        ObjectTypeDefinition delivery = Object<Intake.DeliveryContract>(EntityRole.ValueObject,
            [Property(nameof(Intake.DeliveryContract.PartnerCode), text.Id), Property(nameof(Intake.DeliveryContract.AgreementId), guid.Id)]);
        ObjectTypeDefinition schedule = Object<Intake.ScheduleContract>(EntityRole.ValueObject,
            [Property(nameof(Intake.ScheduleContract.StartDate), date.Id), Property(nameof(Intake.ScheduleContract.StartTime), time.Id), Property(nameof(Intake.ScheduleContract.Interval), duration.Id)]);
        ObjectTypeDefinition polling = Object<Intake.SourcePollingPolicy>(EntityRole.ValueObject,
            [Property(nameof(Intake.SourcePollingPolicy.Interval), duration.Id), Property(nameof(Intake.SourcePollingPolicy.LastSuccessfulPoll), timestamp.Id, required: false)]);
        ObjectTypeDefinition delimited = Object<Intake.CsvSourceSpecification>(EntityRole.ValueObject,
            [Property(nameof(Intake.CsvSourceSpecification.Location), uri.Id), Property(nameof(Intake.CsvSourceSpecification.Delimiter), character.Id)]);
        ObjectTypeDefinition structured = Object<Intake.XmlSourceSpecification>(EntityRole.ValueObject,
            [Property(nameof(Intake.XmlSourceSpecification.Location), uri.Id), Property(nameof(Intake.XmlSourceSpecification.RootElement), text.Id)]);
        ObjectTypeDefinition primaryApi = Object<Intake.PrimaryApiSource>(EntityRole.ValueObject,
            [Property(nameof(Intake.PrimaryApiSource.Endpoint), uri.Id), Property(nameof(Intake.PrimaryApiSource.Token), text.Id, required: false)]);
        ObjectTypeDefinition secondaryApi = Object<Intake.SecondaryApiSource>(EntityRole.ValueObject,
            [Property(nameof(Intake.SecondaryApiSource.Endpoint), uri.Id), Property(nameof(Intake.SecondaryApiSource.Token), text.Id, required: false)]);
        ObjectTypeDefinition normalization = Object<Intake.PostProcessingContract>(EntityRole.ValueObject,
            [Property(nameof(Intake.PostProcessingContract.Enabled), boolean.Id), Property(nameof(Intake.PostProcessingContract.Mode), text.Id)]);
        ObjectTypeDefinition derivedField = Object<Intake.DerivedProperty>(EntityRole.ValueObject,
            [Property(nameof(Intake.DerivedProperty.Name), text.Id), Property(nameof(Intake.DerivedProperty.Expression), text.Id, required: false)]);
        ArrayTypeDefinition derivedFields = Array<IReadOnlyList<Intake.DerivedProperty>>(derivedField.Id);

        ObjectTypeDefinition semanticBase = Object<Intake.Specification>(EntityRole.Entity,
            [Property(nameof(Intake.Specification.Id), guid.Id), Property(nameof(Intake.Specification.SchemaVersion), number.Id), Property(nameof(Intake.Specification.UpdatedAt), timestamp.Id), Property(nameof(Intake.Specification.ExtensionData), text.Id, ("schema.extensionData", "true"))], nameof(Intake.Specification.Id));
        ObjectTypeDefinition workflow = Object<Intake.WorkflowSpecification>(EntityRole.Entity,
            [Property(nameof(Intake.WorkflowSpecification.Id), guid.Id), Property(nameof(Intake.WorkflowSpecification.SchemaVersion), number.Id), Property(nameof(Intake.WorkflowSpecification.UpdatedAt), timestamp.Id), Property(nameof(Intake.WorkflowSpecification.ExtensionData), text.Id, ("schema.extensionData", "true"))], nameof(Intake.WorkflowSpecification.Id));
        ObjectTypeDefinition root = Object<Intake.ImportSpecification>(EntityRole.Entity,
            [Property(nameof(Intake.ImportSpecification.Id), guid.Id), Property(nameof(Intake.ImportSpecification.SchemaVersion), number.Id), Property(nameof(Intake.ImportSpecification.UpdatedAt), timestamp.Id), Property(nameof(Intake.ImportSpecification.ExtensionData), text.Id, ("schema.extensionData", "true")), Property(nameof(Intake.ImportSpecification.ImportType), importType.Id), Property(nameof(Intake.ImportSpecification.OptionalImportType), importType.Id, false), Property(nameof(Intake.ImportSpecification.DeliveryContract), delivery.Id, ("schema.ownedObject", "true")), Property(nameof(Intake.ImportSpecification.Schedule), schedule.Id, ("schema.ownedObject", "true")), Property(nameof(Intake.ImportSpecification.Polling), polling.Id, ("schema.ownedObject", "true")), ConditionalProperty(nameof(Intake.ImportSpecification.CsvSource), delimited.Id, nameof(Intake.ImportType.CsvFile)), ConditionalProperty(nameof(Intake.ImportSpecification.XmlSource), structured.Id, nameof(Intake.ImportType.XmlFile)), ConditionalProperty(nameof(Intake.ImportSpecification.WebService1Source), primaryApi.Id, nameof(Intake.ImportType.WebService1)), ConditionalProperty(nameof(Intake.ImportSpecification.WebService2Source), secondaryApi.Id, nameof(Intake.ImportType.WebService2)), Property(nameof(Intake.ImportSpecification.PostProcessing), normalization.Id, ("schema.ownedObject", "true")), Property(nameof(Intake.ImportSpecification.DerivedProperties), derivedFields.Id, ("schema.ownedCollection", "true"))], nameof(Intake.ImportSpecification.Id));

        ObjectTypeDefinition nonSemanticBase = Object<Intake.VersionedExtensibleObject>(EntityRole.Unspecified,
            [Property(nameof(Intake.VersionedExtensibleObject.SchemaVersion), number.Id), Property(nameof(Intake.VersionedExtensibleObject.ExtensionData), text.Id, ("schema.extensionData", "true"))]);
        ObjectTypeDefinition marker = Object(typeof(Intake.IConfigurationKind<>), EntityRole.Unspecified, []);
        ObjectTypeDefinition equatable = Object(typeof(IEquatable<>), EntityRole.Unspecified, []);
        ObjectTypeDefinition jsonHelper = Object(typeof(System.Text.Json.JsonElement), EntityRole.Unspecified, []);
        ObjectTypeDefinition xmlHelper = Object(typeof(System.Xml.XmlDocument), EntityRole.Unspecified, []);
        TypeDefinition[] types = [guid, text, number, boolean, character, date, time, duration, timestamp, uri, importType, delivery, schedule, polling, delimited, structured, primaryApi, secondaryApi, normalization, derivedField, derivedFields, semanticBase, root, workflow, nonSemanticBase, marker, equatable, jsonHelper, xmlHelper];
        return Model("ImportSpecificationModel", types);
    }

    public static TypeSchemaModel CreateRunState()
    {
        ScalarTypeDefinition guid = Scalar<Guid>(ScalarKind.Guid);
        ScalarTypeDefinition text = Scalar<string>(ScalarKind.String);
        ScalarTypeDefinition number = Scalar<int>(ScalarKind.Integer);
        ScalarTypeDefinition timestamp = Scalar<DateTimeOffset>(ScalarKind.DateTimeOffset);
        ScalarTypeDefinition runId = Scalar<RunState.FulfillmentRunId>(ScalarKind.String);
        ScalarTypeDefinition sourceId = Scalar<RunState.OrderSourceId>(ScalarKind.String);
        ScalarTypeDefinition executionId = Scalar<RunState.SourceExecutionId>(ScalarKind.String);
        ScalarTypeDefinition failureId = Scalar<RunState.ProcessingFailureId>(ScalarKind.String);
        ScalarTypeDefinition operationId = Scalar<RunState.ControlOperationId>(ScalarKind.String);
        ScalarTypeDefinition componentId = Scalar<RunState.ComponentSnapshotId>(ScalarKind.String);
        ScalarTypeDefinition binary = Scalar<ReadOnlyMemory<byte>>(ScalarKind.Binary);

        ObjectTypeDefinition statistics = Object<RunState.OrderSourceStatistics>(EntityRole.ValueObject,
            [Property(nameof(RunState.OrderSourceStatistics.Accepted), number.Id), Property(nameof(RunState.OrderSourceStatistics.Rejected), number.Id)]);
        ObjectTypeDefinition component = Object<RunState.ComponentStateEnvelope>(EntityRole.ValueObject,
            [Property(nameof(RunState.ComponentStateEnvelope.Id), componentId.Id), Property(nameof(RunState.ComponentStateEnvelope.Payload), binary.Id)]);
        ObjectTypeDefinition execution = Object<RunState.SourceExecutionRecord>(EntityRole.ValueObject,
            [Property(nameof(RunState.SourceExecutionRecord.Id), executionId.Id), Property(nameof(RunState.SourceExecutionRecord.StartedAt), timestamp.Id), Property(nameof(RunState.SourceExecutionRecord.CompletedAt), timestamp.Id, required: false)]);
        ObjectTypeDefinition failure = Object<RunState.ProcessingFailureRecord>(EntityRole.ValueObject,
            [Property(nameof(RunState.ProcessingFailureRecord.Id), failureId.Id), Property(nameof(RunState.ProcessingFailureRecord.Code), text.Id), Property(nameof(RunState.ProcessingFailureRecord.Message), text.Id)]);
        ObjectTypeDefinition operation = Object<RunState.ControlOperationRecord>(EntityRole.ValueObject,
            [Property(nameof(RunState.ControlOperationRecord.Id), operationId.Id), Property(nameof(RunState.ControlOperationRecord.Operation), text.Id), Property(nameof(RunState.ControlOperationRecord.RequestedAt), timestamp.Id)]);
        ArrayTypeDefinition executions = Array<IReadOnlyList<RunState.SourceExecutionRecord>>(execution.Id);
        ArrayTypeDefinition failures = Array<IReadOnlyList<RunState.ProcessingFailureRecord>>(failure.Id);
        ArrayTypeDefinition operations = Array<IReadOnlyList<RunState.ControlOperationRecord>>(operation.Id);
        DictionaryTypeDefinition labels = Dictionary<IReadOnlyDictionary<string, string>>(text.Id, text.Id);

        ObjectTypeDefinition root = Object<RunState.OrderFulfillmentRunSnapshot>(EntityRole.Entity,
            [Property(nameof(RunState.OrderFulfillmentRunSnapshot.Id), guid.Id), Property(nameof(RunState.OrderFulfillmentRunSnapshot.RunId), runId.Id), Property(nameof(RunState.OrderFulfillmentRunSnapshot.SourceId), sourceId.Id), Property(nameof(RunState.OrderFulfillmentRunSnapshot.Statistics), statistics.Id, ("schema.ownedObject", "true")), Property(nameof(RunState.OrderFulfillmentRunSnapshot.ComponentState), component.Id, ("schema.ownedObject", "true")), Property(nameof(RunState.OrderFulfillmentRunSnapshot.Executions), executions.Id, ("schema.ownedCollection", "true")), Property(nameof(RunState.OrderFulfillmentRunSnapshot.Failures), failures.Id, ("schema.ownedCollection", "true")), Property(nameof(RunState.OrderFulfillmentRunSnapshot.ControlOperations), operations.Id, ("schema.ownedCollection", "true")), Property(nameof(RunState.OrderFulfillmentRunSnapshot.RawPayload), binary.Id)], nameof(RunState.OrderFulfillmentRunSnapshot.Id));
        ObjectTypeDefinition request = Object<RunState.SaveFulfillmentRunRequest>(EntityRole.Unspecified, [Property(nameof(RunState.SaveFulfillmentRunRequest.Snapshot), root.Id)]);
        ObjectTypeDefinition overview = Object<RunState.FulfillmentRunOverview>(EntityRole.Unspecified, [Property(nameof(RunState.FulfillmentRunOverview.RunId), runId.Id), Property(nameof(RunState.FulfillmentRunOverview.FailureCount), number.Id)]);
        ObjectTypeDefinition repository = Object(typeof(RunState.IFulfillmentRunStateRepository), EntityRole.Unspecified, []);
        TypeDefinition[] types = [guid, text, number, timestamp, runId, sourceId, executionId, failureId, operationId, componentId, binary, statistics, component, execution, failure, operation, executions, failures, operations, labels, root, request, overview, repository];
        return Model("OrderFulfillmentRunStateModel", types);
    }

    public static Intake.ImportSpecification MinimalIntake()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            SchemaVersion = 1,
            UpdatedAt = DateTimeOffset.UtcNow,
            ImportType = Intake.ImportType.CsvFile,
            DeliveryContract = new("partner", Guid.NewGuid()),
            Schedule = new(DateOnly.FromDateTime(DateTime.UtcNow), TimeOnly.MinValue, TimeSpan.FromHours(1)),
            Polling = new(TimeSpan.FromMinutes(5), null),
            PostProcessing = new(false, "none"),
        };
    }

    public static RunState.OrderFulfillmentRunSnapshot MinimalRunState()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            RunId = new(Guid.NewGuid()),
            SourceId = new(Guid.NewGuid()),
            Statistics = new(0, 0),
            ComponentState = new() { Id = new(Guid.NewGuid()), Payload = new byte[] { 1, 2 } },
            RawPayload = new byte[] { 3, 4 },
        };
    }

    private static TypeSchemaModel Model(string id, TypeDefinition[] types)
    {
        return new() { Id = new(id), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static ScalarTypeDefinition Scalar<T>(ScalarKind kind)
    {
        return new() { Id = new(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = kind, Annotations = Clr(typeof(T)) };
    }

    private static EnumTypeDefinition Enum<T>() where T : struct, Enum
    {
        return new() { Id = new(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Enum, Nullability = Nullability.NonNullable, StorageKind = EnumStorageKind.String, Values = [.. System.Enum.GetNames<T>().Select(name => new EnumValueDefinition { Name = name, Value = name, Annotations = new() })], Annotations = Clr(typeof(T)) };
    }

    private static PropertyDefinition ConditionalProperty(string name, TypeId type, string member)
    {
        PropertyDefinition property = Property(name, type, false, ("schema.ownedObject", "true"));
        TypeId enumId = new(typeof(Intake.ImportType).FullName!);
        return property with { Constraints = new ConstraintSet { Conditional = [new ConditionalConstraint { TargetPropertyId = property.Id, SourcePropertyName = nameof(Intake.ImportSpecification.ImportType), SourcePropertyId = new(nameof(Intake.ImportSpecification.ImportType)), SourceTypeId = enumId, Operator = ConditionalConstraintOperator.Equals, Literal = new SemanticLiteral { Kind = SemanticLiteralKind.EnumMember, RawText = member, NormalizedText = member, TypeId = enumId, ClrTypeName = typeof(Intake.ImportType).FullName, Value = member, EnumTypeId = enumId, EnumMemberName = member } }] } };
    }

    private static ObjectTypeDefinition Object<T>(EntityRole role, IReadOnlyList<PropertyDefinition> properties, string? key = null)
    {
        return new() { Id = new(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = Clr(typeof(T)), Semantics = new() { Role = role, IsValueObject = role == EntityRole.ValueObject }, Properties = properties, Keys = key is null ? [] : [new KeyDefinition { Name = $"PK_{typeof(T).Name}", Kind = KeyKind.Primary, Properties = [new(new(key))], Annotations = new() }], Relationships = [] };
    }

    private static ObjectTypeDefinition Object(Type type, EntityRole role, IReadOnlyList<PropertyDefinition> properties)
    {
        return new() { Id = new(type.FullName!), Name = type.Name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = Clr(type), Semantics = new() { Role = role }, Properties = properties, Keys = [], Relationships = [] };
    }

    private static ArrayTypeDefinition Array<T>(TypeId itemType)
    {
        return new() { Id = new(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Array, Nullability = Nullability.NonNullable, ItemType = new(itemType), Annotations = Clr(typeof(T)) };
    }

    private static DictionaryTypeDefinition Dictionary<T>(TypeId keyType, TypeId valueType)
    {
        return new() { Id = new(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Dictionary, Nullability = Nullability.NonNullable, KeyType = new(keyType), ValueType = new(valueType), Annotations = Clr(typeof(T)) };
    }

    private static PropertyDefinition Property(string name, TypeId type, params (string Key, string Value)[] annotations)
    {
        return Property(name, type, true, annotations);
    }

    private static PropertyDefinition Property(string name, TypeId type, bool required, params (string Key, string Value)[] annotations)
    {
        return new() { Id = new(name), Name = name, Type = new(type), Cardinality = new() { IsRequired = required }, Mutability = Mutability.InitOnly, Constraints = new(), Annotations = new() { Items = [new Annotation { Key = new("dotnet.memberName"), Value = name, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }, .. annotations.Select(a => new Annotation { Key = new(a.Key), Value = a.Value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared })] } };
    }

    private static AnnotationBag Clr(Type type)
    {
        List<Annotation> items = [new Annotation { Key = new("dotnet.clrType"), Value = type.AssemblyQualifiedName, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared }];
        if (type.BaseType is { } baseType && baseType != typeof(object))
        {
            items.Add(new Annotation { Key = new("dotnet.baseType"), Value = baseType.FullName, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared });
        }
        return new() { Items = items };
    }
}
#pragma warning restore CS1591
