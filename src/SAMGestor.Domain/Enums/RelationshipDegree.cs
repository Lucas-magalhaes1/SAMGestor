namespace SAMGestor.Domain.Enums;

[System.Flags]
public enum RelationshipDegree
{
    None      = 0,
    Boyfriend = 1 << 0, 
    Spouse    = 1 << 1, 
    Father    = 1 << 2, 
    Mother    = 1 << 3, 
    Friend    = 1 << 4, 
    Cousin    = 1 << 5, 
    Uncle     = 1 << 6, 
    Other     = 1 << 7  
}