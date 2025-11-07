namespace DatabookService.Domain.Entities;

public class DirectoryGroup
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private DirectoryGroup() { } // EF Core

    public DirectoryGroup(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        Name = name;
    }

    public static DirectoryGroup Default =>
        new DirectoryGroup("Без группы");
}