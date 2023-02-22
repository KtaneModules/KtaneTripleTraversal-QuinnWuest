using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

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
    private static readonly string[] _dirNames = new string[4] { "UP", "RIGHT", "DOWN", "LEFT" };
    private int[] _currentPositions = new int[3];
    private bool _isInsideMaze;
    private int _currentMaze;
    private int[] _mazeOrder = new int[3];

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ArrowBtnSels.Length; i++)
            ArrowBtnSels[i].OnInteract += ArrowBtnPress(i);
        MiddleBtnSel.OnInteract += MiddleBtnPress;

        _mazeOrder = Enumerable.Range(0, 3).ToArray().Shuffle();
        _currentPositions = Enumerable.Range(0, 49).ToArray().Shuffle().Take(3).ToArray();

        Debug.LogFormat("[Triple Traversal #{0}] Initial positions: {1}", _moduleId, _currentPositions.Select(c => GetCoord(c)).Join(", "));
        Debug.LogFormat("[Triple Traversal #{0}] Order of mazes: {1}.", _moduleId, _mazeOrder.Select(i => "ABC"[i]).ToArray().Join(", "));
        ShowWalls();
    }

    private KMSelectable.OnInteractHandler ArrowBtnPress(int dir)
    {
        return delegate ()
        {
            ArrowBtnSels[dir].AddInteractionPunch(0.5f);
            if (_moduleSolved)
                return false;
            Audio.PlaySoundAtTransform("Move", transform);
            if (_isInsideMaze && _walls[_mazeOrder[_currentMaze]][dir].Substring(_currentPositions[_mazeOrder[_currentMaze]], 1) == "Y")
            {
                Module.HandleStrike();
                Debug.LogFormat("[Triple Traversal #{0}] While in Maze {1}, you attempted to travel {2} from {3}, but there was a wall. Strike.", _moduleId, "ABC"[_mazeOrder[_currentMaze]], _dirNames[dir], GetCoord(_currentPositions[_mazeOrder[_currentMaze]]));
                _isInsideMaze = false;
                ScreenText.text = "-";
                _currentMaze = 0;
            }
            else
                MoveInMaze(dir);
            return false;
        };
    }

    private bool MiddleBtnPress()
    {
        MiddleBtnSel.AddInteractionPunch(0.5f);
        if (_moduleSolved)
            return false;
        if (!_isInsideMaze)
        {
            _isInsideMaze = true;
            ScreenText.text = "ABC"[_mazeOrder[_currentMaze]].ToString();
            Audio.PlaySoundAtTransform("Move", transform);
            Debug.LogFormat("[Triple Traversal #{0}] Started maze movement. Entering Maze {2}. Current positions: {1}.", _moduleId, _currentPositions.Select(c => GetCoord(c)).Join(", "), "ABC"[_mazeOrder[_currentMaze]]);
            return false;
        }
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
                return false;
            }
            Debug.LogFormat("[Triple Traversal #{0}] Entering Maze {1}. Current positions: {2}.", _moduleId, "ABC"[_mazeOrder[_currentMaze]], _currentPositions.Select(c => GetCoord(c)).Join(", "));
            ScreenText.text = "ABC"[_mazeOrder[_currentMaze]].ToString();
            Audio.PlaySoundAtTransform("Correct", transform);
            return false;
        }
        Module.HandleStrike();
        _isInsideMaze = false;
        ScreenText.text = "-";
        Debug.LogFormat("[Triple Traversal #{0}] Pressed the middle button at {1} of Maze {2} instead of the center cell. Strike.", _moduleId, GetCoord(_currentPositions[_mazeOrder[_currentMaze]]), "ABC"[_mazeOrder[_currentMaze]]);
        _currentMaze = 0;
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
        for (int wall = 0; wall < 4; wall++)
            WallDisplayObjs[wall].GetComponent<MeshRenderer>().material = WallDisplayMats[Enumerable.Range(0, 4).Select(i => Enumerable.Range(0, 3).Where(j => _walls[j][i].Substring(_currentPositions[j], 1) == "Y").Count()).ToArray()[wall]];
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
        var chars = m.Groups[1].Value.Select(i => i.ToString().ToLowerInvariant());
        foreach (var c in chars)
        {
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

    struct QueueItem
    {
        public int Cell;
        public int Parent;
        public int Direction;

        public QueueItem(int cell, int parent, int direction)
        {
            Cell = cell;
            Parent = parent;
            Direction = direction;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        if (!_isInsideMaze)
        {
            MiddleBtnSel.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        for (int st = _currentMaze; st < 3; st++)
        {
            var cur = _currentPositions[_mazeOrder[st]];
            var q = new Queue<QueueItem>();
            var visited = new Dictionary<int, QueueItem>();
            var sol = 24;
            q.Enqueue(new QueueItem(cur, -1, 0));
            while (q.Count > 0)
            {
                var qi = q.Dequeue();
                if (visited.ContainsKey(qi.Cell))
                    continue;
                visited[qi.Cell] = qi;
                if (qi.Cell == sol)
                    break;
                if (_walls[_mazeOrder[st]][0][qi.Cell] == 'N')
                    q.Enqueue(new QueueItem(qi.Cell - 7, qi.Cell, 0));
                if (_walls[_mazeOrder[st]][1][qi.Cell] == 'N')
                    q.Enqueue(new QueueItem(qi.Cell + 1, qi.Cell, 1));
                if (_walls[_mazeOrder[st]][2][qi.Cell] == 'N')
                    q.Enqueue(new QueueItem(qi.Cell + 7, qi.Cell, 2));
                if (_walls[_mazeOrder[st]][3][qi.Cell] == 'N')
                    q.Enqueue(new QueueItem(qi.Cell - 1, qi.Cell, 3));
            }
            var r = sol;
            var path = new List<int>(0);
            while (true)
            {
                var nr = visited[r];
                if (nr.Parent == -1)
                    break;
                path.Add(nr.Direction);
                r = nr.Parent;
            }
            for (int i = path.Count - 1; i >= 0; i--)
            {
                ArrowBtnSels[path[i]].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            MiddleBtnSel.OnInteract();
            if (!_moduleSolved)
                yield return new WaitForSeconds(0.1f);
        }
    }
}