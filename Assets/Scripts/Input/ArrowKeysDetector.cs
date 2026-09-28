using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*public interface IInputDetector
{
    SwipeDirection? DetectInputDirection();
}*/
public class ArrowKeysDetector : MonoBehaviour,IInputDetector
{
    public SwipeDirection? DetectInputDirection()
    {
        if (Input.GetKeyUp(KeyCode.UpArrow)) return SwipeDirection.Up;
        else if (Input.GetKeyUp(KeyCode.DownArrow)) return SwipeDirection.Down;
        else if (Input.GetKeyUp(KeyCode.LeftArrow)) return SwipeDirection.Left;
        else if (Input.GetKeyUp(KeyCode.RightArrow)) return SwipeDirection.Right;
        else return null;
    }
}
