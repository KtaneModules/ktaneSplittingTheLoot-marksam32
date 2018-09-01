using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class SplittingTheLootScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable[] Bags;
    public KMSelectable SubmitBtn;

    public MeshRenderer[] BagsRend;
    public Material BagNormal;
    public Material BagRed;
    public Material BagBlue;
    public TextMesh[] BagsTxt;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool isSolved = false;
    private BagLogic bagLogic;

    private IList<Bag> bags;

    private static readonly Regex SetRegEx = new Regex("^set bag (([1-7]\\s?){1,7}) (red|blue|normal)$");


    // Use this for initialization
    void Start ()
    {
        Module.OnActivate += Activate;
    }

    void Activate()
    {
        _moduleId = _moduleIdCounter++;
        this.bagLogic = new BagLogic();
        this.bagLogic.Reinitialize();
        this.bagLogic.GenerateSolutions();

        this.bags = this.bagLogic.GetBags();
        var solutions = this.bagLogic.GetSolutions();

        for (var x = 0; x < bags.Count; x++)
        {
            var index = x;
            BagsTxt[index].text = bags[index].Label;
            BagsRend[index].material = ColorToMaterial(bags[index].Color);
            Bags[index].OnInteract += delegate
            {
                Bags[index].AddInteractionPunch();
                if (bags[index].Type == BagType.Diamond)
                {
                    Audio.PlaySoundAtTransform("DiamondSound", Bags[index].transform);
                }
                else
                {
                    Audio.PlaySoundAtTransform("MoneySound", Bags[index].transform);
                }

                if (this.isSolved == true)
                {
                    return false;
                }

                if (!bags[index].IsReadOnly)
                {
                    bags[index].Color = TransitionColor(bags[index].Color);
                    BagsRend[index].material = ColorToMaterial(bags[index].Color);
                }

                return false;
            };

        }

        SubmitBtn.OnInteract += delegate
        {
            SubmitBtn.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitBtn.transform);
            if(this.isSolved == true)
            {
                return false;
            }
            bool foundSolution = false;
            var redBags = this.bags.Where(x => x.Color == BagColor.Red).OrderBy(x => x.Index).ToList();
            var blueBags = this.bags.Where(x => x.Color == BagColor.Blue).OrderBy(x => x.Index).ToList(); ;

            foreach (var solution in solutions)
            {
                var redBagsSolution = solution.Where(x => x.Color == BagColor.Red).OrderBy(x => x.Index).ToList();
                var blueBagsSolution = solution.Where(x => x.Color == BagColor.Blue).OrderBy(x => x.Index).ToList();

                if (redBags.Count() == redBagsSolution.Count() && blueBags.Count() == blueBagsSolution.Count())
                {
                    if (ValidateAnswer(redBags, redBagsSolution) && ValidateAnswer(blueBags, blueBagsSolution))
                    {
                        foundSolution = true;
                        break;
                    }
                }
            }
            Debug.LogFormat("[Splitting The Loot #{0}] Submitted:", this._moduleId);
            Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);
            DebugLog(this.bags, _moduleId);
            Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);

            if (!foundSolution)
            {
                Debug.LogFormat("[Splitting The Loot #{0}] That is incorrect. Strike!", this._moduleId);
                Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);
                Module.HandleStrike();
            }
            else
            {
                this.isSolved = true;
                Debug.LogFormat("[Splitting The Loot #{0}] That is correct. Module solved!", this._moduleId);
                Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, SubmitBtn.transform);
                Module.HandlePass();
            }

            return false;
        };

        Debug.LogFormat("[Splitting The Loot #{0}] Found solutions to:\n", _moduleId);
        Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);
        DebugLog(bags, _moduleId);
        Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);
        Debug.LogFormat("[Splitting The Loot #{0}] Solutions:\n", _moduleId);
        Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);
        foreach (var solution in solutions)
        {
            DebugLog(solution, _moduleId);
            Debug.LogFormat("[Splitting The Loot #{0}] -------------------------------------------", _moduleId);
        }
    }

    private static bool ValidateAnswer(List<Bag> answer, List<Bag> solution)
    {
        for (int x = 0; x < answer.Count(); ++x)
        {
            if (answer[x].Label != solution[x].Label)
            {
                return false; 
            }
        }

        return true;
    }

    // Update is called once per frame
    void Update ()
    {	
	}

    private Material ColorToMaterial(BagColor color)
    {
        switch (color)
        {
            case BagColor.Blue:
                return BagBlue;
            case BagColor.Red:
                return BagRed;
            default:
                return BagNormal;
        }
    }

    private BagColor TransitionColor(BagColor color)
    {
        if (color == BagColor.Normal)
        {
            return BagColor.Red;
        }

        if (color == BagColor.Red)
        {
            return BagColor.Blue;
        }

        return BagColor.Normal;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "Set the color of a bag using !{0} set bag 5 red, !{0} set bag 3 8 2 blue or !{0} set bag 1 normal, etc. The bags are numbered in reading order. Submit your answer using !{0} submit.";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        if (command.Equals("submit"))
        {
            yield return null;
            SubmitBtn.OnInteract();
            yield break;
        }

        var match = SetRegEx.Match(command);
        if (match.Success)
        {
            var selectables = new List<KMSelectable>();
            var numbers = match.Groups[1].Value.Split(' ').Select(x => int.Parse(x)).ToList();
            var color = StringToColor(match.Groups[3].Value);

            if (numbers.Any(x => x < 1 || x > 7))
            {
                yield return string.Format("sendtochaterror Check your command! I don't understand it!");
                yield break;
            }

            if (numbers.Distinct().Count() != numbers.Count())
            {
                // Duplicate numbers.
                yield return string.Format("sendtochaterror No duplicate numbers allowed in a command!");
                yield break;
            }

            if (this.bags.Any(x => x.IsReadOnly && numbers.Contains(x.Index)))
            {
                // Trying to manipulate read only bag.
                yield return string.Format("sendtochaterror Don't try to cheat! You are not allowed to change the color of the initially colored bag!");
                yield break;
            }

            foreach (var number in numbers)
            {
                var noOfClicks = FindNumberOfClicks(this.bags[number - 1].Color, color);
                for (int i = 0; i < noOfClicks; ++i)
                {
                    selectables.Add(Bags[number - 1]);
                }
            }

            foreach (var selectable in selectables)
            {
                yield return null;
                selectable.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            yield return string.Format("sendtochaterror Check your command! I don't understand it!");
        }

        yield break;
    }

    private static int FindNumberOfClicks(BagColor currentColor, BagColor wantedColor)
    {
        switch (currentColor)
        {
            case BagColor.Normal:
                return wantedColor == BagColor.Red ? 1 : (wantedColor == BagColor.Blue ? 2 : 0);
            case BagColor.Red:
                return wantedColor == BagColor.Blue ? 1 : (wantedColor == BagColor.Normal ? 2 : 0);
            case BagColor.Blue:
                return wantedColor == BagColor.Normal ? 1 : (wantedColor == BagColor.Red ? 2 : 0);
            default:
                throw new ArgumentException("Unknown color " + currentColor);
        }
    }

    private static void DebugLog(IList<Bag> bags, int modId)
    {        
        Debug.LogFormat("[Splitting The Loot #{0}]\t\t{1}\n", modId, ToLabelValueString(bags.Take(1)));
        Debug.LogFormat("[Splitting The Loot #{0}]\t\t{1}\n", modId, ToColorString(bags.Take(1)));
        Debug.LogFormat("[Splitting The Loot #{0}]\t{1}\n", modId, ToLabelValueString(bags.Skip(1).Take(3)));
        Debug.LogFormat("[Splitting The Loot #{0}]\t{1}\n", modId, ToColorString(bags.Skip(1).Take(3)));
        Debug.LogFormat("[Splitting The Loot #{0}]\t{1}\n", modId, ToLabelValueString(bags.Skip(4).Take(3)));
        Debug.LogFormat("[Splitting The Loot #{0}]\t{1}\n", modId, ToColorString(bags.Skip(4).Take(3)));
    }

    private static string ToLabelValueString(IEnumerable<Bag> bags)
    {
        return string.Join(",   ", bags.Select(x => string.Format("{0}({1})", x.Label, x.Value)).ToArray());
    }
    private static string ToColorString(IEnumerable<Bag> bags)
    {
        return string.Join(",   ", bags.Select(x => x.Color.ToString()).ToArray());
    }

    private BagColor StringToColor(string color)
    {
        switch (color.ToUpperInvariant())
        {
            case "RED":
                return BagColor.Red;
            case "BLUE":
                return BagColor.Blue;
            case "NORMAL":
                return BagColor.Normal;
            default:
                throw new ArgumentException("Unknown bag color " + color);
        }
    }
}