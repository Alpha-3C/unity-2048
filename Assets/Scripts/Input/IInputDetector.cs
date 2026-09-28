using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SwipeDirection
{
    Left, Right, Up, Down
}

public enum SwipeState
{
    SwipeNotStarted,
    SwipeStarted
}

public interface IInputDetector
{
    SwipeDirection? DetectInputDirection();
}
