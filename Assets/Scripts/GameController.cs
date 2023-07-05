using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameController : MonoBehaviour
{
    [SerializeField] private ChessPiece _chessPiece;
    [SerializeField] private LevelGrid _boardState;
    [SerializeField] private GameStates _currentGameState;
    [SerializeField] private GameObject _chessPiecesParent;
    [SerializeField] private List<ChessPiece> _chessPiecesInPlay;
    [SerializeField] private List<Move> _movesConsidered;

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

        // PopulateListOfChessPiecesInPlay
        _chessPiecesInPlay = GetListOfChessPieces();
    }

    public List<ChessPiece> GetListOfChessPieces() 
    {
        List<ChessPiece> _cpList = new();
        for (int i = 0; i < _chessPiecesParent.transform.childCount; i++)
        {
            ChessPiece cp = _chessPiecesParent.transform.GetChild(i).GetComponent<ChessPiece>();
            _cpList.Add(cp);
        }
        return _cpList;
    }
    

    private ChessPiece GetChessPieceOnTile(Tile t) {
        foreach (ChessPiece cp in _chessPiecesInPlay)
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
        Debug.Log("tile clicked");
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
                if (_otherChessPiece.faction == ChessPieceFaction.White) {
                    HighlightValidMoves(_chessPiece, false); // remove the highlight for the legal moves of the old selected cp
                    _chessPiece = GetChessPieceOnTile(tile); // select the new cp
                    HighlightValidMoves(_chessPiece, true); 
                } else if (_otherChessPiece.faction == ChessPieceFaction.Zombie) {
                    Debug.Log("zombie killed");
                    _chessPiecesInPlay.Remove(_otherChessPiece);
                    _otherChessPiece.gameObject.SetActive(false);
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
    
    private void PlayCPUTurn()
    {
        LevelGrid initialBoardState = _boardState;

        List<ChessPiece> ownChessPiecesInPlay = new();
        foreach (Tile t in _boardState.tiles) 
        {

        }
        
        foreach (ChessPiece cp in _chessPiecesInPlay) {
            if (cp.faction == ChessPieceFaction.Zombie) {
                ownChessPiecesInPlay.Add(cp);
            }
        }

        // WIP select one of the own cps in play
        ChessPiece chessPieceToMove = ownChessPiecesInPlay[0];

        //////// WIP select a move to play
        List<Tile> cpValidDestTiles = GetAbsoluteDestinationTiles(chessPieceToMove.type, chessPieceToMove._currentTile, true);

        List<Move> listOfMoves = new();
        foreach (Tile t in cpValidDestTiles)
        {
            Move mv = new() {
                _chessPieceToMove = chessPieceToMove,
                _tileDestination = t
            };

            List<Tile> overwatchedTilesOnNextMove = GetAbsoluteDestinationTiles(chessPieceToMove.type, t, false);
            foreach (Tile overwatchedTile in overwatchedTilesOnNextMove) {
                mv._scoreOfMove += overwatchedTile._overwatchScore;
            }
            
            listOfMoves.Add(mv);      
        }
        // sort list of moves in descending order
        listOfMoves.Sort((x, y) => y._scoreOfMove.CompareTo(x._scoreOfMove));

        // perform the move with the highest score
        PerformMove(listOfMoves[0]);

        // end turn
        _currentGameState = GameStates.SelectChessPieceToMove;
    }

    private void PerformMove(ChessPiece cp, Tile t)
    {
        cp.positionCoordsCurrent = t._tileCoords;
        cp.transform.position = new Vector3(t.transform.position.x, t.transform.position.y, transform.position.z);
        cp._currentTile = t;
    }

    private void PerformMove(Move mv) 
    {
        mv._chessPieceToMove.positionCoordsCurrent = mv._tileDestination._tileCoords;
        mv._chessPieceToMove.transform.position = new Vector3(mv._tileDestination.transform.position.x, mv._tileDestination.transform.position.y, transform.position.z);
        mv._chessPieceToMove._currentTile = mv._tileDestination;
    }

    private List<Tile> GetAbsoluteDestinationTiles(ChessPieceType cpType, Tile startingTile, bool destinationsMustBeValid)
    {

        List<Tile> CheckValidDestinations(List<Vector2Int> relativeValidDestinations)
        {
            List<Tile> moves = new();
            foreach (Vector2Int newCoords in relativeValidDestinations)
            {
                Tile t = _boardState.GetTileAtPosition(newCoords + startingTile._tileCoords);

                if (!t) return moves;
                if (GetChessPieceOnTile(t) && destinationsMustBeValid)
                {
                    if (GetChessPieceOnTile(t).faction == ChessPieceFaction.White)
                    {
                        return moves;
                    }
                    else
                    { // zombie
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
            Tile t = _boardState.GetTileAtPosition(move + startingTile._tileCoords);
            if (!destinationsMustBeValid) return t;
            if (t && !GetChessPieceOnTile(t)) return t;
            else return null;
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


}
