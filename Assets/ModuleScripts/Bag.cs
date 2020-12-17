public class Bag
{
    public BagType Type { get; set; }

    public BagColor Color { get; set; }

    public int Value { get; set; }

    public string Label { get; set; }

    public bool IsReadOnly {get; set; }

    public int Index { get; set; }
}

public enum BagType
{
    Money,
    Diamond
}

public enum BagColor
{
    Normal,
    Red,
    Blue
}

