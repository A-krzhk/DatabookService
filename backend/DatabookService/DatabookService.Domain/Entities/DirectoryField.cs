using DatabookService.Domain.Enums;

namespace DatabookService.Domain.Entities;


public class DirectoryField
{
    public Guid Id { get; private set; }
    public Guid DirectoryTypeId { get; private set; }
    public string Name { get; private set; }
    public string ColumnName { get; private set; }
    public FieldDataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public Guid? ReferenceDirectoryTypeId { get; private set; }
    public int Order { get; private set; }
    
    public bool IsCollection { get; private set; }
    public List<string>? EnumValues { get; set; } = new List<string>();

    public DirectoryType DirectoryType { get; private set; } = null!;
    public DirectoryType? ReferenceDirectoryType { get; private set; }

    private DirectoryField() { } // EF Core

    public DirectoryField(
        Guid directoryTypeId,
        string name,
        string columnName,
        FieldDataType dataType,
        bool isRequired,
        int order,
        bool isCollection,
        Guid? referenceDirectoryTypeId = null,
        List<string>? enumValues = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentException("Column name cannot be empty", nameof(columnName));
        
        if (dataType == FieldDataType.Reference && !referenceDirectoryTypeId.HasValue)
            throw new ArgumentException("Reference directory type must be specified for Reference data type");

        Id = Guid.NewGuid();
        DirectoryTypeId = directoryTypeId;
        Name = name;
        ColumnName = columnName;
        DataType = dataType;
        IsRequired = isRequired;
        Order = order;
        IsCollection = isCollection;
        ReferenceDirectoryTypeId = referenceDirectoryTypeId;
        EnumValues = enumValues;
    }
    
    public void Update(string name, int order, bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
    
        // Можно сделать обязательное поле необязательным, но не наоборот
        if (!IsRequired && isRequired)
            throw new InvalidOperationException("Cannot change non-required field to required");
    
        Name = name;
        Order = order;
        IsRequired = isRequired;
    }
}