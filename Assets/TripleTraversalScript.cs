using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class TripleTraversalScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] ArrowBtnSels;
    public KMSelectable MiddleBtnSel;
    public TextMesh ScreenText;

    public GameObject[] WallDisplayObjs;
    public Material[] WallDisplayMats;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private static readonly string[][] _walls = new string[3][]
    {
        new string[4]{"YYYYYYYNYNYYNNNNYNYNNYYYNNYYYYYNYNNNNYNYNNNYNNNYN","NYYNNYYNYNNYYYYNNYNNYNNNNYNYNNYNNYYYYNYNYYNNYYYNY","NYNYYNNNNYNYNNYYYNNYYYYYNYNNNNYNYNNNYNNNYNYYYYYYY","YNYYNNYYNYNNYYYYNNYNNYNNNNYNYNNYNNYYYYNYNYYNNYYYN"},
        new string[4]{"YYYYYYYYYNYYNYNYNNYNNYNYNNYNNNNNYNNNYNNYNYYYNYNYN","NNNYNNYNYYNYYYNNNYNYYYYNNNNYNYYYNYYNYYNYYYNNNYNNY","YYNYYNYNYNNYNNYNYNNYNNNNNYNNNYNNYNYYYNYNYNYYYYYYY","YNNNYNNYNYYNYYYNNNYNYYYYNNNNYNYYYNYYNYYNYYYNNNYNN"},
        new string[4]{"YYYYYYYNYYYNNNYYNNNYNNNNNYYYNYYNNYYNYYNNNYYYYYNNN","NNYNYNYNNYNYYYNYYYNNYYNNNNNYNNYYNNYNNNYYYYNNNNYNY","NYYYNNNYYNNNYNNNNNYYYNYYNNYYNYYNNNYYYYYNNNYYYYYYY","YNNYNYNYNNYNYYYNYYYNNYYNNNNNYNNYYNNYNNNYYYYNNNNYN"}
    };
    private int[] _currentPositions = new int[3];
    private bool _isInsideMaze;
    private int _currentMaze;
    private static readonly string[] _dirNames = new string[4] { "UP", "RIGHT", "DOWN", "LEFT" };
    private int[] _mazeOrder = new int[3];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ArrowBtnSels.Length; i++)
            ArrowBtnSels[i].OnInteract += ArrowBtnPress(i);
        MiddleBtnSel.OnInteract += MiddleBtnPress;

        _mazeOrder = Enumerable.Range(0, 3).ToArray().Shuffle();
        tryAgain:
        for (int i = 0; i < _currentPositions.Length; i++)
            _currentPositions[i] = Rnd.Range(0, 49);
        if (_currentPositions.Distinct().Count() != 3)
            goto tryAgain;

        Debug.LogFormat("[Triple Traversal #{0}] Initial positions: {1}", _moduleId, _currentPositions.Select(c => GetCoord(c)).Join(", "));
        ShowWalls();
    }

    private KMSelectable.OnInteractHandler ArrowBtnPress(int dir)
    {
        return delegate ()
        {
            ArrowBtnSels[dir].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            //Debug.LogFormat("[Triple Traversal #{0}] Pressed {1}.", _moduleId, dir);
            Audio.PlaySoundAtTransform("Move", transform);
            if (_isInsideMaze)
            {
                if (_walls[_mazeOrder[_currentMaze]][dir].Substring(_currentPositions[_mazeOrder[_currentMaze]], 1) == "Y")
                {
                    Module.HandleStrike();
                    Debug.LogFormat("[Triple Traversal #{0}] While in Maze {1}, you attempted to travel {2} from {3}, but there was a wall. Strike.", _moduleId, "ABC"[_mazeOrder[_currentMaze]], _dirNames[dir], GetCoord(_currentPositions[_mazeOrder[_currentMaze]]));
                    _isInsideMaze = false;
                    ScreenText.text = "-";
                    _currentMaze = 0;
                }
                else
                {
                    MoveInMaze(dir);
                }
            }
            else
            {
                MoveInMaze(dir);
            }
            return false;
        };
    }

    private bool MiddleBtnPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, MiddleBtnSel.transform);
        MiddleBtnSel.AddInteractionPunch(0.5f);
        if (_moduleSolved)
            return false;
        if (!_isInsideMaze)
        {
            _isInsideMaze = true;
            ScreenText.text = "ABC"[_mazeOrder[_currentMaze]].ToString();
            Audio.PlaySoundAtTransform("Move", transform);
            Debug.LogFormat("[Triple Traversal #{0}] Started maze movement. Entering Maze {2}. Current positions: {1}.", _moduleId, _currentPositions.Select(c => GetCoord(c)).Join(", "), "ABC"[_mazeOrder[_currentMaze]]);
        }
        else
        {
            if (_currentPositions[_mazeOrder[_currentMaze]] == 24)
            {
                Debug.LogFormat("[Triple Traversal #{0}] Pressed the middle button at the center cell of Maze {1}.", _moduleId, "ABC"[_mazeOrder[_currentMaze]]);
                _currentMaze++;
                if (_currentMaze == 3)
                {
                    Debug.LogFormat("[Triple Traversal #{0}] Module solved.", _moduleSolved);
                    Audio.PlaySoundAtTransform("Solve", transform);
                    Module.HandlePass();
                    _moduleSolved = true;
                    ScreenText.text = "-";
                    for (int i = 0; i < 4; i++)
                        WallDisplayObjs[i].GetComponent<MeshRenderer>().material = WallDisplayMats[0];
                }
                else
                {
                    Debug.LogFormat("[Triple Traversal #{0}] Entering Maze {1}. Current positions: {2}.", _moduleId, "ABC"[_mazeOrder[_currentMaze]], _currentPositions.Select(c => GetCoord(c)).Join(", "));
                    ScreenText.text = "ABC"[_mazeOrder[_currentMaze]].ToString();
                    Audio.PlaySoundAtTransform("Correct", transform);
                }
            }
            else
            {
                Module.HandleStrike();
                _isInsideMaze = false;
                ScreenText.text = "-";
                Debug.LogFormat("[Triple Traversal #{0}] Pressed the middle button at {1} of Maze {2} instead of the center cell. Strike.", _moduleId, GetCoord(_currentPositions[_currentMaze]), "ABC"[_mazeOrder[_currentMaze]]);
                _currentMaze = 0;
            }
        }
        return false;
    }

    private void MoveInMaze(int dir)
    {
        for (int i = 0; i < 3; i++)
        {
            var c = _currentPositions[i] % 7;
            var r = _currentPositions[i] / 7;
            if (dir == 0)
                r = (r + 6) % 7;
            if (dir == 1)
                c = (c + 1) % 7;
            if (dir == 2)
                r = (r + 1) % 7;
            if (dir == 3)
                c = (c + 6) % 7;
            _currentPositions[i] = r * 7 + c;
        }
        ShowWalls();
    }

    private void ShowWalls()
    {
        var wallDisplays = new int[4];
        for (int wallIx = 0; wallIx < 4; wallIx++)
        {
            var count = 0;
            for (int mazeNum = 0; mazeNum < 3; mazeNum++)
            {
                if (_walls[mazeNum][wallIx].Substring(_currentPositions[mazeNum], 1) == "Y")
                    count++;
            }
            wallDisplays[wallIx] = count;
            WallDisplayObjs[wallIx].GetComponent<MeshRenderer>().material = WallDisplayMats[wallDisplays[wallIx]];
        }
    }

    private string GetCoord(int c)
    {
        return "ABCDEFG".Substring(c % 7, 1) + "1234567".Substring(c / 7, 1);
    }

#pragma warning disable 414
    string TwitchHelpMessage = "Use '!{0} [directions]' to press the buttons, using 'M' as the middle button. Commands can be chained. Example: '!{0} UDRL NSEW MC'";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*([urdlneswmc,; ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        foreach (var ch in m.Groups[1].Value)
        {
            var c = ch.ToString().ToLowerInvariant();
            if (c == " " || c == ";" || c == ",")
                continue;
            if (c == "n" || c == "u")
                ArrowBtnSels[0].OnInteract();
            else if (c == "e" || c == "r")
                ArrowBtnSels[1].OnInteract();
            else if (c == "s" || c == "d")
                ArrowBtnSels[2].OnInteract();
            else if (c == "w" || c == "l")
                ArrowBtnSels[3].OnInteract();
            else if (c == "m" || c == "c")
                MiddleBtnSel.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield break;
    }

    private static readonly string[][] _autoSolvePaths = new string[3][]
    {
        new string[49] { "drdrrdm", "ldrdrrdm", "drddm", "rrddldlm", "rddldlm", "ddldlm", "ddlldlm", "rdrrdm", "drrdm", "rddm", "ddm", "lddm", "dldlm", "dlldlm", "urdrrdm", "rrdm", "rdm", "dm", "dlm", "ldlm", "lldlm", "rrrm", "rrm", "rm", "m", "lm", "dllum", "ldllum", "ddrruruum", "lddrruruum", "llddrruruum", "um", "lum", "llum", "uldllum", "drruruum", "ulddrruruum", "ruum", "uum", "rullum", "ullum", "uuldllum", "rruruum", "ruruum", "uruum", "uuum", "urullum", "ruuuldllum", "uuuldllum" },
        new string[49] { "rrddrdm", "rddrdm", "ddrdm", "lddrdm", "rddldlm", "ddldlm", "lddldlm", "drrrdm", "ldrrrdm", "drdm", "ddm", "lddm", "dldlm", "ddlllm", "rrrdm", "rrdm", "rdm", "dm", "dlm", "ldlm", "dlllm", "druurrdm", "urrdm", "rm", "m", "lm", "llm", "lllm", "ruurrdm", "uurrdm", "urm", "um", "rullm", "ullm", "ulllm", "uruurrdm", "luruurrdm", "uurm", "uum", "luum", "uullm", "dlluluum", "rruuurm", "ruuurm", "uuurm", "luuurm", "uluum", "luluum", "lluluum" },
        new string[49] { "drrddrm", "ldrrddrm", "lldrrddrm", "rdlddm", "dlddm", "rddllulddm", "ddllulddm", "rrddrm", "rddrm", "ddrm", "ddm", "lddm", "urddllulddm", "dllulddm", "rdrrm", "drrm", "drm", "dm", "ulddm", "lulddm", "llulddm", "urdrrm", "rrm", "rm", "m", "lm", "llm", "lllm", "uurdrrm", "luurdrrm", "lluurdrrm", "um", "ulm", "lulm", "llulm", "rrruum", "rruum", "ruum", "uum", "uulm", "ululm", "dluululm", "rrrruuulm", "rrruuulm", "rruuulm", "ruuulm", "uuulm", "uululm", "luululm" }
    };

    private IEnumerator TwitchHandleForcedSolve()
    {
        if (!_isInsideMaze)
        {
            MiddleBtnSel.OnInteract();
            yield return true;
            yield return new WaitForSeconds(0.1f);
        }
        for (int st = _currentMaze; st < 3; st++)
        {
            var path = _autoSolvePaths[_mazeOrder[_currentMaze]][_currentPositions[_mazeOrder[_currentMaze]]];
            for (int pNum = 0; pNum < path.Length; pNum++)
            {
                var c = path[pNum].ToString();
                if (c == "n" || c == "u")
                    ArrowBtnSels[0].OnInteract();
                else if (c == "e" || c == "r")
                    ArrowBtnSels[1].OnInteract();
                else if (c == "s" || c == "d")
                    ArrowBtnSels[2].OnInteract();
                else if (c == "w" || c == "l")
                    ArrowBtnSels[3].OnInteract();
                else if (c == "m" || c == "c")
                    MiddleBtnSel.OnInteract();
                yield return true;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}