using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

/// <summary>
/// Representa a cor identificadora de uma família no retiro.
/// Cada família deve ter uma cor única para facilitar identificação visual.
/// </summary>

public sealed class FamilyColor : ValueObject
{
    public string Name { get; }
    public string HexCode { get; }

    private FamilyColor() { } 

    private FamilyColor(string name, string hexCode)
    {
        Name = name;
        HexCode = hexCode;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name.ToLowerInvariant();
    }

    // Lista predefinida de cores disponíveis
    
    private static readonly List<FamilyColor> _availableColors = new()
    {
        new FamilyColor("Azul", "#2196F3"),
        new FamilyColor("Verde", "#4CAF50"),
        new FamilyColor("Vermelho", "#F44336"),
        new FamilyColor("Amarelo", "#FFEB3B"),
        new FamilyColor("Roxo", "#9C27B0"),
        new FamilyColor("Laranja", "#FF9800"),
        new FamilyColor("Rosa", "#E91E63"),
        new FamilyColor("Ciano", "#00BCD4"),
        new FamilyColor("Lima", "#CDDC39"),
        new FamilyColor("Índigo", "#3F51B5"),
        new FamilyColor("Âmbar", "#FFC107"),
        new FamilyColor("Teal", "#009688"),
        new FamilyColor("Marrom", "#795548"),
        new FamilyColor("Cinza", "#9E9E9E"),
        new FamilyColor("Azul Claro", "#03A9F4"),
        new FamilyColor("Verde Claro", "#8BC34A"),
        new FamilyColor("Vermelho Escuro", "#C62828"),
        new FamilyColor("Roxo Profundo", "#673AB7"),
        new FamilyColor("Coral", "#FF7043"),
        new FamilyColor("Turquesa", "#26C6DA"),
        new FamilyColor("Dourado", "#FFD700"),
        new FamilyColor("Prata", "#C0C0C0"),
        new FamilyColor("Salmão", "#FF8A80"),
        new FamilyColor("Lavanda", "#E1BEE7"),
        new FamilyColor("Pêssego", "#FFCCBC"),
        new FamilyColor("Verde Escuro", "#388E3C"),
        new FamilyColor("Azul Marinho", "#1565C0"),
        new FamilyColor("Magenta", "#D81B60"),
        new FamilyColor("Oliva", "#827717"),
        new FamilyColor("Bordô", "#880E4F"),
        new FamilyColor("Azul Aço", "#455A64"),
        new FamilyColor("Verde Musgo", "#558B2F"),
        new FamilyColor("Violeta", "#8E24AA"),
        new FamilyColor("Terracota", "#D84315"),
        new FamilyColor("Menta", "#80CBC4"),
        new FamilyColor("Laranja Queimado", "#E64A19"),
        new FamilyColor("Rosa Choque", "#F50057"),
        new FamilyColor("Verde Água", "#4DD0E1"),
        new FamilyColor("Mostarda", "#F9A825"),
        new FamilyColor("Bege", "#BCAAA4"),
        new FamilyColor("Azul Petróleo", "#006064"),
        new FamilyColor("Vinho", "#AD1457"),
        new FamilyColor("Esmeralda", "#00897B"),
        new FamilyColor("Carmim", "#C2185B"),
        new FamilyColor("Castanho", "#6D4C41"),
        new FamilyColor("Verde Limão", "#9E9D24"),
        new FamilyColor("Azul Céu", "#81D4FA"),
        new FamilyColor("Damasco", "#FFAB91"),
        new FamilyColor("Rosa Claro", "#F8BBD0"),
        new FamilyColor("Cinza Escuro", "#616161"),
        new FamilyColor("Verde Jade", "#26A69A"),
        new FamilyColor("Lilás", "#BA68C8"),
        new FamilyColor("Caramelo", "#A1887F"),
        new FamilyColor("Azul Elétrico", "#2979FF"),
        new FamilyColor("Verde Neon", "#76FF03"),
        new FamilyColor("Laranja Neon", "#FF6D00"),
        new FamilyColor("Rosa Salmão", "#FF5252"),
        new FamilyColor("Azul Gelo", "#B3E5FC"),
        new FamilyColor("Verde Menta", "#B9F6CA"),
        new FamilyColor("Sépia", "#5D4037")
    };

    public static IReadOnlyList<FamilyColor> AvailableColors => _availableColors.AsReadOnly();

    /// <summary>
    /// Cria uma cor a partir do nome predefinido.
    /// </summary>
    /// 
    public static FamilyColor FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da cor não pode ser vazio.", nameof(name));

        var normalized = name.Trim();
        var color = _availableColors.FirstOrDefault(c => 
            c.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (color is null)
            throw new ArgumentException($"Cor '{name}' não está disponível na lista predefinida.", nameof(name));

        return color;
    }

    /// <summary>
    /// Retorna uma cor aleatória da lista disponível.
    /// </summary>
    /// 
    public static FamilyColor GetRandom()
    {
        var random = new Random();
        var index = random.Next(_availableColors.Count);
        return _availableColors[index];
    }

    /// <summary>
    /// Retorna uma cor aleatória que não esteja na lista de cores já usadas.
    /// </summary>
    /// 
    public static FamilyColor GetRandomExcluding(IEnumerable<string> usedColorNames)
    {
        var usedSet = usedColorNames.Select(n => n.Trim().ToLowerInvariant()).ToHashSet();
        var available = _availableColors.Where(c => !usedSet.Contains(c.Name.ToLowerInvariant())).ToList();

        if (available.Count == 0)
            throw new InvalidOperationException("Não há cores disponíveis. Todas as cores já foram utilizadas.");

        var random = new Random();
        return available[random.Next(available.Count)];
    }

    public static implicit operator string(FamilyColor color) => color.Name;
    
    public override string ToString() => $"{Name} ({HexCode})";
}
