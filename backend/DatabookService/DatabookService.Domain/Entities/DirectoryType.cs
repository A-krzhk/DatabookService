namespace DatabookService.Domain.Entities;

public class DirectoryType
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string TableName { get; private set; }
    public string? Description { get; private set; }
    
    private readonly List<DirectoryField> _fields = new();
    public IReadOnlyCollection<DirectoryField> Fields => _fields.AsReadOnly();

    private DirectoryType() { } // EF Core

    public DirectoryType(string name, string tableName, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be empty", nameof(tableName));
        
        if (!IsValidTableName(tableName))
            throw new ArgumentException("Invalid table name format", nameof(tableName));

        Id = Guid.NewGuid();
        Name = name;
        TableName = tableName;
        Description = description;
    }

    public void AddField(DirectoryField field)
    {
        if (_fields.Any(f => f.ColumnName == field.ColumnName))
            throw new InvalidOperationException($"Field with column name '{field.ColumnName}' already exists");
        
        _fields.Add(field);
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    private static bool IsValidTableName(string tableName)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            tableName, 
            @"^[a-zA-Z_][a-zA-Z0-9_]*$"
        );
    }
    
    public void RemoveField(Guid fieldId)
    {
        var field = _fields.FirstOrDefault(f => f.Id == fieldId);
        if (field == null)
            throw new InvalidOperationException($"Field with ID '{fieldId}' not found");
    
        _fields.Remove(field);
    }

    public DirectoryField? GetField(Guid fieldId)
    {
        return _fields.FirstOrDefault(f => f.Id == fieldId);
    }
}