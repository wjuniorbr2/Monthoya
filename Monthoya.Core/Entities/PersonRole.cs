namespace Monthoya.Core.Entities;

[Flags]
public enum PersonRole
{
    Client = 1,
    Owner = 2,
    Tenant = 4
}
