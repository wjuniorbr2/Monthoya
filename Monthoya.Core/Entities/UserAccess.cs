namespace Monthoya.Core.Entities;

[Flags]
public enum UserAccess
{
    None = 0,
    Dashboard = 1,
    Properties = 2,
    Contracts = 4,
    Financial = 8,
    Documents = 16,
    UserManagement = 32,
    Diagnostics = 64
}
