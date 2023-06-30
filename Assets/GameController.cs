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
        Vector2Int cpCurrentCoords = _chessPiece._positionCoordsCurrent;
        List<Tile> cpValidMoves = _chessPiece.GetValidMoves(_levelGridReference);
        foreach (Tile t in cpValidMoves) {
            if (t) {
                t.HighlightTile(onOff ? true : false);
            }
        }
    }

    private ChessPiece GetChessPieceOnTile(Tile t) {
        foreach (ChessPiece cp in _chessPiecesInPlay)
        {
            if (cp._positionCoordsCurrent == t.coords)
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
            Vector2Int cpCurrentCoords = _chessPiece._positionCoordsCurrent;
            List<Tile> cpValidMoves = _chessPiece.GetValidMoves(_levelGridReference);
            if (cpValidMoves.Contains(tile)) {
                HighlightValidMoves(_chessPiece, false);
                _chessPiece._positionCoordsCurrent = tile.coords;
                //_chessPiece.transform.position = new Vector3(_chessPiece._positionCoordsCurrent.x, _chessPiece._positionCoordsCurrent.y, transform.position.z);            
                _chessPiece.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, transform.position.z);

                _currentGameState = GameStates.SelectChessPieceToMove;
            }
        }


    }

}
