using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public struct SavedChessPiecePosition 
{
    public ChessPiece chessPiece;
    public Tile tile;
    public bool activeState;
}

public class GameController : MonoBehaviour
{

    public LevelGrid _currentBoardState;
    public List <Dictionary<ChessPiece, Tile>> saveSlots;
    [SerializeField] private ChessPiece _chessPiece;
    [SerializeField] private GameStates _currentGameState;
    private UIController _ui;

    private enum GameStates
    {
        SelectChessPieceToMove,
        MoveSelectedChessPiece,
        CPUTurn
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
        for (int i=0; i < 3; i++)
        {
            Dictionary<ChessPiece, Tile> save = new();
            saveSlots.Add(save);
        }
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
                PerformMove(_chessPiece, tile);

                // end turn
                _currentGameState = GameStates.CPUTurn;
                PlayCPUTurn();
            }
        }
    }

    void SaveBoardState(int saveSlot) 
    {
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

    private void PerformMove(ChessPiece cp, Tile t, bool capture=true)
    {
        // if there's a piece to the dest tile, and we're not calling this with the "false" flag from LoadBoardState, then capture it
        if (capture && _currentBoardState.GetChessPieceOnTile(t)) CapturePiece(_currentBoardState.GetChessPieceOnTile(t));        

        // update coords of cp to those of the dest tile
        cp.positionCoordsCurrent = t._tileCoords;
        // move transform
        cp.transform.position = new Vector3(t.transform.position.x, t.transform.position.y, transform.position.z);
        // update cp 
        cp._currentTile = t;

    }

    private void PerformMove(Move mv, bool capture=true)
    {
        // if there's a piece to the dest tile, and we're not calling this with the "false" flag from LoadBoardState, then capture it
        if (capture && _currentBoardState.GetChessPieceOnTile(mv.tileDestination)) CapturePiece(_currentBoardState.GetChessPieceOnTile(mv.tileDestination));

        mv.chessPieceToMove.positionCoordsCurrent = mv.tileDestination._tileCoords;
        mv.chessPieceToMove.transform.position = new Vector3(mv.tileDestination.transform.position.x, mv.tileDestination.transform.position.y, transform.position.z);
        mv.chessPieceToMove._currentTile = mv.tileDestination;
    }

    private void CapturePiece(ChessPiece cp)
    {
        Debug.Log("Piece captured.");
        //_currentBoardState.chessPiecesInPlay.Remove(cp);
        cp.gameObject.SetActive(false);

        // and the tile reference on the cp
        cp._currentTile = null;
    }

    private int EvaluateBoardAndGetPossibleNextMoves(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction, ref List<Move> possibleNextMoves)
    {
        int totalCaptureScore = 0;
        int totalOverwatchScore = 0;
        int totalThreatScore = 0;

        foreach (ChessPiece cp in chessPiecesInPlay)
        {
            if (cp.gameObject.activeSelf == true && cp.faction == faction)
            {
                Debug.Log(cp.faction);
                // see where CP can move to
                List<Tile> validDestTiles = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, cp.faction);

                // compute move score
                foreach (Tile t in validDestTiles)
                {

                    Move mv = new()
                    {
                        chessPieceToMove = cp,
                        tileDestination = t,
                        scoreOfMove = 0
                    };
                    possibleNextMoves.Add(mv);

                    // what is the immediate value of this tile being moved on?
                    // - the value of capturing a piece on this tile
                    if (_currentBoardState.GetChessPieceOnTile(t))
                    {
                        mv.scoreOfMove += _currentBoardState.GetChessPieceOnTile(t)._captureScore;
                        totalCaptureScore += mv.scoreOfMove;
                    }

                    // - the overwatch value, that is the sum of the tile values of all tiles that a piece can "see" from this tile
                    List<Tile> overwatchedTilesOnNextMove = GetAbsoluteDestinationTiles(cp.type, t, cp.faction);
                    foreach (Tile overwatchedTile in overwatchedTilesOnNextMove)
                    {
                        mv.scoreOfMove += overwatchedTile._overwatchScore;
                        totalOverwatchScore += mv.scoreOfMove;

                        // - the "threat score", ie the value of being able to capture a piece in the future, ie. an enemy piece being capturable next turn if nothing changes
                        if (_currentBoardState.GetChessPieceOnTile(overwatchedTile))
                        {
                           mv.scoreOfMove += (_currentBoardState.GetChessPieceOnTile(overwatchedTile)._captureScore / 5);
                          // totalThreatScore += mv.scoreOfMove;
                        }
                    }
                }
            }
        }

        int boardScore = totalCaptureScore + totalOverwatchScore + totalThreatScore;
        //Debug.Log("Future: Capture/Overwatch/Threat/Total: " + totalCaptureScore + "/" + totalOverwatchScore + "/" + totalThreatScore + "=" + boardScore);
        return boardScore;
    }

    private List<Move> GetPossibleNextMovesConsideringFuture(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction)
    {
        Dictionary<Move, int> moveBranches = new(); // root move, total tree score; The Root Move also contains a move score - that's the score of that first move, without accounting for future value of the branches that start with that move

        // first level: root
        List<Move> rootMoves = new();
        int initialBoardScore = EvaluateBoardAndGetPossibleNextMoves(chessPiecesInPlay, faction, ref rootMoves);
        int boardScore;
        int branchScore;
        // second level > starting branches
        foreach (Move mv in rootMoves)
        {
            branchScore = initialBoardScore;    
            boardScore = 0;
            SaveBoardState(1);
            PerformMove(mv);
            List<Move> possibleNextMoves = new();
            boardScore = EvaluateBoardAndGetPossibleNextMoves(chessPiecesInPlay, faction, ref possibleNextMoves);
            branchScore -= boardScore;
            
            // third level 
/*            foreach (Move mv2 in possibleNextMoves)
            {
                SaveBoardState(2);
                PerformMove(mv2);
                List<Move> possibleNextMoves2 = new();
                int boardScore2 = EvaluateBoardAndGetPossibleNextMoves(_currentBoardState.chessPiecesInPlay, GetFaction(true), ref possibleNextMoves2);
                branchScore += boardScore2;
                LoadBoardState(2);
            }*/

            LoadBoardState(1);
            moveBranches.Add(mv, branchScore); // this line should appear at the end of the branch)
        }

        // var bestBranchRootMove = moveBranches.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        // return bestBranchRootMove;

        // copy branch score to Move root score
        List<Move> movesList = new();
        foreach (var branch in moveBranches)
        {
            branch.Key.scoreOfMove = branch.Value;
            movesList.Add(branch.Key);
        }

        return movesList;
    }

    private void PlayCPUTurn()
    {   
        List<Move> possibleMoves = GetPossibleNextMovesConsideringFuture(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie);
        possibleMoves.Sort((x, y) => y.scoreOfMove.CompareTo(x.scoreOfMove));
        PerformMove(possibleMoves[0]);

        // end turn
        _currentGameState = GameStates.SelectChessPieceToMove;
    }

    private List<Tile> GetAbsoluteDestinationTiles(ChessPieceType cpType, Tile startingTile, ChessPieceFaction faction)
    {

        // NOTE: destinationsMustBeValid flag exists because we're using this same method to return overwatch tiles. 
        // If destinationsMustBeValid is true, then this method must only return tiles where this piece can move to
        // so when we encounter a tile occupied by a piece,
        // if the piece is our own: we stop looking in that direction and that tile is NOT a valid move
        // if the piece is enemy: we stop looking in that direction but that tile IS a valid move, capturing the opp piece

        List<Tile> CheckValidDestinations(List<Vector2Int> relativeValidDestinations)
        {
            List<Tile> moves = new();
            foreach (Vector2Int newCoords in relativeValidDestinations)
            {
                Tile t = _currentBoardState.GetTileAtPosition(newCoords + startingTile._tileCoords);

                if (!t) return moves;
                if (_currentBoardState.GetChessPieceOnTile(t))
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
            // tile is not there (ie destination is outside the board) OR tile is occupied by own piece
            if (!t) return null;
            if (_currentBoardState.GetChessPieceOnTile(t)) {
                if (_currentBoardState.GetChessPieceOnTile(t).faction == faction) return null;
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
                relativeValidMoves.Add(new Vector2Int(0, 1));
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
        if (Input.GetKeyDown(KeyCode.W))
        {
            List<Move> whatever = new();
            int white = EvaluateBoardAndGetPossibleNextMoves(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White, ref whatever);
            int black = EvaluateBoardAndGetPossibleNextMoves(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie, ref whatever);
            Debug.Log("White: " + white + "/ Black: " + black);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            _ui.DisplayMoveScores(GetPossibleNextMovesConsideringFuture(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White));
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadBoardState(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadBoardState(2);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadBoardState(0);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveBoardState(0);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            EventManager.RaiseDebugOverlayActivatedEvent();
        }

    }
}
