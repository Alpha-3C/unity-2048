using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemMovementDetails
{
    public GameObject GOToAnimateScale{get; set;}
    public GameObject GOToAnimatePosition{get; set;}

    public int NewRow {  get; set; }
    public int NewColumn { get; set; }

    public ItemMovementDetails(int newRow, int newColumn, GameObject goToAnimatePosition, GameObject goToAnimateScale)
    {
        NewRow = newRow;
        NewColumn = newColumn;
        GOToAnimatePosition = goToAnimatePosition;
        GOToAnimateScale = goToAnimateScale;
    }
}
