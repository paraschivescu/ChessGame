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
        cp.gameObject.SetActive(false);
        cp._currentTile = null;
    }

    /* NOT USED 

        private List<Move> GetMovesListForFaction(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction)
        {
            List<Move> possibleNextMoves = new();

            foreach (ChessPiece cp in chessPiecesInPlay)
            {
                if (cp.gameObject.activeSelf == true && cp.faction == faction)
                {
                    // see where CP can move to
                    List<Tile> validDestTiles = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, cp.faction);

                    // compute move score
                    foreach (Tile t in validDestTiles)
                    {
                        Move mv = new()
                        {
                            chessPieceToMove = cp,
                            tileDestination = t
                        };
                        possibleNextMoves.Add(mv);

                        // what is the immediate value of this tile being moved on?
                        // - the value of capturing a piece on this tile
                        if (_currentBoardState.GetChessPieceOnTile(t))
                        {
                            mv.scoreCapture = _currentBoardState.GetChessPieceOnTile(t)._captureScore;
                            mv.scoreOfMove += mv.scoreCapture;
                        }

                        // - the overwatch value, that is the sum of the tile values of all tiles that a piece can "see" from this tile
                        List<Tile> overwatchedTilesOnNextMove = GetAbsoluteDestinationTiles(cp.type, t, cp.faction, cp);
                        //overwatchedTilesOnNextMove.Add(cp._currentTile);  // because the cp is currently on this tile, it won't be seen as a valid destination
                        foreach (Tile overwatchedTile in overwatchedTilesOnNextMove)
                        {
                            mv.scoreOverwatch += overwatchedTile._overwatchScore;

                            // - the "threat score", ie the value of being able to capture a piece in the future, ie. an enemy piece being capturable next turn if nothing changes
                            if (_currentBoardState.GetChessPieceOnTile(overwatchedTile) && _currentBoardState.GetChessPieceOnTile(overwatchedTile).faction != faction)
                            {
                                mv.scoreThreat += _currentBoardState.GetChessPieceOnTile(overwatchedTile)._captureScore / 5;
                            }
                        }
                        mv.scoreOfMove += mv.scoreThreat;
                        mv.scoreOfMove += mv.scoreOverwatch;
                    }
                }
            }
            return possibleNextMoves;
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
                    // see where CP can move to
                    List<Tile> validDestTiles = GetAbsoluteDestinationTiles(cp.type, cp._currentTile, cp.faction);

                    // compute move score
                    foreach (Tile t in validDestTiles)
                    {

                        Move mv = new()
                        {
                            chessPieceToMove = cp,
                            tileDestination = t
                        };
                        possibleNextMoves.Add(mv);

                        // what is the immediate value of this tile being moved on?
                        // - the value of capturing a piece on this tile
                        if (_currentBoardState.GetChessPieceOnTile(t))
                        {
                            mv.scoreCapture = _currentBoardState.GetChessPieceOnTile(t)._captureScore;
                            mv.scoreOfMove += mv.scoreCapture;
                            totalCaptureScore += mv.scoreCapture;
                        }

                        // - the overwatch value, that is the sum of the tile values of all tiles that a piece can "see" from this tile
                        List<Tile> overwatchedTilesOnNextMove = GetAbsoluteDestinationTiles(cp.type, t, cp.faction, cp);
                        //overwatchedTilesOnNextMove.Add(cp._currentTile);  // because the cp is currently on this tile, it won't be seen as a valid destination
                        foreach (Tile overwatchedTile in overwatchedTilesOnNextMove)
                        {
                            mv.scoreOverwatch += overwatchedTile._overwatchScore;

                            // - the "threat score", ie the value of being able to capture a piece in the future, ie. an enemy piece being capturable next turn if nothing changes
                            if (_currentBoardState.GetChessPieceOnTile(overwatchedTile) && _currentBoardState.GetChessPieceOnTile(overwatchedTile).faction != faction)
                            {
                                mv.scoreThreat += _currentBoardState.GetChessPieceOnTile(overwatchedTile)._captureScore / 5;
                            }
                        }

                        mv.scoreOfMove += mv.scoreThreat;
                        totalThreatScore += mv.scoreThreat;

                        mv.scoreOfMove += mv.scoreOverwatch;
                        totalOverwatchScore += mv.scoreOverwatch;
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
            // WARNING, dictionaries dont allow inserting the smae key twice


            // first level: root
            List<Move> rootMoves = GetMovesListForFaction(chessPiecesInPlay, faction);
    //        int branchScore = 0; // EvaluateBoardAndGetPossibleNextMoves(chessPiecesInPlay, faction, ref rootMoves);        

            // second level > starting branches
            foreach (Move mv in rootMoves)
            {
                int branchScore = 0;
                SaveBoardState(1);
                PerformMove(mv);
                List<Move> possibleOpponentMoves = GetMovesListForFaction(chessPiecesInPlay, GetOtherFaction(faction));
                possibleOpponentMoves.Sort((x, y) => y.scoreOfMove.CompareTo(x.scoreOfMove));
                //branchScore -= possibleOpponentMoves[0].scoreOfMove;
                // third level 
                foreach (Move mv2 in possibleOpponentMoves)
                {
    //                branchScore += mv.scoreOfMove;
                    SaveBoardState(2);
                    PerformMove(mv2);
    //                List<Move> possibleOpponentMoves2 = GetMovesListForFaction(chessPiecesInPlay, GetOtherFaction(faction));
                    int bs = GetBoardScoreForFaction(faction);
                    if (bs > branchScore) branchScore = bs;
                    LoadBoardState(2);
    //                possibleOpponentMoves2.Sort((x, y) => y.scoreOfMove.CompareTo(x.scoreOfMove));
    //                branchScore -= possibleOpponentMoves2[0].scoreOfMove;
                }
                moveBranches.Add(mv, branchScore); // this line should appear at the end of the branch)

                LoadBoardState(1);
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

        private int GetBoardScoreForFaction(ChessPieceFaction faction)// List<Move>thisFactionMoves, List<Move>otherFactionMoves)
        {
            List<Move> thisFactionMoves = GetMovesListForFaction(_currentBoardState.chessPiecesInPlay, faction);
            List<Move> otherFactionMoves = GetMovesListForFaction(_currentBoardState.chessPiecesInPlay, GetOtherFaction(faction));

            int boardScore = GetTotalScoreOfMovesList(thisFactionMoves) - GetTotalScoreOfMovesList(otherFactionMoves);
            return boardScore;
        }

    */

    private ChessPieceFaction GetOtherFaction(ChessPieceFaction thisFaction)
    {
        if (thisFaction == ChessPieceFaction.White) return ChessPieceFaction.Zombie;
        else if (thisFaction == ChessPieceFaction.Zombie) return ChessPieceFaction.White;
        return ChessPieceFaction.Neutral;
    }

    private int EvaluateBoard(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction) 
    {
        int boardScore = 0;
        foreach (ChessPiece cp in chessPiecesInPlay)
        {
            if (cp.gameObject.activeSelf == true)
            {
                if(cp.faction == faction)
                {
                    // use "living" pieces and occupied tiles as the metrics of success
                    boardScore += cp._captureScore;
                    boardScore += cp._currentTile._overwatchScore;
                } else {
                    boardScore -= cp._captureScore;
                    boardScore -= cp._currentTile._overwatchScore; 
                }
            }
        }
        return boardScore;
    }

    private Move GetBestMoveOfFaction(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction)
    {
        List<Move> opponentMoves = GetNextMovesList(chessPiecesInPlay, faction);
        Move oppMvBest = new()
        {
            scoreOfMove = -99999
        };

        foreach (Move oppMv in opponentMoves)
        {
            SaveBoardState(1);
            PerformMove(oppMv);
            oppMv.scoreOfMove = EvaluateBoard(chessPiecesInPlay, faction);
            if (oppMv.scoreOfMove > oppMvBest.scoreOfMove) oppMvBest = oppMv;
            LoadBoardState(1);
        }

        return oppMvBest;
    }

    private List<Move> PerformMovesRecursivelyAndGetMovesList(List<ChessPiece> chessPiecesInPlay, ChessPieceFaction faction)
    {
        int initialBoardScore = EvaluateBoard(chessPiecesInPlay, faction);

        List<(Move, int)> branches = new(); // root move, board score (enemy)
        List<Move> rootMoves = GetNextMovesList(chessPiecesInPlay, faction);
        foreach (Move mv in rootMoves)
        {
            SaveBoardState(0);
            PerformMove(mv);

            Move mv2 = GetBestMoveOfFaction(chessPiecesInPlay, GetOtherFaction(faction));
            PerformMove(mv2);

            Move mv3 = GetBestMoveOfFaction(chessPiecesInPlay, faction);
            PerformMove(mv3);

            Move mv4 = GetBestMoveOfFaction(chessPiecesInPlay, GetOtherFaction(faction));
/*          PerformMove(mv4);

            Move mv5 = GetBestMoveOfFaction(chessPiecesInPlay, faction);
            PerformMove(mv5);

            Move mv6 = GetBestMoveOfFaction(chessPiecesInPlay, GetOtherFaction(faction));
            PerformMove(mv6);

            Move mv7 = GetBestMoveOfFaction(chessPiecesInPlay, faction);
            PerformMove(mv7);

            Move mv8 = GetBestMoveOfFaction(chessPiecesInPlay, GetOtherFaction(faction));
//            PerformMove(mv8);

            Move mv9 = GetBestMoveOfFaction(chessPiecesInPlay, faction);
            PerformMove(mv9);

            Move mv10 = GetBestMoveOfFaction(chessPiecesInPlay, GetOtherFaction(faction));*/

            mv.scoreOfMove = -mv4.scoreOfMove;

            //branches.Add((mv, opponentMoves[0].scoreOfMove));  // assume opponent makes the best possible move and add that
            LoadBoardState(0);
        }


        return rootMoves;
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



    private void PlayCPUTurn()
    {
        // List<Move> possibleMoves = GetPossibleNextMovesConsideringFuture(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie);
        List<Move> possibleMoves = PerformMovesRecursivelyAndGetMovesList(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie);
        possibleMoves.Sort((x, y) => y.scoreOfMove.CompareTo(x.scoreOfMove));
        PerformMove(possibleMoves[0]);

        // end turn
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
            _ui.DisplayMoveScores(PerformMovesRecursivelyAndGetMovesList(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White));
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            _ui.DisplayMoveScores(PerformMovesRecursivelyAndGetMovesList(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.Zombie));
        }


        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log(EvaluateBoard(_currentBoardState.chessPiecesInPlay, ChessPieceFaction.White));
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
