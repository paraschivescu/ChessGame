using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public struct SavedChessPiecePosition 
{
    public Tile tile;
    public bool activeState;
}

public class GameController : MonoBehaviour
{

    public LevelGrid _currentBoardState;
    public List<Dictionary<ChessPiece, Tile>> saveSlots;

    [SerializeField] private ChessPiece _chessPiece;
    [SerializeField] private GameStates _currentGameState;
    private UIController _ui;



    public List<Dictionary<ChessPiece, Tile>> debugSaveSlots;
    public List<int> debugBoardEvaluations;
    private int debugSaveIterator;
    private int debugSaveIteratorCap;

    private enum GameStates
    {
        SelectChessPieceToMove,
        MoveSelectedChessPiece,
        CPUTurn,
        GameOver
    }

    private void Start()
    {
        // get references to other scripts
        _ui = FindObjectOfType<UIController>();

        // set initial game state
        _currentGameState = GameStates.SelectChessPieceToMove;
        
        // register events
        EventManager.TileClicked += OnTileClicked;


        // create save slots
        saveSlots = new();
        for (int i=0; i < 10; i++)
        {
            Dictionary<ChessPiece, Tile> save = new();
            saveSlots.Add(save);
        }

        debugSaveSlots = new();
        debugSaveIterator = 1;
    }

    private void OnTileClicked(Tile tile) {
        void HighlightValidMoves(ChessPiece cp, bool onOff)
        {
            List<Tile> cpValidMoves = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, cp.faction);
            foreach (Tile t in cpValidMoves) {
                if (t) 
                    t.HighlightTile(onOff);
            }
        }

        // if a piece is not selected, select a piece to move 
        if (_currentGameState == GameStates.SelectChessPieceToMove) {          
            _chessPiece = _currentBoardState.GetChessPieceOnTile(tile);
            if (!_chessPiece || (_chessPiece.faction != ChessPieceFaction.White)) 
                return;
            HighlightValidMoves(_chessPiece, true);
            _currentGameState = GameStates.MoveSelectedChessPiece;
        }

        // if a piece is already selected, move to the clicked tile        
        if (_currentGameState == GameStates.MoveSelectedChessPiece) {
            if (_currentBoardState.GetChessPieceOnTile(tile)) { // even though we've already selected a piece to move, we've clicked on a tile occupied by another chess piece
                // depending on the faction, we need to either capture the piece or select it (if it's one of ours)
                ChessPiece _otherChessPiece = _currentBoardState.GetChessPieceOnTile(tile);
                if (_otherChessPiece.faction == ChessPieceFaction.White) {
                    HighlightValidMoves(_chessPiece, false); // remove the highlight for the legal moves of the old selected cp
                    _chessPiece = _currentBoardState.GetChessPieceOnTile(tile); // select the new cp
                    HighlightValidMoves(_chessPiece, true); 
                }
            }
            List<Tile> cpValidMoves = GetAbsoluteDestinationTiles(_chessPiece.type, _chessPiece._currentTile, _chessPiece.faction);
            if (cpValidMoves.Contains(tile)) {
                HighlightValidMoves(_chessPiece, false);
                Move myMove = new ()
                {
                    chessPieceToMove = _chessPiece,
                    tileDestination = tile
                };
                PerformMove(myMove);         

                // end turn
                _currentGameState = GameStates.CPUTurn;
                PlayCPUTurn();
            }
        }
    }

    void DebugSaveBoardState(int boardEval)
    {
        Dictionary<ChessPiece, Tile> newDebugSave = new();
        foreach (ChessPiece cp in _currentBoardState.chessPiecesInPlay)
        {
            if (cp.gameObject.activeSelf == true)
            {
                newDebugSave.Add(cp, cp._currentTile);
            }
        }
        debugSaveSlots.Add(newDebugSave);
        debugBoardEvaluations.Add(boardEval);
        debugSaveIteratorCap = debugSaveSlots.Count-1;
    }

    private int CountBranchTips(List<int> boardEvaluations)
    {
        int count = 0;
        foreach (int eval in boardEvaluations) {
            if (eval != -999) count += 1;
        }
        return count;
    }

    void DebugLoadBoardState(int saveSlot)
    {
        foreach (var pos in debugSaveSlots[saveSlot])
        {
            pos.Key.gameObject.SetActive(true);

            // set cp coords
            pos.Key.positionCoordsCurrent = pos.Value._tileCoords;

            // move transform
            pos.Key.transform.position = new Vector3(pos.Value.transform.position.x, pos.Value.transform.position.y, transform.position.z);

            // set cp tile references on cp
            pos.Key._currentTile = pos.Value;
        }

    }

    void SaveBoardState(int saveSlot) 
    {
        DebugSaveBoardState(-999);

        // saves position for each chess piece
        if (saveSlots[saveSlot].Count > 0 ) saveSlots[saveSlot].Clear();
        foreach (ChessPiece cp in _currentBoardState.chessPiecesInPlay) {
            if (cp.gameObject.activeSelf == true) {
                saveSlots[saveSlot].Add(cp, cp._currentTile);
            }
        }
//        Debug.Log(_savedChessPiecePositions.Count + "/" + _currentBoardState.chessPiecesInPlay.Count);
    }

    void LoadBoardState(int saveSlot)
    {
        // Debug.Log("Loard Board State");
        foreach (var pos in saveSlots[saveSlot])
        {
            pos.Key.gameObject.SetActive(true);

            // set cp coords
            pos.Key.positionCoordsCurrent = pos.Value._tileCoords;

            // move transform
            pos.Key.transform.position = new Vector3(pos.Value.transform.position.x, pos.Value.transform.position.y, transform.position.z);

            // set cp tile references on cp
            pos.Key._currentTile = pos.Value;
        }
        
    }

    private void PerformMove(Move mv)
    {
        if (mv == null) return;
        if (_currentBoardState.GetChessPieceOnTile(mv.tileDestination)) CapturePiece(_currentBoardState.GetChessPieceOnTile(mv.tileDestination));

        mv.chessPieceToMove.positionCoordsCurrent = mv.tileDestination._tileCoords;
        mv.chessPieceToMove.transform.position = new Vector3(mv.tileDestination.transform.position.x, mv.tileDestination.transform.position.y, transform.position.z);
        mv.chessPieceToMove._currentTile = mv.tileDestination;
    }

    private void CapturePiece(ChessPiece cp)
    {
        cp.gameObject.SetActive(false);
        cp._currentTile = null;
    }


    private ChessPieceFaction GetOtherFaction(ChessPieceFaction thisFaction)
    {
        if (thisFaction == ChessPieceFaction.White) return ChessPieceFaction.Zombie;
        else if (thisFaction == ChessPieceFaction.Zombie) return ChessPieceFaction.White;
        return ChessPieceFaction.Neutral;
    }

    private void GameOver()
    {
        Debug.Log("Game Over"); 
        _currentGameState = GameStates.GameOver;
    }

    private int EvaluateBoard(List<ChessPiece> chessPiecesInPlay)
    {
        int boardScore = 0;
        foreach (ChessPiece cp in chessPiecesInPlay)
        {
            if (cp.gameObject.activeSelf == true)
            {
                if (cp.faction == ChessPieceFaction.White)
                {
                    // use "living" pieces and occupied tiles as the metrics of success
                    boardScore += cp._captureScore;
                    boardScore += cp._currentTile._overwatchScore;
                }
                else if (cp.faction == ChessPieceFaction.Zombie)
                {
                    boardScore -= cp._captureScore;
                    boardScore -= cp._currentTile._overwatchScore;
                }
            }
        }
        return boardScore;
    }

    private List<Move> GetNextMovesList(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction)
    {
        // create next moves list
        List<Move> possibleNextMoves = new();   
        foreach (ChessPiece cp in chessPiecesInPlay)
        {
            if (cp.gameObject.activeSelf == true && cp.faction == faction)
            {
                List<Tile> validDestTiles = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, cp.faction);

                foreach (Tile t in validDestTiles)
                {
                    Move mv = new()
                    {
                        chessPieceToMove = cp,
                        tileDestination = t
                    };
                    possibleNextMoves.Add(mv);
                }
            }
        }
        return possibleNextMoves;
    }

    /* 

    ============ MINIMAX STUFF ==============

    */

    public Move MinimaxGetBestMove(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction)
    {
        int bestScore = 0;
        if (faction == ChessPieceFaction.White) bestScore = int.MinValue;
        else if (faction == ChessPieceFaction.Zombie) bestScore = int.MaxValue;

        Move bestMove = null;
        int score;
        List<Move> possibleNextMoves = GetNextMovesList(chessPiecesInPlay, faction);
        foreach (Move mv in possibleNextMoves)
        {
            SaveBoardState(0);
            PerformMove(mv);
            score = MiniMax(chessPiecesInPlay, 3, faction);

            if (faction == ChessPieceFaction.White && score > bestScore)
            {
                bestScore = score;
                bestMove = mv;
            }

            else if (faction == ChessPieceFaction.Zombie && score < bestScore)
            {
                bestScore = score;
                bestMove = mv;
            }
            
            LoadBoardState(0);
        }
        return bestMove;
    }

    private List<Move> GetMovesMinimax(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction)
    {
        List<Move> possibleNextMoves = GetNextMovesList(chessPiecesInPlay, faction);
        foreach (Move mv in possibleNextMoves)
        {
            SaveBoardState(0);
            PerformMove(mv);
            mv.scoreOfMove = MiniMax(chessPiecesInPlay, 3, GetOtherFaction(faction));
            LoadBoardState(0);
        }
        return possibleNextMoves;
    }


    private int MiniMax(List<ChessPiece> chessPiecesInPlay, int depth, ChessPieceFaction faction)
    {
        List<Move> possibleNextMoves = GetNextMovesList(chessPiecesInPlay, faction);

        if (depth == 0 || _currentGameState == GameStates.GameOver || possibleNextMoves.Count == 0) {
            int boardEval = EvaluateBoard(chessPiecesInPlay);
            DebugSaveBoardState(boardEval);
            return boardEval;
        }

        if (faction == ChessPieceFaction.White) {
            int maxEval = int.MinValue;
            foreach (Move mv in possibleNextMoves)
            {
                SaveBoardState(depth);
                PerformMove(mv);
                    int eval = MiniMax(chessPiecesInPlay, depth - 1, ChessPieceFaction.Zombie);
                    maxEval = Mathf.Max(maxEval, eval);
                LoadBoardState(depth);
            }
            return maxEval;

        } else {
            int minEval = int.MaxValue;
            foreach (Move mv in possibleNextMoves)
            {
                SaveBoardState(depth);
                PerformMove(mv);
                    int eval = MiniMax(chessPiecesInPlay, depth - 1, ChessPieceFaction.White);
                    minEval = Mathf.Min(minEval, eval);
                LoadBoardState(depth);
            }
            return minEval;
        }
    }

/* ====================================== */

    private void PlayCPUTurn()
    {
        List<Move> possibleMoves = GetMovesMinimax(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie);
        if (possibleMoves.Count == 0) {
            GameOver();
            return;
        }         

        possibleMoves.Sort((x, y) => x.scoreOfMove.CompareTo(y.scoreOfMove));
        PerformMove(possibleMoves[0]);

        // end turn
        if (GetNextMovesList(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White).Count == 0) 
            GameOver();
        _currentGameState = GameStates.SelectChessPieceToMove;
    }

    private List<Tile> GetAbsoluteDestinationTiles(ChessPieceType cpType, Tile startingTile, ChessPieceFaction faction, ChessPiece cpToIgnore = null)
    {
        List<Tile> CheckValidDestinations(List<Vector2Int> relativeValidDestinations)
        {
            List<Tile> moves = new();
            foreach (Vector2Int newCoords in relativeValidDestinations)
            {
                Tile t = _currentBoardState.GetTileAtPosition(newCoords + startingTile._tileCoords);

                if (!t) return moves;
                if (_currentBoardState.GetChessPieceOnTile(t) && _currentBoardState.GetChessPieceOnTile(t) != cpToIgnore)
                {
                    if (_currentBoardState.GetChessPieceOnTile(t).faction == faction)
                    {
                        return moves;
                    }
                    else
                    { // opponent
                        moves.Add(t);
                        return moves;
                    }
                } else {
                    moves.Add(t);
                }                
            }
            return moves;
        }

        Tile CheckValidDestination(Vector2Int move) 
        {

            Tile t = _currentBoardState.GetTileAtPosition(move + startingTile._tileCoords);
            //ChessPiece chessPieceOnDestinationTile = _currentBoardState.GetChessPieceOnTile(t);
            // tile is not there (ie destination is outside the board) OR tile is occupied by own piece
            if (!t) return null;

            ChessPiece chessPieceOnTile = null;
            if (_currentBoardState.GetChessPieceOnTile(t)) {
                chessPieceOnTile = _currentBoardState.GetChessPieceOnTile(t);
                if (chessPieceOnTile != cpToIgnore)
                {
                    if (chessPieceOnTile.faction == faction) return null;
                }
            };

            if (cpType == ChessPieceType.Pawn)
            {
                if (move.x != 0) { // diagonal pawn movement should only be allowed if there is a piece there to be captured; 
                    if (!chessPieceOnTile) return null;
                } else if (move.x == 0) { // forward movement should not be allowed if there is a piece there
                    if (chessPieceOnTile) return null;
                }
            }

            // tile is occupied by opponent piece or is unoccupied
            return t;
        }
        
        // create lists for relative valid moves for each piece type
        // note: the reason why we create one list for each of the directions in which a piece can move is because of how the AddValidMoves() function works: when it encounters an in accessible tile in one direction, it stops looking in that direction
        List<Vector2Int> relativeValidMoves = new();
        List<Vector2Int> relativeValidMovesLeft = new(), relativeValidMovesRight = new(), relativeValidMovesUp = new(), relativeValidMovesDown = new();
        List<Vector2Int> relativeValidMovesDiagonalNE = new(), relativeValidMovesDiagonalSE = new(), relativeValidMovesDiagonalSW = new(), relativeValidMovesDiagonalNW = new();
        List<Tile> absoluteValidMoves = new();

        switch (cpType)
        {
            case (ChessPieceType.Pawn):
                if (faction == ChessPieceFaction.White) { 
                    relativeValidMoves.Add(new Vector2Int(0, 1));
                    relativeValidMoves.Add(new Vector2Int(-1, 1));
                    relativeValidMoves.Add(new Vector2Int(1, 1));
                } else if (faction == ChessPieceFaction.Zombie) {
                    relativeValidMoves.Add(new Vector2Int(0, -1));
                    relativeValidMoves.Add(new Vector2Int(-1, -1));
                    relativeValidMoves.Add(new Vector2Int(1, -1));
                }
                foreach (Vector2Int move in relativeValidMoves) absoluteValidMoves.Add(CheckValidDestination(move));
            break;

            case (ChessPieceType.Knight):
                relativeValidMoves.Add(new Vector2Int(1, 2));
                relativeValidMoves.Add(new Vector2Int(2, 1));
                relativeValidMoves.Add(new Vector2Int(2, -1));
                relativeValidMoves.Add(new Vector2Int(1, -2));
                relativeValidMoves.Add(new Vector2Int(-1, 2));
                relativeValidMoves.Add(new Vector2Int(-2, 1));
                relativeValidMoves.Add(new Vector2Int(-2, -1));
                relativeValidMoves.Add(new Vector2Int(-1, -2));
                foreach (Vector2Int move in relativeValidMoves) absoluteValidMoves.Add(CheckValidDestination(move));
            break;

            case (ChessPieceType.King):
                relativeValidMoves.Add(new Vector2Int(1, 0));
                relativeValidMoves.Add(new Vector2Int(-1, 0));
                relativeValidMoves.Add(new Vector2Int(0, 1));
                relativeValidMoves.Add(new Vector2Int(0, -1));
                relativeValidMoves.Add(new Vector2Int(-1, -1));
                relativeValidMoves.Add(new Vector2Int(1, 1));
                relativeValidMoves.Add(new Vector2Int(1, -1));
                relativeValidMoves.Add(new Vector2Int(-1, 1));
                foreach (Vector2Int move in relativeValidMoves) absoluteValidMoves.Add(CheckValidDestination(move));
            break;

            case (ChessPieceType.Rook):
                for (int i = 1; i < 10; i++)
                {
                    relativeValidMovesUp.Add(new Vector2Int(0, i));
                    relativeValidMovesDown.Add(new Vector2Int(0, -i));
                    relativeValidMovesRight.Add(new Vector2Int(i, 0));
                    relativeValidMovesLeft.Add(new Vector2Int(-i, 0));
                }
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesUp));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDown));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesRight));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesLeft));
            break;

            case (ChessPieceType.Bishop):
                for (int i = 1; i < 10; i++)
                {
                    relativeValidMovesDiagonalNE.Add(new Vector2Int(i, i));
                    relativeValidMovesDiagonalSE.Add(new Vector2Int(i, -i));
                    relativeValidMovesDiagonalSW.Add(new Vector2Int(-i, -i));
                    relativeValidMovesDiagonalNW.Add(new Vector2Int(-i, i));
                }
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalNE));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalSE));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalSW));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalNW));
            break;

            case (ChessPieceType.Queen):
                for (int i = 1; i < 10; i++)
                {
                    relativeValidMovesUp.Add(new Vector2Int(0, i));
                    relativeValidMovesDown.Add(new Vector2Int(0, -i));
                    relativeValidMovesRight.Add(new Vector2Int(i, 0));
                    relativeValidMovesLeft.Add(new Vector2Int(-i, 0));
                    relativeValidMovesDiagonalNE.Add(new Vector2Int(i, i));
                    relativeValidMovesDiagonalSE.Add(new Vector2Int(i, -i));
                    relativeValidMovesDiagonalSW.Add(new Vector2Int(-i, -i));
                    relativeValidMovesDiagonalNW.Add(new Vector2Int(-i, i));
                }
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesUp));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDown));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesLeft));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesRight));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalNE));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalSE));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalSW));
                absoluteValidMoves.AddRange(CheckValidDestinations(relativeValidMovesDiagonalNW));
            break;
            default:
                Debug.Log("Trying to get valid moves of invalid chess piece type.");
            break;
        }

        absoluteValidMoves.RemoveAll(move => move == null); // prevent null values from appearing in valid moves list
        return absoluteValidMoves;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
//            _ui.DisplayMoveScores(PerformMovesRecursivelyAndGetMovesList(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White));
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log(EvaluateBoard(_currentBoardState.chessPiecesInPlay));
            //Debug.Log(EvaluateBoardForFaction(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie));
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            //_ui.DisplayMoveScores(PerformMovesRecursivelyAndGetMovesList(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie));
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
//            Debug.Log(EvaluateBoardForFaction(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White));
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            _ui.DisplayMoveScores(GetMovesMinimax(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White));
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            //_ui.DisplayMoveScores(MinimaxGetMoveScores(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie));
        }



        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
//            DebugSaveBoardState(0);
            SaveBoardState(9);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
//            DebugLoadBoardState(0);
            LoadBoardState(9);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log(CountBranchTips(debugBoardEvaluations));
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log(MiniMax(_currentBoardState.chessPiecesInPlay, 3, ChessPieceFaction.White));
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log(MiniMax(_currentBoardState.chessPiecesInPlay, 3, ChessPieceFaction.Zombie));
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Load debug board state. Save iterator is "+ debugSaveIterator + "/" + debugSaveIteratorCap + ". Board evaluation: " + debugBoardEvaluations[debugSaveIterator]);
            if (debugSaveIterator <= debugSaveIteratorCap)
            {
                DebugLoadBoardState(debugSaveIterator);
                debugSaveIterator += 1;
            }
        }


        if (Input.GetKeyDown(KeyCode.D))
        {
            EventManager.RaiseDebugOverlayActivatedEvent();
        }
    }
}
