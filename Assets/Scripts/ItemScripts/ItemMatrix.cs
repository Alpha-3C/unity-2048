using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;


public struct CellPosition
{
    public int Row;
    public int Column;

    public CellPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }
}

public class ExtractedTile
{
    public Item Item;
    public CellPosition From; // 提取时立刻复制原坐标
}

public class TileGroup
{
    public Item ResultItem;                 // 还原到矩阵的方块
    public List<ExtractedTile> Sources = new List<ExtractedTile>();     // 普通移动为 1 个，合并为 2 个
}

public class TileMove
{
    public int Value;                       // 动画中显示原方块的数值
    public CellPosition From;
    public CellPosition To;
}

public class SwipeResult
{
    public bool Changed;
    public int ScoreGained;
    public List<TileMove> Moves = new List<TileMove>();
}


public class ItemMatrix
{
    // 第 0 行位于棋盘顶部；null 表示空格。
    private Item[,] matrix = new Item[Globals.Rows, Globals.Columns];

    public Item this[int row, int column]
    {
        get { return matrix[row, column]; }
        set { matrix[row, column] = value; }
    }

    private readonly System.Random random = new System.Random();

    // 本次有效滑动中所有合并产生的分数，供 GameManager 读取。
    public int LastMergeScore { get; private set; }

    public void GetEmptyPosition(out int row, out int column)
    {
        if (CheckMatrixFilled())
        {
            row = -1;
            column = -1;
            Debug.LogWarning("Failed to get empty position:Matrix is filled!");
            return;
        }
        
        do
        {
            row = random.Next(0, Globals.Rows);
            column = random.Next(0, Globals.Columns);

        } while (matrix[row, column] != null);
    }
    

    public bool CheckMatrixFilled()
    {
        for(int row = 0; row < Globals.Rows; row++)
        {
            for(int col = 0; col < Globals.Columns; col++)
            {
                if (matrix[row, col] == null) return false;
            }
        }
        return true;
    }

    public void ResetJustMergedFlag()
    {
        for (int row = 0; row < Globals.Rows; row++)
        {
            for(int column = 0; column < Globals.Columns; column++)
            {
                if (matrix[row, column] != null) matrix[row, column].IsJustMerged = false;
            }
        }


    }

    // 保存移动前后的数字快照；空格记为 0，便于比较棋盘变化。
    int[][] ReserveCurrentMatrix()
    {
        int[][] current_matrix = new int[Globals.Rows][];
        for(int row = 0;row < Globals.Rows; row++)
        {
            current_matrix[row] = new int[Globals.Columns];
            for(int col = 0; col < Globals.Columns; col++)
            {
                current_matrix[row][col] = matrix[row, col]?.Value ?? 0;
            }
        }

        return current_matrix;
    }

    bool IsDifferentMatrix(int[][] matrix1, int[][] matrix2, int rows, int cols)
    {
        for(int row = 0; row < rows; row++)
        {
            for(int col = 0; col < cols; col++)
            {
                if(matrix1[row][col] != matrix2[row][col]) return true;
            }
        }
        return false;
    }

    // 四个方向共用流程：提取非空砖块、合并，再写回棋盘。
    public SwipeResult OperateSwipe(SwipeDirection direction)
    {
        var result = new SwipeResult();
        LastMergeScore = 0;
        ResetJustMergedFlag();
        int[][] old_matrix = ReserveCurrentMatrix();

        int arrayCount = (direction == SwipeDirection.Left||direction == SwipeDirection.Right)? Globals.Rows: Globals.Columns;
        int arraySize = (direction == SwipeDirection.Left || direction == SwipeDirection.Right) ? Globals.Columns : Globals.Rows;


        for(int i=0;i<arrayCount; i++)
        {
            List<ExtractedTile> extracted = ExtractArray(direction, arraySize, i);
            List<TileGroup> merged = ExecuteMerge(extracted);
            List<TileMove> moves = RecoverArrayToMatrix(merged, direction, arraySize, i);
            result.Moves.AddRange(moves) ; 
        }

        //检测是否为合法移动
        int[][] matrixAfterSwipe = ReserveCurrentMatrix();

        result.Changed = IsDifferentMatrix(old_matrix, matrixAfterSwipe, Globals.Rows, Globals.Columns);
        result.ScoreGained = LastMergeScore;
        


        return result;

    }

    // 从滑动目标边开始读取，提取非空砖块。
    private List<ExtractedTile> ExtractArray(SwipeDirection direction, int arraySize, int index)  
    {
        List<ExtractedTile> array = new List<ExtractedTile>();
        switch(direction)
        {
            case SwipeDirection.Left:
                for(int j = 0; j < arraySize; j++)
                {
                    if (matrix[index, j] != null)
                    {
                        var tempTile = new ExtractedTile();
                        tempTile.Item = matrix[index, j];
                        tempTile.From = new CellPosition(index, j);
                        array.Add(tempTile);
                    }
                }
                return array;
            case SwipeDirection.Right:
                for( int j = 0; j < arraySize; j++)
                {
                    if (matrix[index, arraySize - 1 - j]!=null) 
                    {
                        var tempTile = new ExtractedTile();
                        tempTile.Item = matrix[index, arraySize - 1 - j];
                        tempTile.From = new CellPosition(index, arraySize - 1 - j);
                        array.Add(tempTile);
                    }
                }
                return array;
            case SwipeDirection.Up:
                for(int  k = 0; k < arraySize; k++)
                {
                    if (matrix[k, index] != null)
                    {
                        var tempTile = new ExtractedTile();
                        tempTile.Item = matrix[k, index];
                        tempTile.From = new CellPosition(k, index);
                        array.Add(tempTile);
                    }
                }
                return array;
            case SwipeDirection.Down:
                for(int k = 0; k < arraySize; k++)
                {
                    if (matrix[arraySize - k -1, index] != null) 
                    {
                        var tempTile = new ExtractedTile();
                        tempTile.Item = matrix[arraySize - k - 1, index];
                        tempTile.From = new CellPosition(arraySize - k - 1, index);
                        array.Add(tempTile);
                    }
                }
                return array;
            
            default:
                Debug.LogError("Exceptive Swipe Direction State Error!");
                return new List<ExtractedTile>();
        }
    }

    // 每块每次滑动最多合并一次，标记用于阻止重复合并。
    private List<TileGroup> ExecuteMerge(List<ExtractedTile> array)
    {
        List<TileGroup> mergedArray = new List<TileGroup>();
        for(int i = 0; i < array.Count; i++)
        {
            if (i == 0 || array[i].Item.Value != array[i - 1].Item.Value)
            {
                var tempTile = new TileGroup();
                tempTile.ResultItem = array[i].Item;
                tempTile.Sources.Add(array[i]);
                mergedArray.Add(tempTile);
                continue;
            }
            if (array[i].Item.Value == array[i - 1].Item.Value && array[i-1].Item.IsJustMerged == false && array[i].Item.IsJustMerged == false)
            {
                // 合并生成新的逻辑砖块；画面层需处理旧 GO，并创建新显示对象。
                Item newMergedItem = new Item();
                TileGroup newTile = new TileGroup();

                newMergedItem.Value = array[i].Item.Value * 2;
                newTile.ResultItem = newMergedItem;
                newTile.Sources.Add(array[i - 1]);
                newTile.Sources.Add(array[i]);
                LastMergeScore += newMergedItem.Value;
                mergedArray.RemoveAt(mergedArray.Count - 1);  //合并
                mergedArray.Add(newTile);

                array[i].Item.IsJustMerged = true;
                array[i - 1].Item.IsJustMerged = true;
            }
            else
            {
                TileGroup newTile = new TileGroup();
                newTile.ResultItem = array[i].Item;
                newTile.Sources.Add(array[i]);
                mergedArray.Add(newTile);
            }
        }
        return mergedArray;

    }

    // 写回完整 Item 引用，普通移动时保留原有 GO。
    private List<TileMove> RecoverArrayToMatrix(List<TileGroup> array, SwipeDirection direction,int arraySize, int index)
    {
        var recoveredArray = new List<TileMove>();

        switch(direction)
        {
            case SwipeDirection.Left:
                for(int j = 0; j < array.Count; j++)
                {
                    matrix[index, j] = array[j].ResultItem;
                    matrix[index, j].Row = index;
                    matrix[index, j].Column = j;
                    foreach(var source in array[j].Sources)
                    {
                        var tempTileMove = new TileMove();
                        tempTileMove.Value = source.Item.Value;
                        tempTileMove.From = source.From;
                        tempTileMove.To.Row = index;
                        tempTileMove.To.Column = j;
                        recoveredArray.Add(tempTileMove);
                    }
                }
                for(int j = array.Count; j < arraySize; j++)
                {
                    matrix[index, j] = null;
                }
                break;
            case SwipeDirection.Right:
                for (int j = 0; j < array.Count; j++)
                {
                    matrix[index, arraySize - 1 - j] = array[j].ResultItem;
                    matrix[index, arraySize - 1 - j].Row = index;
                    matrix[index, arraySize - 1 - j].Column = arraySize - 1 - j;
                    foreach (var source in array[j].Sources)
                    {
                        var tempTileMove = new TileMove();
                        tempTileMove.Value = source.Item.Value;
                        tempTileMove.From = source.From;
                        tempTileMove.To.Row = index;
                        tempTileMove.To.Column = arraySize - 1 - j;
                        recoveredArray.Add(tempTileMove);
                    }
                }
                for (int j = array.Count; j < arraySize; j++)
                {
                    matrix[index, arraySize - 1 - j] = null;
                }
                break;
            case SwipeDirection.Up:
                for (int j = 0; j < array.Count; j++)
                {
                    matrix[j, index] = array[j].ResultItem;
                    matrix[j, index].Row = j;
                    matrix[j, index].Column = index;
                    foreach (var source in array[j].Sources)
                    {
                        var tempTileMove = new TileMove();
                        tempTileMove.Value = source.Item.Value;
                        tempTileMove.From = source.From;
                        tempTileMove.To.Row = j;
                        tempTileMove.To.Column = index;
                        recoveredArray.Add(tempTileMove);
                    }
                }
                for (int j = array.Count; j < arraySize; j++)
                {
                    matrix[j, index] = null;
                }
                break;
            case SwipeDirection.Down:
                for (int j = 0; j < array.Count; j++)
                {
                    matrix[arraySize - 1 - j, index] = array[j].ResultItem;
                    matrix[arraySize - 1 - j, index].Row = arraySize - 1 - j;
                    matrix[arraySize - 1 - j, index].Column = index;
                    foreach (var source in array[j].Sources)
                    {
                        var tempTileMove = new TileMove();
                        tempTileMove.Value = source.Item.Value;
                        tempTileMove.From = source.From;
                        tempTileMove.To.Row = arraySize - 1 - j;
                        tempTileMove.To.Column = index;
                        recoveredArray.Add(tempTileMove);
                    }
                }
                for (int j = array.Count; j < arraySize; j++)
                {
                    matrix[arraySize - 1 - j, index] = null;
                }
                break;
        }   
        return recoveredArray;
    }

















    /*
    private ItemMovementDetails AreTheseTwoItemsSame(int oldRow, int oldColumn, int toCheckRow, int toCheckColumn)
    {
        if (toCheckRow < 0 || toCheckColumn < 0 || toCheckRow >= Globals.Rows || toCheckColumn >= Globals.Columns)
            return null;
        if(matrix[oldRow, oldColumn] != null && matrix[toCheckRow, toCheckColumn]!=null
            && matrix[oldRow, oldColumn].Value == matrix[toCheckRow, toCheckColumn].Value
            && matrix[toCheckRow, toCheckColumn].IsJustMerged == false)
        {
            matrix[toCheckRow, toCheckColumn].Value *= 2;
            matrix[toCheckRow, toCheckColumn].IsJustMerged = true;

            var GOToAnimateScaleCopy = matrix[oldRow, oldColumn].GO;
            matrix[oldRow, oldColumn] = null;
            return new ItemMovementDetails(toCheckRow, toCheckColumn, matrix[toCheckRow, toCheckColumn].GO, GOToAnimateScaleCopy);
        }
        else return null;
    }

    private ItemMovementDetails MoveItemToNullPositionAndCheckIfSameWithNextOne
        (int oldRow, int newRow, int itemToCheckRow, int oldColumn, int newColumn, int itemToCheckColumn)
    {
        matrix[newRow, newColumn] = matrix[oldRow, oldColumn];
        matrix[oldRow, oldColumn] = null;

        matrix[newRow, newColumn].Row = newRow;
        matrix[newRow, newColumn].Column = newColumn;

        ItemMovementDetails imd2 = AreTheseTwoItemsSame(newRow, newColumn, itemToCheckRow, itemToCheckColumn);
        if (imd2 != null) return imd2;
        else return new ItemMovementDetails(newRow, newColumn, matrix[newRow, newColumn].GO, null);
    }


    
    public List<ItemMovementDetails> SwipeMatrix(SwipeDirection direction)
    {
        ResetJustMergedFlag();

        var movementDetails = new List<ItemMovementDetails>();

        switch (direction)
        {
            case SwipeDirection.Left:
                var columnNumbersL = System.Linq.Enumerable.Range(0, Globals.Columns);
                for(int row =  Globals.Rows - 1;row>=0; row--)
                {
                    foreach (int column in columnNumbersL)
                    {
                        if (matrix[row, column] == null) continue;

                        ItemMovementDetails imd = AreTheseTwoItemsSame(row, column, row, column - 1);
                        if (imd != null)
                        {
                            movementDetails.Add(imd);
                            continue;
                        }
                        int columnFirstNullItem = -1;

                        int numberOfItemsToTake = column; //left

                        bool emptyItemFound = false;

                        foreach (var tempColumnFirstNullItem in columnNumbersL.Take(numberOfItemsToTake))
                        {
                            columnFirstNullItem = tempColumnFirstNullItem;
                            if (matrix[row, columnFirstNullItem] == null)
                            {
                                emptyItemFound = true;
                                break;
                            }
                        }

                        if (!emptyItemFound) continue;

                        ItemMovementDetails newImd = MoveItemToNullPositionAndCheckIfSameWithNextOne(row, row, row, column, columnFirstNullItem, columnFirstNullItem - 1);
                        movementDetails.Add(newImd);
                    }
                }
                return movementDetails;
            case SwipeDirection.Right:
                var columnNumbersR = System.Linq.Enumerable.Range(0, Globals.Columns).Reverse();
                for (int row = Globals.Rows - 1; row >= 0; row--)
                {
                    foreach (int column in columnNumbersR)
                    {
                        if (matrix[row, column] == null) continue;

                        ItemMovementDetails imd = AreTheseTwoItemsSame(row, column, row, column + 1);
                        if (imd != null)
                        {
                            movementDetails.Add(imd);
                            continue;
                        }
                        int columnFirstNullItem = -1;

                        int numberOfItemsToTake = Globals.Columns - 1 - column; // right

                        bool emptyItemFound = false;

                        foreach (var tempColumnFirstNullItem in columnNumbersR.Take(numberOfItemsToTake))
                        {
                            columnFirstNullItem = tempColumnFirstNullItem;
                            if (matrix[row, columnFirstNullItem] == null)
                            {
                                emptyItemFound = true;
                                break;
                            }
                        }

                        if (!emptyItemFound) continue;

                        ItemMovementDetails newImd = MoveItemToNullPositionAndCheckIfSameWithNextOne(row, row, row, column, columnFirstNullItem, columnFirstNullItem + 1);
                        movementDetails.Add(newImd);
                    }
                }
                return movementDetails;
            case SwipeDirection.Up:
                var rowNumberU = System.Linq.Enumerable.Range(0, Globals.Rows).Reverse();
                for (int column = 0; column < Globals.Columns; column++)
                {
                    foreach (int row in rowNumberU)
                    {
                        if (matrix[row, column] == null) continue;

                        ItemMovementDetails imd = AreTheseTwoItemsSame(row, column, row + 1, column);
                        if (imd != null)
                        {
                            movementDetails.Add(imd);
                            continue;
                        }

                        int rowFirstNullItem = -1;
                        bool emptyItemFound = false;
                        foreach (int tempRowFirstNullItem in rowNumberU.Take(Globals.Rows - 1 - row))
                        {
                            rowFirstNullItem = tempRowFirstNullItem;
                            if (matrix[rowFirstNullItem, column] == null)
                            {
                                emptyItemFound = true;
                                break;
                            }
                        }

                        if (!emptyItemFound) continue;

                        ItemMovementDetails newImd = MoveItemToNullPositionAndCheckIfSameWithNextOne(
                            row, rowFirstNullItem, rowFirstNullItem + 1,
                            column, column, column);
                        movementDetails.Add(newImd);
                    }
                }
                return movementDetails;
            case SwipeDirection.Down:
                var rowNumberD = System.Linq.Enumerable.Range(0, Globals.Rows);
                for (int column = 0; column < Globals.Columns; column++)
                {
                    foreach (int row in rowNumberD)
                    {
                        if (matrix[row, column] == null) continue;

                        ItemMovementDetails imd = AreTheseTwoItemsSame(row, column, row - 1, column);
                        if (imd != null)
                        {
                            movementDetails.Add(imd);
                            continue;
                        }

                        int rowFirstNullItem = -1;
                        bool emptyItemFound = false;
                        foreach (int tempRowFirstNullItem in rowNumberD.Take(row))
                        {
                            rowFirstNullItem = tempRowFirstNullItem;
                            if (matrix[rowFirstNullItem, column] == null)
                            {
                                emptyItemFound = true;
                                break;
                            }
                        }

                        if (!emptyItemFound) continue;

                        ItemMovementDetails newImd = MoveItemToNullPositionAndCheckIfSameWithNextOne(
                            row, rowFirstNullItem, rowFirstNullItem - 1,
                            column, column, column);
                        movementDetails.Add(newImd);
                    }
                }
                return movementDetails;
        }

        return movementDetails;
    }
    */

}
