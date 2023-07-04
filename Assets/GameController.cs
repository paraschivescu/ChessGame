using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameController : MonoBehaviour
{
    [SerializeField] private ChessPiece _chessPiece;
    [SerializeField] private LevelGrid _levelGridReference;
    [SerializeField] private GameStates _currentGameState;
    [SerializeField] private GameObject _chessPiecesParent;
    [SerializeField] private List<ChessPiece> _chessPiecesInPlay;

    private enum GameStates
    {
        SelectChessPieceToMove,
        MoveSelectedChessPiece
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
        List<ChessPiece> _cpList = new List<ChessPiece>();
        for (int i = 0; i < _chessPiecesParent.transform.childCount; i++)
        {
            ChessPiece cp = _chessPiecesParent.transform.GetChild(i).GetComponent<ChessPiece>();
            _cpList.Add(cp);
        }
        return _cpList;
    }
    
    private void HighlightValidMoves(ChessPiece cp, bool onOff) {
        Vector2Int cpCurrentCoords = _chessPiece.positionCoordsCurrent;
        List<Tile> cpValidMoves = GetAbsoluteValidMoves(_chessPiece);
        foreach (Tile t in cpValidMoves) {
            if (t) {
                t.HighlightTile(onOff ? true : false);
            }
        }
    }

    public ChessPiece GetChessPieceOnTile(Tile t) {
        foreach (ChessPiece cp in _chessPiecesInPlay)
        {
            if (cp.positionCoordsCurrent == t.coords)
            {
                // there's a piece on this tile, return the piece
                return cp;
            }
        }
        return null;
    }

    private void OnTileClicked(Tile tile) {
        //Debug.Log("Tile clicked: " + tile.name);

        // if a piece is not selected, select a piece to move 
        if (_currentGameState == GameStates.SelectChessPieceToMove) {          
            _chessPiece = GetChessPieceOnTile(tile);
                if (!_chessPiece || (_chessPiece.faction != ChessPieceFaction.White)) {
                    return;
                }
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
            Vector2Int cpCurrentCoords = _chessPiece.positionCoordsCurrent;
            List<Tile> cpValidMoves = GetAbsoluteValidMoves(_chessPiece);
            if (cpValidMoves.Contains(tile)) {
                HighlightValidMoves(_chessPiece, false);
                _chessPiece.positionCoordsCurrent = tile.coords;
                //_chessPiece.transform.position = new Vector3(_chessPiece._positionCoordsCurrent.x, _chessPiece._positionCoordsCurrent.y, transform.position.z);            
                _chessPiece.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, transform.position.z);

                _currentGameState = GameStates.SelectChessPieceToMove;
            }
        }
    }

    private List<Tile> GetAbsoluteValidMoves(ChessPiece cp)
    {
        List<Tile> CheckValidMoves(List<Vector2Int> relativeValidMoves)
        {
            List<Tile> moves = new List<Tile>();
            foreach (Vector2Int move in relativeValidMoves)
            {
                Tile t = _levelGridReference.GetTileAtPosition(move + cp.positionCoordsCurrent);
                if (t && GetChessPieceOnTile(t))
                {
                    return moves;
                }
                else if (t)
                {
                    moves.Add(t);
                }
            }

            return moves;
        }

        Tile CheckValidMove(Vector2Int move) 
        {
            Tile t = _levelGridReference.GetTileAtPosition(move + cp.positionCoordsCurrent);
            if (t && !GetChessPieceOnTile(t)) return t;
            else return null;
        }
        
        // create lists for relative valid moves for each piece type
        // note: the reason why we create one list for each of the directions in which a piece can move is because of how the AddValidMoves() function works: when it encounters an in accessible tile in one direction, it stops looking in that direction
        List<Vector2Int> relativeValidMoves = new List<Vector2Int>();
        List<Vector2Int> relativeValidMovesLeft = new List<Vector2Int>(), relativeValidMovesRight = new List<Vector2Int>(), relativeValidMovesUp = new List<Vector2Int>(), relativeValidMovesDown = new List<Vector2Int>();
        List<Vector2Int> relativeValidMovesDiagonalNE = new List<Vector2Int>(), relativeValidMovesDiagonalSE = new List<Vector2Int>(), relativeValidMovesDiagonalSW = new List<Vector2Int>(), relativeValidMovesDiagonalNW = new List<Vector2Int>();
        List<Tile> absoluteValidMoves = new List<Tile>();

        switch (cp.type)
        {
            case (ChessPieceType.Pawn):
                relativeValidMoves.Add(new Vector2Int(0, 1));
                foreach (Vector2Int move in relativeValidMoves) absoluteValidMoves.Add(CheckValidMove(move));
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
                foreach (Vector2Int move in relativeValidMoves) absoluteValidMoves.Add(CheckValidMove(move));
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
                foreach (Vector2Int move in relativeValidMoves) absoluteValidMoves.Add(CheckValidMove(move));
            break;

            case (ChessPieceType.Rook):
                for (int i = 1; i < 10; i++)
                {
                    relativeValidMovesUp.Add(new Vector2Int(0, i));
                    relativeValidMovesDown.Add(new Vector2Int(0, -i));
                    relativeValidMovesRight.Add(new Vector2Int(i, 0));
                    relativeValidMovesLeft.Add(new Vector2Int(-i, 0));
                }
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesUp));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDown));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesRight));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesLeft));
            break;

            case (ChessPieceType.Bishop):
                for (int i = 1; i < 10; i++)
                {
                    relativeValidMovesDiagonalNE.Add(new Vector2Int(i, i));
                    relativeValidMovesDiagonalSE.Add(new Vector2Int(i, -i));
                    relativeValidMovesDiagonalSW.Add(new Vector2Int(-i, -i));
                    relativeValidMovesDiagonalNW.Add(new Vector2Int(-i, i));
                }
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalNE));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalSE));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalSW));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalNW));
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
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesUp));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDown));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesLeft));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesRight));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalNE));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalSE));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalSW));
                absoluteValidMoves.AddRange(CheckValidMoves(relativeValidMovesDiagonalNW));
            break;
            default:
                Debug.Log("Trying to get valid moves of invalid chess piece type.");
            break;
        }

        return absoluteValidMoves;
    }


}
