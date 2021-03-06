﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class TilesManager : MonoBehaviour {

    private const int ROWS = 19;
    private const int COLUMNS = 34;

    [Serializable]
    public class Count {
        public int minimum;
        public int maximum;

        public Count (int min, int max) {
            minimum = min;
            maximum = max;
        }
    }


    [SerializeField]
    private GameObject portalTile;
    [SerializeField]
    private GameObject baseTile;
    [SerializeField]
    private GameObject floorTile;
    [SerializeField]
    private GameObject wallTile;

    [SerializeField]
    private PathFinder pathFinder;
    [SerializeField]
    private Count wallCount = new Count (10 , 20);

    [SerializeField]
    private List<Vector3> portalPositions = new List<Vector3> ();
    [SerializeField]
    private List<Vector3> basePositions = new List<Vector3> ();


    [SerializeField]
    private string holderName = "Board";


    private Transform tilesHolder;
    private List<Vector3> gridPositions = new List<Vector3> ();
    private Dictionary<Vector3, Tile> grid = new Dictionary<Vector3, Tile> ();

    private void OnValidate () {
        for (int i = 0 ; i < portalPositions.Count ; i++) {
            Vector3 pos = portalPositions[i];
            pos.x = Mathf.Clamp (portalPositions[i].x , 0 , COLUMNS);

            if (pos.x == COLUMNS || pos.x == 0) {
                pos.y = Mathf.Clamp (portalPositions[i].y , 1 , ROWS - 1);
            }
            else {
                pos.y = Mathf.FloorToInt(portalPositions[i].y / ROWS) * ROWS;
            }

            pos.y = Mathf.Clamp (pos.y , 0 , ROWS);
            portalPositions[i] = pos;
        }

        for (int i = 0 ; i < basePositions.Count ; i++) {
            Vector3 pos = basePositions[i];
            pos.x = Mathf.Clamp (basePositions[i].x , 0 , COLUMNS);

            if (pos.x == COLUMNS || pos.x == 0) {
                pos.y = Mathf.Clamp (basePositions[i].y , 1 , ROWS - 1);
            }
            else {
                pos.y = Mathf.FloorToInt (basePositions[i].y / ROWS) * ROWS;
            }

            pos.y = Mathf.Clamp (pos.y , 0 , ROWS);
            basePositions[i] = pos;
        }

    }


    private void Awake () {
        InitizalizeGrid ();
        MapSetup ();
    }


    private void InitizalizeGrid () {
        gridPositions.Clear ();
        for (int x = 0 ; x <= COLUMNS ; x++) {
            for (int y = 0 ; y <= ROWS ; y++) {
                gridPositions.Add (new Vector3 (x, y, 0f));
            }
        }
    }


    private void MapSetup () {
        tilesHolder = new GameObject (holderName).transform;
        FillGrid ();
        RandomTilePlacement (wallTile, wallCount.minimum, wallCount.maximum);
        TrackBlockedTiles ();
    }


    private void FillGrid () {

        for (int gridIndex = 0 ; gridIndex < gridPositions.Count ; gridIndex++) { //loop through the grid
            GameObject tileToInstantiate = floorTile;

            if (gridPositions[gridIndex].x == 0 || gridPositions[gridIndex].x == COLUMNS || gridPositions[gridIndex].y == 0 || gridPositions[gridIndex].y == ROWS) {

                if (portalPositions.Contains (gridPositions[gridIndex])) {
                    tileToInstantiate = portalTile;
                }
                else if (basePositions.Contains (gridPositions[gridIndex])) {
                    tileToInstantiate = baseTile;
                }
                else {
                    tileToInstantiate = wallTile;
                }

            }

            if (tileToInstantiate == null) {
                Debug.LogError ("FillGrid function has nothing to instantiate.");
                continue; 
            }

            GameObject instance = Instantiate (tileToInstantiate, gridPositions[gridIndex], Quaternion.identity, tilesHolder) as GameObject;
            Tile tileToGrid = instance.GetComponent<Tile> ();
            grid.Add (gridPositions[gridIndex], tileToGrid);

        }

    }

    private void RandomTilePlacement (GameObject tilePrefab, int minCount, int maxCount) {
        int randomNumber = Random.Range (minCount, maxCount);
        List<Vector3> usedPositions = new List<Vector3> ();
        Vector3 tilePos = new Vector3 ();

        for (int i = 0 ; i < randomNumber ; i++) {
            tilePos = GenerateRandomTilePosition ();
            if (!usedPositions.Contains (tilePos)) {
                Tile previousTile = grid[tilePos];
                grid[tilePos] = tilePrefab.GetComponent<Tile>();
                pathFinder.FindPaths ();

                try {
                    for (int j = 0 ; j < portalPositions.Count ; j++) {
                        pathFinder.CreatePaths (portalPositions[j]);
                    }
                }
                catch {
                    Debug.LogFormat ("{0} blocked path at {1}. {0} removed." , tilePrefab.name , tilePos);
                    grid[tilePos] = previousTile;
                    i--;
                    continue;
                }
                SwapTile (previousTile.instance , tilePrefab , tilePos);
                usedPositions.Add (tilePos);
            }
        }
    }

    private Vector3 GenerateRandomTilePosition () {
        Vector3 tilePos = new Vector3 ();
        tilePos.x = Random.Range (1 , COLUMNS);
        tilePos.y = Random.Range (1 , ROWS);
        return tilePos;
    }

    private void PlaceTile(GameObject tileToPlace , Vector3 tilePosition) {
        grid[tilePosition] = tileToPlace.GetComponent<Tile> ();
        Instantiate (tileToPlace , tilePosition , Quaternion.identity , tilesHolder);
    }

    private void SwapTile( GameObject tileToDestroy , GameObject tileToPlace , Vector3 tilePosition ) {
        Destroy (tileToDestroy);
        PlaceTile (tileToPlace, tilePosition);
    }

    public List<Vector3> GetPortalPositions () {
        return portalPositions;
    }

    public List<Vector3> GetBasePositions () {
        return basePositions;
    }

    public List<Tile> Neighbours (Vector3 mainNode) {
        List<Tile> neighbours = new List<Tile> ();
        if (grid.ContainsKey(mainNode + Vector3.up))
            neighbours.Add (grid[mainNode + Vector3.up]);
        if (grid.ContainsKey(mainNode + Vector3.right))
            neighbours.Add (grid[mainNode + Vector3.right]);
        if (grid.ContainsKey (mainNode + Vector3.down))
            neighbours.Add (grid[mainNode + Vector3.down]);
        if (grid.ContainsKey (mainNode + Vector3.left))
            neighbours.Add (grid[mainNode + Vector3.left]);
        return neighbours;
    }

    private void TrackBlockedTiles () {
        List<Vector3> reachableTiles = pathFinder.GetPathingGrid ();
        for (int i = 0 ; i < gridPositions.Count ; i++) {
            if (reachableTiles.Contains(gridPositions[i])) {
                continue;
            }
            else {
                SwapTile ( grid[gridPositions[i]].instance, wallTile , gridPositions[i]);
            }
        }
    }

}
