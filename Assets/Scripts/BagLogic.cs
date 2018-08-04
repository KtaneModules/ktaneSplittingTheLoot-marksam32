using System;
using System.Collections.Generic;
using System.Linq;

public class BagLogic
{
    private static Random Rnd = new Random();

    private readonly IDictionary<string, int> LookupTable = new Dictionary<string, int>();
    private readonly int[][] Values = new[]
    {
        new[] {20, 19, 13, 26, 23, 34, 12, 14, 35, 16},
        new[] {10, 21, 13, 25, 24, 11, 11, 30, 19, 39},
        new[] {39, 38, 25, 30, 24, 23, 28, 34, 15, 36},
        new[] {14, 18, 33, 22, 31, 32, 22, 37, 36, 31},
        new[] {40, 20, 26, 12, 32, 33, 28, 15, 38, 17},
        new[] {19, 29, 18, 16, 17, 21, 35, 27, 27, 37}
    };

    private readonly string[] Letters = new[]
    {
        "A", "B", "C", "D", "E", "F", "G", "H", "I", "J"
    };

    private Bag[] bags;

    private IList<Pair<IList<Bag>, IList<Bag>>> solutionTuples;

    public BagLogic()
    {
        this.InitializeDictionary();
        this.InitializeBags();
    }

    public void Reinitialize()
    {
        this.InitializeBags();
    }

    public IList<Bag> GetBags()
    {
        return this.bags.ToList();
    }

    public IList<IList<Bag>> GetSolutions()
    {
        var solutions = new List<IList<Bag>>();
        foreach (var solutionTuple in this.solutionTuples)
        {
            var solution = new List<Bag>();
            foreach (var bag in bags)
            {
                var t = solutionTuple.Item1.Where(y => y.Label == bag.Label).FirstOrDefault();
                if (t == null)
                {
                    t = solutionTuple.Item2.Where(y => y.Label == bag.Label).FirstOrDefault();
                }

                solution.Add(t ?? new Bag
                {
                    Color = bag.Color,
                    Label = bag.Label,
                    Type = bag.Type,
                    Value = bag.Value
                });
            }

            solutions.Add(solution);
        }

        return solutions;
    }

    public IList<Pair<IList<Bag>, IList<Bag>>> GetSolutionAsTuples()
    {
        return this.solutionTuples;
    }

    public void GenerateSolutions()
    {
        this.solutionTuples = FindSolutionsForBags(this.bags);
        while (!this.solutionTuples.Any())
        {
            Reinitialize();
            this.solutionTuples = FindSolutionsForBags(this.bags);
        }
    }

    private void InitializeDictionary()
    {
        for (var x = 0; x < Letters.Length; x++)
        {
            for (var y = 0; y < Values.Length; y++)
            {
                var xy = y + 1;
                LookupTable.Add(Letters[x] + xy.ToString(), Values[y][x]);
            }
        }
    }

    private string GetRandomLabel()
    {
        return Letters[Rnd.Next(0, 10)] + Rnd.Next(1, 7);
    }

    private string GetDiamondBagLabel()
    {
        var label = GetRandomLabel();
        while (bags.Where(t => t != null).Any(t => label.Equals(t.Label)))
        {
            label = GetRandomLabel();
        }

        return label;
    }

    private int FindEmptyBagLocation()
    {
        var index = Rnd.Next(0, 7);
        while (bags[index] != null)
        {
            index = Rnd.Next(0, 7);
        }

        return index;
    }

    private void InitializeBags()
    {
        this.bags = new Bag[7];

        for (var x = 0; x < 3; x++)
        {
            var label = GetDiamondBagLabel();
            var index = FindEmptyBagLocation();

            bags[index] = new Bag
            {
                Type = BagType.Diamond,
                Label = label,
                Value = LookupTable[label],
                Index = index + 1
            };
        }

        for (var x = 0; x < 4; x++)
        {
            var index = FindEmptyBagLocation();
            var amount = Rnd.Next(1, 100);

            bags[index] = new Bag
            {
                Type = BagType.Money,
                Label = amount.ToString("D2"),
                Value = amount,
                Index = index + 1
            };
        }

        var colorRandom = Rnd.Next(0, 7);
        bags[colorRandom].Color = (BagColor)Rnd.Next(1, 3);
        bags[colorRandom].IsReadOnly = true;
    }

    private static IList<Pair<IList<Bag>, IList<Bag>>> FindSolutionsForBags(Bag[] bags)
    {
        var solutions = new List<Pair<IList<Bag>, IList<Bag>>>();
        var mandatory = bags.Where(x => x.Color != BagColor.Normal || x.Type == BagType.Diamond).ToList();
        var nonMandatory = bags.Where(x => x.Color == BagColor.Normal && x.Type == BagType.Money);

        var result = BagSetLogic.GetUniqueCombinations(mandatory.ToList());

        foreach (var tuple in result)
        {
            var subList1 = tuple.Item1;
            var subList2 = tuple.Item2;
            var sum1 = subList1.Sum(x => x.Value);
            var sum2 = subList2.Sum(x => x.Value);

            if (sum1 == sum2)
            {
                solutions.Add(new Pair<IList<Bag>, IList<Bag>>(subList1, subList2));
            }
        }

        var powerSet = BagSetLogic.GetPowerSet(nonMandatory.ToArray());

        foreach (var set in powerSet)
        {
            if (!set.Any())
            {
                continue;
            }

            var newList = new List<Bag>(mandatory);
            newList.AddRange(set.ToList());
            result = BagSetLogic.GetUniqueCombinations(newList.ToList());

            foreach (var tuple in result)
            {
                var subList1 = tuple.Item1;
                var subList2 = tuple.Item2;
                var sum1 = subList1.Sum(x => x.Value);
                var sum2 = subList2.Sum(x => x.Value);

                if (sum1 == sum2)
                {
                    solutions.Add(new Pair<IList<Bag>, IList<Bag>>(subList1, subList2));
                }
            }
        }

        var copy = new List<Pair<IList<Bag>, IList<Bag>>>();
        foreach (var solution in solutions)
        {
            var bagLeft = solution.Item1.Where(x => x.Color != BagColor.Normal).FirstOrDefault();
            var bagRight = solution.Item2.Where(x => x.Color != BagColor.Normal).FirstOrDefault();
            copy.Add(CreateBagPair(solution, bagLeft, bagRight));
        }

        return copy;
    }

    private static Pair<IList<Bag>, IList<Bag>> CreateBagPair(Pair<IList<Bag>, IList<Bag>> solution, Bag left, Bag right)
    {
        var leftColor = left != null ? left.Color : SwapColor(right.Color);
        var rightColor = SwapColor(leftColor);

        var leftBags = solution.Item1.Select(x => new Bag
        {
            Label = x.Label,
            Type = x.Type,
            Value = x.Value,
            Color = leftColor
        }).ToList();

        var rightBags = solution.Item2.Select(x => new Bag
        {
            Label = x.Label,
            Type = x.Type,
            Value = x.Value,
            Color = rightColor
        }).ToList();

        return new Pair<IList<Bag>, IList<Bag>>(leftBags, rightBags);
    }

    private static BagColor SwapColor(BagColor color)
    {
        return color == BagColor.Blue ? BagColor.Red : BagColor.Blue;
    }
}