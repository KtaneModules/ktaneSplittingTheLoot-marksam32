using System.Collections.Generic;
using System.Linq;

public static class BagSetLogic
{
    public static Bag[][] GetPowerSet<Bag>(Bag[] list)
    {
        var powerSet = new Bag[1 << list.Length][];
        powerSet[0] = new Bag[0];
        for (int i = 0; i < list.Length; i++)
        {
            var cur = list[i];
            int count = 1 << i;
            for (int j = 0; j < count; j++)
            {
                var source = powerSet[j];
                var destination = powerSet[count + j] = new Bag[source.Length + 1];
                for (int q = 0; q < source.Length; q++)
                {
                    destination[q] = source[q];
                }
                destination[source.Length] = cur;
            }
        }
        return powerSet;
    }

    public static IList<Pair<IList<Bag>, IList<Bag>>> GetUniqueCombinations(List<Bag> bags)
    {
        var result = new List<Pair<IList<Bag>, IList<Bag>>>();
        GetUniqueCombinations(result, bags, new List<Bag>(), bags);

        return result;
    }

    private static void GetUniqueCombinations(IList<Pair<IList<Bag>, IList<Bag>>> results, IList<Bag> orig, IList<Bag> prefix, IList<Bag> src)
    {
        if (src.Count > 0)
        {
            var prefixCopy = new List<Bag>(prefix); //create a copy to not modify the orig
            var srcCopy = new List<Bag>(src); //copy
            var curr = srcCopy[0];
            srcCopy.RemoveAt(0);

            AddIfNewCombination(results, orig, prefixCopy, curr); // <-- this is the only thing that shouldn't appear in a "normal" combinations method, and which makes it print the list-pairs
            GetUniqueCombinations(results, orig, prefixCopy, srcCopy); // recurse without curr
            prefixCopy.Add(curr);
            GetUniqueCombinations(results, orig, prefixCopy, srcCopy); // recurse with curr
        }
    }

    // print the prefix+curr, as one list, and initial-(prefix+curr) as a second list
    private static void AddIfNewCombination(IList<Pair<IList<Bag>, IList<Bag>>> results, IList<Bag> orig, IList<Bag> prefix, Bag curr)
    {
        var prefixCopy = new List<Bag>(prefix); //copy
        prefixCopy.Add(curr);
        var next = Subtract(orig, prefixCopy);
        if (prefixCopy.Any() && next.Any())
        {
            bool found = false;
            foreach (var result in results)
            {
                var left = next.Except(result.Item1).ToList();
                var right = result.Item1.Except(next).ToList();
                found = !left.Any() && !right.Any();
                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                results.Add(new Pair<IList<Bag>, IList<Bag>>(prefixCopy, next));
            }
        }
    }

    private static IList<Bag> Subtract(IList<Bag> orig, IList<Bag> prefix)
    {
        var copy = new List<Bag>(orig);
        copy.RemoveAll(x => prefix.Contains(x));
        return copy;
    }
}

