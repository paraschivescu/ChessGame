using System.Collections;
using System.Collections.Generic;
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
    //public Dictionary<ChessPiece, Tile> _savedChessPiecePositions;
    public List <SavedChessPiecePosition> _savedChessPiecePositions;

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

//        _savedChessPiecePositions = new Dictionary<ChessPiece, Tile>();
        _savedChessPiecePositions = new List<SavedChessPiecePosition>();
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
                } else if (_otherChessPiece.faction == GetFaction(false)) {
                    CapturePiece(_otherChessPiece);
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
            SavedChessPiecePosition pos = new()
            {
                chessPiece = cp,
                tile = cp._currentTile,
                activeState = cp.gameObject.activeSelf
            };
            _savedChessPiecePositions.Add(pos);
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

    void LoadBoardState()
    {
        foreach (SavedChessPiecePosition pos in _savedChessPiecePositions)
        {
            if (pos.activeState == true) {
                pos.chessPiece.gameObject.SetActive(true);
                PerformMove(pos.chessPiece, pos.tile, false);
                pos.tile._chessPiece = pos.chessPiece;
                _currentBoardState.chessPiecesInPlay.Add(pos.chessPiece);
            }

/*            // if chess piece is still on the board, move it to the appropriate tile
            foreach (ChessPiece cpBoard in _currentBoardState.chessPiecesInPlay)
            {
                if (cpSaved == cpBoard) 
                {
                    PerformMove(cpBoard, _savedChessPiecePositions[cpSaved]);
                    //cpBoard._currentTile = _savedBoardState[cpSaved]);
                    //_currentBoardState.tiles
                }
            }

  */          // if not, then instantiate it on the appropriate tile
        }
    }

    private void PlayCPUTurn()
    {   
        // SaveBoardState();
        // List<ChessPiece> initialChessPiecesInPlay = _boardState.chessPiecesInPlay;

        // i think ima have to alter GetAbsoluteDestinationTiles to also take an argument that is the board, as when weighing possible moves it needs to try it on an invisible board..
        // OR maybe it's better if i let the CPU think using the visible board and just take a snapshot of the original board to restore things at the end

        List<Move> treeRoots = new();
        foreach (ChessPiece cp in _currentBoardState.chessPiecesInPlay) {
            if (cp.faction == ChessPieceFaction.Zombie)
            {
                List<Tile> validDestTiles = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, true);
                foreach (Tile t in validDestTiles) {
                    Move mv = new()
                    {
                        chessPieceToMove = cp,
                        tileDestination = t
                    };

                    // todo: score of move should also include captured pieces
                    if (t._chessPiece)
                    {
                        mv.scoreOfMove += t._chessPiece._captureScore;
                    }

                    // compute score of move
                    // todo: score of move should actually consider the overwatch reach of ALL pieces and not just the moved piece
                    List<Tile> overwatchedTilesOnNextMove = GetAbsoluteDestinationTiles(cp.type, t, false);
                    foreach (Tile overwatchedTile in overwatchedTilesOnNextMove)
                    {
                        mv.scoreOfMove += overwatchedTile._overwatchScore;
                    }
                    
                    treeRoots.Add(mv);
                }
            }
        }

        foreach (Move mv in treeRoots) 
        {
//            SaveBoardState();
//            PerformMove(mv);

            // calculate opponent move scores
  /*          foreach (ChessPiece cp in _currentBoardState.chessPiecesInPlay)
            {
                if (cp.faction == ChessPieceFaction.White)
                {

                }

            }*/

//            LoadBoardState();
        }
        // at this point, the score of each move only takes into account the immediate result and not what the opponent might do
        // we now need to go one level deeper in the tree and see what the opponent might do in response
        


        // perform root move and then do the same as above but for the opponent; then deduct the score from the root move

        // reset board state to what it was before the computation of the next move
//        LoadBoardState();
        // need a function that resets the positions of all pieces to what they are set in the _boardState

        treeRoots.Sort((x, y) => y.scoreOfMove.CompareTo(x.scoreOfMove));
        PerformMove(treeRoots[0]);

        
/*        // WIP iterate own pieces. for each piece, look at possible moves 
        ChessPiece SelectOwnChessPieceToMove() {
            foreach (ChessPiece cp in initialChessPiecesInPlay)
            {
                if (cp.faction == ChessPieceFaction.Zombie)
                {
                    return cp;
                }
            }
            Debug.Log("No more chess pieces in faction. ");
            return null;
        }

        ChessPiece chessPieceToMove = SelectOwnChessPieceToMove();

        //////// WIP select a move to play
        List<Tile> cpValidDestTiles = GetAbsoluteDestinationTiles(chessPieceToMove.type, chessPieceToMove._currentTile, true);

        List<Move> listOfMoves = new();
        foreach (Tile t in cpValidDestTiles)
        {
            Move mv = new() {
                chessPieceToMove = chessPieceToMove,
                tileDestination = t
            };

            List<Tile> overwatchedTilesOnNextMove = GetAbsoluteDestinationTiles(chessPieceToMove.type, t, false);
            foreach (Tile overwatchedTile in overwatchedTilesOnNextMove) {
                mv.scoreOfMove += overwatchedTile._overwatchScore;
            }
            
            listOfMoves.Add(mv);      
        }
        // sort list of moves in descending order
        listOfMoves.Sort((x, y) => y.scoreOfMove.CompareTo(x.scoreOfMove));

        // perform the move with the highest score
        PerformMove(listOfMoves[0]);
*/
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
