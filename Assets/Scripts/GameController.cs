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
    public Dictionary<ChessPiece,Tile> _savedChessPiecePositions;
    [SerializeField] private ChessPiece _chessPiece;
    [SerializeField] private GameStates _currentGameState;

    private enum GameStates
    {
        SelectChessPieceToMove,
        MoveSelectedChessPiece,
        CPUTurn
    }

    private void Start()
    {
        // set initial game state
        _currentGameState = GameStates.SelectChessPieceToMove;
        
        // register events
        EventManager.TileClicked += OnTileClicked;

        _savedChessPiecePositions = new Dictionary<ChessPiece, Tile>();
    }

    private ChessPiece GetChessPieceOnTile(Tile t) {
        foreach (ChessPiece cp in _currentBoardState.chessPiecesInPlay)
        {
            if (cp.positionCoordsCurrent == t._tileCoords)
            {
                // there's a piece on this tile, return the piece
                return cp;
            }
        }
        return null;
    }

    private void OnTileClicked(Tile tile) {
        void HighlightValidMoves(ChessPiece cp, bool onOff)
        {
            List<Tile> cpValidMoves = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, true);
            foreach (Tile t in cpValidMoves) {
                if (t) 
                    t.HighlightTile(onOff);
            }
        }

        // if a piece is not selected, select a piece to move 
        if (_currentGameState == GameStates.SelectChessPieceToMove) {          
            _chessPiece = GetChessPieceOnTile(tile);
            if (!_chessPiece || (_chessPiece.faction != ChessPieceFaction.White)) 
                return;
            HighlightValidMoves(_chessPiece, true);
            _currentGameState = GameStates.MoveSelectedChessPiece;
        }

        // if a piece is already selected, move to the clicked tile        
        if (_currentGameState == GameStates.MoveSelectedChessPiece) {
            if (GetChessPieceOnTile(tile)) { // even though we've already selected a piece to move, we've clicked on a tile occupied by another chess piece
                // depending on the faction, we need to either capture the piece or select it (if it's one of ours)
                ChessPiece _otherChessPiece = GetChessPieceOnTile(tile);
                if (_otherChessPiece.faction == GetFaction(true)) {
                    HighlightValidMoves(_chessPiece, false); // remove the highlight for the legal moves of the old selected cp
                    _chessPiece = GetChessPieceOnTile(tile); // select the new cp
                    HighlightValidMoves(_chessPiece, true); 
                }
            }
            List<Tile> cpValidMoves = GetAbsoluteDestinationTiles(_chessPiece.type, _chessPiece._currentTile, true);
            if (cpValidMoves.Contains(tile)) {
                HighlightValidMoves(_chessPiece, false);
                PerformMove(_chessPiece, tile);

                // end turn
                _currentGameState = GameStates.CPUTurn;
                PlayCPUTurn();
            }
        }
    }

    void SaveBoardState() 
    {
        if (_savedChessPiecePositions.Count > 0 ) _savedChessPiecePositions.Clear();
        foreach (ChessPiece cp in _currentBoardState.chessPiecesInPlay) {
            _savedChessPiecePositions.Add(cp, cp._currentTile);
        }
        Debug.Log(_savedChessPiecePositions.Count + "/" + _currentBoardState.chessPiecesInPlay.Count);
    }

    void LoadBoardState()
    {
        Debug.Log("Loard Board State");

        // in teorie mai tre doar sa setez ca null referinta la CP de pe tile-urile unde ,dupa load, nu mai sunt cp-uri
        
        _currentBoardState.chessPiecesInPlay.Clear();
        foreach (var pos in _savedChessPiecePositions)
        {
            _currentBoardState.chessPiecesInPlay.Add(pos.Key);
            pos.Key.gameObject.SetActive(true);

            // set cp coords
            pos.Key.positionCoordsCurrent = pos.Value._tileCoords;

            // move transform
            pos.Key.transform.position = new Vector3(pos.Value.transform.position.x, pos.Value.transform.position.y, transform.position.z);

            // set cp and tile references on each other
            pos.Key._currentTile = pos.Value;
            pos.Key._currentTile._chessPiece = pos.Key;        
        }
        
    }

    private void PerformMove(ChessPiece cp, Tile t, bool capture=true)
    {
        // if there's a piece to the dest tile, and we're not calling this with the "false" flag from LoadBoardState, then capture it
        if (capture && GetChessPieceOnTile(t)) CapturePiece(GetChessPieceOnTile(t));        

        // update coords of cp to those of the dest tile
        cp.positionCoordsCurrent = t._tileCoords;
        // move transform
        cp.transform.position = new Vector3(t.transform.position.x, t.transform.position.y, transform.position.z);
        // make old tile cp field null and update new tile with cp
        cp._currentTile._chessPiece = null;
        t._chessPiece = cp;
        // update cp 
        cp._currentTile = t;

    }

    private void PerformMove(Move mv, bool capture=true)
    {
        // if there's a piece to the dest tile, and we're not calling this with the "false" flag from LoadBoardState, then capture it
        if (capture && GetChessPieceOnTile(mv.tileDestination)) CapturePiece(GetChessPieceOnTile(mv.tileDestination));

        mv.chessPieceToMove.positionCoordsCurrent = mv.tileDestination._tileCoords;
        mv.chessPieceToMove.transform.position = new Vector3(mv.tileDestination.transform.position.x, mv.tileDestination.transform.position.y, transform.position.z);
        mv.chessPieceToMove._currentTile._chessPiece = null;
        mv.tileDestination._chessPiece = mv.chessPieceToMove;
        mv.chessPieceToMove._currentTile = mv.tileDestination;
    }

    private void CapturePiece(ChessPiece cp)
    {
        Debug.Log("Piece captured.");
        _currentBoardState.chessPiecesInPlay.Remove(cp);
        cp.gameObject.SetActive(false);

        // gotta also change the cp reference onm the tile
        _currentBoardState.GetTileAtPosition(cp.positionCoordsCurrent)._chessPiece = null;

        // and the tile reference on the cp
        cp._currentTile = null;
    }

    private void PlayCPUTurn()
    {   
        // SaveBoardState();
        // List<ChessPiece> initialChessPiecesInPlay = _boardState.chessPiecesInPlay;

        //const int DEPTH = 2;
        // create list of move trees
        Dictionary<Move, int> moveBranches = new(); // root move, total tree score

//        SaveBoardState();
        // iterate own pieces and create a move tree for each valid root move
        foreach (ChessPiece cp in _currentBoardState.chessPiecesInPlay) {
            if (cp.faction == GetFaction(true))
            {
                // see where CP can move to
                List<Tile> validDestTiles = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, true);

                // compute move score
                foreach (Tile t in validDestTiles) {                    
                    int branchScore = 0;
                    
                    Move mv = new()
                    {
                        chessPieceToMove = cp,
                        tileDestination = t
                    };

                    if (t._chessPiece)
                    {
                        mv.scoreOfMove += t._chessPiece._captureScore;
                    }

                    // TODO score of move should actually be the score of the board state; ie. should consider the overwatch reach of ALL pieces and not just the moved piece
                    List<Tile> overwatchedTilesOnNextMove = GetAbsoluteDestinationTiles(cp.type, t, false);
                    foreach (Tile overwatchedTile in overwatchedTilesOnNextMove)
                    {
                        mv.scoreOfMove += overwatchedTile._overwatchScore;
                    }
                    branchScore += mv.scoreOfMove;


                    // perform each move and search further down the tree and keep adding to treeScore
                    // the result will be a large dictionary with a lot of the same keys (the same root move) but different values; we will have to pick the root move with the highest value

/*                    PerformMove(mv);
                    foreach (ChessPiece cp2 in _currentBoardState.chessPiecesInPlay)
                    {
                        if (cp2.faction == GetFaction(false))
                        {
                            List<Tile> validDestTiles2 = GetAbsoluteDestinationTiles(cp2.type, cp2._currentTile, true);
                            foreach (Tile t2 in validDestTiles)
                            {
                                Move mv2 = new()
                                {
                                    chessPieceToMove = cp2,
                                    tileDestination = t2
                                };

                                if (t2._chessPiece)
                                {
                                    mv2.scoreOfMove -= t2._chessPiece._captureScore;
                                }
                                List<Tile> overwatchedTilesOnNextMove2 = GetAbsoluteDestinationTiles(cp.type, t, false);
                                foreach (Tile overwatchedTile in overwatchedTilesOnNextMove)
                                {
                                    mv2.scoreOfMove -= overwatchedTile._overwatchScore;
                                }
                                branchScore -= mv2.scoreOfMove;
                            }
                        }
                    }
                    LoadBoardState();*/
                    moveBranches.Add(mv, branchScore); // this line should appear at the end of the branch)
                }
            }
        }

        var bestBranchRootMove = moveBranches.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        PerformMove(bestBranchRootMove);

        // end turn
        _currentGameState = GameStates.SelectChessPieceToMove;
    }

    private ChessPieceFaction GetFaction(bool ownFaction)
    {
        if (ownFaction) {
            if (_currentGameState == GameStates.SelectChessPieceToMove || _currentGameState == GameStates.MoveSelectedChessPiece) return ChessPieceFaction.White;
            if (_currentGameState == GameStates.CPUTurn) return ChessPieceFaction.Zombie;
        } else {
            if (_currentGameState == GameStates.SelectChessPieceToMove || _currentGameState == GameStates.MoveSelectedChessPiece) return ChessPieceFaction.Zombie;
            if (_currentGameState == GameStates.CPUTurn) return ChessPieceFaction.White;
        }
        return ChessPieceFaction.Neutral;
    }

    private List<Tile> GetAbsoluteDestinationTiles(ChessPieceType cpType, Tile startingTile, bool destinationsMustBeValid)
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
                if (GetChessPieceOnTile(t) && destinationsMustBeValid)
                {
                    if (GetChessPieceOnTile(t).faction == GetFaction(true))
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
            if (GetChessPieceOnTile(t)) {
                if (GetChessPieceOnTile(t).faction == GetFaction(true)) return null;
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
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadBoardState();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveBoardState();
        }        
    }
}
