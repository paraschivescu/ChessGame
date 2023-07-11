using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager
{
    public delegate void TileClickedEventHandler(Tile tile); public static event TileClickedEventHandler TileClicked;
    public delegate void ToggleDebugOverlayEventHandler(); public static event ToggleDebugOverlayEventHandler ToggleDebugOverlay;

    public static void RaiseTileClickedEvent(Tile tile) { if (TileClicked != null) { TileClicked(tile); } else { Debug.Log("No subscribers to event: TileClicked"); } }
    public static void RaiseDebugOverlayActivatedEvent() { if (ToggleDebugOverlay != null) { ToggleDebugOverlay(); } else { Debug.Log("No subscribers to event: RaiseDebugOverlayActivatedEvent"); } }
}
